# SGCustomers → Generate Bill — Business Logic (Pseudocode)

**Purpose:** Review and change requirements here first.  
**Source of truth in code:** `ElectrcityFunctions.GenerateEBillForCustomer`  
**UI:** `/SGCustomers/GenerateBill` → button posts to `EBillU/GenerateElectricityBills`

> Edit the RULES and FORMULAS sections below to describe what you want.  
> Then implement those changes in the files listed at the bottom.

---

## A. Page load (list customers)

```
FUNCTION ShowGenerateBillPage(selectedProject, btNoSearch):

    SHOW OperatorName, BillingMonth, BillingYear
         (from session OperatorSetupDetail / OperatorsSetup)

    LOAD distinct Projects FROM CustomersDetail

    IF selectedProject is empty:
        SHOW empty list
        RETURN

    customers = CustomersDetail WHERE
        Project = selectedProject
        AND (BillGenerationStatus is NULL OR BillGenerationStatus = "Not Generated")

    IF btNoSearch provided:
        customers = customers WHERE Btno CONTAINS btNoSearch

    GROUP customers BY Sector
    SHOW accordion list with checkboxes (value = customer.Uid)
```

**Current gap to review:**  
Page filters on `BillGenerationStatus`, but bill creation updates `BillStatus` only (not `BillGenerationStatus`).

---

## B. Generate Bill button click

```
FUNCTION OnGenerateBillButtonClick():

    selectedIds = all checked customer Uids

    IF selectedIds is empty:
        ALERT "No records selected."
        STOP

    SHOW loading overlay

    POST JSON { selectedIds } TO /GenerateElectricityBills

    IF response.success:
        ALERT "Operation Completed Successfully"
    ELSE:
        ALERT warning message
```

---

## C. API entry — operator / period setup

```
FUNCTION GenerateElectricityBills(selectedIds):

    operatorId = Session["OperatorId"]
    IF operatorId is missing:
        RETURN FAIL "Operator ID not found in session"

    operator = LOAD OperatorsSetup WHERE OperatorID = operatorId
    IF operator not found:
        RETURN FAIL "Operator details not found"

    billingMonth = operator.BillingMonth
    billingYear  = operator.BillingYear

    IF billingMonth OR billingYear is empty:
        RETURN FAIL "Please Update Operator Setup"

    previousMonth, previousYear = PreviousCalendarMonth(billingMonth, billingYear)

    issueDate = operator.IssueDate
    dueDate   = operator.DueDate
    validDate = operator.ValidDate          // NOTE: not currently mapped in CurrentOperatorService

    FPA_Month1, FPA_Year1, FPA_Rate1 = operator FPA set 1
    FPA_Month2, FPA_Year2, FPA_Rate2 = operator FPA set 2
    // NOTE: FPAYEAR2 currently not mapped in CurrentOperatorService

    results = []
    FOR EACH customerId IN selectedIds:
        result = GenerateEBillForCustomer(
            customerId,
            billingMonth, billingYear,
            previousMonth, previousYear,
            issueDate, dueDate, validDate,
            userName,
            FPA_Month1, FPA_Year1, FPA_Rate1,
            FPA_Month2, FPA_Year2, FPA_Rate2
        )
        results.ADD(result)

    RETURN SUCCESS "Bills generated successfully"
    // NOTE: individual per-customer failures are not returned to UI today
```

---

## D. Core bill generation (one customer)

```
FUNCTION GenerateEBillForCustomer(customerId, currentMonth, currentYear, prevMonth, prevYear, ...):

    // ---------- 1. Load customer ----------
    customer = CustomersDetail WHERE Uid = customerId
    IF customer is null:
        RETURN "Customer not found"


    // ---------- 2. Duplicate check ----------
    IF EXISTS ElectricityBills WHERE
           Btno = customer.Btno
       AND BillingMonth = currentMonth
       AND BillingYear  = currentYear:

        customer.BillStatus = "Bill Already Generated for {month} {year}"
        SAVE customer
        RETURN "Bill already generated"


    // ---------- 3. Arrears from previous bill ----------
    arrears = 0
    priorBillCount = COUNT ElectricityBills WHERE Btno = customer.Btno

    IF priorBillCount > 0:
        previousBill = ElectricityBills WHERE
               Btno = customer.Btno
           AND BillingMonth = prevMonth
           AND BillingYear  = prevYear

        IF previousBill is null:
            customer.BillStatus = "No bill found for {prevMonth} {prevYear}"
            SAVE customer
            RETURN "No previous bill found"

        IF previousBill.PaymentStatus IN ("Partially Paid", "partially paid"):
            arrears = BillAmountInDueDate - AmountPaid

        ELSE IF previousBill.PaymentStatus IN ("UnPaid", "unpaid"):
            arrears = BillAmountAfterDueDate

        ELSE:
            arrears = 0
            // Paid / other statuses → no arrears


    // ---------- 4. Meter reading ----------
    reading = ReadingSheet WHERE
           Btno = customer.Btno
       AND Month = currentMonth
       AND Year  = currentYear

    IF reading is null:
        customer.BillStatus = "Reading sheet not found..."
        SAVE customer
        RETURN "Reading sheet not found"

    IF reading.Previous1 < 0 OR reading.Present1 < 0:
        customer.BillStatus = "Previous Or Present Reading cannot be Negative..."
        SAVE customer
        RETURN "Wrong Reading"

    units = Present1 - Previous1

    IF units < 0:
        customer.BillStatus = "Previous Reading cannot be Less Than Current Reading..."
        SAVE customer
        RETURN "Wrong Reading"


    // ---------- 5. Tariff ----------
    tariff = Tarrif WHERE TarrifType = customer.Category

    IF tariff is null:
        customer.BillStatus = "Tariff Not Found..."
        SAVE customer
        RETURN "Tariff not found"

    energyGross = units * tariff.Rate1     // treated as GST-inclusive later


    // ---------- 6. FPA charges ----------
    fpaBill1 = ElectricityBills WHERE
           Btno = customer.Btno
       AND BillingMonth = FPA_Month1
       AND BillingYear  = FPA_Year1

    IF fpaBill1 is null:
        customer.BillStatus = "Bill for FPA not found..."
        SAVE customer
        RETURN "Bill for FPA not found"

    FPACHAR1 = 0
    FPACHAR2 = 0

    IF fpaBill1.TotalUnit > 50:
        FPACHAR1 = CalcFPA(fpaBill1.TotalUnit, FPA_Rate1)

    IF units > 50:
        FPACHAR2 = CalcFPA(units, FPA_Rate2)

    TotalFPA = FPACHAR1 + FPACHAR2


    FUNCTION CalcFPA(unitValue, rate):
        base   = ROUND(unitValue * rate)
        fee15  = ROUND(base * 1.5 / 100)
        gst18  = ROUND((base + fee15) * 0.18)
        RETURN base + fee15 + gst18


    // ---------- 7. Taxes / fees ----------
    opcRate   = SUM TaxInformation WHERE TaxName = "OPC"
                AND ApplicableFor IN (tariff.TarrifType, "All")
    opc       = units * opcRate

    ptvFee    = SUM TaxInformation WHERE TaxName = "PTVFee"
                AND ApplicableFor IN (customer.PlotType, "All")

    furtherTax = SUM TaxInformation WHERE TaxName = "Further Tax"
                 AND ApplicableFor IN (customer.PlotType, "All")
    // NOTE: furtherTax is loaded but NOT added to BillCost today

    gstConfig = SUM TaxInformation WHERE TaxName = "GST"
                AND ApplicableFor IN (customer.PlotType, "All")
    // NOTE: gstConfig rate is loaded; actual GST uses hard-coded 18% split below


    // ---------- 8. Energy + GST split ----------
    EnergyCoast = energyGross
    calcGst     = ROUND(EnergyCoast - (EnergyCoast / 1.18))   // 18% inclusive GST
    EnergyCoast = EnergyCoast - calcGst                       // energy excluding GST


    // ---------- 9. Totals ----------
    BillCost   = ROUND(EnergyCoast + opc + ptvFee + calcGst + TotalFPA)
    surcharge  = ROUND(EnergyCoast * 10 / 100)                // 10% of energy ex-GST

    amountInDueDate     = RoundToNearestTen(BillCost + arrears)
    amountAfterDueDate  = RoundToNearestTen(BillCost + surcharge + arrears)

    FUNCTION RoundToNearestTen(value):
        // remainder >= 5 → round UP to next 10
        // remainder <  5 → round DOWN to previous 10


    // ---------- 10. Save bill ----------
    newBill = NEW ElectricityBill:
        CustomerNo, Btno, CustomerName
        BillingMonth, BillingYear
        IssueDate, DueDate, ValidDate
        EnergyCoast
        CurrentBill              = ROUND(BillCost)
        BillAmountInDueDate      = amountInDueDate
        BillSurcharge            = surcharge
        BillAmountAfterDueDate   = amountAfterDueDate
        OPC, PTVFEE, FURTHERTAX, GST
        Previous/Current readings (1, 2, solar)
        Difference1 = units
        TotalUnit   = units
        BillAmount  = units * tariff.Rate1
        Arrears
        FPACHARGES  = TotalFPA
        AmountPaid  = 0
        // PaymentStatus not set in code → DB default "Unpaid"

    INSERT newBill
    SAVE

    InvoiceNo = YEAR(yyyy) + MONTH(MM) + last 5 digits of CustomerNo
    UPDATE newBill.InvoiceNo

    customer.BillStatus = "Bill Generated for {month} {year}"
    // NOTE: BillGenerationStatus is NOT updated here
    SAVE customer

    RETURN "Bill created successfully"
```

---

## E. Formulas cheat-sheet (current system)

| Item | Current formula |
|---|---|
| Units | `Present1 − Previous1` |
| Energy (gross) | `Units × Tarrif.Rate1` |
| GST (calc) | `Energy − Energy / 1.18` (hard-coded 18%) |
| Energy (net) | `Gross − GST` |
| OPC | `Units × TaxRate(OPC)` |
| PTV Fee | `TaxRate(PTVFee)` by PlotType |
| Further Tax | Loaded, **not in BillCost** |
| FPA (each part) | `round(u×r) + round(×1.5%) + round(×18%)` if units > 50 |
| Bill Cost | `EnergyNet + OPC + PTV + GST + TotalFPA` |
| Surcharge | `10% of EnergyNet` |
| Due (in date) | `Round10(BillCost + Arrears)` |
| Due (after date) | `Round10(BillCost + Surcharge + Arrears)` |

---

## F. YOUR REQUIREMENTS — edit this section

Use this block to write what you want changed. Keep it simple.

```
[ ] Who appears on the list?
    Current: BillGenerationStatus null / "Not Generated"
    Wanted : _______________________________________________

[ ] After success, update which customer field?
    Current: BillStatus only
    Wanted : _______________________________________________

[ ] Duplicate bill rule
    Current: same BTNo + Month + Year blocked
    Wanted : _______________________________________________

[ ] First-time customer (no prior bills)
    Current: arrears = 0, continue
    Wanted : _______________________________________________

[ ] Missing previous-month bill
    Current: fail that customer
    Wanted : _______________________________________________

[ ] Arrears rules
    Current:
      - Partially Paid → DueAmount − AmountPaid
      - UnPaid → AfterDueAmount
      - Else → 0
    Wanted : _______________________________________________

[ ] Reading validation
    Current: required; no negative; Present >= Previous
    Wanted : _______________________________________________

[ ] Tariff match
    Current: Tarrif.TarrifType == customer.Category
    Wanted : _______________________________________________

[ ] FPA
    Current: needs FPA Month1 bill; skip charge if units <= 50
    Wanted : _______________________________________________

[ ] Include Further Tax in BillCost?
    Current: NO
    Wanted : YES / NO

[ ] GST
    Current: hard-coded 18% inclusive split
    Wanted : _______________________________________________

[ ] Surcharge
    Current: 10% of energy (ex-GST)
    Wanted : _______________________________________________

[ ] Rounding
    Current: nearest 10 for due amounts
    Wanted : _______________________________________________

[ ] Show per-customer success/fail on UI?
    Current: always generic success
    Wanted : _______________________________________________

[ ] Other notes:
    _______________________________________________
```

---

## G. Where to change code after you edit requirements

| Topic | File |
|---|---|
| Button / alerts / AJAX | `BMSBT/Views/SGCustomers/GenerateBill.cshtml` |
| Who is listed on page | `BMSBT/Controllers/SGCustomersController.cs` → `GenerateBill` |
| Operator checks / loop | `BMSBT/Controllers/EBillUController.cs` → `GenerateElectricityBills` |
| Operator fields (FPA, dates) | `BMSBT/Interface/ICurrentOperatorService.cs` → `CurrentOperatorService` |
| **All bill formulas & rules** | `BMSBT/BillServices/ElectrcityFunctions.cs` → `GenerateEBillForCustomer` |
| Tax rates data | table `TaxInformation` |
| Tariff rates data | table `Tarrif` |
| Billing month / FPA setup | table `OperatorsSetup` |

---

## H. Tables touched

- `CustomersDetail`
- `OperatorsSetup`
- `ElectricityBills`
- `ReadingSheet`
- `Tarrif`
- `TaxInformation`
