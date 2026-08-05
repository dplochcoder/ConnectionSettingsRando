using System.Collections.Generic;

namespace ConnectionSettingsRando
{
    public static class ConnectionsRegistry
    {
        private static readonly Dictionary<string, ISettingsProvider> connections = new();

        public static void Register(ISettingsProvider provider)
        {
            connections[provider.Name] = provider;
        }

        public static IEnumerable<ISettingsProvider> Providers
            => connections.Values;

        public static bool TryGetProvider(string name, out ISettingsProvider provider)
        {
            return connections.TryGetValue(name, out provider);
        }

        public static void Clear()
        {
            connections.Clear();
        }
    }
}