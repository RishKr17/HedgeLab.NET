using System;
using System.Linq;
using HedgeLab.Curves;
using HedgeLab.Instruments;
using HedgeLab.Risk;
using HedgeLab.Hedge;
using Xunit;

namespace HedgeLab.Tests
{
    public class HedgeOptimizerTests
    {
        private static ZeroCurve SampleCurve()
        {
            var nss = new NelsonSiegelSvensson(0.030, -0.006, 0.004, 0.0, 1.5, 5.0);
            return new ZeroCurve(nss);
        }

        [Fact]
        public void Optimizer_Reduces_KeyRateResiduals()
        {
            var curve = SampleCurve();
            double[] keys = { 2, 5, 7, 10, 20, 30 };

            // Target: a 5Y 3% bond
            var target = new Bond(new DateTime(2025,1,2), new DateTime(2030,1,2), 0.03, 2, 100.0);
            var b = KeyRateDuration.Compute(target, curve, keys); // m

            // Hedgers: 2Y, 5Y, 10Y 3% bonds
            var h1 = new Bond(new DateTime(2025,1,2), new DateTime(2027,1,2), 0.03, 2, 100.0);
            var h2 = new Bond(new DateTime(2025,1,2), new DateTime(2030,1,2), 0.03, 2, 100.0);
            var h3 = new Bond(new DateTime(2025,1,2), new DateTime(2035,1,2), 0.03, 2, 100.0);

            var k1 = KeyRateDuration.Compute(h1, curve, keys);
            var k2 = KeyRateDuration.Compute(h2, curve, keys);
            var k3 = KeyRateDuration.Compute(h3, curve, keys);

            int m = keys.Length;
            int n = 3;
            var A = new double[m, n];
            for (int r = 0; r < m; r++)
            {
                A[r, 0] = k1[r];
                A[r, 1] = k2[r];
                A[r, 2] = k3[r];
            }

            var w = HedgeOptimizer.LeastSquares(A, b, ridgeLambda: 1e-8);

            // Residual r = A w - b
            var rvec = new double[m];
            for (int i = 0; i < m; i++)
            {
                double s = 0.0;
                for (int j = 0; j < n; j++) s += A[i, j] * w[j];
                rvec[i] = s - b[i];
            }

            double norm(double[] v) { double s=0; foreach(var x in v) s += x*x; return Math.Sqrt(s); }

            double rel = norm(rvec) / Math.Max(1e-12, norm(b));

            // With 3 hedgers, residual should be meaningfully smaller than the target norm.
            Assert.True(rel < 0.5, $"Residual too high: {rel:F3}");
        }
    }
}
