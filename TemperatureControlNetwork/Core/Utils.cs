namespace TemperatureControlNetwork.Core
{
    public static class Utils
    {
        private static readonly Random Random = new();

        /// <summary>
        /// Adjusts the temperature by a small random value within the specified range.
        /// </summary>
        /// <param name="temperature">The original temperature value.</param>
        /// <param name="maxAdjustment">The maximum adjustment value.</param>
        /// <returns>The adjusted temperature value.</returns>
        public static double AdjustTemperature(double temperature, double maxAdjustment = .1)
        {
            // Generate a random adjustment value between -maxAdjustment and +maxAdjustment
            double adjustment = (Random.NextDouble() * 2 - 1) * maxAdjustment;
            return temperature + adjustment;
        }
    }
}
