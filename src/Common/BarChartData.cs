namespace myenergy.Common
{
    public record BarChartData(int D, double P, double U, double I, bool J, bool S, MeteoStatData MS, bool M);
    public record MeteoStatData(double tavg, double tmin, double tmax, double prcp, double snow, double wdir, double wspd, double wpgt, double pres, double tsun);
}
