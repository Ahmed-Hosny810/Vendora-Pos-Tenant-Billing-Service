using OpenIddict.Validation.AspNetCore;

namespace Pos.tenant.WebApi.Extensions
{
    public static class AuthenticationServiceExtension
    {
        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme =
                    OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            });

            services.AddOpenIddict()
                .AddValidation(options =>
                {
                    options.SetIssuer(
                        configuration["Services:Identity:Issuer"]!);

                    options.UseSystemNetHttp();

                    options.UseAspNetCore();
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("CanCreateTenantDuringOnboarding", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("user_type", "PendingTenant");
                });

                options.AddPolicy("TenantUserOnly", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("user_type", "Tenant");
                });

                options.AddPolicy("TenantOwnerOnly", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("user_type", "Tenant");
                    policy.RequireRole("TenantOwner");
                });

                options.AddPolicy("PlatformAdmins", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("user_type", "Platform");
                    policy.RequireRole("TenantAdmin");
                });
            });

            return services;
        }
    }
}
