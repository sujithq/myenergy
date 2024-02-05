namespace myenergy.Common
{
    public record AnomalyRecord(int Y, int D, float P, float U, float I, float tavg, float tmin, float tmax, float prcp, float snow, float wdir, float wspd, float wpgt, float pres, float tsun, bool C);
    public record AnomalyQuarterRecord(int Y, int D, float P, float U, float I, float tavg, float tmin, float tmax, float prcp, float snow, float wdir, float wspd, float wpgt, float pres, float tsun, bool C, float VV, int IDX, string T, TimeOnly SR, TimeOnly SS);
}
