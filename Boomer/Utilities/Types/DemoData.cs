namespace Boomer
{
    public class DemoData
    {
        public int Boost { get; set; }
        public int BoostUsed { get; set; }
        public int DemoCount { get; set; }
        public int DeathCount { get; set; }
        public int MissCount { get; set; }
        public int AvoidCount { get; set; }

        public double DemoChance { get; set; }
        public const double MinDemoChance = 0.3;
        public const double MaxDemoChance = 0.7;

        public double AvoidChance { get; set; }
        public const double MinAvoidChance = 0.3;
        public const double MaxAvoidChance = 0.7;

        public DemoData(int boost, int demoCount, int deathCount, double demoChance, double avoidChance, int avoidCount, int missCount)
        {
            Boost = Util.Clamp(boost, 0, 100);

            DemoCount = Util.Clamp(demoCount, 0, int.MaxValue);
            DeathCount = Util.Clamp(deathCount, 0, int.MaxValue);

            DemoChance = Util.Clamp(demoChance, MinDemoChance, MaxDemoChance);
            AvoidChance = Util.Clamp(avoidChance, MinAvoidChance, MaxAvoidChance);

            MissCount = Util.Clamp(missCount, 0, int.MaxValue);
            AvoidCount = Util.Clamp(avoidCount, 0, int.MaxValue);
        }
    }
}
