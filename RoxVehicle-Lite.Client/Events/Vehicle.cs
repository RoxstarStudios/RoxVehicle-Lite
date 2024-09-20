using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Threading;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;
using CitizenFX.Core.Native;
using static CitizenFX.Core.Native.API;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using RoxVehicle_Lite.Client;
using static RoxVehicle_Lite.Client.Structs.VehicleStructs;
using static RoxVehicle_Lite.Client.Functions.StriptFunctions;
using static RoxVehicle_Lite.Client.Main;
using Vector3 = CitizenFX.Core.Vector3;
using static RoxVehicle_Lite.Client.TurboFix.TurboFix;

namespace RoxVehicle_Lite.Client.Transmission
{
    public class Transmission
    {
        private static readonly object _padlock = new();
        private static Transmission _instance;
        public static Dictionary<uint, VehicleConfig> vehList = new();
        public static Config scriptConfig = JsonConvert.DeserializeObject<Config>(LoadResourceFile(GetCurrentResourceName(), "config.jsonc") ?? "{}");
        public static int lastGear = 0;
        public static float vehicleBoostLast = 0.0f;
        public static float vehicleBoostLastT2 = 0.0f;
        public static Vehicle lastVehicle = null;
        public static float maxHorsepower = 0.0f;
        public static float maxTorque = 0.0f;

        internal static Transmission Instance
        {
            get
            {
                lock (_padlock)
                {
                    return _instance ??= new Transmission();
                }
            }
        }
        private Transmission()
        {
            foreach (string soundBank in scriptConfig.AntiLagSounds.RegisterSoundBank)
            {
                RequestScriptAudioBank(soundBank, true);
            }
            Main.Instance.AttachTick(TransmissionHandler);
            AddStateBagChangeHandler("EnableExaustPops", null, EnableExaustPops);
            foreach (var listValues in scriptConfig.Vehicles)
            {
                if (!vehList.ContainsKey(Game.GenerateHashASCII(listValues.Key)))
                {
                    vehList.Add(Game.GenerateHashASCII(listValues.Key), listValues.Value);
                }
            }
        }
        private async Task TransmissionHandler()
        {
            if (Game.PlayerPed.IsInVehicle())
            {
                if (GetVehicle().Driver == Game.PlayerPed)
                {
                    Vehicle vehicle = GetVehicle();
                    uint modelhash = vehicle.Model;
                    if (vehList.ContainsKey(modelhash))
                    {
                        if (lastVehicle == vehicle)
                        {
                            VehicleConfig vehicleData = vehList[modelhash];
                            float RPM = GetVehicleRPM(vehicle, vehicleData.Engine.EngineConfig.IdleRPM, vehicleData.Engine.EngineConfig.MaxRPM);
                            float vehicleBoostT2 = 0.0f;
                            if (vehicleData.Turbo2.MaxBoost > 0)
                            {
                                Tuple<float, float> turbo2Data = GetTurboBoost(vehicle, vehicleData, vehicleBoostLastT2 + vehicleBoostLast, RPM, true);
                                vehicleBoostT2 = turbo2Data.Item1;
                                vehicleBoostLastT2 = vehicleBoostT2;
                            }
                            Tuple<float, float> turboData = GetTurboBoost(vehicle, vehicleData, vehicleBoostLast, RPM);
                            float vehicleBoost = turboData.Item1;
                            await SetVehicleExaustPops(vehicle, false);
                            vehicleBoostLast = vehicleBoost;
                            float currentTorque = ApplyEngine(vehicle, vehicleData, vehicleBoost + vehicleBoostT2);
                            ApplyTransmission(vehicle, vehicleData.Transmission);
                            if (scriptConfig.DisableMuscleCarWheelie)
                            {
                                if (GetVehicleClass(vehicle.Handle) == 4)
                                {
                                    SetVehicleWheelieState(vehicle.Handle, 1);
                                }
                            }
                        }
                        else
                        {
                            lastGear = 0;
                            maxHorsepower = 0.0f;
                            vehicleBoostLast = 0.0f;
                            vehicleBoostLastT2 = 0.0f;
                            maxTorque = 0.0f;
                            lastVehicle = vehicle;
                        }
                    }
                    else
                    { // other vehicles thread
                        if (scriptConfig.ElevationLossOtherVehicles)
                        {
                            ApplyOtherVehicles(vehicle);
                        }
                    }
                }
            }
            await Task.FromResult(0);
        }
        public static void ApplyOtherVehicles(Vehicle vehicle)
        {
            if (scriptConfig.ElevationLoss)
            {
                Vector3 vehicleCoords = GetEntityCoords(vehicle.Handle, true);
                float elevationLoss = Math.Min(1.0f, Map(GetEntityHeight(vehicle.Handle, vehicleCoords.X, vehicleCoords.Y, vehicleCoords.Z, true, true) * 3.28084f, 0.0f, 1000.0f, 0.0f, scriptConfig.ElevationLossPercentage/100));
                if (scriptConfig.ElevationLossDisplay)
                {
                    float scale = 1.0f * ((scriptConfig.ElevationLossPercentage / 100) / 0.03f);
                    DrawRect(0.1558f, 0.989f - (0.1753f / 2), 0.01f, 0.1753f, 10, 10, 10, 149);
                    DrawRect(0.1558f, 0.989f - (0.1753f / 2), 0.005f, 0.1753f, 255, 255, 0, 80);
                    DrawRect(0.1558f, 0.989f - (Math.Max(0.0f, Math.Min(Map(elevationLoss, 0.0f, 0.25f * scale, 0.0f, 0.3f), 0.1753f)) / 2), 0.005f, Math.Max(0.0f, Math.Min(Map(elevationLoss, 0.0f, 0.25f * scale, 0.0f, 0.3f), 0.1753f)), 255, 255, 0, 140);
                    if (IsControlPressed(0, 21))
                    {
                        DrawRect(0.1608f, 0.989f - Math.Max(0.0f, Math.Min(Map(elevationLoss, 0.0f, 0.25f * scale, 0.0f, 0.3f), 0.1753f)), 0.015f, 0.0025f, 255, 255, 255, 255);
                        DrawTextOnScreen(0.1608f, 0.967f - Math.Max(0.0f, Math.Min(Map(elevationLoss, 0.0f, 0.25f * scale, 0.0f, 0.3f), 0.1753f)), Math.Round(elevationLoss * 100, 1) + "%", 0.35f, 255, 255, 255);
                    }
                }
                SetVehicleCheatPowerIncrease(vehicle.Handle, 1.0f - (1.0f * elevationLoss));
            }
        }
        public static float ApplyEngine(Vehicle vehicle, VehicleConfig vehicleData, float boost)
        {
            float rpmInternal = GetVehicleRPM(vehicle, vehicleData.Engine.EngineConfig.IdleRPM, vehicleData.Engine.EngineConfig.MaxRPM);
            float torque = CalculateTorque(rpmInternal, vehicleData.Engine.TorqueCurve);
            if (scriptConfig.EnableEngineDamageEffectsPower)
            {
                float damageValue = Map(Math.Max(0.0f, GetVehicleEngineHealth(vehicle.Handle)), 0.0f, 1000.0f, scriptConfig.EnableEngineDamageMinimumPercent/100, 1.0f);
                torque = torque * damageValue;
            }
            int tuningLevel = GetVehicleMod(vehicle.Handle, 11);
            float tuningMultiplier = 0.0f;
            if (tuningLevel > -1)
            {
                int modValue = Function.Call<int>(Hash.GET_VEHICLE_MOD_MODIFIER_VALUE, vehicle.Handle, 11, tuningLevel);
                tuningMultiplier = Map((float)modValue, 0.0f, 100.0f, 0.0f, 0.2f);
            }
            if (scriptConfig.EnableEngineEMSMods)
            {
                torque = torque + (torque * (tuningMultiplier * vehicleData.Engine.EngineConfig.EngineModScale));
            }
            torque = torque + (torque * boost * 0.45f);
            if (scriptConfig.ElevationLoss)
            {
                Vector3 vehicleCoords = GetEntityCoords(vehicle.Handle, true);
                float elevationLoss = Math.Min(1.0f, Map(GetEntityHeight(vehicle.Handle, vehicleCoords.X, vehicleCoords.Y, vehicleCoords.Z, true, true) * 3.28084f, 0.0f, 1000.0f, 0.0f, scriptConfig.ElevationLossPercentage/100));
                if (scriptConfig.ElevationLossDisplay)
                {
                    float scale = 1.0f * ((scriptConfig.ElevationLossPercentage / 100) / 0.03f);
                    DrawRect(0.1558f, 0.989f - (0.1753f / 2), 0.01f, 0.1753f, 10, 10, 10, 149);
                    DrawRect(0.1558f, 0.989f - (0.1753f / 2), 0.005f, 0.1753f, 255, 255, 0, 80);
                    DrawRect(0.1558f, 0.989f - (Math.Max(0.0f, Math.Min(Map(elevationLoss, 0.0f, 0.25f * scale, 0.0f, 0.3f), 0.1753f)) / 2), 0.005f, Math.Max(0.0f, Math.Min(Map(elevationLoss, 0.0f, 0.25f * scale, 0.0f, 0.3f), 0.1753f)), 255, 255, 0, 140);
                    if (IsControlPressed(0, 21))
                    {
                        DrawRect(0.1608f, 0.989f - Math.Max(0.0f, Math.Min(Map(elevationLoss, 0.0f, 0.25f * scale, 0.0f, 0.3f), 0.1753f)), 0.015f, 0.0025f, 255, 255, 255, 255);
                        DrawTextOnScreen(0.1608f, 0.967f - Math.Max(0.0f, Math.Min(Map(elevationLoss, 0.0f, 0.25f * scale, 0.0f, 0.3f), 0.1753f)), Math.Round(elevationLoss * 100, 1) + "%", 0.35f, 255, 255, 255);
                    }
                }
                torque = torque - (torque * elevationLoss);
            }

            float driveForcePremapping = (torque * 1.3558179483f) / GetVehicleHandlingFloat(vehicle.Handle, "CHandlingData", "fMass");
            float driveForce = CalculateMappedDriveForce(vehicle, driveForcePremapping, GetVehicleCurrentGear(vehicle.Handle), 1.0f);
            if (GetEntitySpeed(vehicle.Handle) * 2.236936f > Math.Abs(((GetVehicleHandlingFloat(vehicle.Handle, "CHandlingData", "fInitialDriveMaxFlatVel") / 1.609f) / GetVehicleGearRatio(vehicle.Handle, vehicle.CurrentGear)) * 1.2f))
            {
                driveForce = 0.0f;
            }
            SetVehicleHandlingFloat(vehicle.Handle, "CHandlingData", "fInitialDriveForce", driveForce);
            ModifyVehicleTopSpeed(vehicle.Handle, 1.0f);
            if (scriptConfig.EngineDisplay)
            {
                EngineDisplay(vehicle, rpmInternal, torque, driveForce, boost, vehicleData);
            }
            return torque;
        }
        void ApplyTransmission(Vehicle vehicle, Structs.VehicleStructs.Transmission transmissionData)
        {
            if (GetVehicleMod(vehicle.Handle, 13) != -1)
            {
                SetVehicleModKit(vehicle.Handle, 0);
                SetVehicleMod(vehicle.Handle, 13, -1, GetVehicleModVariation(vehicle.Handle, 23));
            }
            SetVehicleHandlingFloat(vehicle.Handle, "CHandlingData", "fInitialDriveMaxFlatVel", ((transmissionData.TopSpeedMPH * 1.609f) * transmissionData.GearRatios.Last()) / 1.2f);
            SetVehicleGearCount(vehicle, transmissionData.GearRatios.Count - 1);
            for (int i = 0; i < transmissionData.GearRatios.Count; i++)
            {
                SetVehicleGearRatio(vehicle.Handle, i, transmissionData.GearRatios[i]);
            }
        }
        public static void EngineDisplay(Vehicle vehicle, float rpm, float torque, float driveForce, float vehicleBoost, VehicleConfig vehicleData)
        {
            float horsepower = torque * rpm / 5252;
            if (horsepower > maxHorsepower)
            {
                maxHorsepower = horsepower;
            }
            if (torque > maxTorque)
            {
                maxTorque = torque;
            }

            DrawRect(0.08f, 0.80f, 0.14f, 0.025f, 10, 10, 10, 149);
            DrawRect(0.08f, 0.806f, 0.14f, 0.004f, 200, 200, 0, 80);
            DrawRect(0.08f, 0.802f, 0.14f, 0.004f, 0, 200, 0, 80);
            DrawRect(0.08f, 0.798f, 0.14f, 0.004f, 0, 100, 200, 80);
            DrawRect(0.08f, 0.794f, 0.14f, 0.004f, 200, 0, 0, 80);
            float turboMaxBoost = vehicleData.Turbo2.MaxBoost > 0 ? vehicleData.Turbo2.MaxBoost + vehicleData.Turbo.MaxBoost : vehicleData.Turbo.MaxBoost;
            float boostMeter = Math.Max(0.0f, Math.Min(0.14f, Map(vehicleBoost, 0.0f, turboMaxBoost / 14.5038f, 0.0f, 0.14f)));
            float rpmMeter = Math.Max(0.0f, Math.Min(0.14f, Map(rpm, 0.0f, vehicleData.Engine.EngineConfig.MaxRPM, 0.0f, 0.14f)));
            float torqueMeter = Math.Max(0.0f, Math.Min(0.14f, Map(torque, 0.0f, maxTorque, 0.0f, 0.14f)));
            float horsepowerMeter = Math.Max(0.0f, Math.Min(0.14f, Map(horsepower, 0.0f, maxHorsepower, 0.0f, 0.14f)));

            DrawRect(0.01f + boostMeter / 2, 0.806f, boostMeter, 0.004f, 200, 200, 0, 255);
            DrawRect(0.01f + rpmMeter / 2, 0.802f, rpmMeter, 0.004f, 0, 200, 0, 255);
            DrawRect(0.01f + torqueMeter / 2, 0.798f, torqueMeter, 0.004f, 0, 100, 200, 255);
            DrawRect(0.01f + horsepowerMeter / 2, 0.794f, horsepowerMeter, 0.004f, 200, 0, 0, 255);

            if (IsControlPressed(0, 21))
            {
                DrawTextOnScreen(0.01f, 0.761f, "Boost: " + Math.Round(vehicleBoost * 14.5038f, 1), 0.4f, 200, 200, 0);
                DrawTextOnScreen(0.01f, 0.741f, "RPM: " + Math.Round(rpm, 1), 0.4f, 0, 200, 0);
                DrawTextOnScreen(0.08f, 0.761f, "Torque: " + Math.Round(torque, 1), 0.4f, 0, 100, 200);
                DrawTextOnScreen(0.08f, 0.741f, "Horsepower: " + Math.Round(horsepower, 1), 0.4f, 200, 0, 0);
            }
        }
        private async Task SetVehicleExaustPops(Vehicle vehicle, bool enabled)
        {
            while (vehicle.State.Get("EnableExaustPops") != null ? vehicle.State.Get("EnableExaustPops") != enabled.ToString() : true)
            {
                vehicle.State.Set("EnableExaustPops", enabled.ToString(), true);
                await BaseScript.Delay(500);
            }
        }
        private InputArgument EnableExaustPops = new Action<string, string, string>((bagName, key, value) =>
        {
            int vehicle = GetEntityFromStateBagName(bagName);
            EnableVehicleExhaustPops(vehicle, value == "true");
        });
    }
}