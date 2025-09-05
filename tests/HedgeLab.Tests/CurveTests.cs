using System;
using System.Linq;
using HedgeLab.Curves;
using Xunit;

namespace HedgeLab.Tests
{
    public class CurveTests
    {
        [Fact]
        public void ZeroRate_IsFiniteAndReasonable()
        {
            // A plausible curve: long-end ~3%, slight downward slope at front, mild hump.
            var nss = new NelsonSiegelSvensson(beta0:0.03, beta1:-0.01, beta2:0.005, beta3:0.0, tau1:1.5, tau2:5.0);
            var zc  = new ZeroCurve(nss);
            var tenors = new[] { 0.25, 1, 2, 5, 10, 30 };
            var rs = tenors.Select(zc.ZeroRate).ToArray();

            // sanity: not insane negatives or > 20%
            Assert.All(rs, r => Assert.InRange(r, -0.05, 0.20));
        }

        [Fact]
        public void DiscountFactors_DecreaseWithMaturity()
        {
            var nss = new NelsonSiegelSvensson(0.03, -0.01, 0.005, 0.0, 1.5, 5.0);
            var zc  = new ZeroCurve(nss);

            double d1 = zc.DiscountFactor(1.0);
            double d2 = zc.DiscountFactor(2.0);
            double d5 = zc.DiscountFactor(5.0);

            Assert.True(d1 > d2 && d2 > d5);
            Assert.InRange(d1, 0.0, 1.0);
        }

        [Fact]
        public void ZeroRate_LimitAtShortEnd_IsBeta0PlusBeta1()
        {
            var nss = new NelsonSiegelSvensson(0.03, -0.01, 0.005, 0.0, 1.5, 5.0);
            var zc  = new ZeroCurve(nss);

            double expected = 0.03 + (-0.01);
            double near0 = zc.ZeroRate(1e-12);

            Assert.InRange(near0, expected - 1e-8, expected + 1e-8);
        }
    }
}
