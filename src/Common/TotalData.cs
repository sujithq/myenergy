namespace myenergy.Common
{
    public record TotalData(double TotalProduction, double TotalNetInjection, double TotalNetUsage, double TotalUsage, double TotalSolarUsage);
    public record Consolidated(double P, double U, double I);
}
