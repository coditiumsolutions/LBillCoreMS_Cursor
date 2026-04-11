using System.Text.Json.Serialization;

namespace BMSBT.DTO;

/// <summary>
/// Input DTO for creating a MaintenanceBill record without touching existing MaintenanceNew UI.
/// </summary>
public class MaintenanceBillCreateDto
{
    // Core customer identity
    public string CustomerNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    // Location / plot info
    public string? BTNo { get; set; }

    /// <summary>
    /// Optional extra BT numbers (e.g. primary vs maintenance id) used only to match rows in AdditionalCharges.
    /// </summary>
    public List<string>? BtNosForAdditionalChargeLookup { get; set; }

    public string? PlotStatus { get; set; }

    // Meter info (optional)
    public string? MeterNo { get; set; }

    // Tariff matching attributes (required for tariff lookup)
    public string? Project { get; set; }
    public string? Category { get; set; }

    /// <summary>Legacy JSON field name; matches MaintenanceTarrif.Category in the database.</summary>
    [JsonPropertyName("plotType")]
    public string? PlotType { get; set; }
    public string? Size { get; set; }

    /// <summary>When Block contains "apartment", tariff uses Category Apartment and ignores Size.</summary>
    public string? Block { get; set; }

    // Billing period
    public string? BillingMonth { get; set; }
    public string? BillingYear { get; set; }

    // Billing-related dates (optional, can be populated from OperatorsSetup)
    public DateOnly? BillingDate { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? ValidDate { get; set; }
}

