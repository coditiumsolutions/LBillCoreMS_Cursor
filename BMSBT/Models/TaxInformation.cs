using System;
using System.Collections.Generic;

namespace BMSBT.Models;

public partial class TaxInformation
{
    public int TaxId { get; set; }

    public string? TaxName { get; set; }

    public decimal? TaxRate { get; set; }

    public string? ApplicableFor { get; set; }

    public string? IsActive { get; set; }

    public string? Range { get; set; }
}
