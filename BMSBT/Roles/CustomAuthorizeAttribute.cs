using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BMSBT.Roles
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public CustomAuthorizeAttribute(string roles)
        {
            _roles = roles
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new RedirectToRouteResult(new { controller = "Login", action = "Index" });
                return;
            }

            var userRoles = RoleHelper.GetRolesFromClaims(user);

            if (!RoleHelper.HasAnyRole(userRoles, _roles))
            {
                context.Result = new RedirectToRouteResult(new { controller = "Login", action = "AccessDenied" });
            }
        }
    }
}
