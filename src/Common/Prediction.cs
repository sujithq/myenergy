using Microsoft.ML.Data;

namespace myenergy.Common
{
    public class Prediction
    {
        // Properties from AnomalyRecord
        public int Y { get; set; }
        public int D { get; set; }
        public float P { get; set; }

        [VectorType(3)]
        public double[]? Scores { get; set; }
    }
}
