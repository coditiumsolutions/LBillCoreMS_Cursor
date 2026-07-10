using System.Text.Json;
using BMSBT.Models;
using Microsoft.EntityFrameworkCore;

namespace BMSBT.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            IServiceScopeFactory scopeFactory,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditLogService> logger)
        {
            _scopeFactory = scopeFactory;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogAsync(string tableName, string operation, string recordId, object? oldData, object? newData, string moduleName)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var changedBy = httpContext?.User?.Identity?.Name
                                ?? httpContext?.Session.GetString("UserName")
                                ?? "System";
                var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();

                var auditLog = new AuditLog
                {
                    TableName = tableName,
                    Operation = operation,
                    RecordId = recordId,
                    OldData = ToJson(oldData),
                    NewData = ToJson(newData),
                    ModuleName = moduleName,
                    ChangedBy = changedBy,
                    ChangedAt = DateTime.Now,
                    IPAddress = ipAddress
                };

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BmsbtContext>();
                context.AuditLogs.Add(auditLog);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit log write failed for {TableName} {Operation} {RecordId}", tableName, operation, recordId);
            }
        }

        private static string? ToJson(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is string text)
            {
                return text;
            }

            return JsonSerializer.Serialize(value);
        }
    }
}
