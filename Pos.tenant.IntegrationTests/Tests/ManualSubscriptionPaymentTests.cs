using FluentAssertions;
using Pos.tenant.Application.Features.SubscriptionPayments.Commands.CreateCommand;
using Pos.tenant.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace Pos.tenant.IntegrationTests.Tests
{
    public class ManualSubscriptionPaymentTests
    {
        private readonly TestFixture _fixture;

        public ManualSubscriptionPaymentTests()
        {
            _fixture = new TestFixture();
        }

        [Fact]
        public async Task RegisterManualPayment_WhenMarkAsCompleted_ShouldCreateCompletedPaymentAndMarkInvoicePaid()
        {
            // Arrange
            var tenant = TestDataFactory.CreateTenant();
            var subscription = TestDataFactory.CreateSubscription(tenant.Id);
            var invoice = TestDataFactory.CreateInvoice(tenant.Id);

            await _fixture.DbContext.Tenants.AddAsync(tenant);
            await _fixture.DbContext.TenantSubscriptions.AddAsync(subscription);
            await _fixture.DbContext.SubscriptionInvoices.AddAsync(invoice);
            await _fixture.DbContext.SaveChangesAsync();

            var command = new RecordManualSubscriptionPaymentCommand
            {
                InvoiceId = invoice.Id,
                Amount = invoice.Total,
                Method = PaymentMethods.BankTransfer,
                ReferenceNumber = "BANK-REF-001",
                MarkAsCompleted = true
            };

            // Act
            var result = await _fixture.Mediator.Send(command);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Should().NotBeNull();
            result.Value!.InvoiceId.Should().Be(invoice.Id);
            result.Value.Status.Should().Be(PaymentStatuses.Completed);
            result.Value.PaidAt.Should().NotBeNull();

            var payment = await _fixture.DbContext.SubscriptionPayments
                .FirstOrDefaultAsync(x => x.InvoiceId == invoice.Id);

            payment.Should().NotBeNull();
            payment!.Status.Should().Be(PaymentStatuses.Completed);
            payment.Provider.Should().Be(PaymentProviders.Manual);
            payment.PaidAt.Should().NotBeNull();
            payment.ReferenceNumber.Should().Be("BANK-REF-001");

            var updatedInvoice = await _fixture.DbContext.SubscriptionInvoices
                .FirstAsync(x => x.Id == invoice.Id);

            updatedInvoice.Status.Should().Be(InvoiceStatuses.Paid);
            updatedInvoice.PaidAt.Should().NotBeNull();
        }

        [Fact]
        public async Task RegisterManualPayment_WhenMarkAsCompletedButAmountDoesNotEqualInvoiceTotal_ShouldFail()
        {
            // Arrange
            var tenant = TestDataFactory.CreateTenant();
            var subscription = TestDataFactory.CreateSubscription(tenant.Id);
            var invoice = TestDataFactory.CreateInvoice(tenant.Id);

            await _fixture.DbContext.Tenants.AddAsync(tenant);
            await _fixture.DbContext.TenantSubscriptions.AddAsync(subscription);
            await _fixture.DbContext.SubscriptionInvoices.AddAsync(invoice);
            await _fixture.DbContext.SaveChangesAsync();

            var command = new RecordManualSubscriptionPaymentCommand
            {
                InvoiceId = invoice.Id,
                Amount = invoice.Total - 100,
                Method = PaymentMethods.BankTransfer,
                ReferenceNumber = "BANK-REF-002",
                MarkAsCompleted = true
            };

            // Act
            var result = await _fixture.Mediator.Send(command);

            // Assert
            result.IsFailure.Should().BeTrue();

            var paymentsCount = await _fixture.DbContext.SubscriptionPayments.CountAsync();

            paymentsCount.Should().Be(0);

            var updatedInvoice = await _fixture.DbContext.SubscriptionInvoices
                .FirstAsync(x => x.Id == invoice.Id);

            updatedInvoice.Status.Should().Be(InvoiceStatuses.Unpaid);
        }

        [Fact]
        public async Task RegisterManualPayment_WhenNotCompleted_ShouldCreatePendingPaymentAndKeepInvoiceUnpaid()
        {
            // Arrange
            var tenant = TestDataFactory.CreateTenant();
            var subscription = TestDataFactory.CreateSubscription(tenant.Id);
            var invoice = TestDataFactory.CreateInvoice(tenant.Id);

            await _fixture.DbContext.Tenants.AddAsync(tenant);
            await _fixture.DbContext.TenantSubscriptions.AddAsync(subscription);
            await _fixture.DbContext.SubscriptionInvoices.AddAsync(invoice);
            await _fixture.DbContext.SaveChangesAsync();

            var command = new RecordManualSubscriptionPaymentCommand
            {
                InvoiceId = invoice.Id,
                Amount = invoice.Total,
                Method = PaymentMethods.BankTransfer,
                ReferenceNumber = "BANK-PENDING-001",
                MarkAsCompleted = false
            };

            // Act
            var result = await _fixture.Mediator.Send(command);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Should().NotBeNull();
            result.Value!.Status.Should().Be(PaymentStatuses.Pending);

            var payment = await _fixture.DbContext.SubscriptionPayments
                .FirstOrDefaultAsync(x => x.InvoiceId == invoice.Id);

            payment.Should().NotBeNull();
            payment!.Status.Should().Be(PaymentStatuses.Pending);
            payment.PaidAt.Should().BeNull();

            var updatedInvoice = await _fixture.DbContext.SubscriptionInvoices
                .FirstAsync(x => x.Id == invoice.Id);

            updatedInvoice.Status.Should().Be(InvoiceStatuses.Unpaid);
            updatedInvoice.PaidAt.Should().BeNull();
        }

        [Fact]
        public async Task RegisterManualPayment_WhenInvoiceIsPaid_ShouldFail()
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

            var command = new RecordManualSubscriptionPaymentCommand
            {
                InvoiceId = invoice.Id,
                Amount = invoice.Total,
                Method = PaymentMethods.BankTransfer,
                ReferenceNumber = "BANK-REF-003",
                MarkAsCompleted = true
            };

            // Act
            var result = await _fixture.Mediator.Send(command);

            // Assert
            result.IsFailure.Should().BeTrue();

            var paymentsCount = await _fixture.DbContext.SubscriptionPayments.CountAsync();

            paymentsCount.Should().Be(0);
        }

        [Fact]
        public async Task RegisterManualPayment_WhenInvoiceIsCancelled_ShouldFail()
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

            var command = new RecordManualSubscriptionPaymentCommand
            {
                InvoiceId = invoice.Id,
                Amount = invoice.Total,
                Method = PaymentMethods.BankTransfer,
                ReferenceNumber = "BANK-REF-004",
                MarkAsCompleted = true
            };

            // Act
            var result = await _fixture.Mediator.Send(command);

            // Assert
            result.IsFailure.Should().BeTrue();

            var paymentsCount = await _fixture.DbContext.SubscriptionPayments.CountAsync();

            paymentsCount.Should().Be(0);
        }
    }
}
