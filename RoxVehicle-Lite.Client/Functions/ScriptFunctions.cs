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
    }
}