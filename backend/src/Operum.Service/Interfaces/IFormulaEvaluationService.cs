using Operum.Model.Models;

namespace Operum.Service.Interfaces
{
    public interface IFormulaEvaluationService
    {
        Task EvaluateAndPersistCalculatedFields(
            string trackerId,
            string entryId,
            List<FieldValue> currentFieldValues,
            List<Field> allFields);
    }
}
