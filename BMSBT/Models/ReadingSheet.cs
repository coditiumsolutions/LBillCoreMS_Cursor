using System;
using System.Collections.Generic;

namespace BMSBT.Models;

public partial class ReadingSheet
{
    public int Uid { get; set; }

    public string Btno { get; set; } = null!;

    public string Year { get; set; } = null!;

    public string Month { get; set; } = null!;

    public string? CustomerNo { get; set; }

    public string? TarrifName { get; set; }

    public string? MeterType { get; set; }

    public int? Previous1 { get; set; }

    public int? Present1 { get; set; }

    public int? Difference1 { get; set; }

    public int? Previous2 { get; set; }

    public int? Present2 { get; set; }

    public int? Difference2 { get; set; }

    public int? Previous3 { get; set; }

    public int? Present3 { get; set; }

    public int? Difference3 { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? History { get; set; }
}
