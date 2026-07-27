using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pos.tenant.Application.Features.SubscriptionPayments.Commands.CheckoutCommand;
using Pos.tenant.Application.Interfaces.Payment;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Infrastructure.Persistence.Contexts;
using Pos.tenant.Infrastructure.Persistence.Repositories;
using Pos.tenant.Infrastructure.Persistence.UnitOfWorks;
using Pos.tenant.IntegrationTests.Fakes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.IntegrationTests
{
    public class TestFixture
    {
        public ServiceProvider ServiceProvider { get; }
        public ApplicationDbContext DbContext { get; }
        public IMediator Mediator { get; }

        public TestFixture()
        {
            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase($"TenantBillingTestDb_{Guid.NewGuid()}");
            });

            /*
                Register MediatR handlers manually from Application assembly.
                Any handler class from your Application project can be used here.
            */
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(
                    typeof(StartPaymobCheckoutCommandHandler).Assembly);
            });

            /*
                Register repositories.
                Adjust names if your concrete repository classes are different.
            */
            services.AddScoped<ISubscriptionInvoiceRepositoryAsync, SubscriptionInvoiceRepositoryAsync>();
            services.AddScoped<ISubscriptionPaymentRepositoryAsync, SubscriptionPaymentRepositoryAsync>();
            services.AddScoped<ITenantRepositoryAsync, TenantRepositoryAsync>();
            services.AddScoped<ITenantSubscriptionRepositoryAsync, TenantSubscriptionRepositoryAsync>();
            services.AddScoped<IPaymobWebhookVerifier, FakePaymobWebhookVerifier>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            /*
                Fake external services.
            */
            services.AddScoped<IPaymobPaymentService, FakePaymobPaymentService>();
            services.AddScoped<ITenantSubscriptionActivationService, FakeTenantSubscriptionActivationService>();

            ServiceProvider = services.BuildServiceProvider();

            DbContext = ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Mediator = ServiceProvider.GetRequiredService<IMediator>();

            DbContext.Database.EnsureCreated();
        }
    }
}
