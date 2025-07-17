using System;
using System.Collections.Generic;

namespace BMSBT.Models;

public partial class Configuration
{
    public int Uid { get; set; }

    public int ConfigId { get; set; }

    public string? ConfigKey { get; set; }

    public string? ConfigValue { get; set; }
}
