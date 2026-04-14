using System.Text.Json;
using BMSBT.Models;

namespace BMSBT.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly BmsbtContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(BmsbtContext context, IHttpContextAccessor httpContextAccessor, ILogger<AuditLogService> logger)
        {
            _context = context;
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

                if (httpContext?.Items != null)
                {
                    httpContext.Items["SkipEfAudit"] = true;
                }

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit log write failed for {TableName} {Operation} {RecordId}", tableName, operation, recordId);
            }
            finally
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.Items != null)
                {
                    httpContext.Items.Remove("SkipEfAudit");
                }
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
