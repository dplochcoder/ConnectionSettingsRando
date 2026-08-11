using System;
using System.Collections.Generic;
using System.Configuration.Provider;
using System.Reflection;

namespace ConnectionSettingsRando
{
    public class RandomizationPipeline
    {
        private static readonly SettingsRandomizer randomizer = new();
        private readonly Dictionary<Type, MethodInfo> randomizeMethods = [];
        private MethodInfo GetRandomizeMethod(Type settingsType)
        {
            if (!randomizeMethods.TryGetValue(settingsType, out MethodInfo method))
            {
                method = typeof(SettingsRandomizer)
                    .GetMethod(nameof(SettingsRandomizer.Randomize))!
                    .MakeGenericMethod(settingsType);
                randomizeMethods[settingsType] = method;
            }

            return method;
        }
        private object Randomize(object settings, Type settingsType, Random rng, string providerName)
        {
            MethodInfo method = GetRandomizeMethod(settingsType);
            return method.Invoke(randomizer, [settings, rng, providerName])!;
        }
        public void RandomizeAll(Random rng)
        {
            foreach (ISettingsProvider provider in ConnectionsRegistry.Providers)
            {
                if (!RandoInterop.Settings.RandomizedSettings.Contains(provider.Name))
                    continue;

                try
                {
                    provider.OverrideSettings(
                        Randomize(
                            provider.GetSettings(),
                            provider.SettingsType,
                            rng,
                            provider.Name));
                    RandomizationStats stats = randomizer.LastStats;
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