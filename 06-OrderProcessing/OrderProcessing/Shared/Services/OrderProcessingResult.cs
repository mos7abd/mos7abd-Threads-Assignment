using System;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Services;

public sealed record OrderProcessingResult(
    Order Order,
    TimeSpan Elapsed);