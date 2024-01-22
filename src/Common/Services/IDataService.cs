using myenergy.Common;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace myenergy.Common.Services
{
    public interface IDataService
    {
        Task LoadDataAsync();
        Dictionary<int, List<BarChartData>> Data { get; }
        event Action OnDataChanged;
    }
}
