namespace BMSBT.Services
{
    public static class AuditDiffHelper
    {
        public static (Dictionary<string, object?> oldData, Dictionary<string, object?> newData) BuildDiff(
            IReadOnlyDictionary<string, object?> oldValues,
            IReadOnlyDictionary<string, object?> newValues)
        {
            var oldData = new Dictionary<string, object?>();
            var newData = new Dictionary<string, object?>();

            foreach (var key in oldValues.Keys.Union(newValues.Keys))
            {
                oldValues.TryGetValue(key, out var oldValue);
                newValues.TryGetValue(key, out var newValue);

                if (Equals(oldValue, newValue))
                {
                    continue;
                }

                oldData[key] = oldValue;
                newData[key] = newValue;
            }

            return (oldData, newData);
        }
    }
}
