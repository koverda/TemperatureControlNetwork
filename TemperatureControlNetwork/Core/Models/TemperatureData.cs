namespace TemperatureControlNetwork.Core.Models
{
    public class TemperatureData
    {
        public int WorkerId { get; init; }
        public DateTime Timestamp { get; init; }
        public double Temperature { get; init; }
    }
}
