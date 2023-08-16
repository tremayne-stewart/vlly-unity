using UnityEngine;

namespace vlly
{
    public static partial class Vlly {
        public static void Log(string s) {
            if (Config.ShowDebug) {
                Debug.Log("[Vlly] " + s);
            }
        }

        public static void LogError(string s) {
            if (Config.ShowDebug) {
                Debug.LogError("[Vlly] " + s);
            }
        }
    }
}
