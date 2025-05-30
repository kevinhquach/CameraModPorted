// Credits to Nevernamed who made the original Camera Mod, available at https://modworkshop.net/mod/34530

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CameraModPorted
{
    [BepInDependency(ETGModMainBehaviour.GUID)]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "triceraquach.etg.cameramodported";
        public const string NAME = "CameraModPorted";
        public const string VERSION = "1.0.0";
        public const string TEXT_COLOR = "#00FFFF";

        public static float currentRotPitch = 0;
        public static float currentRotYaw = 0;
        public static float currentRotRoll = 0;
        public static float spinPitchDegreesPersecond = 0;
        public static float spinYawDegreesPersecond = 0;
        public static float spinRollDegreesPersecond = 0;
        //public static bool camLock = false;
        //public static Vector3 currentPosition;

        public static ConfigEntry<float> configZoom;
        public static ConfigEntry<float> configPitch;
        public static ConfigEntry<float> configYaw;
        public static ConfigEntry<float> configRoll;
        public static ConfigEntry<float> configSpinPitch;
        public static ConfigEntry<float> configSpinYaw;
        public static ConfigEntry<float> configSpinRoll;
        public static ConfigEntry<bool> configOrthographic;
        public static ConfigEntry<float> configFov;

        public static CameraController camControl;

        public void Start()
        {
            ETGModMainBehaviour.WaitForGameManagerStart(GMStart);
        }

        public void Awake()
        {
            configZoom = Config.Bind("_Recommended", "Zoom", 1f, "Default zoom level (> 0 recommended), try it with mouseAimLook: false in SlotA.options");
            configPitch = Config.Bind("Misc", "Pitch", 0f, "Default pitch (degrees)");
            configYaw = Config.Bind("Misc", "Yaw", 0f, "Default yaw (degrees)");
            configRoll = Config.Bind("Misc", "Roll", 0f, "Default roll (degrees)");
            configSpinPitch = Config.Bind("Misc", "SpinPitch", 0f, "Default pitch spin (degrees / second)");
            configSpinYaw = Config.Bind("Misc", "SpinYaw", 0f, "Default yaw spin (degrees / second)");
            configSpinRoll = Config.Bind("Misc", "SpinRoll", 0f, "Default roll spin (degrees / second)");
            configOrthographic = Config.Bind("Misc", "Orthographic", true, "Default camera type");
            configFov = Config.Bind("Misc", "FOV", 60f, "Default fov, used with non-orthographic camera (> 0 recommended)");
        }

        public static void InitDefaultCamera()
        {
            camControl.OverrideZoomScale = configZoom.Value;
            camControl.Camera.fieldOfView = configFov.Value;
            camControl.Camera.orthographic = configOrthographic.Value;
            //g.MainCameraController.SetManualControl(false, true);
            //camLock = false;
            spinPitchDegreesPersecond = configSpinPitch.Value;
            spinYawDegreesPersecond = configSpinYaw.Value;
            spinRollDegreesPersecond = configSpinRoll.Value;
            camControl.Camera.transform.rotation = Quaternion.Euler(configPitch.Value, configYaw.Value, configSpinRoll.Value);
            currentRotPitch = configPitch.Value;
            currentRotYaw = configYaw.Value;
            currentRotRoll = configSpinRoll.Value;
            if (AllCameraValuesZero() || (spinPitchDegreesPersecond == 0) || (spinYawDegreesPersecond == 0) || (spinRollDegreesPersecond == 0))
            {
                Pixelator.Instance.DoOcclusionLayer = true;
            }
            else
            {
                Pixelator.Instance.DoOcclusionLayer = false;
            }
        }
        public static bool AllCameraValuesZero()
        {
            bool val = true;
            if (camControl.Camera.transform.rotation.x != 0) val = false;
            if (camControl.Camera.transform.rotation.y != 0) val = false;
            if (camControl.Camera.transform.rotation.z != 0) val = false;
            return val;
        }

        public void GMStart(GameManager g)
        {
            ETGModMainBehaviour.Instance.gameObject.AddComponent<CameraFuckeryComp>();
            camControl = g.MainCameraController;
            new Harmony(GUID).PatchAll();
            ETGModConsole.Commands.AddGroup("camera", args => {
                Log("All camera commands: save, load, reset, zoom #, pitch #, yaw #, roll #, spin_pitch #, spin_yaw #, spin_roll #, orthographic_toggle, fov #", TEXT_COLOR);
                Log($"Current camera: {g.MainCameraController.Camera.transform.rotation}", TEXT_COLOR);
            });
            ETGModConsole.Commands.GetGroup("camera").AddUnit("zoom", args => {
                if (args != null && args.Length > 0)
                {
                    if (args.Length == 1)
                    {
                        float.TryParse(args[0], out float numValue);
                        g.MainCameraController.OverrideZoomScale = numValue;
                        Log($"Setting zoom to: {numValue}", TEXT_COLOR);
                    }
                    else Log("Error: Too many arguments!", "#FF1500");
                }
                else
                {
                    Log($"Current zoom: {g.MainCameraController.OverrideZoomScale}", TEXT_COLOR);
                }
            });
            ETGModConsole.Commands.GetGroup("camera").AddUnit("reset", args => {
                g.MainCameraController.OverrideZoomScale = 1;
                g.MainCameraController.Camera.fieldOfView = 60;
                g.MainCameraController.Camera.orthographic = true;
                //g.MainCameraController.SetManualControl(false, true);
                //camLock = false;
                Pixelator.Instance.DoOcclusionLayer = true;
                spinPitchDegreesPersecond = 0;
                spinYawDegreesPersecond = 0;
                spinRollDegreesPersecond = 0;
                g.MainCameraController.Camera.transform.rotation = Quaternion.Euler(0, 0, 0);
                currentRotPitch = 0;
                currentRotYaw = 0;
                currentRotRoll = 0;
                Log("Camera reset!", TEXT_COLOR);
            });
            ETGModConsole.Commands.GetGroup("camera").AddUnit("load", args => {
                InitDefaultCamera();
                Log("Camera config loaded!", TEXT_COLOR);
            });
            ETGModConsole.Commands.GetGroup("camera").AddUnit("save", args => {
                configZoom.Value = g.MainCameraController.OverrideZoomScale;
                configFov.Value = g.MainCameraController.Camera.fieldOfView;
                configOrthographic.Value = g.MainCameraController.Camera.orthographic;
                configSpinPitch.Value = spinPitchDegreesPersecond;
                configSpinYaw.Value = spinYawDegreesPersecond;
                configSpinRoll.Value = spinRollDegreesPersecond;
                configPitch.Value = currentRotPitch;
                configYaw.Value = currentRotYaw;
                configSpinRoll.Value = currentRotRoll;
                Config.Save();
                Log("Camera config saved!", TEXT_COLOR);
            });

            ETGModConsole.Commands.GetGroup("camera").AddUnit("orthographic_toggle", args =>
            {
                if (g.MainCameraController.Camera.orthographic)
                {
                    g.MainCameraController.Camera.orthographic = false;
                    Log("Camera mode is now set to non-orthographic.", TEXT_COLOR);
                }
                else
                {
                    g.MainCameraController.Camera.orthographic = true;
                    Log("Camera mode is now set to orthographic.", TEXT_COLOR);
                }
            });
            // fov works with orthographic_toggle
            ETGModConsole.Commands.GetGroup("camera").AddUnit("fov", args => {
                if (args != null && args.Length > 0)
                {
                    if (args.Length == 1)
                    {
                        float.TryParse(args[0], out float numValue);
                        g.MainCameraController.Camera.fieldOfView = numValue;
                        Log($"Setting zoom to: {numValue}", TEXT_COLOR);
                    }
                    else Log("Error: Too many arguments!", "#FF1500");
                }
                else
                {
                    Log($"Current fov: {g.MainCameraController.Camera.fieldOfView}", TEXT_COLOR);
                }
            });
            // doesn't seem to work, feel free to try yourself
            //ETGModConsole.Commands.GetGroup("camera").AddUnit("lock_toggle", args =>
            //{
            //    if (!camLock)
            //    {
            //        g.MainCameraController.SetManualControl(true, true);
            //        g.MainCameraController.OverridePosition = g.MainCameraController.transform.position;
            //        //g.MainCameraController.UpdateOverridePosition(g.MainCameraController.transform.position, 2000);
            //        //currentPosition = g.MainCameraController.transform.position;
            //        Log("Camera is now locked.");
            //        camLock = true;
            //    }
            //    else
            //    {
            //        g.MainCameraController.SetManualControl(false, true);
            //        Log("Camera is now unlocked.");
            //        camLock = false;
            //    }
            //});
            ETGModConsole.Commands.GetGroup("camera").AddUnit("spin_pitch", args =>
            {
                if (args != null && args.Length > 0)
                {
                    if (args.Length == 1)
                    {
                        float.TryParse(args[0], out float numValue);
                        //currentRotPitch = 0;
                        spinPitchDegreesPersecond = numValue;
                        if (AllCameraValuesZero(g) || (spinPitchDegreesPersecond == 0))
                        {
                            Pixelator.Instance.DoOcclusionLayer = true;
                        }
                        else
                        {
                            Pixelator.Instance.DoOcclusionLayer = false;
                        }
                        Log($"Setting pitch spin to: {numValue}", TEXT_COLOR);
                    }
                    else Log("Error: Too many arguments!", "#FF1500");
                }
                else Log($"Current pitch spin: {spinPitchDegreesPersecond}", TEXT_COLOR);
            });
            ETGModConsole.Commands.GetGroup("camera").AddUnit("spin_yaw", args =>
            {
                if (args != null && args.Length > 0)
                {
                    if (args.Length == 1)
                    {
                        float.TryParse(args[0], out float numValue);
                        //currentRotYaw = 0;
                        spinYawDegreesPersecond = numValue;
                        if (AllCameraValuesZero(g) || (spinYawDegreesPersecond == 0))
                        {
                            Pixelator.Instance.DoOcclusionLayer = true;
                        }
                        else
                        {
                            Pixelator.Instance.DoOcclusionLayer = false;
                        }
                        Log($"Setting yaw spin to: {numValue}", TEXT_COLOR);
                    }
                    else Log("Error: Too many arguments!", "#FF1500");
                }
                else Log($"Current yaw spin: {spinYawDegreesPersecond}", TEXT_COLOR);
            });
            ETGModConsole.Commands.GetGroup("camera").AddUnit("spin_roll", args =>
            {
                if (args != null && args.Length > 0)
                {
                    if (args.Length == 1)
                    {
                        float.TryParse(args[0], out float numValue);
                        //currentRotRoll = 0;
                        spinRollDegreesPersecond = numValue;
                        if (AllCameraValuesZero(g) || (spinRollDegreesPersecond == 0))
                        {
                            Pixelator.Instance.DoOcclusionLayer = true;
                        }
                        else
                        {
                            Pixelator.Instance.DoOcclusionLayer = false;
                        }
                        Log($"Setting roll spin to: {numValue}", TEXT_COLOR);
                    }
                    else Log("Error: Too many arguments!", "#FF1500");
                }
                else Log($"Current roll spin: {spinRollDegreesPersecond}", TEXT_COLOR);
            });
            ETGModConsole.Commands.GetGroup("camera").AddUnit("pitch", args =>
            {
                if (args != null && args.Length > 0)
                {
                    if (args.Length == 1)
                    {
                        float.TryParse(args[0], out float numValue);
                        currentRotPitch = numValue;
                        g.MainCameraController.Camera.transform.rotation = Quaternion.Euler(currentRotPitch, currentRotYaw, currentRotRoll);
                        if (AllCameraValuesZero(g))
                        {
                            Pixelator.Instance.DoOcclusionLayer = true;
                        }
                        else
                        {
                            Pixelator.Instance.DoOcclusionLayer = false;
                        }
                        Log($"Setting pitch to: {numValue}", TEXT_COLOR);
                    }
                    else Log("Error: Too many arguments!", "#FF1500");
                }
                else Log($"Current pitch: {currentRotPitch}", TEXT_COLOR);
            });
            ETGModConsole.Commands.GetGroup("camera").AddUnit("yaw", args =>
            {
                if (args != null && args.Length > 0)
                {
                    if (args.Length == 1)
                    {
                        float.TryParse(args[0], out float numValue);
                        currentRotYaw = numValue;
                        g.MainCameraController.Camera.transform.rotation = Quaternion.Euler(currentRotPitch, currentRotYaw, currentRotRoll);
                        if (AllCameraValuesZero(g))
                        {
                            Pixelator.Instance.DoOcclusionLayer = true;
                        }
                        else
                        {
                            Pixelator.Instance.DoOcclusionLayer = false;
                        }
                        Log($"Setting yaw to: {numValue}", TEXT_COLOR);
                    }
                    else Log("Error: Too many arguments!", "#FF1500");
                }
                else Log($"Current yaw: {currentRotYaw}", TEXT_COLOR);
            });
            ETGModConsole.Commands.GetGroup("camera").AddUnit("roll", args =>
            {
                if (args != null && args.Length > 0)
                {
                    if (args.Length == 1)
                    {
                        float.TryParse(args[0], out float numValue);
                        currentRotRoll = numValue;
                        g.MainCameraController.Camera.transform.rotation = Quaternion.Euler(currentRotPitch, currentRotYaw, currentRotRoll);
                        if (AllCameraValuesZero(g))
                        {
                            Pixelator.Instance.DoOcclusionLayer = true;
                        }
                        else
                        {
                            Pixelator.Instance.DoOcclusionLayer = false;
                        }
                        Log($"Setting roll to: {numValue}", TEXT_COLOR);
                    }
                    else Log("Error: Too many arguments!", "#FF1500");
                }
                else Log($"Current roll: {currentRotRoll}", TEXT_COLOR);
            });

            Log($"{NAME} v{VERSION} started successfully.", TEXT_COLOR);
        }

        public static void Log(string text, string color="#FFFFFF")
        {
            ETGModConsole.Log($"<color={color}>{text}</color>");
        }
        public static bool AllCameraValuesZero(GameManager g)
        {
            bool val = true;
            if (g.MainCameraController.Camera.transform.rotation.x != 0) val = false;
            if (g.MainCameraController.Camera.transform.rotation.y != 0) val = false;
            if (g.MainCameraController.Camera.transform.rotation.z != 0) val = false;
            return val;
        }
    }
    public class CameraFuckeryComp : MonoBehaviour
    {
        private void Start()
        {
        }


        private void FixedUpdate()
        {
            if (Plugin.spinPitchDegreesPersecond != 0 || Plugin.spinYawDegreesPersecond != 0 || Plugin.spinRollDegreesPersecond != 0)
            {
                Plugin.currentRotPitch += ((float)Plugin.spinPitchDegreesPersecond / 50f);
                Plugin.currentRotYaw += ((float)Plugin.spinYawDegreesPersecond / 50f);
                Plugin.currentRotRoll += ((float)Plugin.spinRollDegreesPersecond / 50f);
                GameManager.Instance.MainCameraController.Camera.transform.rotation = Quaternion.Euler(Plugin.currentRotPitch, Plugin.currentRotYaw, Plugin.currentRotRoll);
                Pixelator.Instance.DoOcclusionLayer = false;
            }
            // too much stuttering
            //if (Plugin.camLock)
            //{
            //    GameManager.Instance.MainCameraController.transform.position = Plugin.currentPosition;
            //}
        }
    }
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.Start))]
    public class RunStartHook
    {
        [HarmonyPrefix]
        static void Prefix()
        {
            Plugin.InitDefaultCamera();
        }
    }
}
