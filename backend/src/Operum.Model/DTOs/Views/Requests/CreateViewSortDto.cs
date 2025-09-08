using FluentValidation;

namespace Operum.Model.DTOs.Views.Requests
{
    public class CreateViewSortDto
    {
        public string FieldId { get; set; } = string.Empty;
        public bool Descending { get; set; }
    }

    public class CreateViewSortDtoValidator : AbstractValidator<CreateViewSortDto>
    {
        public CreateViewSortDtoValidator()
        {
            RuleFor(x => x.FieldId)
                .NotEmpty().WithMessage("FieldId is required.")
                .MaximumLength(100).WithMessage("FieldId cannot exceed 100 characters.");
        }
    }
}
