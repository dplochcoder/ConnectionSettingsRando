using RandomizerMod.Logging;
using RandomizerMod.RC;

namespace ConnectionSettingsRando
{
    internal static class RandoInterop
    {
        public static CSRSettings Settings => ConnectionSettingsRando.Instance.GS;
        public static OptOutManager OptOutManager { get; } = new();
        public static void Hook()
        {
            ConnectionMenu.Hook();
            OptOutManager.Load();
            SettingsLog.AfterLogSettings += AddFileSettings;
            RandoController.OnBeginRun += Execute;
        }

        private static void Execute(RandoController rc)
        {
            if (Settings.Enabled)
                CSR.RandomizeAll(rc.rng);
        }

        private static void AddFileSettings(LogArguments args, System.IO.TextWriter tw)
        {
            if (!Settings.Enabled)
                return;

            // Log settings into the settings file
            tw.WriteLine("ConnectionSettingsRandomizer was enabled.");
        }        
    }
}