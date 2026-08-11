using Modding;
using System;
using System.IO;
using UnityEngine;

namespace ConnectionSettingsRando
{
    public class ConnectionSettingsRando : Mod, IGlobalSettings<CSRSettings> 
    {
        new public string GetName() => "ConnectionSettingsRando";
        public override string GetVersion() => "1.2.0.0";
        public CSRSettings GS { get; internal set; } = new();
        public void OnLoadGlobal(CSRSettings s) => GS = s;
        public CSRSettings OnSaveGlobal() => GS;
        public static readonly string ModDirectory = Path.Combine(Application.persistentDataPath, "ConnectionSettingsRando");
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

            if (ModHooks.GetMod("RandoSettingsManager") is Mod)
                RSM_Interop.Hook();
        }
    }   
}