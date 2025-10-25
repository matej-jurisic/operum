using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Views.Requests
{
    public class CreateViewSortDto
    {
        public required string FieldId { get; set; } = string.Empty;
        public bool Descending { get; set; }
    }

    public class CreateViewSortDtoValidator : AbstractValidator<CreateViewSortDto>
    {
        public CreateViewSortDtoValidator()
        {
            RuleFor(x => x.FieldId)
                .NotEmpty().WithMessage((x) => Messages.Required("field id"));
        }
    }
}
