using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Queries.Requests
{
    public class CreateQuerySortDto
    {
        public required string FieldId { get; set; } = string.Empty;
        public bool Descending { get; set; }
    }

    public class CreateQuerySortDtoValidator : AbstractValidator<CreateQuerySortDto>
    {
        public CreateQuerySortDtoValidator()
        {
            RuleFor(x => x.FieldId)
                .NotEmpty().WithMessage((x) => Messages.Required("field id"));
        }
    }
}
