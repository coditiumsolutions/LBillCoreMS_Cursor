using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Security.Claims;

namespace BMSBT.Roles
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public CustomAuthorizeAttribute(string roles)
        {
            _roles = roles.Split(','); // Convert "Admin,Manager" to an array
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new RedirectToRouteResult(new { controller = "Account", action = "Login" });
                return;
            }

            // Get user roles from claims
            var userRoles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).FirstOrDefault();

            // Check if any of the user's roles match the required roles
            if (userRoles == null || !_roles.Any(role => userRoles.Split(',').Contains(role.Trim())))
            {
                context.Result = new RedirectToRouteResult(new { controller = "Login", action = "AccessDenied" });
            }
        }
    }
}
