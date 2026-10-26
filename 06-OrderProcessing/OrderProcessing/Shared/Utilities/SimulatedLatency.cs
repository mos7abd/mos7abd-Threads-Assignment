using OrderProcessing.Shared.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Utilities
{

    public static class SimulatedLatency
    {
        public static async Task DelayAsync(
            string operation,
            int milliseconds,
            CancellationToken ct)
        {
            Log.Info(
                $"[{operation}] Starting delay ({milliseconds} ms)");

            await Task.Delay(milliseconds, ct);

            Log.Success($"[{operation}] Delay completed");
        }

        public static int RandomMilliseconds(int min, int max)
        {
            return Random.Shared.Next(min, max + 1);
        }
    }


}
