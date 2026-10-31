using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Interfaces
{
    public interface IEmailService
    {
        Task SendConfirmationAsync(
            Order order,
            Customer customer,
            CancellationToken ct = default);
    }
}
