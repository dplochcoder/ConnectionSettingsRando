using System;

namespace ConnectionSettingsRando
{
    public static class CSR
    {
        private static readonly RandomizationPipeline pipeline = new();
        public static void Register<T>(
            string name,
            Func<T> getter,
            Action<T> overrideSettings)
            where T : new()
        {
            ConnectionsRegistry.Register(
                new SettingsProvider<T>(
                    name,
                    getter,
                    overrideSettings));
        }
        public static void RandomizeAll(Random rng)
        {
            pipeline.RandomizeAll(rng);
        }
    }
    
}