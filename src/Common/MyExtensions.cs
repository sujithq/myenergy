using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myenergy.Common.Extensions
{
    public static class MyExtensions
    {
        public static LocalDate DayOfYearLocalDate(this int doy, int year)
        {
            return new LocalDate(year, 1, 1).PlusDays(doy - 1);
        }

        public static LocalDateTime BelgiumTime()
        {
            return SystemClock.Instance.GetCurrentInstant().InZone(DateTimeZoneProviders.Tzdb["Europe/Brussels"]).LocalDateTime;
        }
    }
}
