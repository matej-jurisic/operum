using Operum.Model.Models;

namespace Operum.Service.Interfaces
{
    public interface IFormulaEvaluationService
    {
        /// <param name="currentFieldValues">
        /// Every one of the entry's field values, not just the ones that changed -- formulas
        /// resolve their tokens out of this list, so a partial one silently fails to resolve
        /// the fields it omits.
        /// </param>
        /// <param name="timeZone">
        /// The zone a constant's date-based conditional filters are evaluated in. Defaults to
        /// the signed-in user's when omitted, which is right for a request but wrong for a
        /// caller with no HTTP context -- a background sync must pass the tracker owner's
        /// zone explicitly, or every such filter quietly resolves in UTC instead.
        /// </param>
        Task EvaluateAndPersistCalculatedFields(
            string trackerId,
            string entryId,
            List<FieldValue> currentFieldValues,
            List<Field> allFields,
            TimeZoneInfo? timeZone = null);
    }
}
