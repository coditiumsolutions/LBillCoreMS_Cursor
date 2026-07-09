using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BMSBT.Roles
{
    public class RestrictAuditOnlyUsersFilter : IActionFilter
    {
        private static readonly HashSet<string> AllowedControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Login",
            "Audit"
        };

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.RouteData.Values["controller"]?.ToString();
            if (string.IsNullOrEmpty(controller) || AllowedControllers.Contains(controller))
            {
                return;
            }

            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var roles = RoleHelper.GetRolesFromClaims(user);
            if (RoleHelper.IsAuditOnlyUser(roles))
            {
                context.Result = new RedirectToActionResult("Index", "Audit", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
