using System;
using System.Collections.Generic;
using HedgeLab.Curves;

namespace HedgeLab.Instruments
{
    /// <summary>
    /// Fixed-rate bullet bond with regular coupons.
    /// Coupon: annual rate (decimal), Frequency: payments per year (e.g., 2), Face: nominal.
    /// Day count: ACT/365.25 for year fractions (simple; can be swapped later).
    /// </summary>
    public sealed class Bond
    {
        public DateTime Settlement { get; }
        public DateTime Maturity   { get; }
        public double   Coupon     { get; }   // annual rate in decimal, e.g., 0.03
        public int      Frequency  { get; }   // payments per year, e.g., 2
        public double   Face       { get; }   // typically 100

        public Bond(DateTime settlement, DateTime maturity, double coupon, int frequency = 2, double face = 100.0)
        {
            if (maturity <= settlement) throw new ArgumentException("Maturity must be after settlement.");
            if (frequency <= 0)         throw new ArgumentException("Frequency must be positive.");
            if (coupon < 0)             throw new ArgumentException("Coupon cannot be negative.");
            Settlement = settlement.Date;
            Maturity   = maturity.Date;
            Coupon     = coupon;
            Frequency  = frequency;
            Face       = face;
        }

        /// <summary>
        /// Payment schedule strictly after Settlement, ending at Maturity.
        /// Intermediate payments: coupon only; final: coupon + principal.
        /// </summary>
        public IEnumerable<(DateTime date, double cashflow)> Cashflows()
        {
            int months = 12 / Frequency;

            // Find first coupon date strictly after Settlement by walking back from Maturity.
            DateTime d = Maturity;
            while (d > Settlement) d = d.AddMonths(-months);
            DateTime pay = d.AddMonths(months);

            double cpn = Face * Coupon / Frequency;
            while (pay < Maturity)
            {
                yield return (pay, cpn);
                pay = pay.AddMonths(months);
            }
            yield return (Maturity, cpn + Face);
        }

        private static double YearFrac(DateTime start, DateTime end)
            => (end - start).TotalDays / 365.25; // ACT/365.25

        /// <summary>
        /// Present value using the provided zero curve.
        /// </summary>
        public double Price(ZeroCurve curve)
        {
            double pv = 0.0;
            foreach (var (date, cf) in Cashflows())
            {
                double t = YearFrac(Settlement, date);
                pv += cf * curve.DiscountFactor(t);
            }
            return pv;
        }

        /// <summary>
        /// DV01 per 1bp: (P(-1bp) - P(+1bp)) / 2.
        /// Implemented as a true parallel shift: r_new(t) = r_old(t) ± delta
        /// => DF_new = DF_old * exp(∓ delta * t).
        /// </summary>
        public double DV01(ZeroCurve curve, double bumpBp = 1.0)
        {
            double delta = bumpBp / 10000.0;
            double pUp = 0.0, pDn = 0.0;

            foreach (var (date, cf) in Cashflows())
            {
                double t  = YearFrac(Settlement, date);
                double df = curve.DiscountFactor(t);
                pUp += cf * df * Math.Exp(-delta * t); // rates up -> more discount -> lower price
                pDn += cf * df * Math.Exp(+delta * t); // rates down -> less discount -> higher price
            }
            return (pDn - pUp) / 2.0;
        }
    }
}
