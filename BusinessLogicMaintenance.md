# Maintenance Bill Business Logic

This document explains how Maintenance bills are created from the `Generate MBill` flow at `MaintenanceNew/GenerateBill`.

## 1) Entry Point and Main Flow

- UI page: `MaintenanceNew/GenerateBill`
- API action for generation: `POST MaintenanceNew/GenerateMaintenanceBills`
- Core calculator/service currently used by this action: `MaintenanceFunctions.GenerateBillForCustomer(...)`

### High-level sequence

1. Read logged-in operator from session.
2. Load operator setup (billing month/year and dates).
3. Validate required setup (month/year/operator id).
4. For each selected customer ID:
   - Fetch customer from `CustomersMaintenance`.
   - Check duplicate bill for same `BTNo + BillingMonth + BillingYear`.
   - Fetch tariff from `MaintenanceTarrifs` using:
     - `Project`
     - `PlotType`
     - `Size`
   - Resolve previous billing period.
   - Find previous bill and compute arrears if unpaid.
   - Load fine amount from `Fine` table for Maintenance service.
   - Load additional charges (Water/Other) from `AdditionalCharges`.
   - Apply formulas and create `MaintenanceBills` record.
   - Generate invoice no and update customer bill status fields.

## 2) Data Sources Used

- `CustomersMaintenance` (customer profile, BTNo, meter, project/plot/size)
- `MaintenanceTarrifs` (base maintenance charges + tax)
- `MaintenanceBills` (duplicate check + previous bill + insert new bill)
- `Fine` (sum of `FineToCharge` for service `Maintenance`)
- `AdditionalCharges`:
  - `ChargesName = "Water Charges"`
  - `ChargesName = "Other Charges"`
  - and `ServiceType = "Maintenance"`
- `OperatorsSetups` (billing month/year, issue date, due date)

## 3) Validations and Rules

- If operator billing month/year is missing: bill generation is blocked.
- If customer not found: skip with error message.
- If bill already exists for same customer/month/year: no new bill.
- If tariff not found for `Project + PlotType + Size`: no new bill.
- Previous bill handling:
  - If previous bill exists and is **not paid**, arrears are carried forward.
  - Paid statuses considered paid:
    - `paid`
    - `paid with surcharge`
    - `paidwithsurcharge`
  - If previous bill missing and customer is not new: generation is blocked for that customer.

## 4) Formula Logic Used for Bill Creation

From code in `MaintenanceFunctions` / insert service:

- `MaintCharges = tariff.Charges`
- `TaxAmount = tariff.Tax`
- `Arrears = previousBill.BillAmountAfterDueDate` (only if previous bill unpaid, else `0`)
- `Fine = SUM(Fine.FineToCharge)` where:
  - `Fine.BTNo == customer.BTNo`
  - `Fine.FineMonth == current month`
  - `Fine.FineYear == current year`
  - `Fine.FineService == "Maintenance"`
- `WaterCharges` from `AdditionalCharges` (`Maintenance`, `Water Charges`)
- `OtherCharges` from `AdditionalCharges` (`Maintenance`, `Other Charges`)

### Final formulas

1. **BillAmountInDueDate**

`BillAmountInDueDate = MaintCharges + TaxAmount + Arrears + Fine + WaterCharges + OtherCharges`

2. **BillSurcharge**

`BillSurcharge = 10% * (MaintCharges + TaxAmount)`

3. **BillAmountAfterDueDate**

`BillAmountAfterDueDate = BillAmountInDueDate + BillSurcharge`

### Rounding / Type behavior

- Values are rounded to whole numbers before storing.
- Many output columns are stored as `int`, so decimals are converted/rounded.

## 5) Invoice Number Formula

Per code requirement:

`InvoiceNo = YYYYMM + Last5Digits(CustomerNo)`

Example:
- Date part: `202601`
- CustomerNo tail: `22306`
- InvoiceNo: `20260122306`

## 6) Default Fields Set on New Bill

- `PaymentStatus = "unpaid"`
- `PaymentMethod = "NA"`
- `BankDetail = "NA"`
- `BillingDate = today` (or operator-provided date path in insert service)
- `IssueDate` and `DueDate` from operator setup when provided
- `ValidDate` defaults from code path (commonly now or next month depending on path)
- `LastUpdated = now`

## 7) Note About Two Implementations

There are two implementations in the codebase:

1. `MaintenanceFunctions` (currently used by `GenerateMaintenanceBills` in controller)
2. `MaintenanceBillInsertService` (isolated service with similar formulas)

Both use the same core billing math and 10% surcharge concept. The active `Generate MBill` flow is using `MaintenanceFunctions` in the controller path.
