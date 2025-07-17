using Microsoft.AspNetCore.Mvc;

namespace BMSBT.Controllers
{
    public class ConsumeApiController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // Inject IHttpClientFactory into the controller
        public ConsumeApiController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // This method makes the API call and retrieves the PDF



        [HttpGet]
        public async Task<IActionResult> GenerateBill()
        {
            return View();
        }


        //
           [HttpPost]
        public async Task<IActionResult> GenerateBill(string billingMonth, string billingYear)
        {
            // Create an HttpClient instance using IHttpClientFactory
            var client = _httpClientFactory.CreateClient();

            // API URL with query parameters
            var url = $"https://localhost:7050/api/Customer/GetMaintenanceBill?BillingMonth={billingMonth}&BillingYear={billingYear}";

            // Send GET request to the API
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                // If successful, get the byte array (PDF content)
                var pdfData = await response.Content.ReadAsByteArrayAsync();

                // Return the PDF as a file to the client
                return File(pdfData, "application/pdf", "MaintenanceBill.pdf");
            }
            else
            {
                // Handle error (API call failed)
                return NotFound("Could not generate the bill.");
            }
        }



        [HttpGet]
        public async Task<IActionResult> ElectricityBill()
        {
            return View();
        }



        [HttpPost]
        public async Task<IActionResult> GenerateEBill(string monthDropdown, string yearDropdown)
        {
            // Create an HttpClient instance using IHttpClientFactory
            var client = _httpClientFactory.CreateClient();

            // API URL with query parameters
            var url = $"https://localhost:7050/api/Customer/GetElectrcityBill?BillingMonth={monthDropdown}&BillingYear={yearDropdown}";


            // Send GET request to the API
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                // If successful, get the byte array (PDF content)
                var pdfData = await response.Content.ReadAsByteArrayAsync();

                // Return the PDF as a file to the client
                return File(pdfData, "application/pdf", "MaintenanceBill.pdf");
            }
            else
            {
                // Handle error (API call failed)
                return NotFound("Could not generate the bill.");
            }
        }








    }
}

