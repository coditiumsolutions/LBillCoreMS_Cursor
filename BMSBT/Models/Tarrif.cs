using System;
using System.Collections.Generic;

namespace BMSBT.Models;

public partial class Tarrif
{
    public int Uid { get; set; }

    public string TarrifName { get; set; } = null!;

    public string TarrifType { get; set; } = null!;

    public string MeterType { get; set; } = null!;

    public string Slab { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Sequence { get; set; }

    public string Range { get; set; } = null!;

    public decimal? Rate1 { get; set; }

    public double Rate2 { get; set; }

    public double Nm { get; set; }

    public double? MinCharges { get; set; }

    public string Details { get; set; } = null!;
}
