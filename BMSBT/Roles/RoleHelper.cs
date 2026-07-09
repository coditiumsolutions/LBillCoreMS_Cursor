using System.Security.Claims;

namespace BMSBT.Roles
{
    public static class RoleHelper
    {
        public static readonly string[] FullAccessRoles = { "Admin", "Manager", "E-Bill", "M-Bill", "COO" };
        public static readonly string[] AuditModuleRoles = { "Admin", "Audit", "COO" };

        public static IReadOnlyList<string> ParseRoles(string? roleString)
        {
            if (string.IsNullOrWhiteSpace(roleString))
            {
                return Array.Empty<string>();
            }

            return roleString
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        public static IReadOnlyList<string> GetRolesFromClaims(ClaimsPrincipal user)
        {
            var roleClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return ParseRoles(roleClaim);
        }

        public static bool HasAnyRole(IEnumerable<string> userRoles, params string[] requiredRoles)
        {
            var roles = userRoles.ToList();
            return requiredRoles.Any(required =>
                roles.Contains(required, StringComparer.OrdinalIgnoreCase));
        }

        public static bool CanAccessAuditModule(IEnumerable<string> userRoles)
            => HasAnyRole(userRoles, AuditModuleRoles);

        public static bool IsAuditOnlyUser(IEnumerable<string> userRoles)
        {
            var roles = userRoles.ToList();
            return roles.Any(r => r.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                && !HasAnyRole(roles, FullAccessRoles);
        }
    }
}
