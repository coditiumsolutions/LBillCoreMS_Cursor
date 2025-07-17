using System;
using System.Collections.Generic;

namespace BMSBT.Models;

public partial class Reading
{
    public int Uid { get; set; }

    public int ReadingId { get; set; }

    public string CustomerNo { get; set; } = null!;

    public string MeterType { get; set; } = null!;

    public int? Previous1 { get; set; }

    public int? Present1 { get; set; }

    public int? Difference1 { get; set; }

    public int? Previous2 { get; set; }

    public int? Present2 { get; set; }

    public int? Difference2 { get; set; }

    public int? Previous3 { get; set; }

    public int? Present3 { get; set; }

    public int? Difference3 { get; set; }
}
