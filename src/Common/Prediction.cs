using Microsoft.ML.Data;

namespace myenergy.Common
{
    public class Prediction
    {
        // Properties from AnomalyRecord
        public int Y { get; set; }
        public int D { get; set; }

        [VectorType(3)]
        public double[]? Scores { get; set; }
    }

    public class PredictionQuarter
    {
        // Properties from AnomalyRecord
        public int Y { get; set; }
        public int D { get; set; }
        //public DateTime DT { get; set; }
        public int IDX { get; set; }
        public string T { get; set; }



        [VectorType(3)]
        public double[]? Scores { get; set; }
    }
}
