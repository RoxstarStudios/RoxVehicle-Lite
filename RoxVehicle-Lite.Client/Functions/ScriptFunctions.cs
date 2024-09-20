using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

using RoxVehicle_Lite.Client;
using static RoxVehicle_Lite.Client.Structs.VehicleStructs;

namespace RoxVehicle_Lite.Client.Functions
{
    public class StriptFunctions
    {
        
        public static Config scriptConfig = JsonConvert.DeserializeObject<Config>(LoadResourceFile(GetCurrentResourceName(), "config.jsonc") ?? "{}");
        public static void Error(string message, bool blink = true, bool SaveToBrief = true)
        {
            Custom($"~h~Error:~h~ {message}", blink, SaveToBrief);
        } 
        public static void Success(string message, bool blink = true, bool SaveToBrief = true)
        {
            Custom($"~h~Success:~h~ {message}", blink, SaveToBrief);
        }   
        public static void Info(string message, bool blink = true, bool SaveToBrief = true)
        {
            Custom($"~h~Info:~h~ {message}", blink, SaveToBrief);
        }   
        public static void Alert(string message, bool blink = true, bool SaveToBrief = true)
        {
            Custom($"~h~Alert:~h~ {message}", blink, SaveToBrief);
        }   
        public static void Custom(string message, bool blink = true, bool saveToBrief = true)
        {
            SetNotificationTextEntry("CELL_EMAIL_BCON");
            foreach (var s in Screen.StringToArray(message))
            {
                AddTextComponentSubstringPlayerName(s);
            }
            DrawNotification(blink, saveToBrief);
        }  

        public static CitizenFX.Core.Vehicle GetVehicle(bool lastVehicle = false)
        {
            if (lastVehicle)
            {
                return Game.PlayerPed.LastVehicle;
            }
            else
            {
                if (Game.PlayerPed.IsInVehicle())
                {
                    return Game.PlayerPed.CurrentVehicle;
                }
            }
            return null;
        }
        public static float GetVehicleRPM(CitizenFX.Core.Vehicle Vehicle, float IdleRPM, float MaxRPM)
        {
            float RPM = 0f;
            if (Vehicle.CurrentRPM >= 0.2)
            {
                RPM = Map(Vehicle.CurrentRPM, 0.2f, 1.0f, IdleRPM, MaxRPM);
            }
            else
            {
                RPM = Map(Vehicle.CurrentRPM, 0.0f, 0.2f, 0.0f, IdleRPM);
            }
            return Math.Min(RPM, MaxRPM);
        }
        public static float GetVehicleRPMGTA(float realRPM, float IdleRPM, float MaxRPM)
        {
            float RPM = 0f;
            if (realRPM >= IdleRPM)
            {
                RPM = Map(realRPM, IdleRPM, MaxRPM, 0.2f, 1.0f);
            }
            else
            {
                RPM = Map(realRPM, 0.0f, IdleRPM, 0.0f, 0.2f);
            }
            return Math.Min(RPM, 1.0f);
        }
        public static float Map(float value, float min_in, float max_in, float min_out, float max_out)
        {
            return ((value - min_in) * (max_out - min_out) / (max_in - min_in)) + min_out;
        }
        public static double Map(double value, double min_in, double max_in, double min_out, double max_out)
        {
            return ((value - min_in) * (max_out - min_out) / (max_in - min_in)) + min_out;
        }
        public static void DrawTextOnScreen(float xPosition = 0.0f, float yPosition = 0.0f, string text = "Default", float size = 0.5f, int red = 255, int green = 255, int blue = 255, CitizenFX.Core.UI.Alignment justification = Alignment.Left, int font = 6, bool disableTextOutline = false)
        {
            if (IsHudPreferenceSwitchedOn() && !IsPlayerSwitchInProgress() && IsScreenFadedIn() && !IsPauseMenuActive() && !IsFrontendFading() && !IsPauseMenuRestarting() && !IsHudHidden())
            {
                SetTextFont(font);
                SetTextScale(1.0f, size);
                SetTextColour(red, green, blue, 255);
                if (justification == CitizenFX.Core.UI.Alignment.Right)
                {
                    SetTextWrap(0f, xPosition);
                }
                SetTextJustification((int)justification);
                if (!disableTextOutline) { SetTextOutline(); }
                BeginTextCommandDisplayText("STRING");
                AddTextComponentSubstringPlayerName(text);
                EndTextCommandDisplayText(xPosition, yPosition);
            }
        }
        public static float CalculateTorque(float inputRPM, Dictionary<float, float> torqueTable)
        {
            float previousRPM = 0f, previousTorque = 0f;
            foreach (var data in torqueTable)
            {
                float rpm = data.Key;
                float torque = data.Value;
        
                if (inputRPM <= rpm)
                {
                    float rpmDifference = rpm - previousRPM;
                    float torqueDifference = torque - previousTorque;
                    float torqueRatio = torqueDifference / rpmDifference;
        
                    float rpmOffset = inputRPM - previousRPM;
                    float calculatedTorque = previousTorque + rpmOffset * torqueRatio;
        
                    return calculatedTorque;
                }
        
                previousRPM = rpm;
                previousTorque = torque;
            }
        
            return torqueTable[torqueTable.Count - 1];
        }

        private static Dictionary<float, float> BaseTorqueModMap1 = new Dictionary<float, float>
        {
            { 0.0f, 0.0f },
            { 0.8f, 0.0f },
            { 1.0f, 0.4f },
        };
        private static Dictionary<float, float> BaseTorqueModMap2 = new Dictionary<float, float>
        {
            { 0.0f, 0.0f },
            { 0.8f, 0.0f },
            { 1.0f, 2.0f/3.0f },
        };
        
        public static float CalculateMappedDriveForce(CitizenFX.Core.Vehicle Vehicle, float DriveForce, int Gear, float torquemap)
        {
            float RPM = Math.Min(Vehicle.CurrentRPM, 1.0f);
            float baseMod1 = CalculateTorque(RPM, BaseTorqueModMap1) * DriveForce;
            float baseMod2 = CalculateTorque(RPM, BaseTorqueModMap2) * DriveForce;
            float baseMod = Map(RPM, 0.8f, 1.0f, baseMod1, baseMod2);

            int tuningLevel = GetVehicleMod(Vehicle.Handle, 11);

            float tuningMultiplier = 1.0f;
            if (tuningLevel > -1)
            {
                int modValue = Function.Call<int>(Hash.GET_VEHICLE_MOD_MODIFIER_VALUE, Vehicle.Handle, 11, tuningLevel);
                tuningMultiplier = Map((float)modValue, 0.0f, 100.0f, 1.0f, 1.2f);
            }

            if (RPM <= 0.8f)
                baseMod = 0.0f;

            if (Gear < 2)
                baseMod = 0.0f;

            float finalForce = (DriveForce + baseMod) / tuningMultiplier * torquemap;

            return finalForce;
        }
        public static float NonLinearMap(float t, float minValue, float maxValue, float easingAmount)
        {
            t = Math.Max(0, Math.Min(1, t));
    
            float normalizedValue = (float)Math.Pow((double)t, (double)easingAmount);
            return minValue + (maxValue - minValue) * normalizedValue;
        }
        public static void SetVehicleGearCount(Vehicle vehicle, int count)
        {
            if (GetVehicleHandlingInt(vehicle.Handle, "CHandlingData", "nInitialDriveGears") != count)
            {
                SetVehicleHandlingInt(vehicle.Handle, "CHandlingData", "nInitialDriveGears", count);
            }
            if (GetVehicleHighGear(vehicle.Handle) != count)
            {
                SetVehicleHighGear(vehicle.Handle, count);
            }
        }
        public static float Lerp(float value1, float value2, float amount)
        {
             return value1 * (1 - amount) + value2 * amount;
        }
        public static float Clamp(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
        public static bool GetVehicleExhaustBone(int vehicle, int index, ref int boneIndex, ref bool axisX) 
        {
            OutputArgument boneIndexRef = new OutputArgument(-1);
            OutputArgument axisXRef = new OutputArgument(false);
            bool result = Function.Call<bool>((Hash)0xE728F090D538CB18, vehicle, index, boneIndexRef, axisXRef);
            boneIndex = boneIndexRef.GetResult<int>();
            axisX = axisXRef.GetResult<bool>();
            return result;
        }
        public static int GetVehicleMaxExhaustBoneCount()
        {
            return Function.Call<int>((Hash)0x3EE18B00CD86C54F); 
        }
        public static void FixCuntStarsEdgeCaseBug(Vehicle vehicle, Random rnd, float explSz, VehicleConfig vehicleData)
        {
            int exhaustCount = 0;
            for (int i = 0; i < GetVehicleMaxExhaustBoneCount(); i++)
            {
                int boneIndex = -1;
                bool axisX = false;
                GetVehicleExhaustBone(vehicle.Handle, i, ref boneIndex, ref axisX);
                if (!(boneIndex == -1))
                {
                    Vector3 boneLoc = GetEntityBonePosition_2(vehicle.Handle, boneIndex);
                    Vector3 boneLocReal = GetWorldPositionOfEntityBone(vehicle.Handle, boneIndex);
                    if (GetVehicleMod(vehicle.Handle, 4) != -1)
                    {
                        if (boneLocReal == Vector3.Zero)
                        {
                            exhaustCount++;
                        }
                    }
                }  

            }
            for (int i = 0; i < GetVehicleMaxExhaustBoneCount(); i++)
            {
                int boneIndex = -1;
                bool axisX = false;
                GetVehicleExhaustBone(vehicle.Handle, i, ref boneIndex, ref axisX);
                if (!(boneIndex == -1))
                {
                    Vector3 boneLoc = GetEntityBonePosition_2(vehicle.Handle, boneIndex);
                    Vector3 boneLocReal = GetWorldPositionOfEntityBone(vehicle.Handle, boneIndex);
                    if (GetVehicleMod(vehicle.Handle, 4) != -1)
                    {
                        if (boneLocReal == Vector3.Zero || exhaustCount == 0)
                        {
                            UseParticleFxAssetNextCall("core");
                            StartNetworkedParticleFxNonLoopedOnEntityBone("veh_backfire", vehicle.Handle, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, boneIndex, explSz, axisX, false, false); 
                            ExaustPopSounds exaustPopSounds = scriptConfig.AntiLagSounds.ExaustPopSounds[vehicleData.Turbo.AntiLag.AntiLagSounds[rnd.Next(0, vehicleData.Turbo.AntiLag.AntiLagSounds.Count-1)]];
                            PlaySoundFromCoord(-1, exaustPopSounds.SoundString, boneLoc.X, boneLoc.Y, boneLoc.Z, exaustPopSounds.Ref, true, (int)(400 * explSz), false); 
                        }
                    }
                    else
                    {
                        UseParticleFxAssetNextCall("core");
                        StartNetworkedParticleFxNonLoopedOnEntityBone("veh_backfire", vehicle.Handle, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, boneIndex, explSz, axisX, false, false); 
                        ExaustPopSounds exaustPopSounds = scriptConfig.AntiLagSounds.ExaustPopSounds[vehicleData.Turbo.AntiLag.AntiLagSounds[rnd.Next(0, vehicleData.Turbo.AntiLag.AntiLagSounds.Count-1)]];
                        PlaySoundFromCoord(-1, exaustPopSounds.SoundString, boneLoc.X, boneLoc.Y, boneLoc.Z, exaustPopSounds.Ref, true, (int)(400 * explSz), false); 
                    }
                }  
            }
        }
        public static void Draw3DText(float x, float y, float z, float sclFactor, string text)
        {
            bool onScreen;
            float x2 = 0.0f;
            float y2 = 0.0f;

            // Convert 3D world coordinates to 2D screen coordinates
            onScreen = World3dToScreen2d(x, y, z, ref x2, ref y2);

            // Get the camera's position
            Vector3 camCoords = GetGameplayCamCoords();

            // Calculate the distance between the camera and the 3D point
            float distance = GetDistanceBetweenCoords(camCoords.X, camCoords.Y, camCoords.Z, x, y, z, true);

            // Calculate the scale based on the distance and field of view
            float scale = (1.0f / distance) * 2.0f;
            float fov = (1.0f / GetGameplayCamFov()) * 100.0f;
            scale = scale * fov * sclFactor;

            if (onScreen)
            {
                // Set the text scale
                SetTextScale(0.0f, scale);
                SetTextFont(0);
                SetTextProportional(true);

                // Set the text color
                SetTextColour(255, 255, 255, 215);

                // Set the text effects
                SetTextDropshadow(0, 0, 0, 0, 255);
                SetTextEdge(2, 0, 0, 0, 150);
                SetTextDropShadow();
                SetTextOutline();

                // Prepare the text
                SetTextEntry("STRING");
                SetTextCentre(true);
                AddTextComponentString(text);

                // Draw the text on the screen
                DrawText(x2, y2);
            }
        }
    }
}