
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Repositories;

public class CouponRepository
{
    private readonly JsonFileStore _store;

    public CouponRepository(JsonFileStore store)
    {
        _store = store;
    }

    public async Task<Coupon?> GetByCodeAsync(
        string code,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            Log.Info(
                "CouponRepository -> Coupon code is empty.");

            return null;
        }

        Log.Info(
            "CouponRepository -> Opening connection...");

        await SimulatedLatency.DelayAsync(
            "CouponRepository - Open connection",
            200,
            ct);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            $"CouponRepository -> Executing query for Coupon '{code}'...");

        await SimulatedLatency.DelayAsync(
            "CouponRepository - Execute query",
            queryDelay,
            ct);

        List<Coupon> coupons =
            await _store.ReadAsync<List<Coupon>>(
                DataFiles.Coupons,
                ct)
            ?? new List<Coupon>();

        Coupon? coupon = coupons.FirstOrDefault(c =>
            string.Equals(
                c.Code,
                code,
                StringComparison.OrdinalIgnoreCase));

        if (coupon is not null)
        {
            Log.Success(
                $"CouponRepository -> Coupon '{coupon.Code}' found");
        }
        else
        {
            Log.Info(
                $"CouponRepository -> Coupon '{code}' not found");
        }

        return coupon;
    }
}
