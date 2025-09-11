using System;

namespace HedgeLab.Hedge
{
    /// <summary>
    /// Solve min_w ||A w - b||_2 with optional ridge regularization.
    /// A: (m x n) matrix of hedger KRDs (rows = keys, cols = instruments)
    /// b: (m)   target KRD vector
    /// Returns weights w (n).
    /// 
    /// Implementation uses normal equations (A^T A + λI) w = A^T b
    /// with a tiny ridge (λ) for numerical stability, solved by
    /// Gaussian elimination with partial pivoting.
    /// </summary>
    public static class HedgeOptimizer
    {
        public static double[] LeastSquares(double[,] A, double[] b, double ridgeLambda = 1e-8)
        {
            int m = A.GetLength(0);
            int n = A.GetLength(1);
            if (b.Length != m) throw new ArgumentException("b length must match A rows.");

            // Build AtA and Atb
            var AtA = new double[n, n];
            var Atb = new double[n];

            for (int i = 0; i < n; i++)
            {
                for (int j = i; j < n; j++)
                {
                    double sum = 0.0;
                    for (int r = 0; r < m; r++)
                        sum += A[r, i] * A[r, j];
                    AtA[i, j] = AtA[j, i] = sum;
                }
                AtA[i, i] += ridgeLambda; // ridge
                double s = 0.0;
                for (int r = 0; r < m; r++)
                    s += A[r, i] * b[r];
                Atb[i] = s;
            }

            return SolveLinearSystem(AtA, Atb);
        }

        /// <summary> Classic Gaussian elimination with partial pivoting. </summary>
        private static double[] SolveLinearSystem(double[,] M, double[] y)
        {
            int n = y.Length;

            // Augmented matrix
            var A = new double[n, n + 1];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++) A[i, j] = M[i, j];
                A[i, n] = y[i];
            }

            // Forward elimination with pivoting
            for (int k = 0; k < n; k++)
            {
                // Find pivot
                int piv = k;
                double max = Math.Abs(A[k, k]);
                for (int i = k + 1; i < n; i++)
                {
                    double v = Math.Abs(A[i, k]);
                    if (v > max) { max = v; piv = i; }
                }
                if (max < 1e-15) throw new InvalidOperationException("Matrix is singular or near-singular.");

                // Swap rows
                if (piv != k)
                {
                    for (int j = k; j <= n; j++)
                    {
                        double tmp = A[k, j];
                        A[k, j] = A[piv, j];
                        A[piv, j] = tmp;
                    }
                }

                // Normalize & eliminate
                double diag = A[k, k];
                for (int j = k; j <= n; j++) A[k, j] /= diag;

                for (int i = 0; i < n; i++)
                {
                    if (i == k) continue;
                    double factor = A[i, k];
                    if (Math.Abs(factor) < 1e-18) continue;
                    for (int j = k; j <= n; j++)
                        A[i, j] -= factor * A[k, j];
                }
            }

            // Extract solution
            var x = new double[n];
            for (int i = 0; i < n; i++)
                x[i] = A[i, n];
            return x;
        }
    }
}
