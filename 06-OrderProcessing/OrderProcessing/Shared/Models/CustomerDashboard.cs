using System.Collections.Generic;

namespace OrderProcessing.Shared.Models;

public class CustomerDashboard
{
    public Customer Customer { get; set; } = null!;
    public List<Order> Orders { get; set; } = new();
    public List<PaymentTransaction> Payments { get; set; } = new();
    public List<EmailMessage> Emails { get; set; } = new();


}