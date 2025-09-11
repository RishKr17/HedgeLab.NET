using System;
using System.Linq;
using HedgeLab.Curves;
using HedgeLab.Instruments;
using HedgeLab.Risk;
using Xunit;

namespace HedgeLab.Tests
{
    public class KrdTests
    {
        private static ZeroCurve SampleCurve()
        {
            // ~3% long end with mild hump
            var nss = new NelsonSiegelSvensson(0.030, -0.006, 0.004, 0.0, 1.5, 5.0);
            return new ZeroCurve(nss);
        }

        [Fact]
        public void SumOfKRDs_ApproximatelyMatches_ParallelDV01()
        {
            var curve = SampleCurve();
            var bond  = new Bond(new DateTime(2025,1,2), new DateTime(2030,1,2), 0.03, 2, 100.0);

            double[] keys = { 2, 5, 7, 10, 20, 30 };
            var krd = KeyRateDuration.Compute(bond, curve, keys, 1.0); // per 1bp
            double krdSum = krd.Sum();

            double dv01 = bond.DV01(curve, 1.0);

            // Allow some slack because localized bumps don't perfectly reproduce a parallel shift.
            Assert.InRange(krdSum, 0.7 * dv01, 1.3 * dv01);
        }

        [Fact]
        public void AtLeastOneKeyRateIsPositive()
        {
            var curve = SampleCurve();
            var bond  = new Bond(new DateTime(2025,1,2), new DateTime(2030,1,2), 0.03, 2, 100.0);

            double[] keys = { 2, 5, 7, 10, 20, 30 };
            var krd = KeyRateDuration.Compute(bond, curve, keys, 1.0);

            Assert.True(krd.Any(x => x > 0));
        }
    }
}
