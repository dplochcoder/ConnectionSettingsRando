using System;

namespace ConnectionSettingsRando
{
    public interface ISettingsProvider
    {
        string Name { get; }
        Type SettingsType { get; }
        object GetSettings();
        void OverrideSettings(object settings);
    }

    internal class SettingsProvider<T> : ISettingsProvider
        where T : new()
    {
        public string Name { get; }
        public Type SettingsType => typeof(T);
        private readonly Func<T> getter;
        private readonly Action<T> apply;
        public SettingsProvider(
            string name,
            Func<T> getter,
            Action<T> apply)
        {
            Name = name;
            this.getter = getter;
            this.apply = apply;
        }

        public object GetSettings()
        {
            return getter();
        }

        public void OverrideSettings(object settings)
        {
            apply((T)settings);
        }
    }
}