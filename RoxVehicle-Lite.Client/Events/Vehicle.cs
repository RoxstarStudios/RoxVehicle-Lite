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

namespace RoxVehicle_Lite.Client.Transmission
{
    public class Transmission
    {
        private static readonly object _padlock = new();
        private static Transmission _instance;
        public static Dictionary<uint, VehicleConfig> vehList = new();
        public static Config scriptConfig = JsonConvert.DeserializeObject<Config>(LoadResourceFile(GetCurrentResourceName(), "config.jsonc") ?? "{}");
        public static int lastGear = 0;
        public static float maxHorsepower = 0.0f;
        public static float vehicleBoostLast = 0.0f;
        public static float maxTorque = 0.0f;
        public static Vehicle lastVehicle = null;

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
            Main.Instance.AttachTick(TransmissionHandler);
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
                            float vehicleBoost = GetCustomTurboBoost(vehicle, vehicleData, vehicleBoostLast, RPM);
                            vehicleBoostLast = vehicleBoost;
                            ApplyEngine(vehicle, vehicleData, vehicleBoost);
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
                            maxTorque = 0.0f;
                            lastVehicle = vehicle;
                        }
                    }
                }
            }
            await Task.FromResult(0);
        }
        public static void ApplyEngine(Vehicle vehicle, VehicleConfig vehicleData, float boost)
        {
            float rpmInternal = GetVehicleRPM(vehicle, vehicleData.Engine.EngineConfig.IdleRPM, vehicleData.Engine.EngineConfig.MaxRPM);
            float torque = CalculateTorque(rpmInternal, vehicleData.Engine.TorqueCurve);
            int tuningLevel = GetVehicleMod(vehicle.Handle, 11);
            float tuningMultiplier = 0.0f;
            if (tuningLevel > -1)
            {
                int modValue = Function.Call<int>(Hash.GET_VEHICLE_MOD_MODIFIER_VALUE, vehicle.Handle, 11, tuningLevel);
                tuningMultiplier = Map((float)modValue, 0.0f, 100.0f, 0.0f, 0.2f);
            }
            if (scriptConfig.EnableEngineEMSMods)
            {
                torque = torque + (torque * tuningMultiplier);
            }
            torque = torque + (torque * boost * 0.45f);
            if (scriptConfig.ElevationLoss)
            {
                Vector3 vehicleCoords = GetEntityCoords(vehicle.Handle, true);
                float elevationLoss = Math.Min(1.0f, Map(GetEntityHeight(vehicle.Handle, vehicleCoords.X, vehicleCoords.Y, vehicleCoords.Z, true, true) * 3.28084f, 0.0f, 1000.0f, 0.0f, scriptConfig.ElevationLossPercentage/100));
                if (scriptConfig.ElevationLossDisplay)
                {
                    float scale = 1.0f * ((scriptConfig.ElevationLossPercentage / 100) / 0.03f);
                    DrawRect(0.1558f, 0.989f - 0.1753f / 2, 0.01f, 0.1753f, 10, 10, 10, 149);
                    DrawRect(0.1558f, 0.989f - 0.1753f / 2, 0.005f, 0.1753f, 255, 255, 0, 80);
                    DrawRect(0.1558f, 0.989f - Math.Max(0.0f, Math.Min(Map(elevationLoss, 0.0f, 0.25f * scale, 0.0f, 0.3f), 0.1753f)) / 2, 0.005f, Math.Max(0.0f, Math.Min(Map(elevationLoss, 0.0f, 0.25f * scale, 0.0f, 0.3f), 0.1753f)), 255, 255, 0, 140);
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
        public static float GetCustomTurboBoost(Vehicle vehicle, VehicleConfig vehicleData, float lastBoost, float realRPM)
        {
            float boost = lastBoost;
            if (IsToggleModOn(vehicle.Handle, 18))
            {
                float throttle = GetVehicleThrottleOffset(vehicle.Handle);
                float timeStep = 0.015f;
                float timeStepBoost = (timeStep * 2) * (realRPM / vehicleData.Engine.EngineConfig.MaxRPM);
                float timeStepVacuum = 0.0065f;
                float RPM = vehicle.CurrentRPM;
                float maxBoost = vehicleData.Turbo.MaxBoost / 14.5038f;
                float maxVacuum = vehicleData.Turbo.MaxVacuum / 14.5038f;
                float boostRate = vehicleData.Turbo.BoostRate;
                float vacuumRate = vehicleData.Turbo.VacuumRate;
                int gear = vehicle.CurrentGear; 
                if (gear != lastGear)
                {
                    if (lastBoost > 0.0f)
                    {
                        boost = lastBoost / 2f;
                    }
                    lastGear = gear;
                }
                throttle = Math.Abs(throttle);

                if (RPM < 0.25f)
                {
                    throttle = 0.1f;
                }

                float desiredManifoldPressure = (maxBoost * -1) + throttle;
                if (boost > desiredManifoldPressure && throttle < 1.0f)
                {
                    boost -= vacuumRate * timeStepVacuum;
                    if (boost < (maxVacuum * -1))
                    {
                        boost = (maxVacuum * -1);
                    }
                }
                else
                {
                    boost += boostRate * timeStepBoost;

                    if (boost > maxBoost)
                    {
                        boost = maxBoost;
                    }
                }
                lastGear = gear;
            }
            else
            {
                boost = 0f;
            }
            return boost;
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

            float boostMeter = Math.Max(0.0f, Math.Min(0.14f, Map(vehicleBoost, 0.0f, vehicleData.Turbo.MaxBoost / 14.5038f, 0.0f, 0.14f)));
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
    }
}