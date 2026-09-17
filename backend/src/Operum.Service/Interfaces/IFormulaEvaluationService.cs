using Operum.Model.Models;

namespace Operum.Service.Interfaces
{
    public interface IFormulaEvaluationService
    {
        /// <param name="currentFieldValues">Must be the entry's full set of values, not just the changed ones, or formulas fail to resolve the omitted fields.</param>
        /// <param name="timeZone">Defaults to the signed-in user's zone; a background sync with no HTTP context must pass the tracker owner's zone explicitly.</param>
        Task EvaluateAndPersistCalculatedFields(
            string trackerId,
            string entryId,
            List<FieldValue> currentFieldValues,
            List<Field> allFields,
            TimeZoneInfo? timeZone = null);
    }
}
