using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pos.tenant.Application.DTOS;
using Pos.tenant.Application.Features.SubscriptionPayments.Commands.HandlePaymobWebhook;
using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Models;
using Pos.tenant.IntegrationTests.Fakes;

namespace Pos.tenant.IntegrationTests.Tests;

public class PaymobWebhookTests
{
    private readonly TestFixture _fixture;

    public PaymobWebhookTests()
    {
        _fixture = new TestFixture();
    }

    [Fact]
    public async Task Webhook_WhenPendingPaymentReceivesSuccess_ShouldCompletePaymentAndMarkInvoicePaid()
    {
        // Arrange
        var tenant = TestDataFactory.CreateTenant();
        var subscription = TestDataFactory.CreateSubscription(tenant.Id);
        var invoice = TestDataFactory.CreateInvoice(tenant.Id);

        var payment = CreatePaymobPayment(
            tenantId: tenant.Id,
            invoiceId: invoice.Id,
            amount: invoice.Total,
            status: PaymentStatuses.Pending,
            providerPaymentReference: "paymob_ref_success_001");

        await SeedAsync(tenant, subscription, invoice, payment);

        FakePaymobWebhookVerifier.NextResult = new PaymobWebhookResult
        {
            PaymentId = payment.Id,
            ProviderPaymentReference = payment.ProviderPaymentReference,
            ProviderTransactionId = "txn_success_001",
            ProviderStatus = "success",
            Success = true,
            Pending = false,
            AmountCents = ToCents(invoice.Total),
            Currency = "EGP",
            FailureReason = null
        };

        var command = CreateCommand();

        // Act
        var result = await _fixture.Mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _fixture.DbContext.ChangeTracker.Clear();

        var updatedPayment = await _fixture.DbContext.SubscriptionPayments
            .FirstAsync(x => x.Id == payment.Id);

        updatedPayment.Status.Should().Be(PaymentStatuses.Completed);
        updatedPayment.PaidAt.Should().NotBeNull();
        updatedPayment.FailureReason.Should().BeNull();
        updatedPayment.ProviderTransactionId.Should().Be("txn_success_001");
        updatedPayment.ProviderStatus.Should().Be("success");

        var updatedInvoice = await _fixture.DbContext.SubscriptionInvoices
            .FirstAsync(x => x.Id == invoice.Id);

        updatedInvoice.Status.Should().Be(InvoiceStatuses.Paid);
        updatedInvoice.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Webhook_WhenFailedPaymentReceivesSuccess_ShouldMoveFailedToCompleted()
    {
        // Arrange
        var tenant = TestDataFactory.CreateTenant();
        var subscription = TestDataFactory.CreateSubscription(tenant.Id);
        var invoice = TestDataFactory.CreateInvoice(tenant.Id);

        var payment = CreatePaymobPayment(
            tenantId: tenant.Id,
            invoiceId: invoice.Id,
            amount: invoice.Total,
            status: PaymentStatuses.Failed,
            providerPaymentReference: "paymob_ref_retry_success_001");

        payment.FailureReason = "First card attempt failed";

        await SeedAsync(tenant, subscription, invoice, payment);

        FakePaymobWebhookVerifier.NextResult = new PaymobWebhookResult
        {
            PaymentId = payment.Id,
            ProviderPaymentReference = payment.ProviderPaymentReference,
            ProviderTransactionId = "txn_retry_success_001",
            ProviderStatus = "success",
            Success = true,
            Pending = false,
            AmountCents = ToCents(invoice.Total),
            Currency = "EGP",
            FailureReason = null
        };

        var command = CreateCommand();

        // Act
        var result = await _fixture.Mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _fixture.DbContext.ChangeTracker.Clear();

        var updatedPayment = await _fixture.DbContext.SubscriptionPayments
            .FirstAsync(x => x.Id == payment.Id);

        updatedPayment.Status.Should().Be(PaymentStatuses.Completed);
        updatedPayment.PaidAt.Should().NotBeNull();
        updatedPayment.FailureReason.Should().BeNull();
        updatedPayment.ProviderTransactionId.Should().Be("txn_retry_success_001");
        updatedPayment.ProviderStatus.Should().Be("success");

        var updatedInvoice = await _fixture.DbContext.SubscriptionInvoices
            .FirstAsync(x => x.Id == invoice.Id);

        updatedInvoice.Status.Should().Be(InvoiceStatuses.Paid);
        updatedInvoice.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Webhook_WhenPendingPaymentReceivesFailure_ShouldMarkPaymentFailedAndKeepInvoiceUnpaid()
    {
        // Arrange
        var tenant = TestDataFactory.CreateTenant();
        var subscription = TestDataFactory.CreateSubscription(tenant.Id);
        var invoice = TestDataFactory.CreateInvoice(tenant.Id);

        var payment = CreatePaymobPayment(
            tenantId: tenant.Id,
            invoiceId: invoice.Id,
            amount: invoice.Total,
            status: PaymentStatuses.Pending,
            providerPaymentReference: "paymob_ref_failed_001");

        await SeedAsync(tenant, subscription, invoice, payment);

        FakePaymobWebhookVerifier.NextResult = new PaymobWebhookResult
        {
            PaymentId = payment.Id,
            ProviderPaymentReference = payment.ProviderPaymentReference,
            ProviderTransactionId = "txn_failed_001",
            ProviderStatus = "failed",
            Success = false,
            Pending = false,
            AmountCents = ToCents(invoice.Total),
            Currency = "EGP",
            FailureReason = "Insufficient funds"
        };

        var command = CreateCommand();

        // Act
        var result = await _fixture.Mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _fixture.DbContext.ChangeTracker.Clear();

        var updatedPayment = await _fixture.DbContext.SubscriptionPayments
            .FirstAsync(x => x.Id == payment.Id);

        updatedPayment.Status.Should().Be(PaymentStatuses.Failed);
        updatedPayment.PaidAt.Should().BeNull();
        updatedPayment.FailureReason.Should().Be("Insufficient funds");
        updatedPayment.ProviderTransactionId.Should().Be("txn_failed_001");
        updatedPayment.ProviderStatus.Should().Be("failed");

        var updatedInvoice = await _fixture.DbContext.SubscriptionInvoices
            .FirstAsync(x => x.Id == invoice.Id);

        updatedInvoice.Status.Should().Be(InvoiceStatuses.Unpaid);
        updatedInvoice.PaidAt.Should().BeNull();
    }

    [Fact]
    public async Task Webhook_WhenCompletedPaymentReceivesFailure_ShouldNotDowngradePayment()
    {
        // Arrange
        var tenant = TestDataFactory.CreateTenant();
        var subscription = TestDataFactory.CreateSubscription(tenant.Id);
        var invoice = TestDataFactory.CreateInvoice(tenant.Id);

        var paidAt = DateTime.UtcNow.AddMinutes(-10);
        invoice.MarkPaid(paidAt);

        var payment = CreatePaymobPayment(
            tenantId: tenant.Id,
            invoiceId: invoice.Id,
            amount: invoice.Total,
            status: PaymentStatuses.Completed,
            providerPaymentReference: "paymob_ref_completed_001");

        payment.PaidAt = paidAt;
        payment.ProviderTransactionId = "txn_original_success_001";
        payment.ProviderStatus = "success";

        await SeedAsync(tenant, subscription, invoice, payment);

        FakePaymobWebhookVerifier.NextResult = new PaymobWebhookResult
        {
            PaymentId = payment.Id,
            ProviderPaymentReference = payment.ProviderPaymentReference,
            ProviderTransactionId = "txn_late_failed_001",
            ProviderStatus = "failed",
            Success = false,
            Pending = false,
            AmountCents = ToCents(invoice.Total),
            Currency = "EGP",
            FailureReason = "Late failed webhook"
        };

        var command = CreateCommand();

        // Act
        var result = await _fixture.Mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _fixture.DbContext.ChangeTracker.Clear();

        var updatedPayment = await _fixture.DbContext.SubscriptionPayments
            .FirstAsync(x => x.Id == payment.Id);

        updatedPayment.Status.Should().Be(PaymentStatuses.Completed);
        updatedPayment.ProviderTransactionId.Should().Be("txn_original_success_001");
        updatedPayment.ProviderStatus.Should().Be("success");
        updatedPayment.FailureReason.Should().BeNull();
        updatedPayment.PaidAt.Should().NotBeNull();

        var updatedInvoice = await _fixture.DbContext.SubscriptionInvoices
            .FirstAsync(x => x.Id == invoice.Id);

        updatedInvoice.Status.Should().Be(InvoiceStatuses.Paid);
    }

    [Fact]
    public async Task Webhook_WhenCompletedPaymentReceivesDuplicateSuccess_ShouldRemainCompleted()
    {
        // Arrange
        var tenant = TestDataFactory.CreateTenant();
        var subscription = TestDataFactory.CreateSubscription(tenant.Id);
        var invoice = TestDataFactory.CreateInvoice(tenant.Id);

        var paidAt = DateTime.UtcNow.AddMinutes(-10);
        invoice.MarkPaid(paidAt);

        var payment = CreatePaymobPayment(
            tenantId: tenant.Id,
            invoiceId: invoice.Id,
            amount: invoice.Total,
            status: PaymentStatuses.Completed,
            providerPaymentReference: "paymob_ref_duplicate_success_001");

        payment.PaidAt = paidAt;
        payment.ProviderTransactionId = "txn_success_original_001";
        payment.ProviderStatus = "success";

        await SeedAsync(tenant, subscription, invoice, payment);

        FakePaymobWebhookVerifier.NextResult = new PaymobWebhookResult
        {
            PaymentId = payment.Id,
            ProviderPaymentReference = payment.ProviderPaymentReference,
            ProviderTransactionId = "txn_success_original_001",
            ProviderStatus = "success",
            Success = true,
            Pending = false,
            AmountCents = ToCents(invoice.Total),
            Currency = "EGP",
            FailureReason = null
        };

        var command = CreateCommand();

        // Act
        var result = await _fixture.Mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _fixture.DbContext.ChangeTracker.Clear();

        var updatedPayment = await _fixture.DbContext.SubscriptionPayments
            .FirstAsync(x => x.Id == payment.Id);

        updatedPayment.Status.Should().Be(PaymentStatuses.Completed);
        updatedPayment.ProviderTransactionId.Should().Be("txn_success_original_001");
        updatedPayment.FailureReason.Should().BeNull();
        updatedPayment.PaidAt.Should().NotBeNull();

        var paymentsCount = await _fixture.DbContext.SubscriptionPayments
            .CountAsync(x => x.InvoiceId == invoice.Id);

        paymentsCount.Should().Be(1);

        var updatedInvoice = await _fixture.DbContext.SubscriptionInvoices
            .FirstAsync(x => x.Id == invoice.Id);

        updatedInvoice.Status.Should().Be(InvoiceStatuses.Paid);
    }

    [Fact]
    public async Task Webhook_WhenPaymobReferenceDoesNotExist_ShouldFail()
    {
        // Arrange
        FakePaymobWebhookVerifier.NextResult = new PaymobWebhookResult
        {
            PaymentId = Guid.NewGuid(),
            ProviderPaymentReference = "unknown_paymob_reference",
            ProviderTransactionId = "txn_unknown_001",
            ProviderStatus = "success",
            Success = true,
            Pending = false,
            AmountCents = 50000,
            Currency = "EGP",
            FailureReason = null
        };

        var command = CreateCommand();

        // Act
        var result = await _fixture.Mediator.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Webhook_WhenSuccessAmountDoesNotMatchInvoiceTotal_ShouldFailAndKeepPaymentPending()
    {
        // Arrange
        var tenant = TestDataFactory.CreateTenant();
        var subscription = TestDataFactory.CreateSubscription(tenant.Id);
        var invoice = TestDataFactory.CreateInvoice(tenant.Id);

        var payment = CreatePaymobPayment(
            tenantId: tenant.Id,
            invoiceId: invoice.Id,
            amount: invoice.Total,
            status: PaymentStatuses.Pending,
            providerPaymentReference: "paymob_ref_amount_mismatch_001");

        await SeedAsync(tenant, subscription, invoice, payment);

        FakePaymobWebhookVerifier.NextResult = new PaymobWebhookResult
        {
            PaymentId = payment.Id,
            ProviderPaymentReference = payment.ProviderPaymentReference,
            ProviderTransactionId = "txn_amount_mismatch_001",
            ProviderStatus = "success",
            Success = true,
            Pending = false,
            AmountCents = ToCents(invoice.Total - 100),
            Currency = "EGP",
            FailureReason = null
        };

        var command = CreateCommand();

        // Act
        var result = await _fixture.Mediator.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();

        _fixture.DbContext.ChangeTracker.Clear();

        var updatedPayment = await _fixture.DbContext.SubscriptionPayments
            .FirstAsync(x => x.Id == payment.Id);

        updatedPayment.Status.Should().Be(PaymentStatuses.Pending);
        updatedPayment.PaidAt.Should().BeNull();

        var updatedInvoice = await _fixture.DbContext.SubscriptionInvoices
            .FirstAsync(x => x.Id == invoice.Id);

        updatedInvoice.Status.Should().Be(InvoiceStatuses.Unpaid);
        updatedInvoice.PaidAt.Should().BeNull();
    }

    [Fact]
    public async Task Webhook_WhenPendingWebhookArrives_ShouldKeepPaymentPending()
    {
        // Arrange
        var tenant = TestDataFactory.CreateTenant();
        var subscription = TestDataFactory.CreateSubscription(tenant.Id);
        var invoice = TestDataFactory.CreateInvoice(tenant.Id);

        var payment = CreatePaymobPayment(
            tenantId: tenant.Id,
            invoiceId: invoice.Id,
            amount: invoice.Total,
            status: PaymentStatuses.Pending,
            providerPaymentReference: "paymob_ref_pending_001");

        await SeedAsync(tenant, subscription, invoice, payment);

        FakePaymobWebhookVerifier.NextResult = new PaymobWebhookResult
        {
            PaymentId = payment.Id,
            ProviderPaymentReference = payment.ProviderPaymentReference,
            ProviderTransactionId = "txn_pending_001",
            ProviderStatus = "pending",
            Success = false,
            Pending = true,
            AmountCents = ToCents(invoice.Total),
            Currency = "EGP",
            FailureReason = null
        };

        var command = CreateCommand();

        // Act
        var result = await _fixture.Mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _fixture.DbContext.ChangeTracker.Clear();

        var updatedPayment = await _fixture.DbContext.SubscriptionPayments
            .FirstAsync(x => x.Id == payment.Id);

        updatedPayment.Status.Should().Be(PaymentStatuses.Pending);
        updatedPayment.PaidAt.Should().BeNull();
        updatedPayment.FailureReason.Should().BeNull();
        updatedPayment.ProviderTransactionId.Should().Be("txn_pending_001");
        updatedPayment.ProviderStatus.Should().Be("pending");

        var updatedInvoice = await _fixture.DbContext.SubscriptionInvoices
            .FirstAsync(x => x.Id == invoice.Id);

        updatedInvoice.Status.Should().Be(InvoiceStatuses.Unpaid);
        updatedInvoice.PaidAt.Should().BeNull();
    }

    private async Task SeedAsync(
        Tenant tenant,
        TenantSubscription subscription,
        SubscriptionInvoice invoice,
        SubscriptionPayment payment)
    {
        await _fixture.DbContext.Tenants.AddAsync(tenant);
        await _fixture.DbContext.TenantSubscriptions.AddAsync(subscription);
        await _fixture.DbContext.SubscriptionInvoices.AddAsync(invoice);
        await _fixture.DbContext.SubscriptionPayments.AddAsync(payment);

        await _fixture.DbContext.SaveChangesAsync();

        _fixture.DbContext.ChangeTracker.Clear();
    }

    private static SubscriptionPayment CreatePaymobPayment(
        Guid tenantId,
        Guid invoiceId,
        decimal amount,
        string status,
        string providerPaymentReference)
    {
        return new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = invoiceId,
            Amount = amount,
            Method = PaymentMethods.OnlineCard,
            Provider = PaymentProviders.Paymob,
            Status = status,
            ProviderPaymentReference = providerPaymentReference,
            ProviderClientSecret = $"client_secret_{Guid.NewGuid():N}",
            IdempotencyKey = Guid.NewGuid().ToString()
        };
    }

    private static HandlePaymobWebhookCommand CreateCommand()
    {
        return new HandlePaymobWebhookCommand
        {
            Payload = new PaymobWebhookRequest(),
            Hmac = "fake_hmac"
        };
    }

    private static long ToCents(decimal amount)
    {
        return (long)(amount * 100);
    }
}