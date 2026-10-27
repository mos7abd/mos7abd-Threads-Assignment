
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Repositories;

public class SyncCouponRepository
{
    private readonly JsonFileStore _store;

    public SyncCouponRepository(JsonFileStore store)
    {
        _store = store;
    }

    public Coupon? GetByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            Log.Info(
                "SyncCouponRepository -> Coupon code is empty.");

            return null;
        }

        Log.Info(
            "SyncCouponRepository -> Opening connection...");

        Thread.Sleep(200);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            $"SyncCouponRepository -> Executing query for Coupon '{code}'...");

        Thread.Sleep(queryDelay);

        List<Coupon> coupons =
            _store.Read<List<Coupon>>(DataFiles.Coupons)
            ?? new List<Coupon>();

        Coupon? coupon =
            coupons.FirstOrDefault(c =>
                string.Equals(
                    c.Code,
                    code,
                    StringComparison.OrdinalIgnoreCase));

        if (coupon is not null)
        {
            Log.Success(
                $"SyncCouponRepository -> Coupon '{coupon.Code}' found.");
        }
        else
        {
            Log.Info(
                $"SyncCouponRepository -> Coupon '{code}' not found.");
        }

        return coupon;
    }
}
