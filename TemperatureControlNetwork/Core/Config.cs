namespace TemperatureControlNetwork.Core
{
    public static class Config
    {
        public const int NumberOfWorkers = 10;
        public const double StartingTemperature = 20.0;

        public const int WorkerLoopDelay = 50;
        public const int WorkerStreamDelay = 10;
        public const int CoordinatorLoopDelay = 100;

        public const double MinTemperature = 10.0;
        public const double MaxTemperature = 30.0;
        public const double MaxAdjustment = 0.5;

        public const double LowTemperature = 15.0;
        public const double HighTemperature = 25.0;
    }
}