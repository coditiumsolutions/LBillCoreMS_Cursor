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
    public string? OperatorId;

    public DateTime? IssueDate { get; set; }
    public DateTime? DueDate { get; set; }

    public DateOnly? ReadingDate;
    public DateOnly? PaidDate;
    public DateOnly? ValidDate;

    public string? FPAMonth1 { get; set; }
    public string? FPAYEAR1 { get; set; }
    public decimal? FPARate1 { get; set; }

    public string? FPAMonth2 { get; set; }
    public string? FPAYEAR2 { get; set; }
    public decimal? FPARate2 { get; set; }
}

public interface ICurrentOperatorService
{
    Task InitializeAsync(string operatorId);
    OperatorContext GetCurrentOperator();
    void Clear();
}

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
        if (string.IsNullOrWhiteSpace(operatorId))
        {
            throw new ArgumentException("Operator ID is required.", nameof(operatorId));
        }

        // Key by OperatorId so users don't share stale billing month/year
        var cacheKey = GetCacheKey(operatorId);

        // Always reload from DB so Operator Setup changes apply immediately
        var oid = operatorId.Trim();
        var operatorData = (await _dbContext.OperatorsSetups
            .AsNoTracking()
            .ToListAsync())
            .FirstOrDefault(o => string.Equals(o.OperatorID?.Trim(), oid, StringComparison.OrdinalIgnoreCase));

        if (operatorData == null)
        {
            throw new KeyNotFoundException($"Operator with ID {operatorId} not found");
        }

        var operatorContext = new OperatorContext
        {
            OperatorId = operatorData.OperatorID,
            BillingMonth = operatorData.BillingMonth?.Trim(),
            BillingYear = operatorData.BillingYear?.Trim(),
            OperatorName = operatorData.OperatorName,
            IssueDate = operatorData.IssueDate,
            DueDate = operatorData.DueDate,
            ValidDate = operatorData.ValidDate.HasValue
                ? DateOnly.FromDateTime(operatorData.ValidDate.Value)
                : null,
            FPARate1 = operatorData.FPARate1,
            FPAMonth1 = operatorData.FPAMonth1,
            FPAYEAR1 = operatorData.FPAYEAR1,
            FPARate2 = operatorData.FPARate2,
            FPAMonth2 = operatorData.FPAMonth2,
            FPAYEAR2 = operatorData.FPAYEAR2
        };

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(15))
            .SetAbsoluteExpiration(TimeSpan.FromHours(2));

        _cache.Set(cacheKey, operatorContext, cacheOptions);

        // Also store under request-scoped key for GetCurrentOperator
        var requestKey = GetRequestCacheKey();
        _cache.Set(requestKey, operatorContext, cacheOptions);
    }

    public OperatorContext GetCurrentOperator()
    {
        var requestKey = GetRequestCacheKey();
        if (_cache.TryGetValue(requestKey, out OperatorContext? operatorContext) && operatorContext != null)
        {
            return operatorContext;
        }

        throw new InvalidOperationException("Operator data not initialized. Please login first.");
    }

    public void Clear()
    {
        var requestKey = GetRequestCacheKey();
        if (_cache.TryGetValue(requestKey, out OperatorContext? ctx) && ctx?.OperatorId != null)
        {
            _cache.Remove(GetCacheKey(ctx.OperatorId));
        }
        _cache.Remove(requestKey);
    }

    private string GetCacheKey(string operatorId) => $"{CACHE_KEY_PREFIX}{operatorId.Trim()}";

    private string GetRequestCacheKey()
    {
        if (_httpContextAccessor.HttpContext == null)
        {
            return $"{CACHE_KEY_PREFIX}background_current";
        }

        // Prefer session OperatorId so each login gets the correct setup
        var operatorId = _httpContextAccessor.HttpContext.Session.GetString("OperatorId");
        if (!string.IsNullOrWhiteSpace(operatorId))
        {
            return $"{CACHE_KEY_PREFIX}current_{operatorId.Trim()}";
        }

        var userName = _httpContextAccessor.HttpContext.Session.GetString("UserName");
        if (!string.IsNullOrWhiteSpace(userName))
        {
            return $"{CACHE_KEY_PREFIX}current_user_{userName.Trim()}";
        }

        return $"{CACHE_KEY_PREFIX}anonymous_current";
    }
}
