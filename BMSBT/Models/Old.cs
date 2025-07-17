using System;
using System.Collections.Generic;

namespace BMSBT.Models;

public partial class Old
{
    public int Uid { get; set; }

    public int TariffId { get; set; }

    public string TariffName { get; set; } = null!;

    public string? Frequency { get; set; }

    public string? TariffType { get; set; }

    public string? RateType { get; set; }

    public int? StartRange { get; set; }

    public int? EndRange { get; set; }

    public decimal? Rate { get; set; }

    public decimal? Phrate { get; set; }

    public decimal? Ophrate { get; set; }

    public int? MinCharges { get; set; }
}
