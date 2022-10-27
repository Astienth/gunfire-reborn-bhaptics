﻿using BepInEx;
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

        public static int heroId = 0;

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

        public static int getHeroId()
        {
            if (heroId == 0)
            {
                heroId = HeroCtrlMgr.GetHeroID();
            }
            return heroId;
        }

        public static string getHandSide(int weaponId)
        {
            //dog case
            if (getHeroId() == 268437476)
            {
                NewPlayerObject heroObj = HeroAttackCtrl.HeroObj;
                return (weaponId == heroObj.PlayerCom.CurWeaponID) ? "R" : "L";
            }
            return "R";
        }
    }

    #region guns

    /**
     * Many different classes for guns, not a single parent one.
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
                Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_" + Plugin.getHandSide(__instance.ItemID));
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
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_" + Plugin.getHandSide(__instance.ItemID));
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_" + Plugin.getHandSide(__instance.ItemID));
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
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_" + Plugin.getHandSide(__instance.ItemID));
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_" + Plugin.getHandSide(__instance.ItemID));
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
            Plugin.tactsuitVr.PlaybackHaptics("ChargedShotVest_" + Plugin.getHandSide(__instance.ItemID));
            Plugin.tactsuitVr.PlaybackHaptics("ChargedShotArm_" + Plugin.getHandSide(__instance.ItemID));
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
                Plugin.tactsuitVr.StartChargingWeapon(Plugin.getHandSide(__instance.ItemID));
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
                Plugin.tactsuitVr.StopChargingWeapon(Plugin.getHandSide(__instance.ItemID));

                Plugin.tactsuitVr.PlaybackHaptics("ChargedShotRelease_" + Plugin.getHandSide(__instance.ItemID));
                Plugin.tactsuitVr.PlaybackHaptics("ChargedShotReleaseArms_" + Plugin.getHandSide(__instance.ItemID));
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
            Plugin.tactsuitVr.StartContinueWeapon(Plugin.getHandSide(__instance.ItemID));
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
                Plugin.tactsuitVr.StopContinueWeapon(Plugin.getHandSide(__instance.ItemID));
            }
        }
    }

    /**
     * DownUpShoot (Wild hunt) arms and vest 
     * using ASBaseShoot.StartBulletSkill to cover only the wildhunt itemID == 995
     */
    [HarmonyPatch(typeof(ASBaseShoot), "StartBulletSkill")]
    public class bhaptics_OnFireDownUpShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASBaseShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            if (__instance.ItemID == 995)
            {
                Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_" + Plugin.getHandSide(__instance.ItemID));
                Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_" + Plugin.getHandSide(__instance.ItemID));
            }
        }
    }
    
    // this method will activate feedback only when cloud weaver
    // transitions from 1 sword held in hand (inactive state/entering new
    // zones or switching to cloud weaver) to active state in which the 5
    // cloud weaver swords begin spinng around the wrist,
    // this can only be activated by initiating sword spinning, it will
    // not activate again until cloud weaver is inactive (new zone or switching) 
    [HarmonyPatch(typeof(ASFlyswordShoot), "StartBulletSkill")]
    public class bhaptics_OnFireFlySwordStart
    {
        [HarmonyPostfix]
        public static void Postfix(ASFlyswordShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.StartCloudWeaver(Plugin.getHandSide(__instance.ItemID));
        }
    }

    /**
     * LANCE : Might not be enough and not covering
     * when downed, when ui on (scrolls, pause menu, etc)
     */
    [HarmonyPatch(typeof(ASFlyswordShoot), "Destroy")]
    public class bhaptics_OnFireFlySwordStopHaptics
    {
        [HarmonyPostfix]
        public static void Postfix(ASFlyswordShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.StopCloudWeaver(Plugin.getHandSide(__instance.ItemID));
        }
    }

    // this method will activate feedback only while cloud weaver is
    // actively hitting enemies, does not activate from any button presses,
    // may be ideal to change from flyswordvest and flyswordarmwristspinning
    // to recoil variants
    [HarmonyPatch(typeof(ASFlyswordShoot), "FlyswordOnDown")]
    public class bhaptics_OnFireFlySwordOnDown
    {
        [HarmonyPostfix]
        public static void Postfix(ASFlyswordShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_" + Plugin.getHandSide(__instance.ItemID));
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_" + Plugin.getHandSide(__instance.ItemID));
        }
    }


    /* this method will activate haptic feedback "heartbeat" at every down press 
     * of Right trigger only.  This will activate without exception at every down 
     * press of right trigger including while there are no enemies present or with 
     * enemies present and being attacked.
    [HarmonyPatch(typeof(ASFlyswordShoot), "OnDown")]
    public class bhaptics_OnFireFSOnDown
    {
        [HarmonyPostfix]
        public static void Postfix(ASFlyswordShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.Log.LogMessage("Cloud Weaver FSOnDown");
            Plugin.tactsuitVr.PlaybackHaptics("HeartBeat");
            
        }
    }
    


    /*
     * this method activates haptic feedback, "heal" at every "on up" or 
     * release after pressing of buttons Y, X, and Right trigger only. 
     * This will persistently activate, even in menus.
    [HarmonyPatch(typeof(ASFlyswordShoot), "OnUp")]
    public class bhaptics_OnFireFSOnUp
    {
        [HarmonyPostfix]
        public static void Postfix(ASFlyswordShoot __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled || __instance == null)
            {
                return;
            }
            Plugin.Log.LogMessage("Cloud Weaver FSOnUp");
            Plugin.tactsuitVr.PlaybackHaptics("Heal");

        }
    }
    */

    #endregion

    #region Primary skills (furies)

    [HarmonyPatch(typeof(HeroAttackCtrl), "OnButtonUpActiveSkills")]
    public class bhaptics_OnPrimarySkill
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            //heroIds switch cases
            switch (Plugin.heroId)
            {
                //dog
                case 268437476:
                    Plugin.tactsuitVr.PlaybackHaptics("Heal");
                    break;
                default:
                    return;
            }
        }
    }

    #endregion

    #region Secondary skills (grenades)

    #endregion

    #region Moves

    /**
     * OnJumping
     */
    [HarmonyPatch(typeof(HeroMoveState.HeroMoveMotor), "OnJump")]
    public class bhaptics_OnJumping
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("OnJump", true, 0.5f);
        }
    }

    /**
     * After jumps when touching floor
     */
    [HarmonyPatch(typeof(HeroMoveManager), "OnLand")]
    public class bhaptics_OnLanding
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("LandAfterJump", true, 0.3f);
        }
    }
    
    /**
     * On Dashing
     */
    [HarmonyPatch(typeof(SkillBolt.CAction1310), "Action")]
    public class bhaptics_OnDashing
    {
        [HarmonyPostfix]
        public static void Postfix(SkillBolt.CSkillBase skill)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            if (SkillBolt.CServerArg.IsHeroCtrl(skill))
            {
                Plugin.tactsuitVr.PlaybackHaptics("Dash");
            }
        }
    }

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
     * When low health starts
     */
    [HarmonyPatch(typeof(HeroBeHitCtrl), "PlayLowHpAndShield")]
    public class bhaptics_OnLowHealthStart
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            if (HeroBeHitCtrl.NearlyDeadAction != -1)
            {
                Plugin.tactsuitVr.StartHeartBeat();
            }
        }
    }
    
    /**
     * When low hp stops
     */
    [HarmonyPatch(typeof(HeroBeHitCtrl), "DelLowHpAndShield")]
    public class bhaptics_OnLowHealthStop
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopHeartBeat();
        }
    }

    /**
     * Can't find hit transform object, using static class
     * as an gydrator or factory of some sort in the original code, ugly
     * 
     * Use this function as well for geros with armor instead of shield
     * 
     * Death effect
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
            //armor break for heros with armor and no shield
            PlayerProp playerProp = NewObjectCache.GetPlayerProp(HeroBeHitCtrl.HeroID);
            if (playerProp.ArmorMax > 0 &&  playerProp.Armor <= 0)
            {
                Plugin.tactsuitVr.PlaybackHaptics("ShieldBreak");
            }
            //death
            if (playerProp.HP <= 0)
            {
               
                Plugin.tactsuitVr.PlaybackHaptics("Death");
                Plugin.tactsuitVr.StopThreads();
                Plugin.tactsuitVr.StartHeartBeat();
            }
        }
    }

    /**
     * When player gives up after death
     */
    [HarmonyPatch(typeof(UIScript.PCResurgencePanel_Logic), "GiveUp")]
    public class bhaptics_OnGiveUp
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopHeartBeat();
        }
    }

    /**
     * When player is NOT back to life, stop heartbeat
     */
    [HarmonyPatch(typeof(SalvationManager), "AskEnterWatch")]
    public class bhaptics_OnNotRevived
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopHeartBeat();
        }
    }

    /**
     * When player is back to life, stop heartbeat
     */
    [HarmonyPatch(typeof(NewPlayerManager), "PlayerRelife")]
    public class bhaptics_OnBackToLife
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopHeartBeat();
        }
    }

    /**
     * When healing item used
     */
    [HarmonyPatch(typeof(BoltBehavior.CAction1069), "Action")]
    public class bhaptics_OnHealing
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Heal");
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

