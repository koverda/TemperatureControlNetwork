namespace TemperatureControlNetwork.Core
{
    public class TemperatureData
    {
        public int WorkerId { get; set; }
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
    }
}
