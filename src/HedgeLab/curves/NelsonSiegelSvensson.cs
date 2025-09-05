using System;

namespace HedgeLab.Curves
{
    /// <summary>
    /// Nelson–Siegel–Svensson zero rate:
    /// r(t) = β0
    ///      + β1 * ((1 - e^{-t/τ1}) / (t/τ1))
    ///      + β2 * ( ((1 - e^{-t/τ1}) / (t/τ1)) - e^{-t/τ1} )
    ///      + β3 * ( ((1 - e^{-t/τ2}) / (t/τ2)) - e^{-t/τ2} )
    /// t in years, rates in decimals (e.g., 0.03 = 3%).
    /// </summary>
    public sealed class NelsonSiegelSvensson
    {
        public double Beta0 { get; }
        public double Beta1 { get; }
        public double Beta2 { get; }
        public double Beta3 { get; }
        public double Tau1  { get; }
        public double Tau2  { get; }

        public NelsonSiegelSvensson(double beta0, double beta1, double beta2, double beta3, double tau1, double tau2)
        {
            if (tau1 <= 0 || tau2 <= 0) throw new ArgumentException("Tau1 and Tau2 must be positive.");
            Beta0 = beta0; Beta1 = beta1; Beta2 = beta2; Beta3 = beta3; Tau1 = tau1; Tau2 = tau2;
        }

        public double ZeroRate(double tYears)
        {
            // As t -> 0, the NSS limit is β0 + β1 (short-end level).
            if (tYears <= 1e-10) return Beta0 + Beta1;

            double x1 = tYears / Tau1;
            double x2 = tYears / Tau2;

            double term1 = (1.0 - Math.Exp(-x1)) / x1;
            double term2 = term1 - Math.Exp(-x1);
            double term3 = (1.0 - Math.Exp(-x2)) / x2 - Math.Exp(-x2);

            return Beta0 + Beta1 * term1 + Beta2 * term2 + Beta3 * term3;
        }

        /// <summary>
        /// Continuous compounding discount factor: P(0,t) = exp(-r(t) * t)
        /// </summary>
        public double DiscountFactor(double tYears)
        {
            double r = ZeroRate(tYears);
            return Math.Exp(-r * tYears);
        }
    }
}
