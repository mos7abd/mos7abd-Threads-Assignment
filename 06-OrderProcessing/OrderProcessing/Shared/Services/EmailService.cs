
using OrderProcessing.Interfaces;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderProcessing.Shared.Services;

public sealed class EmailService : IEmailService
{
    private const int MaxAttempts = 4;

    private readonly JsonFileStore _store;
    private readonly Dictionary<int, int> _failAttempts;

    public EmailService(
     JsonFileStore store,
     IReadOnlyDictionary<int, int>? failAttempts = null,
     bool failuresEnabled = true)
    {
        _store = store;

        _failAttempts = failuresEnabled
            ? new Dictionary<int, int>(
                failAttempts ?? new Dictionary<int, int>())
            : new Dictionary<int, int>();
    }

    public async Task SendConfirmationAsync(
        Order order,
        Customer customer,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(customer);

        ct.ThrowIfCancellationRequested();

        int failuresToSimulate =
            _failAttempts.TryGetValue(order.Id, out int failures)
                ? failures
                : 0;

        if (failuresToSimulate < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failuresToSimulate));
        }

        int attempt = 0;

        // Wait before retrying: 500ms, 1000ms, 2000ms
        int[] retryDelays = { 500, 1000, 2000 };

        while (attempt < MaxAttempts)
        {
            ct.ThrowIfCancellationRequested();

            attempt++;

            try
            {
                Log.Info(
                    $"EmailService -> Sending confirmation for Order #{order.Id}. " +
                    $"Attempt {attempt}/{MaxAttempts}");

                // Render email
                await Task.Delay(200, ct);

                // Connect to SMTP server
                await Task.Delay(300, ct);

                bool shouldFail = attempt <= failuresToSimulate;

                if (shouldFail)
                {
                    throw new SmtpUnavailableException(
                        "SMTP server is unavailable.");
                }

                // Send email
                await Task.Delay(500, ct);

                ct.ThrowIfCancellationRequested();

                var email = new EmailMessage
                {
                    OrderId = order.Id,
                    To = customer.Email,
                    Subject = $"Order #{order.Id} Confirmation",
                    Body = $"Your order #{order.Id} has been completed.",
                    SentAt = DateTime.UtcNow,
                    Attempts = attempt,
                    LastError = null
                };

                await _store.UpdateAsync<List<EmailMessage>>(
                    DataFiles.Emails,
                    emails =>
                    {
                        emails.Add(email);
                        return emails;
                    },
                    () => new List<EmailMessage>(),
                    ct);

                Log.Info(
                    $"EmailService -> Email sent successfully for Order #{order.Id} " +
                    $"on attempt {attempt}");

                return;
            }
            catch (SmtpUnavailableException ex)
            {
                Log.Error(
                    $"EmailService -> Attempt {attempt}/{MaxAttempts} failed " +
                    $"for Order #{order.Id}: {ex.Message}");

                ct.ThrowIfCancellationRequested();

                if (attempt == MaxAttempts)
                {
                    var retryRecord = new EmailMessage
                    {
                        OrderId = order.Id,
                        To = customer.Email,
                        Subject = $"Order #{order.Id} Confirmation",
                        Body = $"Your order #{order.Id} has been completed.",
                        SentAt = DateTime.UtcNow,
                        Attempts = attempt,
                        LastError = ex.Message
                    };

                    await _store.UpdateAsync<List<EmailMessage>>(
                        DataFiles.EmailRetry,
                        emails =>
                        {
                            int existingIndex = emails.FindIndex(
                                e => e.OrderId == order.Id);

                            if (existingIndex >= 0)
                            {
                                emails[existingIndex] = retryRecord;
                            }
                            else
                            {
                                emails.Add(retryRecord);
                            }

                            return emails;
                        },
                        () => new List<EmailMessage>(),
                        ct);

                    Log.Info(
                        $"Email for Order #{order.Id} queued for retry later");

                    // Do not throw: email failure must not fail the completed order.
                    return;
                }

                Log.Info(
                    $"EmailService -> Retrying email for Order #{order.Id}...");

                int delay = retryDelays[attempt - 1];

                await Task.Delay(delay, ct);
            }
            catch (OperationCanceledException)
            {
                Log.Info(
                    $"EmailService -> Email operation cancelled for Order #{order.Id}");

                throw;
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"EmailService -> Unexpected error for Order #{order.Id}: {ex.Message}");

                throw;
            }
        }


    }

    public void SendConfirmation(Order order, Customer customer)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(customer);

        int failuresToSimulate =
            _failAttempts.TryGetValue(order.Id, out int failures)
                ? failures
                : 0;

        if (failuresToSimulate < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failuresToSimulate));
        }

        int attempt = 0;

        // Wait before retrying: 500ms, 1000ms, 2000ms
        int[] retryDelays = { 500, 1000, 2000 };

        while (attempt < MaxAttempts)
        {
            attempt++;

            try
            {
                Log.Info(
                    $"EmailService -> Sending confirmation for Order #{order.Id}. " +
                    $"Attempt {attempt}/{MaxAttempts}");

                // Render email
                Thread.Sleep(200);

                // Connect to SMTP server
                Thread.Sleep(300);

                bool shouldFail = attempt <= failuresToSimulate;

                if (shouldFail)
                {
                    throw new SmtpUnavailableException(
                        "SMTP server is unavailable.");
                }

                // Send email
                Thread.Sleep(500);

                var email = new EmailMessage
                {
                    OrderId = order.Id,
                    To = customer.Email,
                    Subject = $"Order #{order.Id} Confirmation",
                    Body = $"Your order #{order.Id} has been completed.",
                    SentAt = DateTime.UtcNow,
                    Attempts = attempt,
                    LastError = null
                };

                _store.Update<List<EmailMessage>>(
                    DataFiles.Emails,
                    emails =>
                    {
                        emails.Add(email);
                        return emails;
                    },
                    () => new List<EmailMessage>());

                Log.Info(
                    $"EmailService -> Email sent successfully for Order #{order.Id} " +
                    $"on attempt {attempt}");

                return;
            }
            catch (SmtpUnavailableException ex)
            {
                Log.Error(
                    $"EmailService -> Attempt {attempt}/{MaxAttempts} failed " +
                    $"for Order #{order.Id}: {ex.Message}");

                if (attempt == MaxAttempts)
                {
                    var retryRecord = new EmailMessage
                    {
                        OrderId = order.Id,
                        To = customer.Email,
                        Subject = $"Order #{order.Id} Confirmation",
                        Body = $"Your order #{order.Id} has been completed.",
                        SentAt = DateTime.UtcNow,
                        Attempts = attempt,
                        LastError = ex.Message
                    };

                    _store.Update<List<EmailMessage>>(
                        DataFiles.EmailRetry,
                        emails =>
                        {
                            int existingIndex = emails.FindIndex(
                                e => e.OrderId == order.Id);

                            if (existingIndex >= 0)
                            {
                                emails[existingIndex] = retryRecord;
                            }
                            else
                            {
                                emails.Add(retryRecord);
                            }

                            return emails;
                        },
                        () => new List<EmailMessage>());

                    Log.Info(
                        $"Email for Order #{order.Id} queued for retry later");

                    // Email failure must not fail the completed order.
                    return;
                }

                Log.Info(
                    $"EmailService -> Retrying email for Order #{order.Id}...");

                int delay = retryDelays[attempt - 1];

                Thread.Sleep(delay);
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"EmailService -> Unexpected error for Order #{order.Id}: {ex.Message}");

                throw;
            }
        }
    }
}

