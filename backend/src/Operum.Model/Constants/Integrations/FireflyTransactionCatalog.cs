using Operum.Model.Constants.Fields;
using Operum.Model.Integrations;

namespace Operum.Model.Constants.Integrations
{
    /// <summary>
    /// One entry is written per transaction split, keyed on its journal id. Keying on the
    /// group id instead would collapse a split transaction into one entry and lose money.
    /// </summary>
    public static class FireflyTransactionCatalog
    {
        public const string ResourceType = "transactions";

        public const string JournalIdKey = "journal_id";

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

        // Transfers are left positive: between the user's own accounts, so neither income nor expense.
        public static double ApplySign(double amount, string? type) =>
            string.Equals(type, "withdrawal", StringComparison.OrdinalIgnoreCase)
                ? -Math.Abs(amount)
                : Math.Abs(amount);
    }
}
