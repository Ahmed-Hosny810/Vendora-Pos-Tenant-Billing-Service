using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pos.tenant.Application.Interfaces.Payment;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Infrastructure.Shared.Constants;
using Pos.tenant.Infrastructure.Shared.Payment;
using Pos.tenant.Infrastructure.Shared.Payment.Paymob;
using Pos.tenant.Infrastructure.Shared.Services;
using Pos.tenant.Infrastructure.Shared.Workers;

namespace Pos.tenant.Infrastructure.Shared
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddSharedInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<PaymobSettings>(
                configuration.GetSection("Paymob"));

            services.AddHttpClient<IPaymobPaymentService, PaymobPaymentService>(
                (serviceProvider, client) =>
                {
                    var settings = serviceProvider
                        .GetRequiredService<IOptions<PaymobSettings>>()
                        .Value;

                    client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
                });
            services.AddScoped<IPaymobWebhookVerifier, PaymobWebhookVerifier>();

            services.AddScoped< ITenantSubscriptionActivationService,TenantSubscriptionActivationService>();

            services.AddScoped<ISubscriptionRenewalService,SubscriptionRenewalService>();

            services.AddScoped<ISubscriptionExpiryService,SubscriptionExpiryService>();

            services.AddHostedService<SubscriptionRenewalWorker>();

            services.AddHostedService<SubscriptionExpiryWorker>();

            services.Configure<SubscriptionRenewalOptions>(
                configuration.GetSection(nameof(SubscriptionRenewalOptions)));

            services.Configure<SubscriptionExpiryOptions>(
                configuration.GetSection(nameof(SubscriptionExpiryOptions)));


            return services;
        }
    }
}
