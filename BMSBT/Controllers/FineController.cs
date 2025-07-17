using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BMSBT.Controllers
{
    public class FineController : Controller
    {
        private readonly BmsbtContext _dbContext;
        //public IActionResult Index()
        //{
        //    var fines = _dbContext.Fine.ToList();
        //    return View(fines); // ✅ This passes the model to the view
        //}




        
           
            public FineController(BmsbtContext context)
            {
                _dbContext = context;
            }




        public IActionResult Index(string BTNo, string CustomerName, string FineMonth, string FineYear)
        {
            var fines = _dbContext.Fine.AsQueryable();

            if (!string.IsNullOrEmpty(BTNo))
                fines = fines.Where(f => f.BTNo.Contains(BTNo));

            if (!string.IsNullOrEmpty(CustomerName))
                fines = fines.Where(f => f.CustomerName.Contains(CustomerName));

            if (!string.IsNullOrEmpty(FineMonth))
                fines = fines.Where(f => f.FineMonth == FineMonth);

            if (!string.IsNullOrEmpty(FineYear))
                fines = fines.Where(f => f.FineYear.ToString() == FineYear);

            var model = fines.ToList();

            return View(model);
        }






        //public IActionResult GetCustomerByBTNo(string btNo, string project)
        //{
        //    if (string.IsNullOrEmpty(btNo) || string.IsNullOrEmpty(project))
        //    {
        //        return Json(new { success = false, message = "BT No or Project is missing." });
        //    }

        //    object customer = null;

        //    if (project == "Orchards")
        //    {
        //        customer = _dbContext.CustomersDetails.FirstOrDefault(c => c.Btno == btNo);
        //    }
        //    else if (project == "Mohlanwal")
        //    {
        //        customer = _dbContext.CustomersMaintenance.FirstOrDefault(c => c.BTNo == btNo);
        //    }

        //    if (customer == null)
        //    {
        //        return Json(new { success = false, message = "Customer not found." });
        //    }

        //    // You may need to map to a view model depending on your schema
        //    return Json(new
        //    {
        //        success = true,
        //        model = new
        //        {
        //            projectName = project,
        //            CustomerName = customer.customerName,
        //            Block = customer.Block,
        //            Sector = customer.sector,
        //            BTNo = customer.Btno
        //        }
        //    });
        //}




        // CONTROLLER CODE - Fixed to return JSON instead of View
        [HttpGet]
        public JsonResult GetCustomerByBTNo(string btNo, string project)
        {
            try
            {
                if (string.IsNullOrEmpty(btNo) || string.IsNullOrEmpty(project))
                {
                    return Json(new { success = false, message = "BT No or Project is missing." });
                }

                // Temporary object to store common data
                string btNoValue = "";
                string projectName = "";
                string customerName = "";
                string block = "";
                string sector = "";

                if (project == "Electricity")
                {
                    var customer = _dbContext.CustomersDetails.FirstOrDefault(c => c.Btno == btNo);
                    if (customer == null)
                        return Json(new { success = false, message = "Customer not found in Electricity records." });

                    // Assigning manually due to naming mismatch
                    btNoValue = customer.Btno;
                    projectName = customer.Project;
                    customerName = customer.CustomerName;
                    block = customer.Block;
                    sector = customer.Sector;
                }
                else if (project == "Maintenance")
                {
                    var customer = _dbContext.CustomersMaintenance.FirstOrDefault(c => c.BTNo == btNo);
                    if (customer == null)
                        return Json(new { success = false, message = "Customer not found in Maintenance records." });

                    btNoValue = customer.BTNo;
                    projectName = customer.Project;
                    customerName = customer.CustomerName;
                    block = customer.Block;
                    sector = customer.Sector;
                }

                // Now safely create your Fine model
                var fineModel = new Fine
                {
                    BTNo = btNoValue,
                    ProjectName = projectName,
                    CustomerName = customerName,
                    Block = block,
                    Sector = sector,
                    FineMonth = DateTime.Now.ToString("MMMM"),
                    FineYear = DateTime.Now.Year,
                    DateFine = DateTime.Now,
                    FineEnterDate = DateTime.Now,
                    FineEnteredBy = User.Identity?.Name ?? "System"
                };

                return Json(new { success = true, model = fineModel });
            }
            catch (Exception ex)
            {
                // Optionally log ex
                return Json(new { success = false, message = "Server error occurred." });
            }
        }









        //// CONTROLLER CODE - Fixed to return JSON instead of View
        //[HttpGet]
        //public IActionResult GetCustomerByBTNo(string btNo)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(btNo))
        //        {
        //            return Json(new { success = false, message = "BT No is required" });
        //        }

        //        var customer = _dbContext.CustomersDetails
        //            .FirstOrDefault(c => c.Btno == btNo);

        //        if (customer == null)
        //        {
        //            return Json(new { success = false, message = "Customer not found" });
        //        }

        //        // Create a Fine model with customer data populated
        //        var fineModel = new Fine
        //        {
        //            BTNo = customer.Btno,
        //            ProjectName = customer.Project,
        //            CustomerName = customer.CustomerName,
        //            Block = customer.Block,
        //            Sector = customer.Sector,
        //            // Set default values for other required fields
        //            FineMonth = DateTime.Now.Month.ToString(), // Default month
        //            FineYear = DateTime.Now.Year,
        //            DateFine = DateTime.Now,
        //            FineEnterDate = DateTime.Now,
        //            FineEnteredBy = User.Identity.Name ?? "System" // Current user
        //        };

        //        return Json(new
        //        {
        //            success = true,
        //            model = fineModel // Return the entire model as JSON
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the exception (you should implement proper logging)
        //        return Json(new { success = false, message = "Server error occurred" });
        //    }
        //}




        // GET: Fine/Create
        public IActionResult Create(string btNo)
        {
            var fine = new Fine
            {
                BTNo = btNo // pre-fill the BTNo field if value exists
            };

            return View(fine);
        }


        // POST: Fine/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Fine fine)
        {
            // Get the session value
            var userName = HttpContext.Session.GetString("UserName");
            
            // Set FineEnteredBy before saving
            fine.FineEnteredBy = userName ?? "Unknown";

            if (ModelState.IsValid)
            {
                _dbContext.Fine.Add(fine);
                await _dbContext.SaveChangesAsync();

                return RedirectToAction("Index"); // Or another view as needed
            }
            return View(fine);
        }




        // GET: Fine/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fine = _dbContext.Fine.FirstOrDefault(f => f.FineID == id);

            if (fine == null)
            {
                return NotFound();
            }

            return View(fine);
        }




        // GET: Fine/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var fine = await _dbContext.Fine.FindAsync(id);
            if (fine == null)
                return NotFound();

            return View(fine);
        }

        // POST: Fine/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Fine fine)
        {
            if (id != fine.FineID)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _dbContext.Update(fine);
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_dbContext.Fine.Any(e => e.FineID == id))
                        return NotFound();
                    else
                        throw;
                }
            }
            return View(fine);
        }





    }


}
