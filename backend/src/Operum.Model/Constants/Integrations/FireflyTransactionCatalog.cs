using Operum.Model.Constants.Fields;
using Operum.Model.Integrations;

namespace Operum.Model.Constants.Integrations
{
    /// <summary>
    /// The values a Firefly III transaction split can supply.
    /// <para>
    /// A Firefly transaction is a <em>group</em> holding one or more splits, each with its own
    /// <c>transaction_journal_id</c>. One entry is written per split, keyed on that journal id
    /// -- keying on the group instead would collapse a split transaction into a single entry
    /// and lose money.
    /// </para>
    /// </summary>
    public static class FireflyTransactionCatalog
    {
        public const string ResourceType = "transactions";

        /// <summary>The split's own id, and the entry's ExternalId.</summary>
        public const string JournalIdKey = "journal_id";

        /// <summary>The group the split belongs to, and the entry's ExternalGroupId.</summary>
        public const string GroupIdKey = "group_id";

        public const string AmountKey = "amount";
        public const string TypeKey = "type";

        public static readonly IReadOnlyList<SourceField> Fields =
        [
            new(JournalIdKey, DataTypes.String, "Transaction ID",
                "Firefly's id for this split. Useful for linking back."),
            new(GroupIdKey, DataTypes.String, "Group ID",
                "The transaction this split belongs to. Split transactions share one."),

            new("date", DataTypes.DateTime, "Date"),
            new(AmountKey, DataTypes.Number, "Amount",
                "Signed: withdrawals are negative, deposits positive, so a sum reads as a net."),
            new("currency_code", DataTypes.String, "Currency",
                "Operum has no money type, so the amount is a plain number and this is its unit."),

            new(TypeKey, DataTypes.String, "Type", "withdrawal, deposit or transfer."),
            new("description", DataTypes.String, "Description"),
            new("category_name", DataTypes.String, "Category"),
            new("budget_name", DataTypes.String, "Budget"),
            new("source_name", DataTypes.String, "Source account"),
            new("destination_name", DataTypes.String, "Destination account"),
            new("notes", DataTypes.String, "Notes"),
            new("tags", DataTypes.String, "Tags", "Joined with commas; a list has no field type of its own."),

            new("foreign_amount", DataTypes.Number, "Foreign amount"),
            new("foreign_currency_code", DataTypes.String, "Foreign currency"),

            new("reconciled", DataTypes.Bool, "Reconciled"),
        ];

        /// <summary>
        /// Signs an amount from Firefly's positive-with-a-type convention. A mixed column only
        /// sums to something meaningful if the sign is real, and Sum is the obvious analytic
        /// over a ledger.
        /// <para>
        /// A transfer is left positive: it moves money between the user's own accounts, so it
        /// is neither income nor expense and signing it either way would distort a total.
        /// </para>
        /// </summary>
        public static double ApplySign(double amount, string? type) =>
            string.Equals(type, "withdrawal", StringComparison.OrdinalIgnoreCase)
                ? -Math.Abs(amount)
                : Math.Abs(amount);
    }
}
