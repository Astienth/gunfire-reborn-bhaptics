using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MyBhapticsTactsuit;
using BepInEx.IL2CPP;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Events;

namespace GunfireRebornBhaptics
{
    [BepInPlugin("org.bepinex.plugins.GunfireRebornBhaptics", "GunfireReborn bhaptics integration", "1.0")]
    public class Plugin : BasePlugin
    {
#pragma warning disable CS0109 // Remove unnecessary warning
        internal static new ManualLogSource Log;
#pragma warning restore CS0109
        public static TactsuitVR tactsuitVr; 
        public static bool chargeWeaponCanShoot = false; 
        public static bool continueWeaponCanShoot = false;

        public override void Load()
        {
            // Make my own logger so it can be accessed from the Tactsuit class
            Log = base.Log;
            // Plugin startup logic
            //Logger.LogMessage("Plugin GunfireRebornBhaptics is loaded!");
            tactsuitVr = new TactsuitVR();
            // one startup heartbeat so you know the vest works correctly
            tactsuitVr.PlaybackHaptics("HeartBeat");
            //delay patching
            SceneManager.sceneLoaded += (UnityAction<Scene, LoadSceneMode>)new Action<Scene, LoadSceneMode>(OnSceneLoaded);
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            // patch all functions
            var harmony = new Harmony("bhaptics.patch.GunfireRebornBhaptics");
            harmony.PatchAll();
        }
    }

    #region guns

    /**
     * Many different classes for guns, not a single parent one.
     * TODO : make an associative array with weapon id => tact pattern ?
    */

    [HarmonyPatch(typeof(ASBaseShoot), "OnReload")]
    public class bhaptics_OnReload
    {
        [HarmonyPostfix]
        public static void Postfix(ASBaseShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            if (__instance.ReloadComponent.m_IsReload)
            {
                Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_R");
            }
        }
    }

    /**
     * pistols and derivatives
     */
    [HarmonyPatch(typeof(ASAutoShoot), "AttackOnce")]
    public class bhaptics_OnFireAutoShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASAutoShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R");
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_R");
        }
    }

    /**
     * Single shot weapons (snipers, some bows) arms and vest 
     */
    [HarmonyPatch(typeof(ASSingleShoot), "AttackOnce")]
    public class bhaptics_OnFireSingleShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASSingleShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R");
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_R");
        }
    }

    /**
     * testing for charged attack once (charging vest, charging arm r) 
     */
    [HarmonyPatch(typeof(ASSingleChargeShoot), "ClearChargeAttack")]
    public class bhaptics_OnFireSingleChargeShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASSingleChargeShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("ChargedShotVest");
            Plugin.tactsuitVr.PlaybackHaptics("ChargedShotArm_R");
        }
    }

    /**
     * Charging weapons effect when charging
     */
    [HarmonyPatch(typeof(ASAutoChargeShoot), "ShootCanAttack")]
    public class bhaptics_OnFireAutoChargeShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASAutoChargeShoot __instance, bool __result)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            if (__result)
            {
                Plugin.chargeWeaponCanShoot = true;
                //start thread
                Plugin.tactsuitVr.StartChargingWeapon();
            }
        }
    }

    /**
     * Charging weapons post charging release
     */
    [HarmonyPatch(typeof(ASAutoChargeShoot), "OnUp")]
    public class bhaptics_OnChargingRelease
    {
        [HarmonyPostfix]
        public static void Postfix(ASAutoChargeShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            if (Plugin.chargeWeaponCanShoot)
            {
                Plugin.chargeWeaponCanShoot = false;
                //stop thread
                Plugin.tactsuitVr.StopChargingWeapon();

                Plugin.tactsuitVr.PlaybackHaptics("ChargedShotRelease");
                Plugin.tactsuitVr.PlaybackHaptics("ChargedShotRelease_RT");
            }
        }
    }

    /**
     * Continue shoot weapons when activating
     */
    [HarmonyPatch(typeof(ASContinueShoot), "StartBulletSkill")]
    public class bhaptics_OnContinueShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASContinueShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.continueWeaponCanShoot = true;
            //start thread
            Plugin.tactsuitVr.StartContinueWeapon();
        }
    }

    /**
     * Continue shoot weapons when stop firing
     */
    [HarmonyPatch(typeof(ASContinueShoot), "EndContinueAttack")]
    public class bhaptics_OnContinueStop
    {
        [HarmonyPostfix]
        public static void Postfix(ASContinueShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            if (Plugin.continueWeaponCanShoot)
            {
                Plugin.continueWeaponCanShoot = false;
                //stop thread
                Plugin.tactsuitVr.StopContinueWeapon();
            }
        }
    }

    /**
     * DownUpShoot (Wild hunt) arms and vest 
     */
    [HarmonyPatch(typeof(ASDownUpShoot), "OnUp")]
    public class bhaptics_OnFireDownUpShootUp
    {
        [HarmonyPostfix]
        public static void Postfix(ASDownUpShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }

            Plugin.Log.LogMessage("RELOADED "+ __instance.ReloadComponent.m_IsReload);

            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R");
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_R");
        }
    }

    [HarmonyPatch(typeof(ASDownUpShoot), "OnDown")]
    public class bhaptics_OnFireDownUpShootDown
    {
        [HarmonyPostfix]
        public static void Postfix(ASDownUpShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }

            Plugin.Log.LogMessage("BULLET " + __instance.bulletuse);

            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R");
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_R");
        }
    }

    #endregion

    #region Moves

    /**
     * After jumps when touching floor
     
    [HarmonyPatch(typeof(), "", new Type[] { typeof() })]
    public class bhaptics_OnLanding
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("ChargedShotRelease");
        }
    }
    */

    #endregion

    #region Health and shield

    /**
     * When Shield breaks
     */
    [HarmonyPatch(typeof(BoltBehavior.CAction46), "Action")]
    public class bhaptics_OnShieldBreak
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            Plugin.tactsuitVr.PlaybackHaptics("ShieldBreak");
        }
    }

    /**
     * Can't find hit transform object, using static class
     * as an gydrator or factory of some sort in the original code, ugly
     */
    [HarmonyPatch(typeof(HeroBeHitCtrl), "HeroInjured")]
    public class bhaptics_OnInjured
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Impact");
        }
    }

    #endregion

    #region

    /**
     * DEBUG
     */
    [HarmonyPatch(typeof(Bhaptics.Tact.HapticPlayer), "TurnOff", new Type[] { typeof(string) })]
    public class bhaptics_OnDebug
    {
        [HarmonyPostfix]
        public static void Postfix(Bhaptics.Tact.HapticPlayer __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }

            /**
            PlayerProp playerProp = NewObjectCache.GetPlayerProp(HeroBeHitCtrl.HeroID);

            Plugin.Log.LogMessage(" SHIELD MAX " + playerProp.ShieldMax);
            Plugin.Log.LogMessage(" NAME " + playerProp.Name);
            Plugin.Log.LogMessage(" HP " + playerProp.HP);
            Plugin.Log.LogMessage(" HPMAX " + playerProp.HPMax);
            Plugin.Log.LogMessage(" ARMOR " + playerProp.Armor);
            Plugin.Log.LogMessage(" ARMORMAX " + playerProp.ArmorMax);
            Plugin.Log.LogMessage(" SHield " + playerProp.Shield);
            Plugin.Log.LogMessage(" STATUS " + playerProp.ShieldStatus);
            Plugin.Log.LogMessage(" BREAK TIMER " + HeroBeHitCtrl.breakTimer);
            */

            Plugin.Log.LogMessage(
                string.Join(" ",
                string.Join(" ", Traverse.Create(__instance).Field("_activeKeys").GetValue())));
        }
    }


    #endregion
}

