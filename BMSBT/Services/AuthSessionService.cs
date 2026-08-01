using System.Security.Claims;
using System.Text.Json;
using BMSBT.BillServices;
using BMSBT.Models;
using Microsoft.EntityFrameworkCore;

namespace BMSBT.Services
{
    public interface IAuthSessionService
    {
        Task PopulateSessionAsync(HttpContext httpContext, User user);
        Task RestoreSessionFromClaimsAsync(HttpContext httpContext);
    }

    public class AuthSessionService : IAuthSessionService
    {
        private readonly BmsbtContext _context;

        public AuthSessionService(BmsbtContext context)
        {
            _context = context;
        }

        public async Task PopulateSessionAsync(HttpContext httpContext, User user)
        {
            httpContext.Session.SetString("UserName", user.Username);
            httpContext.Session.SetString("Role", user.Role ?? "");
            httpContext.Session.SetString("LoginTime", DateTime.Now.ToString("hh:mm tt"));

            // Prefer OperatorsSetup.OperatorName = login name (not Users.EmployeeId).
            // shahid had EmployeeId=1002 (ASAD/May) while Operator Setup "Shahid" is July.
            var operatorSetup = OperatorSetupResolver.Resolve(_context, user.Username, user.EmployeeId);

            if (operatorSetup != null)
            {
                var operatorSetupDetail = new Dictionary<string, string>
                {
                    { "OperatorId", operatorSetup.OperatorID ?? "" },
                    { "OperatorName", operatorSetup.OperatorName ?? "" },
                    { "BillingMonth", operatorSetup.BillingMonth ?? "" },
                    { "BillingYear", operatorSetup.BillingYear ?? "" }
                };

                httpContext.Session.SetString("OperatorSetupDetail", JsonSerializer.Serialize(operatorSetupDetail));
                httpContext.Session.SetString("OperatorId", operatorSetup.OperatorID ?? "");

                BillCreationState.CurrentMonth = operatorSetup.BillingMonth ?? "";
                BillCreationState.CurrentYear = operatorSetup.BillingYear ?? "";
            }
            else
            {
                httpContext.Session.SetString("OperatorId", user.EmployeeId ?? "");
            }

            await Task.CompletedTask;
        }

        public async Task RestoreSessionFromClaimsAsync(HttpContext httpContext)
        {
            if (httpContext.User?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            if (!string.IsNullOrEmpty(httpContext.Session.GetString("UserName")))
            {
                return;
            }

            var username = httpContext.User.Identity?.Name
                ?? httpContext.User.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user != null)
            {
                await PopulateSessionAsync(httpContext, user);
            }
        }
    }
}
