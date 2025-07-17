using System;
using System.Collections.Generic;

namespace BMSBT.Models;

public partial class User
{
    public int Uid { get; set; }

    public string? EmployeeId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Role { get; set; }
}
