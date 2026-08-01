using BMSBT.Models;
using Microsoft.EntityFrameworkCore;

namespace BMSBT.Services
{
    /// <summary>
    /// Resolves OperatorsSetup for the logged-in user.
    /// Prefers OperatorName = username so billing month matches Operator Setup UI,
    /// then falls back to OperatorID (session / EmployeeId).
    /// </summary>
    public static class OperatorSetupResolver
    {
        public static OperatorsSetup? Resolve(BmsbtContext db, string? userName, string? operatorId)
        {
            if (!string.IsNullOrWhiteSpace(userName))
            {
                var name = userName.Trim();
                var byName = db.OperatorsSetups
                    .AsEnumerable()
                    .FirstOrDefault(o => string.Equals(o.OperatorName?.Trim(), name, StringComparison.OrdinalIgnoreCase));
                if (byName != null)
                {
                    return byName;
                }
            }

            if (!string.IsNullOrWhiteSpace(operatorId))
            {
                var oid = operatorId.Trim();
                return db.OperatorsSetups
                    .AsEnumerable()
                    .FirstOrDefault(o => string.Equals(o.OperatorID?.Trim(), oid, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }
    }
}
