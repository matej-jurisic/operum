using Operum.Model.Common;
using Operum.Model.Integrations;

namespace Operum.Service.Domain.Integrations
{
    // Shared by both ingest paths (pull tick and webhook delivery).
    public static class SourceRecordProjector
    {
        public static EntryWriteRecord Project(SourceRecord record, IReadOnlyList<FieldMapping> mappings)
        {
            if (record.Operation == SourceOperation.Delete)
            {
                return new EntryWriteRecord(
                    record.ExternalId,
                    EntryWriteOperation.Delete,
                    new Dictionary<string, string?>(),
                    record.GroupId);
            }

            var values = new Dictionary<string, string?>();

            foreach (var mapping in mappings)
            {
                // Key absent means the provider said nothing; distinct from an explicit null value.
                if (!record.ValuesBySourceKey.TryGetValue(mapping.SourceKey, out var value))
                    continue;

                // Omitting the key tells the writer to leave the field as-is; including it with null clears it.
                if (value == null && mapping.SkipWhenNull)
                    continue;

                values[mapping.FieldId] = value;
            }

            return new EntryWriteRecord(record.ExternalId, EntryWriteOperation.Upsert, values, record.GroupId);
        }

        public static List<EntryWriteRecord> Project(
            IEnumerable<SourceRecord> records,
            IReadOnlyList<FieldMapping> mappings) =>
            [.. records.Select(record => Project(record, mappings))];
    }
}
