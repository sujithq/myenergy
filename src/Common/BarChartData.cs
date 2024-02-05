namespace myenergy.Common
{
    public record BarChartData(int D, double P, double U, double I, bool J, bool S, MeteoStatData MS, bool M, AnomalyData AS, QuarterData Q, bool C = false, SunRiseSet SRS = default! );

    public record AnomalyData(double P, double U, double I, bool A);

    public record AnomalyModalData(int Y, int D, AnomalyData A);

    public record QuarterData(List<double> C, List<double> I, List<double> G, List<double> P);

    public record SunRiseSet(TimeOnly R, TimeOnly S);
}
