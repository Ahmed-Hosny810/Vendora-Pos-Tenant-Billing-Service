using FluentAssertions;
using Pos.tenant.Application.Features.SubscriptionPayments.Commands.CheckoutCommand;
using Pos.tenant.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace Pos.tenant.IntegrationTests.Tests
{
    public class SubscriptionPaymentCheckoutTests
    {
        private readonly TestFixture _fixture;

        public SubscriptionPaymentCheckoutTests()
        {
            _fixture = new TestFixture();
        }

        [Fact]
        public async Task StartCheckout_ShouldCreatePendingPaymobPayment()
        {
            // Arrange
            var tenant = TestDataFactory.CreateTenant();
            var subscription = TestDataFactory.CreateSubscription(tenant.Id);
            var invoice = TestDataFactory.CreateInvoice(tenant.Id);

            await _fixture.DbContext.Tenants.AddAsync(tenant);
            await _fixture.DbContext.TenantSubscriptions.AddAsync(subscription);
            await _fixture.DbContext.SubscriptionInvoices.AddAsync(invoice);
            await _fixture.DbContext.SaveChangesAsync();

            var command = new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = Guid.NewGuid().ToString()
            };

            // Act
            var result = await _fixture.Mediator.Send(command);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Should().NotBeNull();
            result.Value!.InvoiceId.Should().Be(invoice.Id);
            result.Value.Status.Should().Be(PaymentStatuses.Pending);
            result.Value.Provider.Should().Be(PaymentProviders.Paymob);
            result.Value.CheckoutUrl.Should().NotBeNullOrWhiteSpace();

            var payment = await _fixture.DbContext.SubscriptionPayments
                .FirstOrDefaultAsync(x => x.InvoiceId == invoice.Id);

            payment.Should().NotBeNull();
            payment!.Status.Should().Be(PaymentStatuses.Pending);
            payment.Provider.Should().Be(PaymentProviders.Paymob);
            payment.ProviderClientSecret.Should().NotBeNullOrWhiteSpace();
            payment.ProviderPaymentReference.Should().NotBeNullOrWhiteSpace();
            payment.IdempotencyKey.Should().Be(command.IdempotencyKey);
        }

        [Fact]
        public async Task StartCheckout_WithSamePendingIdempotencyKey_ShouldReturnSamePayment()
        {
            // Arrange
            var tenant = TestDataFactory.CreateTenant();
            var subscription = TestDataFactory.CreateSubscription(tenant.Id);
            var invoice = TestDataFactory.CreateInvoice(tenant.Id);

            await _fixture.DbContext.Tenants.AddAsync(tenant);
            await _fixture.DbContext.TenantSubscriptions.AddAsync(subscription);
            await _fixture.DbContext.SubscriptionInvoices.AddAsync(invoice);
            await _fixture.DbContext.SaveChangesAsync();

            var idempotencyKey = Guid.NewGuid().ToString();

            var firstResult = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = idempotencyKey
            });

            firstResult.IsSuccess.Should().BeTrue();

            // Act
            var secondResult = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = idempotencyKey
            });

            // Assert
            secondResult.IsSuccess.Should().BeTrue();

            secondResult.Value.Should().NotBeNull();
            secondResult.Value!.PaymentId.Should().Be(firstResult.Value!.PaymentId);
            secondResult.Value.InvoiceId.Should().Be(invoice.Id);
            secondResult.Value.Status.Should().Be(PaymentStatuses.Pending);

            var paymentsCount = await _fixture.DbContext.SubscriptionPayments
                .CountAsync(x => x.InvoiceId == invoice.Id);

            paymentsCount.Should().Be(1);
        }

        [Fact]
        public async Task StartCheckout_WithSameIdempotencyKeyForDifferentInvoice_ShouldFail()
        {
            // Arrange
            var tenant = TestDataFactory.CreateTenant();
            var subscription = TestDataFactory.CreateSubscription(tenant.Id);
            var invoice1 = TestDataFactory.CreateInvoice(tenant.Id);
            var invoice2 = TestDataFactory.CreateInvoice(tenant.Id);

            await _fixture.DbContext.Tenants.AddAsync(tenant);
            await _fixture.DbContext.TenantSubscriptions.AddAsync(subscription);
            await _fixture.DbContext.SubscriptionInvoices.AddRangeAsync(invoice1, invoice2);
            await _fixture.DbContext.SaveChangesAsync();

            var idempotencyKey = Guid.NewGuid().ToString();

            var firstResult = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice1.Id,
                IdempotencyKey = idempotencyKey
            });

            firstResult.IsSuccess.Should().BeTrue();

            // Act
            var secondResult = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice2.Id,
                IdempotencyKey = idempotencyKey
            });

            // Assert
            secondResult.IsFailure.Should().BeTrue();

            var paymentsCount = await _fixture.DbContext.SubscriptionPayments.CountAsync();

            paymentsCount.Should().Be(1);
        }

        [Fact]
        public async Task StartCheckout_WhenInvoiceIsPaid_ShouldFail()
        {
            // Arrange
            var tenant = TestDataFactory.CreateTenant();
            var subscription = TestDataFactory.CreateSubscription(tenant.Id);
            var invoice = TestDataFactory.CreateInvoice(tenant.Id);

            invoice.MarkPaid(DateTime.UtcNow);

            await _fixture.DbContext.Tenants.AddAsync(tenant);
            await _fixture.DbContext.TenantSubscriptions.AddAsync(subscription);
            await _fixture.DbContext.SubscriptionInvoices.AddAsync(invoice);
            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var result = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = Guid.NewGuid().ToString()
            });

            // Assert
            result.IsFailure.Should().BeTrue();

            var paymentsCount = await _fixture.DbContext.SubscriptionPayments.CountAsync();

            paymentsCount.Should().Be(0);
        }

        [Fact]
        public async Task StartCheckout_WhenInvoiceIsCancelled_ShouldFail()
        {
            // Arrange
            var tenant = TestDataFactory.CreateTenant();
            var subscription = TestDataFactory.CreateSubscription(tenant.Id);
            var invoice = TestDataFactory.CreateInvoice(tenant.Id);

            invoice.Status = InvoiceStatuses.Cancelled;

            await _fixture.DbContext.Tenants.AddAsync(tenant);
            await _fixture.DbContext.TenantSubscriptions.AddAsync(subscription);
            await _fixture.DbContext.SubscriptionInvoices.AddAsync(invoice);
            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var result = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = Guid.NewGuid().ToString()
            });

            // Assert
            result.IsFailure.Should().BeTrue();

            var paymentsCount = await _fixture.DbContext.SubscriptionPayments.CountAsync();

            paymentsCount.Should().Be(0);
        }

        [Fact]
        public async Task StartCheckout_WhenExistingPaymentIsCompleted_ShouldFail()
        {
            // Arrange
            var tenant = TestDataFactory.CreateTenant();
            var subscription = TestDataFactory.CreateSubscription(tenant.Id);
            var invoice = TestDataFactory.CreateInvoice(tenant.Id);

            await _fixture.DbContext.Tenants.AddAsync(tenant);
            await _fixture.DbContext.TenantSubscriptions.AddAsync(subscription);
            await _fixture.DbContext.SubscriptionInvoices.AddAsync(invoice);
            await _fixture.DbContext.SaveChangesAsync();

            var idempotencyKey = Guid.NewGuid().ToString();

            var firstResult = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = idempotencyKey
            });

            firstResult.IsSuccess.Should().BeTrue();

            var payment = await _fixture.DbContext.SubscriptionPayments
                .FirstAsync(x => x.Id == firstResult.Value!.PaymentId);

            payment.MarkCompleted(DateTime.UtcNow, "fake_transaction_id");

            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var secondResult = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = idempotencyKey
            });

            // Assert
            secondResult.IsFailure.Should().BeTrue();

            var paymentsCount = await _fixture.DbContext.SubscriptionPayments
                .CountAsync(x => x.InvoiceId == invoice.Id);

            paymentsCount.Should().Be(1);
        }

        [Fact]
        public async Task StartCheckout_WhenExistingPaymentIsFailed_ShouldFailAndRequireNewIdempotencyKey()
        {
            // Arrange
            var tenant = TestDataFactory.CreateTenant();
            var subscription = TestDataFactory.CreateSubscription(tenant.Id);
            var invoice = TestDataFactory.CreateInvoice(tenant.Id);

            await _fixture.DbContext.Tenants.AddAsync(tenant);
            await _fixture.DbContext.TenantSubscriptions.AddAsync(subscription);
            await _fixture.DbContext.SubscriptionInvoices.AddAsync(invoice);
            await _fixture.DbContext.SaveChangesAsync();

            var idempotencyKey = Guid.NewGuid().ToString();

            var firstResult = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = idempotencyKey
            });

            firstResult.IsSuccess.Should().BeTrue();

            var payment = await _fixture.DbContext.SubscriptionPayments
                .FirstAsync(x => x.Id == firstResult.Value!.PaymentId);

            payment.MarkFailed("Test failure", "fake_transaction_id");

            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var secondResult = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = idempotencyKey
            });

            // Assert
            secondResult.IsFailure.Should().BeTrue();

            var paymentsCount = await _fixture.DbContext.SubscriptionPayments
                .CountAsync(x => x.InvoiceId == invoice.Id);

            paymentsCount.Should().Be(1);
        }

        [Fact]
        public async Task StartCheckout_WhenExistingPaymentIsRefunded_ShouldFailAndRequireNewIdempotencyKey()
        {
            // Arrange
            var tenant = TestDataFactory.CreateTenant();
            var subscription = TestDataFactory.CreateSubscription(tenant.Id);
            var invoice = TestDataFactory.CreateInvoice(tenant.Id);

            await _fixture.DbContext.Tenants.AddAsync(tenant);
            await _fixture.DbContext.TenantSubscriptions.AddAsync(subscription);
            await _fixture.DbContext.SubscriptionInvoices.AddAsync(invoice);
            await _fixture.DbContext.SaveChangesAsync();

            var idempotencyKey = Guid.NewGuid().ToString();

            var firstResult = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = idempotencyKey
            });

            firstResult.IsSuccess.Should().BeTrue();

            var payment = await _fixture.DbContext.SubscriptionPayments
                .FirstAsync(x => x.Id == firstResult.Value!.PaymentId);

            payment.Refund();

            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var secondResult = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = idempotencyKey
            });

            // Assert
            secondResult.IsFailure.Should().BeTrue();

            var paymentsCount = await _fixture.DbContext.SubscriptionPayments
                .CountAsync(x => x.InvoiceId == invoice.Id);

            paymentsCount.Should().Be(1);
        }

        [Fact]
        public async Task StartCheckout_AfterFailedPayment_WithNewIdempotencyKey_ShouldCreateNewPayment()
        {
            // Arrange
            var tenant = TestDataFactory.CreateTenant();
            var subscription = TestDataFactory.CreateSubscription(tenant.Id);
            var invoice = TestDataFactory.CreateInvoice(tenant.Id);

            await _fixture.DbContext.Tenants.AddAsync(tenant);
            await _fixture.DbContext.TenantSubscriptions.AddAsync(subscription);
            await _fixture.DbContext.SubscriptionInvoices.AddAsync(invoice);
            await _fixture.DbContext.SaveChangesAsync();

            var firstResult = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = Guid.NewGuid().ToString()
            });

            firstResult.IsSuccess.Should().BeTrue();

            var failedPayment = await _fixture.DbContext.SubscriptionPayments
                .FirstAsync(x => x.Id == firstResult.Value!.PaymentId);

            failedPayment.MarkFailed("Test failure", "fake_transaction_id");

            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var secondResult = await _fixture.Mediator.Send(new StartPaymobCheckoutCommand
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = Guid.NewGuid().ToString()
            });

            // Assert
            secondResult.IsSuccess.Should().BeTrue();

            secondResult.Value.Should().NotBeNull();
            secondResult.Value!.PaymentId.Should().NotBe(firstResult.Value!.PaymentId);

            var paymentsCount = await _fixture.DbContext.SubscriptionPayments
                .CountAsync(x => x.InvoiceId == invoice.Id);

            paymentsCount.Should().Be(2);
        }
    }
}
