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
            if (string.IsNullOrEmpty(model.BillingMonth) ||
                string.IsNullOrEmpty(model.BillingYear) ||
                string.IsNullOrEmpty(model.Btno))
            {
                ModelState.AddModelError("", "Please provide Billing Month, Billing Year, and Btno.");
                return View("PaymentForm", model);
            }

            // Query the database for a matching bill
            var bill = _dbContext.ElectricityBills
                .FirstOrDefault(e =>
                    e.BillingMonth == model.BillingMonth &&
                    e.BillingYear == model.BillingYear &&
                     e.PaymentStatus == "Unpaid" &&
                    e.Btno == model.Btno);

            if (bill == null)
            {
                TempData["ErrorMessage"] = "No unpaid bill found for selected month.";
                return View("PaymentForm", model);
            }
            // Update the model with values from the database
            model.ReferenceNumber = bill.CustomerNo;
            model.CustomerName = bill.CustomerName;

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
