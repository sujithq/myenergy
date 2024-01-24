using myenergy.Common.Extensions;
using NodaTime;
using System.Net.Http.Json;

namespace myenergy.Common.Services
{
    public class DataService : IDataService
    {
        static LocalDateTime zonedDateTimeBrussels = MyExtensions.BelgiumTime();

        public event Action OnDataChanged;
        HttpClient Http;

        public Dictionary<int, List<BarChartData>> Data { get ; private set ; }

        public DataService(HttpClient Http)
        {
            this.Http = Http;
        }

        public async Task LoadDataAsync()
        {
            // Fetch data from API
            Data = (await Http.GetFromJsonAsync<Dictionary<int, List<BarChartData>>>($"https://raw.githubusercontent.com/sujithq/myenergy/main/src/myenergy/wwwroot/Data/data.json?v{zonedDateTimeBrussels.TickOfSecond}"))!;

            // Notify subscribers that data has changed
            OnDataChanged?.Invoke();
        }



    }
}
