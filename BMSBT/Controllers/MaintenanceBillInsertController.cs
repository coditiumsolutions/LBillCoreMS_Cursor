using BMSBT.DTO;
using BMSBT.Models;
using BMSBT.Services;
using Microsoft.AspNetCore.Mvc;

namespace BMSBT.Controllers;

/// <summary>
/// Lightweight API controller for inserting MaintenanceBills.
/// This is intentionally isolated from existing MaintenanceNew UI and controllers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MaintenanceBillInsertController : ControllerBase
{
    private readonly IMaintenanceBillInsertService _service;
    private readonly BmsbtContext _dbContext;

    public MaintenanceBillInsertController(IMaintenanceBillInsertService service, BmsbtContext dbContext)
    {
        _service = service;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates a new MaintenanceBills record using default business rules.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MaintenanceBillCreateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _service.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Uid }, result);
    }

    /// <summary>
    /// Simple lookup endpoint mainly to satisfy CreatedAtAction.
    /// </summary>
    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        // This endpoint is intentionally minimal and read-only,
        // to avoid interfering with existing MaintenanceNew flows.
        return NoContent();
    }

    /// <summary>
    /// Bulk create MaintenanceBills for a list of CustomersMaintenance UIDs.
    /// This is designed to be called from the MaintenanceNew/GenerateBill checkboxes.
    /// Uses BillingMonth/BillingYear from OperatorsSetup where OperatorName = 'Shahid'.
    /// </summary>
    [HttpPost("from-customers")]
    public async Task<IActionResult> CreateFromCustomerSelection([FromBody] int[] customerUids, CancellationToken cancellationToken)
    {
        try
        {
            if (customerUids == null || customerUids.Length == 0)
            {
                return BadRequest(new { success = false, message = "No customers selected." });
            }

            // Pick BillingMonth, BillingYear and dates from OperatorsSetup for OperatorName = 'Shahid'
            var op = _dbContext.OperatorsSetups.FirstOrDefault(o => o.OperatorName == "Shahid");
            if (op == null)
            {
                return BadRequest(new { success = false, message = "Operator 'Shahid' not found in OperatorsSetup." });
            }

            if (string.IsNullOrEmpty(op.BillingMonth) || string.IsNullOrEmpty(op.BillingYear))
            {
                return BadRequest(new { success = false, message = "Please update OperatorsSetup for 'Shahid' with BillingMonth and BillingYear." });
            }

            string billingMonth = op.BillingMonth;
            string billingYear = op.BillingYear;
            DateOnly? billingDate = op.ReadingDate.HasValue ? DateOnly.FromDateTime(op.ReadingDate.Value) : (DateOnly?)null;
            DateOnly? issueDate = op.IssueDate.HasValue ? DateOnly.FromDateTime(op.IssueDate.Value) : (DateOnly?)null;
            DateOnly? dueDate = op.DueDate.HasValue ? DateOnly.FromDateTime(op.DueDate.Value) : (DateOnly?)null;
            DateOnly? validDate = op.ValidDate.HasValue ? DateOnly.FromDateTime(op.ValidDate.Value) : (DateOnly?)null;

            var customers = _dbContext.CustomersMaintenance
                .Where(c => customerUids.Contains(c.Uid))
                .ToList();

            if (!customers.Any())
            {
                return NotFound(new { success = false, message = "No matching customers found." });
            }

            var updates = new List<object>();

            foreach (var customer in customers)
            {
                string btNoForLookup = customer.BTNoMaintenance ?? customer.BTNo;
                string statusValue = "";
                bool shouldGenerate = false;

                // 1. Calculate Last Month
                var (lastMonth, lastYear) = GetPreviousMonthYear(billingMonth, billingYear);
                
                // 2. Check if Last Month Bill exists
                var lastMonthBillExists = _dbContext.MaintenanceBills.Any(b => 
                    b.Btno == btNoForLookup && 
                    b.BillingMonth == lastMonth && 
                    b.BillingYear == lastYear);

                if (lastMonthBillExists)
                {
                    // Normal flow: Last month exists, generate current month
                    shouldGenerate = true;
                }
                else
                {
                    // 3. Last Month NOT found, check previous 3 months (before the last month)
                    var olderMonths = GetPreviousMonths(lastMonth, lastYear, 3);
                    bool anyOlderBillExists = false;

                    foreach (var m in olderMonths)
                    {
                        if (_dbContext.MaintenanceBills.Any(b => 
                            b.Btno == btNoForLookup && 
                            b.BillingMonth == m.month && 
                            b.BillingYear == m.year))
                        {
                            anyOlderBillExists = true;
                            break;
                        }
                    }

                    if (!anyOlderBillExists)
                    {
                        // Case: ZERO bills found in the last 4 months (Last month + 3 months before it)
                        // Result: Generate current month bill
                        shouldGenerate = true;
                    }
                    else
                    {
                        // Case: Last month missing BUT older bills exist
                        // Result: Skip generation, update status
                        statusValue = "Last Bill Not Exist";
                        customer.BillGenerationStatus = statusValue;
                        shouldGenerate = false;
                    }
                }

                if (shouldGenerate)
                {
                    var dto = new MaintenanceBillCreateDto
                    {
                        CustomerNo = customer.CustomerNo ?? string.Empty,
                        CustomerName = customer.CustomerName ?? string.Empty,
                        BTNo = btNoForLookup,
                        PlotStatus = customer.PlotType,
                        MeterNo = customer.MeterNo,

                        // Tariff matching attributes (required for tariff lookup)
                        Project = customer.Project,
                        PlotType = customer.PlotType,
                        Size = customer.Size,

                        // Billing period and dates
                        BillingMonth = billingMonth,
                        BillingYear = billingYear,
                        BillingDate = billingDate,
                        IssueDate = issueDate,
                        DueDate = dueDate,
                        ValidDate = validDate
                    };

                    await _service.CreateAsync(dto, cancellationToken);

                    // Update BillGenerationStatus in CustomersMaintenance table
                    statusValue = $"{billingMonth}-{billingYear}";
                    customer.BillGenerationStatus = statusValue;
                }

                updates.Add(new { uid = customer.Uid, status = statusValue });
            }

            // Save all customer status updates to the database
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, message = $"Maintenance bills process completed for {billingMonth} {billingYear}.", updates });
        }
        catch (Exception ex)
        {
            // Log the full exception details
            var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return StatusCode(500, new { success = false, message = $"Error generating MBills: {message}", details = ex.StackTrace });
        }
    }

    /// <summary>
    /// Helper to get a list of previous months and years.
    /// </summary>
    private List<(string month, string year)> GetPreviousMonths(string startMonth, string startYear, int count)
    {
        var result = new List<(string month, string year)>();
        var months = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        
        int monthIdx = Array.IndexOf(months, startMonth);
        if (monthIdx == -1 || !int.TryParse(startYear, out int year)) return result;

        for (int i = 1; i <= count; i++)
        {
            int targetIdx = monthIdx - i;
            int targetYear = year;
            
            while (targetIdx < 0)
            {
                targetIdx += 12;
                targetYear -= 1;
            }
            
            result.Add((months[targetIdx], targetYear.ToString()));
        }
        
        return result;
    }

    private (string month, string year) GetPreviousMonthYear(string currentMonth, string currentYear)
    {
        var months = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        int monthIdx = Array.IndexOf(months, currentMonth);

        if (monthIdx == -1) return (currentMonth, currentYear);
        if (!int.TryParse(currentYear, out int year)) return (currentMonth, currentYear);

        int prevMonthIdx = monthIdx == 0 ? 11 : monthIdx - 1;
        int prevYear = monthIdx == 0 ? year - 1 : year;

        return (months[prevMonthIdx], prevYear.ToString());
    }
}

