namespace myenergy.Common
{
    public record BarChartData(int D, double P, double U, double I, bool J, bool S, MeteoStatData MS, bool M, AnomalyData AS);

    public record AnomalyData(double P, double U, double I, bool A);

    public record AnomalyModalData(int Y, int D, AnomalyData A);
}
