using BMSBT.Models;

namespace BMSBT.Services
{
    public static class UserAuditHelper
    {
        public const string TableName = "Users";
        public const string ModuleName = "User Management";

        public static Dictionary<string, object?> CreateSnapshot(User user, bool passwordChanged = false)
        {
            var snapshot = new Dictionary<string, object?>
            {
                ["EmployeeId"] = user.EmployeeId,
                ["Username"] = user.Username,
                ["Role"] = user.Role
            };

            if (passwordChanged)
            {
                snapshot["Password"] = "Changed";
            }

            return snapshot;
        }

        public static (Dictionary<string, object?> oldData, Dictionary<string, object?> newData) BuildDiff(
            IReadOnlyDictionary<string, object?> oldValues,
            IReadOnlyDictionary<string, object?> newValues)
            => AuditDiffHelper.BuildDiff(oldValues, newValues);
    }
}
