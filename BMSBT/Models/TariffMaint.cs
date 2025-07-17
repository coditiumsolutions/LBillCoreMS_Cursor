using System;
using System.Collections.Generic;

namespace BMSBT.Models;

public partial class TariffMaint
{
    public int Uid { get; set; }

    public string? TariffPrefix { get; set; }

    public string? TariffType { get; set; }

    public string? TariffSize { get; set; }

    public int? Amount { get; set; }

    public int? Tax { get; set; }
}
