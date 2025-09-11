using System;
using System.Collections.Generic;
using HedgeLab.Curves;
using HedgeLab.Instruments;

namespace HedgeLab.Risk
{
    /// <summary>
    /// Computes Key-Rate DV01s (per 1bp) at specified key maturities.
    /// We approximate a "1bp bump at a key" with a piecewise-linear local bump
    /// that is 1.0 at the key and goes to 0.0 at the adjacent keys.
    /// Outside [firstKey, lastKey] the weight is 0.
    /// 
    /// For each key k:
    ///   DF_bumped(t) = DF(t) * exp(∓ delta * weight_k(t) * t), delta = 1bp = 0.0001
    /// KRD(k) = (P_down - P_up)/2, using the localized bump.
    /// </summary>
    public static class KeyRateDuration
    {
        /// <param name="bond">Bond to measure.</param>
        /// <param name="curve">Zero curve used for discounting.</param>
        /// <param name="keysYears">Key maturities in YEARS, ascending (e.g., 2,5,7,10,20,30).</param>
        /// <param name="bumpBp">Basis point size, default 1bp.</param>
        /// <returns>Array of KRDs (same order/length as keysYears), in price units per bp.</returns>
        public static double[] Compute(Bond bond, ZeroCurve curve, IReadOnlyList<double> keysYears, double bumpBp = 1.0)
        {
            if (keysYears == null || keysYears.Count == 0)
                throw new ArgumentException("keysYears must be non-empty and ascending.");

            // Pre-read cashflows and year-fractions
            var cfs = new List<(double tYears, double cf)>();
            foreach (var (date, cf) in bond.Cashflows())
            {
                double t = YearFrac(bond.Settlement, date);
                if (t > 0) cfs.Add((t, cf));
            }

            double delta = bumpBp / 10000.0;
            int m = keysYears.Count;
            var krd = new double[m];

            for (int k = 0; k < m; k++)
            {
                double pUp = 0.0, pDn = 0.0;
                foreach (var (t, cf) in cfs)
                {
                    double w = LocalWeight(t, k, keysYears);
                    double df = curve.DiscountFactor(t);
                    // Localized parallel shift scaled by weight at each cashflow time
                    pUp += cf * df * Math.Exp(-delta * w * t);
                    pDn += cf * df * Math.Exp(+delta * w * t);
                }
                krd[k] = (pDn - pUp) / 2.0;
            }

            return krd;
        }

        /// <summary>
        /// Piecewise-linear "tent" weight around key index k.
        /// Weight = 1 at keys[k], decays linearly to 0 at adjacent keys,
        /// 0 outside [prev, next].
        /// </summary>
        private static double LocalWeight(double tYears, int k, IReadOnlyList<double> keys)
        {
            int m = keys.Count;
            double key = keys[k];
            double prev = (k > 0) ? keys[k - 1] : keys[0];                    // clamp at first key
            double next = (k < m - 1) ? keys[k + 1] : keys[m - 1];            // clamp at last key

            if (tYears <= prev || tYears >= next) return 0.0;
            if (tYears <= key)
            {
                double width = key - prev;
                if (width <= 0) return 0.0;
                return (tYears - prev) / width; // rises 0->1
            }
            else
            {
                double width = next - key;
                if (width <= 0) return 0.0;
                return (next - tYears) / width; // falls 1->0
            }
        }

        private static double YearFrac(DateTime start, DateTime end) =>
            (end - start).TotalDays / 365.25; // same as Bond
    }
}
