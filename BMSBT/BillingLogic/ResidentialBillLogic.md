# Residential Maintenance Billing Logic

> Source: IBM Notes LotusScript legacy system (IBMNotesCode.md)
> Target: ASP.NET Core – BMSBT project
> Category: **Residential**

---

## PHASE 1 – MAIN LOOP (LoopGeneration)

For every **unprocessed customer** document:

1. Run **VerifyProcess()** – if fails → skip to next customer
2. Run **PreviousBill()** – if fails → skip to next customer
3. Run **GenerateBill()** – creates the maintenance bill

After all customers processed → display total bills generated count.

---

## PHASE 2 – VERIFY PROCESS (Pre-Generation Validation)

### Step 2.1 – Duplicate Bill Check

Build duplicate key:

    Key = {BTNo} + "-" + {BillingYear} + "-" + {BillingMonth}

Look up existing bills by this key.

- IF bill already exists for this key:
  - Set BillGenStatus = "Bill Already Generated-{BillingYear}-{BillingMonth}"
  - Save customer record
  - **SKIP** this customer

### Step 2.2 – Disconnected Customer Check

- IF customer.ConnectionStatus = "Disconnected":
  - Set BillGenStatus = "Disconnected Customer"
  - Save customer record
  - **SKIP** this customer

### Step 2.3 – Validation Passes

- IF all checks pass → proceed to PreviousBill()

---

## PHASE 3 – PREVIOUS BILL LOOKUP

### Step 3.1 – Calculate Previous Billing Period

| Current Month | Previous Month Key        |
|---------------|---------------------------|
| January       | {PreviousYear}-December   |
| February      | {BillingYear}-January     |
| March         | {BillingYear}-February    |
| April         | {BillingYear}-March       |
| May           | {BillingYear}-April       |
| June          | {BillingYear}-May         |
| July          | {BillingYear}-June        |
| August        | {BillingYear}-July        |
| September     | {BillingYear}-August      |
| October       | {BillingYear}-September   |
| November      | {BillingYear}-October     |
| December      | {BillingYear}-November    |

When current month is January, the previous year is BillingYear − 1.

### Step 3.2 – Look Up Previous Bill

    Key = {BTNo} + "-" + {PreviousMonth}

Search for previous bill by this key.

- **IF found** → go to Step 3.3
- **IF NOT found**:
  - Search for **any** previous bill by BTNo alone
  - IF no bills at all exist → this is a **new customer** → proceed (treat as first bill)
  - IF bills exist but not for previous month:
    - Set BillGenStatus = "previous bill not exist"
    - Set Arrears = 0
    - **SKIP** this customer

### Step 3.3 – Validate Previous Bill Amounts

- IF previous bill's AmountDueDate is empty OR AmountAfterDueDate is empty:
  - Set BillGenStatus = "wrong previous bill amount"
  - Set Arrears = 0
  - **SKIP** this customer
- ELSE → proceed to GenerateBill()

---

## PHASE 4 – GENERATE BILL (Orchestration)

Execute the following sub-steps **in this exact order**:

1. SetVariableZero()
2. NewBill()
3. SetPaymentStatus()
4. AddMonthYear()
5. AddCustomerDetail()
6. RateAndServiceTax()
7. AddFine()
8. AddDefferedAmount()
9. AddWaterCharges()
10. AddInvoiceNo()
11. AddValidDate()
12. AddArrears()
13. AddOtherCharges()
14. **CalculateBill()**

After CalculateBill() completes:

- Set customer.IsGenerated = "1"
- Set customer.GeneratedMonth = "{BillingMonth}-{BillingYear}"
- Set customer.BillGenStatus = "Generated"
- Set customer.PaymentStatus = "Unpaid"
- **Save** both the new bill document and the customer document

---

## STEP 4.1 – SetVariableZero (Initialize All Variables)

Reset all calculation variables to zero before each bill:

    SumAmount       = 0
    Tariff          = ""
    Arrears         = 0
    CurrentBill     = 0
    TotalPayable    = 0
    LPSurcharge     = 0
    LatePaymentAmount = 0
    PreviousCharges = 0
    NewsCharges     = 0
    MaintCharges    = 0
    AmountSurcharge = 0
    OverallBill     = 0
    Surcharge       = 0
    LateBill        = 0

---

## STEP 4.2 – NewBill

Create a new bill document with Form = "Maintenance Bill".

---

## STEP 4.3 – SetPaymentStatus

    Bill.PaymentStatus = "unpaid"

All newly generated bills start as **unpaid**.

---

## STEP 4.4 – AddMonthYear

Copy from Operator Setup to Bill **and** Customer:

    Bill.BillingYear  = OperatorSetup.BillingYear
    Bill.BillingMonth = OperatorSetup.BillingMonth
    Customer.BillingYear  = OperatorSetup.BillingYear
    Customer.BillingMonth = OperatorSetup.BillingMonth

---

## STEP 4.5 – AddCustomerDetail

Copy the following fields from Customer to Bill:

| Field on Bill               | Source (Customer)                |
|-----------------------------|----------------------------------|
| Tariff                      | Customer.Tariff                  |
| ReferenceNoBarCode (BTNo)   | Customer.ReferenceNoBarCode      |
| NewCustomer_Reference       | Customer.NewCustomer_Reference   |
| Customer_Name               | Customer.Customer_Name           |
| Customer_Reference          | Customer.Customer_Reference      |
| CustomerNo                  | Customer.CustomerNo              |
| Plot_Number                 | Customer.Plot_Number             |
| Street_Number               | Customer.Street_Number           |
| Meter_Number                | Customer.Meter_Number            |
| Sector                      | Customer.Sector                  |
| Block                       | Customer.Block                   |
| Plot_Size                   | Customer.Plot_Size               |
| Feeder                      | Customer.Feeder                  |
| Meter_Type                  | Customer.Meter_Type              |
| NIC_Number                  | Customer.NIC_Number              |
| IncomeTaxWithHeld_Flag      | Customer.IncomeTaxWithHeld_Flag  |
| FurtherTax_Flag             | Customer.FurtherTax_Flag         |
| IncomeTaxWithHeld           | Customer.Arrears_IncomeTaxWithHeld |
| FPA1, FPA2, FPA3            | Customer.FPA1, FPA2, FPA3        |
| TotalFuelPrice              | Customer.Total_FuelAdj           |
| ProjectName                 | Customer.ProjectName             |
| Barcode                     | Customer.Barcode                 |
| InstalledOn                 | Customer.InstalledOn (formatted dd-MMM-yyyy) |
| UnpaidCount (txtunpaid)     | Customer.txtunpaid               |
| Plot_Category               | Customer.Plot_Category           |
| Plot_Status                 | Customer.Plot_Status             |
| LastMonthStatus             | Customer.txtlastmonthstatus      |
| LastMonthAmount             | Customer.txtlastmonthamount      |
| LastMonthAfterDueDate       | Customer.txtlastmonthafterduedate|
| IsTwoReadingMeter           | Customer.IsTwoReadingMeter       |

### From Operator Setup:

| Field on Bill    | Source (OperatorSetup)     |
|------------------|----------------------------|
| Date_Reading     | OperatorSetup.ReadingDate  |
| Date_Issue       | OperatorSetup.IssueDate    |
| Date_Due         | OperatorSetup.DueDate      |

### Other:

    Bill.Method = "Multi"
    Customer.BillGenStatus = "Generated-{BillingYear}-{BillingMonth}"
    Customer.BillGenerated = "1"
    TotalBills = TotalBills + 1

Save both documents after this step.

---

## STEP 4.6 – RateAndServiceTax (Residential)

### Category Routing Logic

The system first checks for **special block types** before falling back to category:

1. **IF** Block is "Plaza Floor" → call **RatesPlazaFloor()** → exit
2. **IF** Block is "Awami Apartments" / "Awami Apartment" / "Apartments EMC" → call **RatesApartments()** → exit
3. **IF** Plot_Category = "Residential" → call **RatesResidential()** → exit
4. **IF** Plot_Category = "Commercial" → call **RatesCommercial()** → exit (see CommercialBillLogic.md)

### Fallback Rate Lookup (if none of the above match)

Build rate lookup key:

    IF Plot_Category = "Cinema"
        Key = "Cinema-1 M"
    ELSE
        Key = {Plot_Category} + "-" + {Plot_Size}

    IF Block contains "commercial"
        Key = "Commercial-1 M"
        IF Plot_Size is not empty
            Key = {Plot_Category} + "-" + {Plot_Size}

### Rate Lookup from Rates Setup

Look up from Rates view using the built key.

- **IF no rate found**:
  - Use customer's own stored values:
    - MaintCharges = Customer.Maintenance_Charges
    - ServiceTaxGovt = Customer.ServiceTaxGovt
  - Set BillGenStatus = "rate not defined in rates setup"

- **IF rate found**:
  - MaintCharges = Rate from Rates Setup (0 if Rate field is empty)
  - ServiceTaxGovt = ServiceTaxGovt from Rates Setup (0 if empty)

### ServiceTaxGovt Rounding Rule (Residential)

- For standard residential sizes (5 M, 4 M, 3 M, 1 M) → ServiceTaxGovt is used **as-is**
- For **all other sizes** → ServiceTaxGovt = Round(ServiceTaxGovt, nearest 100)

### Merging Rule

- IF Customer.MergingStatus = "Merged":
  - MaintCharges = MaintCharges / 2
  - MaintCharges = Round(MaintCharges, 0)

### Plaza Floor Override

- IF Customer.PlazaFloor = "Plaza Floor":
  - MaintCharges = Customer.Maintenance_Charges
  - ServiceTaxGovt = Customer.ServiceTaxGovt
  (overrides whatever was calculated above)

### Store on Bill

    Bill.MaintCharges = MaintCharges
    Bill.ServiceTaxGovt = ServiceTaxGovt

---

## STEP 4.7 – AddFine

### Fine Lookup

Look up Fine records from Fine database:

    Key = {BTNo} + "-" + {BillingMonth} + "-" + {BillingYear}

### Fine Calculation

- IF no fine records found → Fine = 0
- IF fine records found:
  - Loop through **all** matching fine records
  - Sum all FineAmount values: `Fine = SUM(FineAmount)`
  - Sum all WaivedOffAmount values: `WaivedOffAmount = SUM(WaivedOffAmount)`
  - Store FineDept (department) from fine record → Bill.FineDept

### Apply Waiver

    Fine = Fine - WaivedOffAmount
    IF Fine < 0 THEN Fine = 0

### Adjustment Check

- IF the first fine record's Type = "Adjustment":
  - Adjustment = AdjAmount (0 if empty)
  - Fine = 0 (fine is zeroed when there is an adjustment)

### Store on Bill

    Bill.Fine = Fine
    Bill.Adjustment = Adjustment

---

## STEP 4.8 – AddDefferedAmount

    IF Customer.DefferedAmount is empty THEN
        DefferedAmount = 0
    ELSE
        DefferedAmount = Customer.DefferedAmount

    Bill.DefferedAmount = DefferedAmount

> Note: DefferedAmount is stored on the bill but is **NOT** included in CalculateBill() in the current active code.

---

## STEP 4.9 – AddWaterCharges

    IF Customer.WaterCharges is empty THEN
        WaterCharges = 0
    ELSE
        WaterCharges = Customer.WaterCharges

    Bill.WaterCharges = WaterCharges

---

## STEP 4.10 – AddInvoiceNo

    IF CustomerNo is empty THEN
        InvoiceNo = {InvoiceNoLeft} + "-"
    ELSE
        InvoiceNo = {InvoiceNoLeft} + RIGHT(CustomerNo, 5)

Where `InvoiceNoLeft` = YYYYMM (current year + month as 6-digit prefix).

Example: CustomerNo = "BT-22306", InvoiceNoLeft = "202601"
→ InvoiceNo = "20260122306"

    Bill.InvoiceNo = InvoiceNo

---

## STEP 4.11 – AddValidDate

    Bill.ValidDate = OperatorSetup.ValidDate (formatted dd-MMM-yyyy)

---

## STEP 4.12 – AddArrears

### Arrears Determination (from Previous Bill)

| Previous Bill Status     | Arrears Source                                |
|--------------------------|-----------------------------------------------|
| No previous bill exists  | Arrears = 0                                   |
| Customer status = "Paid" | Arrears = 0                                   |
| Customer status = "Paid with Surcharge" | Arrears = 0                      |
| Previous bill = "Unpaid" or empty | Arrears = PreviousBill.AmountAfterDueDate |
| Previous bill = "Partially Paid" | RemainingAmount = BillAmountInDueDate - PaymentAmount; Arrears = max(RemainingAmount, 0) |
| Any other status         | Arrears = 0                                   |

### Additional Manual Arrears

After determining arrears from previous bill, **add** any manually set arrears:

    IF Customer.main_arrears is not empty THEN
        Arrears = Arrears + Customer.main_arrears

    IF Customer.maint_arrears is not empty THEN
        Arrears = Arrears + Customer.maint_arrears

### Store on Bill

    Bill.Arrears = Arrears

---

## STEP 4.13 – AddOtherCharges

    IF Customer.OtherCharges is empty THEN
        OtherCharges = 0
    ELSE
        OtherCharges = Customer.OtherCharges

    Bill.OtherCharges = OtherCharges

---

## PHASE 5 – CALCULATE BILL (Final Calculation)

### Step 5.1 – Current Bill (Total charges before arrears)

    CurrentBill = MaintCharges
               + ServiceTaxGovt
               + Fine
               + WaterCharges
               + OtherCharges
               + PreviousCharges
               + NewsCharges

> In active code PreviousCharges = 0 and NewsCharges = 0 (their functions are commented out).
> Effectively: **CurrentBill = MaintCharges + ServiceTaxGovt + Fine + WaterCharges + OtherCharges**

### Step 5.2 – Rounding CurrentBill

    CurrentBill = Round(CurrentBill, nearest 10)

Example: 4573 → 4570, 4575 → 4580

    Bill.TotalMaintenanceCharges = CurrentBill

### Step 5.3 – Surcharge Base

    AmountSurcharge = MaintCharges + ServiceTaxGovt

> Surcharge is calculated on **MaintCharges + Tax only**, NOT on the full bill.

### Step 5.4 – Overall Bill (Amount In Due Date)

    OverallBill = CurrentBill + Arrears
    OverallBill = Round(OverallBill, 0)
    OverallBill = OverallBill - Adjustment

    Bill.AmountDueDate = OverallBill
    Bill.GTotal = OverallBill

### Step 5.5 – Surcharge Calculation (10%)

    IF AmountSurcharge <= 0 THEN
        Surcharge = 0
    ELSE
        Surcharge = AmountSurcharge * 10 / 100
        Surcharge = Round(Surcharge, 0)

    IF OverallBill < 0 THEN
        Surcharge = 0

    Bill.Surcharge = Surcharge

### Step 5.6 – Late Bill (Amount After Due Date)

    LateBill = OverallBill + Surcharge

    Bill.AmountAfterDueDate = LateBill

### Step 5.7 – Store Totals

    Bill.TotalElecCharges = CurrentBill
    Bill.BillTotal = CurrentBill
    Bill.Surcharge = Surcharge
    Bill.AmountDueDate = OverallBill
    Bill.GTotal = OverallBill
    Bill.AmountAfterDueDate = LateBill
    Customer.AmountDueDate = OverallBill

---

## COMPLETE FORMULA SUMMARY

```
MaintCharges     ← from Tariff/Rates Setup (halved if Merged)
ServiceTaxGovt   ← from Tariff/Rates Setup (rounded to 100 for non-standard sizes)
Fine             ← SUM(FineAmount) − WaivedOff (0 if Adjustment type)
Adjustment       ← AdjAmount from Fine record (if Type = "Adjustment")
WaterCharges     ← from Customer record
OtherCharges     ← from Customer record
DefferedAmount   ← from Customer record (stored only, not in calculation)
Arrears          ← from Previous Bill + manual arrears fields

CurrentBill      = MaintCharges + ServiceTaxGovt + Fine + WaterCharges + OtherCharges
CurrentBill      = Round(CurrentBill, nearest 10)

OverallBill      = CurrentBill + Arrears
OverallBill      = Round(OverallBill, 0) − Adjustment

AmountSurcharge  = MaintCharges + ServiceTaxGovt
Surcharge        = IF (AmountSurcharge > 0 AND OverallBill >= 0)
                       THEN Round(AmountSurcharge × 10%, 0)
                       ELSE 0

LateBill         = OverallBill + Surcharge

Bill.AmountDueDate      = OverallBill
Bill.AmountAfterDueDate = LateBill
Bill.Surcharge          = Surcharge
```

---

## FIELD MAPPING: Legacy → .NET

| Legacy Field (IBM Notes)        | .NET Field (MaintenanceBill)  |
|---------------------------------|-------------------------------|
| main_charges                    | MaintCharges                  |
| ServiceTaxGovt                  | TaxAmount                     |
| fine                            | Fine                          |
| adjustment                      | Adjustment                    |
| watercharges                    | WaterCharges                  |
| othercharges                    | OtherCharges                  |
| DefferedAmount                  | DefferedAmount                |
| arrears                         | Arrears                       |
| TotalMaintenanceCharges         | TotalMaintenanceCharges       |
| amountduedate / GTotal          | BillAmountInDueDate           |
| surcharge                       | BillSurcharge                 |
| amountafterdate                 | BillAmountAfterDueDate        |
| InvoiceNo                       | InvoiceNo                     |
| paymentstatus                   | PaymentStatus                 |
| Billing_Year                    | BillingYear                   |
| Billing_Month                   | BillingMonth                  |
| date_issue                      | IssueDate                     |
| date_due                        | DueDate                       |
| ValidDate                       | ValidDate                     |
| BillingDate / date_reading      | BillingDate                   |
| method                          | Method ("Multi")              |
| refrencenobarcode               | Btno                          |
| Customer_Name                   | CustomerName                  |
| customerno                      | CustomerNo                    |
| Plot_Category                   | PlotCategory                  |
| Plot_Status                     | PlotStatus                    |
| MergingStatus                   | MergingStatus                 |
| BillGenStatus                   | BillGenerationStatus          |

---

## EDGE CASES & SPECIAL RULES

1. **Merged Plots**: MaintCharges is halved and rounded to 0 decimals.
2. **Plaza Floor**: Overrides rate lookup — uses customer's own stored MaintCharges and ServiceTaxGovt.
3. **Apartments** (Awami Apartments / Apartments EMC): Separate rate function (RatesApartments).
4. **Rate Not Found**: Falls back to customer's own Maintenance_Charges and ServiceTaxGovt fields.
5. **Negative OverallBill**: Surcharge is forced to 0 (no surcharge on credit balances).
6. **Negative Fine**: Fine is floored at 0 after waiver subtraction.
7. **Adjustment Type Fine**: When fine record is an "Adjustment", Fine = 0 and AdjAmount is subtracted from OverallBill.
8. **Partially Paid Previous Bill**: Arrears come from PaymentRemaining, not AmountAfterDueDate.
9. **CurrentBill Rounding**: Rounded to nearest 10 (Round with -1 precision).
10. **ServiceTaxGovt Rounding**: For non-standard residential sizes, rounded to nearest 100.

---
