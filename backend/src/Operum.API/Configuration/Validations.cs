using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Operum.Model.Common;
using Operum.Model.DTOs.Auth.Requests;

namespace Operum.API.Configuration
{
    public static class Validations
    {
        public static void Configure(this IServiceCollection services)
        {
            services.AddValidation();
            services.AddFluentValidation();
        }

        private static void AddValidation(this IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(ms => ms.Value?.Errors.Count > 0)
                        .SelectMany(ms => ms.Value!.Errors.Select(e => e.ErrorMessage));

                    var result = new ApiResponse
                    {
                        StatusCode = Model.Enums.StatusCodeEnum.BadRequest,
                        Messages = errors
                    };

                    return new BadRequestObjectResult(result);
                };
            });
        }

        private static void AddFluentValidation(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(typeof(LoginRequestDtoValidator).Assembly);
            services.AddFluentValidationAutoValidation();
        }
    }
}