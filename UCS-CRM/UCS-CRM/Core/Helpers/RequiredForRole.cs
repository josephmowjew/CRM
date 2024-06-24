namespace UCS_CRM.Core.Helpers
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using UCS_CRM.Persistence.Interfaces;

    public class RequiredIfNotRoleAttribute : ValidationAttribute
    {
        private readonly string _exemptRole;

        public RequiredIfNotRoleAttribute(string exemptRole)
        {
            _exemptRole = exemptRole;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var serviceProvider = validationContext.GetService(typeof(IServiceProvider)) as IServiceProvider;
            var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
            var userRepository = serviceProvider.GetService<IUserRepository>();

            var userName = httpContextAccessor?.HttpContext?.User?.Identity?.Name;

            if (userRepository != null && userName != null)
            {
                var userRole = userRepository.GetUserWithRole(userName).Result;

                // If the user's role is not the exempt role, the field is required
                if (!string.Equals(userRole.RoleName, _exemptRole, StringComparison.OrdinalIgnoreCase))
                {
                    if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
                    {
                        return new ValidationResult(ErrorMessage ?? $"This field is required for non-{_exemptRole} users.");
                    }
                }
            }

            return ValidationResult.Success;
        }
    }
}