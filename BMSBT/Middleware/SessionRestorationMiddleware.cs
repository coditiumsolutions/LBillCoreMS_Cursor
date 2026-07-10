using BMSBT.Services;
using Microsoft.Data.SqlClient;

namespace BMSBT.Middleware
{
    public class SessionRestorationMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionRestorationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthSessionService authSessionService)
        {
            await authSessionService.RestoreSessionFromClaimsAsync(context);
            await _next(context);
        }
    }

    public static class PersistentAuthExtensions
    {
        public const string SessionCacheTableName = "SessionCache";

        public static async Task EnsureSessionCacheTableAsync(string connectionString)
        {
            const string sql = """
                IF OBJECT_ID(N'[dbo].[SessionCache]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[SessionCache](
                        [Id] nvarchar(449) NOT NULL,
                        [Value] varbinary(MAX) NOT NULL,
                        [ExpiresAtTime] datetimeoffset NOT NULL,
                        [SlidingExpirationInSeconds] bigint NULL,
                        [AbsoluteExpiration] datetimeoffset NULL,
                        CONSTRAINT [PK_SessionCache] PRIMARY KEY CLUSTERED ([Id] ASC)
                    );
                    CREATE NONCLUSTERED INDEX [Index_ExpiresAtTime] ON [dbo].[SessionCache]([ExpiresAtTime]);
                END
                """;

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
