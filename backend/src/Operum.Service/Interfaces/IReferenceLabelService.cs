using Operum.Model.Models;

namespace Operum.Service.Interfaces
{
    // Keeps the cached display label on reference field values in sync with the entry each one
    // links to. The label lives in FieldValue.StringValue so filters/sorts/analytics can treat
    // a reference like a plain string.
    public interface IReferenceLabelService
    {
        // Clears the link if the target no longer exists or sits in the wrong tracker.
        Task ResolveEntryReferences(string entryId, List<FieldValue> currentFieldValues, List<Field> allFields);

        // Propagates a rename of the target entry to every reference that points at it.
        Task RefreshReferencesToEntry(string changedEntryId);

        Task RefreshFieldReferences(string fieldId);
    }
}
