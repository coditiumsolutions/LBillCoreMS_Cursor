// 1. Create a model to hold operator data
using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

public class OperatorContext
{
    public string? BillingMonth;
    public string? BillingYear;
    public string? OperatorName;

    public DateTime? IssueDate { get; set; }      // <-- Change this
    public DateTime? DueDate { get; set; }        // <-- And this

    public DateOnly? ReadingDate;

    public DateOnly? PaidDate;

    public DateOnly ValidDate;
    // Add other properties you need from OperatorsSetup
    public string? FPAMonth1 { get; set; }

    public string? FPAYEAR1 { get; set; }

    public decimal? FPARate1 { get; set; }


    public string? FPAMonth2 { get; set; }

    public string? FPAYEAR2 { get; set; }

    public decimal? FPARate2 { get; set; }

}

// 2. Create interface for the service
public interface ICurrentOperatorService
{
    Task InitializeAsync(string operatorId);
    OperatorContext GetCurrentOperator();
    void Clear();
}

// 3. Create the service implementation with memory cache
public class CurrentOperatorService : ICurrentOperatorService
{
    private readonly IMemoryCache _cache;
    private readonly BmsbtContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CACHE_KEY_PREFIX = "OperatorData_";

    public CurrentOperatorService(
        IMemoryCache cache,
        BmsbtContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _cache = cache;
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task InitializeAsync(string operatorId)
    {
        var cacheKey = GetCacheKey();

        // Check if data exists in cache
        if (!_cache.TryGetValue(cacheKey, out OperatorContext operatorContext))
        {
            // Get from database
            var operatorData = await _dbContext.OperatorsSetups
                .FirstOrDefaultAsync(o => o.OperatorID == operatorId);

            if (operatorData == null)
            {
                throw new KeyNotFoundException($"Operator with ID {operatorId} not found");
            }

            // Map to context object
            operatorContext = new OperatorContext
            {
                BillingMonth = operatorData.BillingMonth,   
                BillingYear = operatorData.BillingYear, 
                OperatorName = operatorData.OperatorName,
                IssueDate=operatorData.IssueDate,
                DueDate=operatorData.DueDate,
                FPARate1=operatorData.FPARate1,
                FPAMonth1=operatorData.FPAMonth1,
                FPAYEAR1=operatorData.FPAYEAR1,
                FPARate2 = operatorData.FPARate2,
                FPAMonth2 = operatorData.FPAMonth2,
         
            };

            // Cache the data with sliding expiration
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1))
                .SetAbsoluteExpiration(TimeSpan.FromHours(8));

            _cache.Set(cacheKey, operatorContext, cacheOptions);
        }
    }

    public OperatorContext GetCurrentOperator()
    {
        var cacheKey = GetCacheKey();

        if (_cache.TryGetValue(cacheKey, out OperatorContext operatorContext))
        {
            return operatorContext;
        }

        throw new InvalidOperationException("Operator data not initialized. Please login first.");
    }

    public void Clear()
    {
        var cacheKey = GetCacheKey();
        _cache.Remove(cacheKey);
    }

    private string GetCacheKey()
    {
        // Handle cases where HttpContext might not be available
        if (_httpContextAccessor.HttpContext == null)
        {
            // For background tasks or startup, use a default key
            return $"{CACHE_KEY_PREFIX}background";
        }

        var userId = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            // For unauthenticated requests
            return $"{CACHE_KEY_PREFIX}anonymous";
        }

        return $"{CACHE_KEY_PREFIX}{userId}";
    }
}

