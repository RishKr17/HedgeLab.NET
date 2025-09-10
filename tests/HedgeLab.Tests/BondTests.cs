using System;
using HedgeLab.Curves;
using HedgeLab.Instruments;
using Xunit;

namespace HedgeLab.Tests
{
    public class BondTests
    {
        private static ZeroCurve SampleCurve()
        {
            // Plausible NSS: ~3% long end, mild hump
            var nss = new NelsonSiegelSvensson(beta0: 0.030, beta1: -0.005, beta2: 0.004, beta3: 0.0, tau1: 1.5, tau2: 5.0);
            return new ZeroCurve(nss);
        }

        [Fact]
        public void Price_IsWithinReasonableRange()
        {
            var zc = SampleCurve();
            var bond = new Bond(settlement: new DateTime(2025, 1, 2),
                                maturity:   new DateTime(2030, 1, 2),
                                coupon: 0.03, frequency: 2, face: 100.0);

            double p = bond.Price(zc);
            Assert.InRange(p, 80.0, 120.0);
        }

        [Fact]
        public void DV01_IsPositive_AndSensible()
        {
            var zc = SampleCurve();
            var bond = new Bond(new DateTime(2025,1,2), new DateTime(2030,1,2), 0.03, 2);
            double dv01 = bond.DV01(zc, 1.0);
            Assert.True(dv01 > 0);               // long bond -> positive DV01
            Assert.InRange(dv01, 0.01, 1.50);    // very loose bounds in price units per bp
        }

        [Fact]
        public void DV01_ScalesApproximatelyLinearly_ForSmallBumps()
        {
            var zc = SampleCurve();
            var bond = new Bond(new DateTime(2025,1,2), new DateTime(2030,1,2), 0.03, 2);

            double dv01_1bp  = bond.DV01(zc, 1.0);
            double dv01_half = bond.DV01(zc, 0.5) * 2.0;   // scale up linear approx

            Assert.InRange(Math.Abs(dv01_1bp - dv01_half), 0.0, Math.Max(0.002, 0.05 * dv01_1bp));
        }
    }
}
