namespace BMSBT.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(string tableName, string operation, string recordId, object? oldData, object? newData, string moduleName);
    }
}
