using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


namespace BMSBT.Controllers
{
    public class OperatorSetupController : Controller
    {
        private readonly BmsbtContext _context;
        public OperatorSetupController(BmsbtContext context)
        {
            _context = context;

        }
        public async Task<IActionResult> Index()
        {
            var data = await _context.OperatorsSetups.ToListAsync(); // ✅ works with EF Core
            return View(data);
        }


        private void PopulateDropdowns()
        {
            var months = new List<SelectListItem>
    {
        new("January", "January"),
        new("February", "February"),
        new("March", "March"),
        new("April", "April"),
        new("May", "May"),
        new("June", "June"),
        new("July", "July"),
        new("August", "August"),
        new("September", "September"),
        new("October", "October"),
        new("November", "November"),
        new("December", "December")
    };

            var years = Enumerable.Range(DateTime.Now.Year, 10) // current year to +9
                .Select(y => new SelectListItem { Text = y.ToString(), Value = y.ToString() })
                .ToList();

            ViewBag.Months = months;
            ViewBag.Years = years;
        }



        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OperatorsSetup model)
        {
            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns();
            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var data = await _context.OperatorsSetups.FirstOrDefaultAsync(o => o.Uid == id);
            if (data == null) return NotFound();

            PopulateDropdowns();
            return View(data);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OperatorsSetup model)
        {
            if (id != model.Uid) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.OperatorsSetups.FirstOrDefaultAsync(o => o.Uid == id);
                    if (existing == null) return NotFound();

                    // Update fields
                    existing.OperatorID = model.OperatorID;
                    existing.OperatorName = model.OperatorName;
                    existing.BillingMonth = model.BillingMonth;
                    existing.BillingYear = model.BillingYear;
                    existing.BankName = model.BankName;
                    existing.IssueDate = model.IssueDate;
                    existing.DueDate = model.DueDate;
                    existing.FPARate1 = model.FPARate1;
                    existing.FPAMonth1 = model.FPAMonth1;
                    existing.FPAYEAR1 = model.FPAYEAR1;
                    existing.FPARate2 = model.FPARate2;
                    existing.FPAMonth2 = model.FPAMonth2;
                    existing.FPAYEAR2 = model.FPAYEAR2;

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.OperatorsSetups.Any(e => e.Uid == id))
                        return NotFound();
                    else
                        throw;
                }
            }

            PopulateDropdowns();
            return View(model);
        }




        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var data = await _context.OperatorsSetups.FirstOrDefaultAsync(o => o.Uid == id);
            if (data == null)
                return NotFound();

            return View(data);
        }



        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var record = await _context.OperatorsSetups.FirstOrDefaultAsync(o => o.Uid == id);
            if (record == null)
                return NotFound();

            _context.OperatorsSetups.Remove(record);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }



    }
}
