using Microsoft.AspNetCore.Hosting;

namespace BMSBT.Services;

public interface IBillingLogicReader
{
    Task<string> ReadBillingLogicAsync(string billingCategory, CancellationToken cancellationToken = default);
}

public class BillingLogicReaderService : IBillingLogicReader
{
    private readonly IWebHostEnvironment _environment;

    public BillingLogicReaderService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> ReadBillingLogicAsync(string billingCategory, CancellationToken cancellationToken = default)
    {
        var fileName = billingCategory?.Trim().ToLowerInvariant() switch
        {
            "residential" => "ResidentialBillLogic.md",
            "commercial" => "CommercialBillLogic.md",
            _ => throw new ArgumentException("Unsupported billing category. Use Residential or Commercial.")
        };

        var filePath = Path.Combine(_environment.ContentRootPath, "BillingLogic", fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Billing logic file not found for category '{billingCategory}'.", filePath);
        }

        return await File.ReadAllTextAsync(filePath, cancellationToken);
    }
}
