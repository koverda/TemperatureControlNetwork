namespace TemperatureControlNetwork.Core
{
    public class TemperatureData
    {
        public int WorkerId { get; init; }
        public DateTime Timestamp { get; init; }
        public double Temperature { get; init; }
    }
}
