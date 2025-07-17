using BMSBT.Models;
using BMSBT.Roles;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace BMSBT.Controllers
{
   
    public class TariffController : Controller
    {
        private readonly BmsbtContext db;

        public TariffController(BmsbtContext _context)
        {
            db = _context;
        }
        public IActionResult Index(int? page)
        {
            int pageSize = 100; // Number of items per page
            int pageNumber = (page ?? 1); // Default to page 1 if not specified

            var data = db.Tarrifs.ToList().ToPagedList(pageNumber, pageSize);

            return View(data);
        }


        public IActionResult Create()
        {
            return View(new Tarrif());
        }

        [HttpPost]
        public IActionResult Create(Tarrif model)
        {
           
                db.Tarrifs.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index");
            
           
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var tarrif = db.Tarrifs.Find(id);
            if (tarrif == null)
            {
                return NotFound();
            }
            return View(tarrif);
        }

        [HttpPost]
        public IActionResult Edit(Tarrif model)
        {
            if (ModelState.IsValid)
            {
                db.Update(model);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }


    }
}
