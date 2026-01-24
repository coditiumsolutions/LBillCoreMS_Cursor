using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace BMSBT.Controllers
{
    public class AdjustmentsController : Controller
    {
        private readonly BmsbtContext _dbContext;

        public AdjustmentsController(BmsbtContext context)
        {
            _dbContext = context;
        }

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");
            base.OnActionExecuting(context);
        }

        // GET: Adjustments
        public IActionResult Index(int? page)
        {
            int pageSize = 20;
            int pageNumber = page ?? 1;

            var items = _dbContext.Adjustments
                .OrderBy(a => a.BTNo)
                .ThenBy(a => a.BillingType)
                .ThenBy(a => a.AdjustmentName)
                .ToPagedList(pageNumber, pageSize);

            return View(items);
        }

        // GET: Adjustments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _dbContext.Adjustments
                .FirstOrDefaultAsync(m => m.AdjustmentId == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // GET: Adjustments/Create
        public IActionResult Create()
        {
            return View(new Adjustment());
        }

        // POST: Adjustments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Adjustment model)
        {
            if (ModelState.IsValid)
            {
                _dbContext.Adjustments.Add(model);
                await _dbContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "Adjustment record created successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Adjustments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _dbContext.Adjustments.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: Adjustments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Adjustment model)
        {
            if (id != model.AdjustmentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _dbContext.Update(model);
                    await _dbContext.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Adjustment record updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdjustmentExists(model.AdjustmentId))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            return View(model);
        }

        // GET: Adjustments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _dbContext.Adjustments
                .FirstOrDefaultAsync(m => m.AdjustmentId == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: Adjustments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _dbContext.Adjustments.FindAsync(id);
            if (item != null)
            {
                _dbContext.Adjustments.Remove(item);
                await _dbContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "Adjustment record deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AdjustmentExists(int id)
        {
            return _dbContext.Adjustments.Any(e => e.AdjustmentId == id);
        }
    }
}
