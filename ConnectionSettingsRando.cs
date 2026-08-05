using Modding;
using System;

namespace ConnectionSettingsRando
{
    public class ConnectionSettingsRando : Mod, IGlobalSettings<CSRSettings> 
    {
        new public string GetName() => "ConnectionSettingsRando";
        public override string GetVersion() => "1.0.0.0";
        public CSRSettings GS { get; internal set; } = new();
        public void OnLoadGlobal(CSRSettings s) => GS = s;
        public CSRSettings OnSaveGlobal() => GS;
        private static ConnectionSettingsRando _instance;
        public ConnectionSettingsRando() : base()
        {
            _instance = this;
        }
        internal static ConnectionSettingsRando Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"{nameof(ConnectionSettingsRando)} was never initialized");
                }
                return _instance;
            }
        }
        public override void Initialize()
        {
            Log("Initializing Mod...");
            RandoInterop.Hook();
        }
    }   
}