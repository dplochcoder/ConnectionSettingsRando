using RandoSettingsManager;
using RandoSettingsManager.SettingsManagement;
using RandoSettingsManager.SettingsManagement.Versioning;

namespace ConnectionSettingsRando
{
    internal static class RSM_Interop
    {
        public static void Hook()
        {
            RandoSettingsManagerMod.Instance.RegisterConnection(new AccessSettingsProxy());
        }
    }

    internal class AccessSettingsProxy : RandoSettingsProxy<CSRSettings, string>
    {
        public override string ModKey => ConnectionSettingsRando.Instance.GetName();

        public override VersioningPolicy<string> VersioningPolicy { get; }
            = new EqualityVersioningPolicy<string>(ConnectionSettingsRando.Instance.GetVersion());

        public override void ReceiveSettings(CSRSettings settings)
        {
            if (settings != null)
            {
                ConnectionMenu.Instance!.Apply(settings);
            }
            else
            {
                ConnectionMenu.Instance!.Disable();
            }
        }

        public override bool TryProvideSettings(out CSRSettings settings)
        {
            settings = RandoInterop.Settings;
            return settings.Enabled;
        }
    }
}