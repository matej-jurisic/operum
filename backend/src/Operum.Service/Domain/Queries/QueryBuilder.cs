using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Queries;
using Operum.Model.Enums;
using Operum.Service.Domain.Views;

namespace Operum.Service.Domain.Queries
{
    // Validates a field-agnostic clause (one filter or one sort over a data type). Used by
    // ViewsService and the DashboardView editor; the concrete-field checks that used to live
    // here have moved to whoever binds the clause to a field.
    public static class QueryBuilder
    {
        public static Result ValidateClause(string kind, string dataType, string? op, string? value, bool descending)
        {
            if (!QueryKinds.IsValid(kind))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("kind"));

            if (!DataTypes.IsValid(dataType))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"data type '{dataType}'"));

            // A sort is just a data type and a direction, so there is nothing else to check.
            if (kind != QueryKinds.Filter)
                return Result.Success();

            if (op == null || !ViewFilterValidator.IsValidOperatorForFieldType(op, dataType))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"operator '{op}' for data type '{dataType}'"));

            if (value != null && !ViewFilterValidator.IsValidFieldValue(value, dataType))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"value '{value}' for data type '{dataType}'"));

            return Result.Success();
        }

        public static Result ValidateClause(ClauseDto clause) =>
            ValidateClause(clause.Kind, clause.DataType, clause.Operator, clause.Value, clause.Descending);
    }
}
