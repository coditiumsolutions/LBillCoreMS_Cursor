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

            foreach (var customer in customers)
            {
                var dto = new MaintenanceBillCreateDto
                {
                    CustomerNo = customer.CustomerNo ?? string.Empty,
                    CustomerName = customer.CustomerName ?? string.Empty,
                    BTNo = customer.BTNoMaintenance ?? customer.BTNo,
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
            }

            return Ok(new { success = true, message = $"Maintenance bills inserted successfully for {billingMonth} {billingYear}." });
        }
        catch (Exception ex)
        {
            // Log the full exception details
            var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return StatusCode(500, new { success = false, message = $"Error generating MBills: {message}", details = ex.StackTrace });
        }
    }
}

