namespace myenergy.Common
{
    public record BarChartData(int D, double P, double U, double I, bool J, bool S, MeteoStatData MS, bool M, AnomalyData AS, QuarterData2 Q);

    public record AnomalyData(double P, double U, double I, bool A);

    public record AnomalyModalData(int Y, int D, AnomalyData A);

    public record QuarterData(List<Coordinates> C, List<Coordinates> I, List<Coordinates> G, List<Coordinates> P);
    public record Coordinates(double y);
    public record QuarterData2(List<double> C, List<double> I, List<double> G, List<double> P);
}
