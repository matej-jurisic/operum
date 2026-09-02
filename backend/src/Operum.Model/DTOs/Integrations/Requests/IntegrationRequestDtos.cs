using FluentValidation;

namespace Operum.Model.DTOs.Integrations.Requests
{
    public class ConnectIntegrationDto
    {
        public required string Provider { get; set; }

        /// <summary>
        /// API key or token. Verified with the provider before anything is stored, so a bad
        /// one is refused at the point it was typed rather than at the first sync.
        /// </summary>
        public string? Credential { get; set; }

        /// <summary>The user's own instance, for self-hosted providers.</summary>
        public string? BaseUrl { get; set; }
    }

    public class ConnectIntegrationDtoValidator : AbstractValidator<ConnectIntegrationDto>
    {
        public ConnectIntegrationDtoValidator()
        {
            RuleFor(x => x.Provider).NotEmpty();
            RuleFor(x => x.BaseUrl).MaximumLength(500);
            RuleFor(x => x.Credential).MaximumLength(500);
        }
    }

    public class SaveIntegrationTargetDto
    {
        public required string TrackerId { get; set; }
        public required string ResourceType { get; set; }

        /// <summary>"Pull" or "Push"; must be something the provider supports.</summary>
        public string? Mode { get; set; }

        public bool IsEnabled { get; set; } = true;
        public DateOnly? BackfillFrom { get; set; }
        public List<FieldMappingDto> Mappings { get; set; } = [];
    }

    public class SaveIntegrationTargetDtoValidator : AbstractValidator<SaveIntegrationTargetDto>
    {
        public SaveIntegrationTargetDtoValidator()
        {
            RuleFor(x => x.TrackerId).NotEmpty();
            RuleFor(x => x.ResourceType).NotEmpty();

            // A target with no mappings would sync nothing; the field cap bounds the top end,
            // since a mapping has to name a field that exists.
            RuleFor(x => x.Mappings).NotEmpty();

            RuleForEach(x => x.Mappings).ChildRules(mapping =>
            {
                mapping.RuleFor(m => m.SourceKey).NotEmpty();
                mapping.RuleFor(m => m.FieldId).NotEmpty();
            });
        }
    }
}
