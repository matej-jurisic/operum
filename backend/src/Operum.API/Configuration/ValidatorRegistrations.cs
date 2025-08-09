using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Operum.Model.Common;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.DTOs.Entry.Requests;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Trackers.Requests;

namespace Operum.API.Configuration
{
    public static class ValidatorRegistrations
    {
        public static void RegisterValidations(this IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(ms => ms.Value != null && ms.Value.Errors.Count > 0)
                        .Select(ms => new
                        {
                            Field = ms.Key,
                            Messages = ms.Value!.Errors.Select(e => e.ErrorMessage)
                        });

                    var result = new ApiResponse
                    {
                        StatusCode = Model.Enums.StatusCodeEnum.BadRequest,
                        Messages = errors.SelectMany(x => x.Messages)
                    };

                    return new BadRequestObjectResult(result);
                };
            });

            services.AddFluentValidationAutoValidation();

            // Auth
            services.AddValidatorsFromAssemblyContaining<LoginRequestDtoValidator>();
            services.AddValidatorsFromAssemblyContaining<ModifyUserRoleRequestDtoValidator>();
            services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();
            services.AddValidatorsFromAssemblyContaining<UpdateApplicationUserRequestDtoValidator>();

            // Entries
            services.AddValidatorsFromAssemblyContaining<CreateEntryDtoValidator>();
            services.AddValidatorsFromAssemblyContaining<UpdateEntryDtoValidator>();

            // Fields
            services.AddValidatorsFromAssemblyContaining<CreateFieldDtoValidator>();
            services.AddValidatorsFromAssemblyContaining<UpdateFieldDtoValidator>();

            // Trackers
            services.AddValidatorsFromAssemblyContaining<CreateTrackerDtoValidator>();
            services.AddValidatorsFromAssemblyContaining<UpdateTrackerDtoValidator>();
        }
    }
}
