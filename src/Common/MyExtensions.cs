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

        private static readonly char[] separator = [' '];

        public static TimeSpan ToTimeSpan(this string input)
        {
            //string input = "15m"; // This can be "1d 3h 15m", "3h 15m", "1d 15m", "1d 3h", "3h", "1d", or just "15m"
            string[] parts = input.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            int days = 0, hours = 0, minutes = 0;

            foreach (var part in parts)
            {
                if (part.EndsWith('d'))
                {
                    int.TryParse(part.Replace("d", string.Empty), out days);
                }
                else if (part.EndsWith('h'))
                {
                    int.TryParse(part.Replace("h", string.Empty), out hours);   
                }
                else if (part.EndsWith('n'))
                {
                    int.TryParse(part.Replace("n", string.Empty), out minutes);
                }
            }

            TimeSpan time = new(days, hours, minutes, 0);

            return time;
        }
    }
}
