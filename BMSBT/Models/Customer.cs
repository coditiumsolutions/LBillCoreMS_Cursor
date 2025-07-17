using System;
using System.Collections.Generic;

namespace BMSBT.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? ZipCode { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? MeterNumber { get; set; }

    public DateOnly? ConnectionDate { get; set; }

    public string? AccountStatus { get; set; }

    public DateOnly? LastPaymentDate { get; set; }

    public decimal? OutstandingBalance { get; set; }

    public string? TariffName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
