using BMSBT.DTO;
using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BMSBT.Controllers
{
    public class BillPaymentController : Controller
    {
        private readonly BmsbtContext _dbContext;
        private readonly ICurrentOperatorService _operatorService;
        public BillPaymentController(BmsbtContext dbContext , ICurrentOperatorService operatorService)
        {
            _dbContext = dbContext;
            _operatorService = operatorService;
        }

        public async Task<IActionResult> PaymentForm()
        {
            string operatorId = HttpContext.Session.GetString("OperatorId");

            if (string.IsNullOrEmpty(operatorId))
            {
                return new JsonResult(new { success = false, message = "Operator ID not found in session" });
            }

            await _operatorService.InitializeAsync(operatorId);
            var currentOperator = _operatorService.GetCurrentOperator();

            var model = new BillViewModel
            {
                BillingMonth = currentOperator.BillingMonth,
                BillingYear = currentOperator.BillingYear
                
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult OpenBill(BillViewModel model)
        {
            if (string.IsNullOrEmpty(model.Btno) ||
                string.IsNullOrEmpty(model.BillingMonth) ||
                string.IsNullOrEmpty(model.BillingYear))
            {
                ModelState.AddModelError("", "Please provide complete Btno, Billing Month, and Billing Year.");
                return View("PaymentForm", model);
            }

            // Search unpaid bill
            var bill = _dbContext.ElectricityBills
                .FirstOrDefault(e =>
                    e.BillingMonth == model.BillingMonth &&
                    e.BillingYear == model.BillingYear &&
                    e.Btno == model.Btno &&
                    e.PaymentStatus == "Unpaid");

            if (bill == null)
            {
                TempData["ErrorMessage"] = "No unpaid bill found for the selected reference.";
                return View("PaymentForm", model);
            }

            // Populate result
            model.ReferenceNumber = bill.CustomerNo;
            model.CustomerName = bill.CustomerName;

            TempData["SuccessMessage"] = "Bill found successfully.";
            ModelState.Clear();
            return View("PaymentForm", model);
        }

        [HttpPost]
        public IActionResult MarkPaid(BillViewModel model)
        {
            var bill = _dbContext.ElectricityBills
               .FirstOrDefault(e => e.BillingMonth == model.BillingMonth &&
                                    e.BillingYear == model.BillingYear &&
                                    e.Btno == model.Btno &&
                                    e.PaymentStatus == "Unpaid"

                                    );
            if (bill == null)
            {
                TempData["ErrorMessage"] = "No unpaid bill found for selected month.";
                return View("PaymentForm", model);
            }

            // Update bill payment details
            bill.PaymentStatus = string.IsNullOrEmpty(model.PaymentType) ? "Paid" : model.PaymentType;
            bill.PaymentDate = DateOnly.FromDateTime(DateTime.Now);
            bill.BankDetail = model.BankBranch;

            _dbContext.SaveChanges();

            TempData["SuccessMessage"] = "Bill marked as paid successfully!";
            ModelState.Clear();
            return View("PaymentForm", model);
        }
    }
}
