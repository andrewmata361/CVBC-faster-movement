using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using MotherBase.EntityFramework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace RoseSpeedMod
{
    [BepInPlugin("local.rose.speed", "Rose Speed", "1.3.0")]
    public class RoseSpeedPlugin : BasePlugin
    {
        public static ManualLogSource Logger;
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<float> Multiplier;
        public static ConfigEntry<bool> AffectAir;
        public static ConfigEntry<string> ToggleKey;

        public static string Toast = "";
        public static float ToastUntil;
        public static bool GuiBroken;

        private static bool _loggedBoost;
        private static bool _loggedFailure;
        private static float _baseAnimSpeed = -1f;
        private static bool _loggedAnim;
        private static bool _loggedAnimFail;
        private static CharacterMovementState _movementState;
        private static float _savedLocomotionSpeed;
        private static bool _restoreLocomotionSpeed;

        public override void Load()
        {
            Logger = Log;
            Enabled = Config.Bind("Speed", "Enabled", true, "Multiply Rose's ground run speed.");
            Multiplier = Config.Bind("Speed", "Multiplier", 1.5f, "How much faster Rose runs. 1.5 is 50% above the base game. Values are clamped from 0.5 to 3.");
            AffectAir = Config.Bind("Speed", "AffectAir", false, "Also multiply airborne control. Off keeps jumps and air drift at the base game speed.");
            ToggleKey = Config.Bind("Speed", "ToggleKey", "F4", "Keyboard key that turns the speed change on and off while playing.");

            Harmony harmony = new Harmony("local.rose.speed");
            MethodInfo movement = AccessTools.Method(typeof(McMovementFeature), "GetMovementSpeed");
            if (movement == null)
                Log.LogError("McMovementFeature.GetMovementSpeed was not found. Rose's speed was not changed.");
            else
            {
                harmony.Patch(movement, postfix: new HarmonyMethod(typeof(RoseSpeedPlugin).GetMethod("BoostSpeed")));
                Log.LogInfo("Patched " + movement.DeclaringType.FullName + "." + movement.Name);
            }

            MethodInfo animation = AccessTools.Method(typeof(MCMovementAnimation.Instance), "Update");
            if (animation == null)
                Log.LogError("MCMovementAnimation.Instance.Update was not found. Run animation was not changed.");
            else
            {
                harmony.Patch(
                    animation,
                    prefix: new HarmonyMethod(typeof(RoseSpeedPlugin).GetMethod("BeforeRunAnimation")),
                    postfix: new HarmonyMethod(typeof(RoseSpeedPlugin).GetMethod("AfterRunAnimation")));
                Log.LogInfo("Patched " + animation.DeclaringType.FullName + "." + animation.Name);
            }

            ClassInjector.RegisterTypeInIl2Cpp<RoseSpeedBehaviour>();
            AddComponent<RoseSpeedBehaviour>();
            Log.LogInfo("Rose Speed 1.3.0 loaded. " + ToggleKey.Value + " toggles it. Ground multiplier " + Multiplier.Value.ToString("0.00") + ".");
        }

        public static float CurrentScale()
        {
            if (Enabled == null || !Enabled.Value)
                return 1f;
            float scale = Multiplier.Value;
            if (scale < 0.5f) scale = 0.5f;
            if (scale > 3f) scale = 3f;
            return scale;
        }

        public static void BoostSpeed(CharacterMovementState characterMovement, ref float __result)
        {
            try
            {
                if (Enabled == null || !Enabled.Value || characterMovement == null)
                    return;

                if (!AffectAir.Value && characterMovement.locomotionState == CharacterMovementState.LocomotionState.Air)
                    return;

                float scale = CurrentScale();
                if (Math.Abs(scale - 1f) < 0.001f)
                    return;

                __result *= scale;

                if (!_loggedBoost && __result > 0.1f && characterMovement.locomotionState == CharacterMovementState.LocomotionState.GroundMove)
                {
                    _loggedBoost = true;
                    Logger.LogInfo("Ground run boosted. Sample speed after multiplier: " + __result.ToString("0.00"));
                }
            }
            catch (Exception ex)
            {
                if (_loggedFailure)
                    return;
                _loggedFailure = true;
                Logger.LogWarning("Rose speed hook failed and left this frame unchanged: " + ex.Message);
            }
        }

        public static void BeforeRunAnimation(Entity e, MCMovementAnimation.Instance __instance)
        {
            _restoreLocomotionSpeed = false;
            try
            {
                if (__instance == null)
                    return;

                if (_baseAnimSpeed < 0f && __instance._animSpeed >= 0.05f)
                    _baseAnimSpeed = __instance._animSpeed;

                float authored = _baseAnimSpeed >= 0.05f ? _baseAnimSpeed : 1f;
                float scale = CurrentScale();
                bool boost = scale > 1.001f;
                __instance._animSpeed = authored;

                if (!boost || e == null)
                    return;

                Component component = e.GetComponentData(Il2CppType.Of<CharacterMovementState>());
                _movementState = component == null ? null : component.Cast<CharacterMovementState>();
                if (_movementState == null)
                    return;

                _savedLocomotionSpeed = _movementState.locomotionSpeed;
                _movementState.locomotionSpeed = _savedLocomotionSpeed / scale;
                _restoreLocomotionSpeed = true;
                __instance._animSpeed = authored * scale;

                if (!_loggedAnim)
                {
                    _loggedAnim = true;
                    Logger.LogInfo("Run cycle kept on the normal clip and played at " + scale.ToString("0.00") + "x so footsteps keep looping.");
                }
            }
            catch (Exception ex)
            {
                if (_loggedAnimFail)
                    return;
                _loggedAnimFail = true;
                Logger.LogWarning("Run animation match failed: " + ex.Message);
            }
        }

        public static void AfterRunAnimation()
        {
            if (!_restoreLocomotionSpeed || _movementState == null)
                return;
            _movementState.locomotionSpeed = _savedLocomotionSpeed;
            _restoreLocomotionSpeed = false;
        }
    }

    public class RoseSpeedBehaviour : MonoBehaviour
    {
        private bool _keyWasDown;
        private Key _key;
        private bool _keyReady;

        public RoseSpeedBehaviour(IntPtr pointer) : base(pointer)
        {
        }

        public void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (!_keyReady)
            {
                _key = Key.F4;
                Key parsed;
                if (Enum.TryParse(RoseSpeedPlugin.ToggleKey.Value, true, out parsed))
                    _key = parsed;
                _keyReady = true;
            }

            KeyControl control = keyboard[_key];
            if (control == null)
                return;

            bool down = control.isPressed;
            if (down && !_keyWasDown)
            {
                RoseSpeedPlugin.Enabled.Value = !RoseSpeedPlugin.Enabled.Value;
                string message = RoseSpeedPlugin.Enabled.Value
                    ? "Rose speed " + RoseSpeedPlugin.Multiplier.Value.ToString("0.0") + "x"
                    : "Rose speed off";
                RoseSpeedPlugin.Toast = message;
                RoseSpeedPlugin.ToastUntil = Time.unscaledTime + 2.5f;
                RoseSpeedPlugin.Logger.LogInfo(message);
            }

            _keyWasDown = down;
        }

        public void OnGUI()
        {
            if (RoseSpeedPlugin.GuiBroken || Time.unscaledTime > RoseSpeedPlugin.ToastUntil)
                return;

            try
            {
                GUI.Label(new Rect(28f, 28f, 720f, 40f), RoseSpeedPlugin.Toast);
            }
            catch (Exception ex)
            {
                RoseSpeedPlugin.GuiBroken = true;
                RoseSpeedPlugin.Logger.LogWarning("Speed message on screen disabled: " + ex.Message);
            }
        }
    }
}
