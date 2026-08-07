using System.Collections.Generic;
using MenuChanger.Attributes;

namespace ConnectionSettingsRando {
    public class CSRSettings {
        public bool Enabled = false;
        public bool IncludeBooleans = false;
        public bool IncludeCategorical = false;
        public bool IncludeNumeric = false;
        [MenuRange(0f, 1f)]
        public float SettingOdds = 0.5f;
        public List<string> RandomizedSettings = new();
    }
}