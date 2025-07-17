using System;
using System.Collections.Generic;

namespace BMSBT.Models;

public partial class MeterType
{
    public int Uid { get; set; }

    public int MeterId { get; set; }

    public string MeterType1 { get; set; } = null!;

    public string MeterNo { get; set; } = null!;

    public string Btno { get; set; } = null!;

    public decimal? MultiplyFactor { get; set; }
}
