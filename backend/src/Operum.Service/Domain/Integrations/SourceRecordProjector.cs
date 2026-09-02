using Operum.Model.Common;
using Operum.Model.Integrations;

namespace Operum.Service.Domain.Integrations
{
    /// <summary>
    /// The middle stage of the ingest pipeline: turns what a provider said into what the
    /// write path takes, by applying one target's mappings. Pure, and shared by both ingest
    /// paths -- a pull tick and a webhook delivery differ only in how they got their records.
    /// </summary>
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
                // The provider said nothing at all about this key, so neither do we. Distinct
                // from it saying "no value" -- that arrives as a present null below.
                if (!record.ValuesBySourceKey.TryGetValue(mapping.SourceKey, out var value))
                    continue;

                // SkipWhenNull is resolved here and nowhere else. Omitting the key is how the
                // writer is told to leave the field as it found it; including it with null is
                // how it is told to clear it.
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
