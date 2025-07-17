using BMSBT.Models;
using DevExpress.XtraRichEdit.Model;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

public class TaxController : Controller
{

    private readonly BmsbtContext _dbContext;
    private readonly ICurrentOperatorService _operatorService;

    public TaxController(BmsbtContext dbContext, ICurrentOperatorService operatorService)
    {
        _dbContext = dbContext;
        _operatorService = operatorService;
    }

  

    public IActionResult Index(int? page)
    {
        int pageSize = 100; // Number of items per page
        int pageNumber = (page ?? 1); // Default to page 1 if not specified

        var data = _dbContext.TaxInformations.ToList().ToPagedList(pageNumber, pageSize);

        return View(data);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TaxInformation tax)
    {
        if (ModelState.IsValid)
        {
            tax.TaxId = 0;
            _dbContext.TaxInformations.Add(tax);
            _dbContext.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        return View(tax);
    }

    public IActionResult Edit(int id)
    {
        var tax = _dbContext.TaxInformations.Find(id);
        if (tax == null)
        {
            return NotFound();
        }
        return View(tax);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(TaxInformation tax)
    {
        if (!ModelState.IsValid)
        {
            return View(tax);
        }

        _dbContext.Attach(tax);
        // Mark only the desired properties as modified
        _dbContext.Entry(tax).Property(x => x.TaxName).IsModified = true;
        _dbContext.Entry(tax).Property(x => x.TaxRate).IsModified = true;
        _dbContext.Entry(tax).Property(x => x.ApplicableFor).IsModified = true;
        _dbContext.Entry(tax).Property(x => x.Range).IsModified = true;
        _dbContext.Entry(tax).Property(x => x.IsActive).IsModified = true;

        

        _dbContext.SaveChanges();

        return RedirectToAction("Index");
    }


    public IActionResult Delete(int id)
    {
        var tax = _dbContext.TaxInformations.Find(id);
        if (tax == null)
        {
            return NotFound();
        }
        return View(tax);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var tax = _dbContext.TaxInformations.Find(id);
        if (tax == null)
        {
            return NotFound();
        }
        _dbContext.TaxInformations.Remove(tax);
        _dbContext.SaveChanges();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Details(int id)
    {
        var tax = _dbContext.TaxInformations.Find(id);
        if (tax == null)
        {
            return NotFound();
        }
        return View(tax);
    }
}
