using System;

namespace ConnectionSettingsRando
{
    public class RandomizationPipeline
    {
        private static readonly SettingsRandomizer randomizer = new();

        public void RandomizeAll(Random rng)
        {
            foreach (ISettingsProvider provider in ConnectionsRegistry.Providers)
            {
                if (!RandoInterop.Settings.RandomizedSettings.Contains(provider.Name))
                    continue;

                try
                {
                    RandomizationStats stats = provider.Randomize(rng);
                    ConnectionSettingsRando.Instance.Log(
                    $"{provider.Name}: " +
                    $"{stats.RandomizedCount} settings randomized, " +
                    $"{stats.EnforcedCount} settings enforced, " +
                    $"{stats.SkippedCount} skipped");
                }
                catch (Exception ex)
                {
                    ConnectionSettingsRando.Instance.LogError(
                        $"Failed to randomize {provider.Name}: {ex}");
                }
            }
        }
    }
}