namespace HedgeLab.Curves
{
    /// <summary>
    /// Thin wrapper so you can later swap NSS for splines/PCHIP without changing callers.
    /// </summary>
    public sealed class ZeroCurve
    {
        private readonly NelsonSiegelSvensson _nss;

        public ZeroCurve(NelsonSiegelSvensson nss) => _nss = nss;

        public double ZeroRate(double tYears) => _nss.ZeroRate(tYears);
        public double DiscountFactor(double tYears) => _nss.DiscountFactor(tYears);
    }
}
