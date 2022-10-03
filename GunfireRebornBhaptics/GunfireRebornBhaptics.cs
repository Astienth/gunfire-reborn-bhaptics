using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MyBhapticsTactsuit;
using BepInEx.IL2CPP;


namespace GunfireRebornBhaptics
{
    [BepInPlugin("org.bepinex.plugins.GunfireRebornBhaptics", "GunfireReborn bhaptics integration", "1.0")]
    public class Plugin : BasePlugin
    {
#pragma warning disable CS0109 // Remove unnecessary warning
        internal static new ManualLogSource Log;
#pragma warning restore CS0109
        public static TactsuitVR tactsuitVr;


        public override void Load()
        {
            // Make my own logger so it can be accessed from the Tactsuit class
            Log = base.Log;
            // Plugin startup logic
            //Logger.LogMessage("Plugin GunfireRebornBhaptics is loaded!");
            tactsuitVr = new TactsuitVR();
            // one startup heartbeat so you know the vest works correctly
            tactsuitVr.PlaybackHaptics("HeartBeat");
            // patch all functions
            var harmony = new Harmony("bhaptics.patch.GunfireRebornBhaptics");
            harmony.PatchAll();
        }
    }

    /**[HarmonyPatch(typeof(Food), "OnEat", new Type[] { })]
    public class bhaptics_OnEat
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Eating");
        }
    }
    **/
}

