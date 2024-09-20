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

namespace RoxVehicle_Lite.Client.TurboFix
{
    public class TurboFix
    {
        static int LastFxTime;
        static int LastLoudTime;
        static float LastThrottle;
        static Dictionary<int, Tuple<int, int>> AntiLagPops = new();
        public static Config scriptConfig = JsonConvert.DeserializeObject<Config>(LoadResourceFile(GetCurrentResourceName(), "config.jsonc") ?? "{}");

        public static Tuple<float, float> GetTurboBoost(Vehicle vehicle, VehicleConfig vehicleData, float currentBoostOld, float realRPM, bool turbo2 = false) 
        {
            Turbo turboData = vehicleData.Turbo;
            if (turbo2)
            {
                turboData = vehicleData.Turbo2;

            }
            else
            {
                turboData = vehicleData.Turbo;
            }
            float elevationLoss = 0.0f;
            if (scriptConfig.ElevationLoss && turboData.IncreaseBoostToCounteractElevationLoss)
            {
                Vector3 vehicleCoords = GetEntityCoords(vehicle.Handle, true);
                elevationLoss = Math.Min(1.0f, Map(GetEntityHeight(vehicle.Handle, vehicleCoords.X, vehicleCoords.Y, vehicleCoords.Z, true, true) * 3.28084f, 0.0f, 1000.0f, 0.0f, scriptConfig.ElevationLossPercentage/100));
            }
            SetVehicleTurboPressure(vehicle.Handle, 0.0f);
            float currentBoost;
            float newBoost;
            float maxBoost = turboData.MaxBoost / 14.5038f;
            maxBoost = maxBoost + maxBoost * elevationLoss;
            float maxVacuum = (turboData.MaxVacuum / 14.5038f) * -1;
            float FalloffBoost = turboData.FalloffBoost / 14.5038f;
            if (!IsToggleModOn(vehicle.Handle, 18) || !GetIsVehicleEngineRunning(vehicle.Handle)) 
            {
                currentBoost = currentBoostOld;
                newBoost = Lerp(currentBoost, 0.0f, 1.0f - (float)Math.Pow(1.0f - turboData.VacuumRate, 0.0166667f));
                return new Tuple<float, float> (newBoost, 0.0f);
            }

            currentBoost = currentBoostOld;
            currentBoost = Clamp(currentBoost, maxVacuum, maxBoost);

            float rpm = realRPM;

            float boostClosed = Map(rpm, vehicleData.Engine.EngineConfig.IdleRPM, vehicleData.Engine.EngineConfig.MaxRPM, 0.0f, maxVacuum);
            boostClosed = Clamp(boostClosed, maxVacuum, 0.0f);

            float boostWOT = Map(rpm, turboData.RPMSpoolStart, turboData.RPMSpoolEnd, 0.0f, maxBoost);
            boostWOT = Clamp(boostWOT, 0.0f, maxBoost);

            float now = Map(GetVehicleThrottleOffset(vehicle.Handle), 0.0f, 1.0f, boostClosed, boostWOT);

            float lerpRate;
            if (now > currentBoost)
                lerpRate = turboData.BoostRate;
            else
                lerpRate = turboData.VacuumRate;

            newBoost = Lerp(currentBoost, now, 1.0f - (float)Math.Pow(1.0f - lerpRate, 0.0166667f));
            float limBoost = maxBoost;

            newBoost = Clamp(newBoost, maxVacuum, maxBoost);

            float antilagVal = 0.0f;
            if (turboData.AntiLag.Enable) {
                Tuple<float, float> antilagData = UpdateAntiLag(vehicle, vehicleData, realRPM, currentBoost, newBoost, maxBoost);
                antilagVal = antilagData.Item2;
                newBoost = antilagData.Item1;
            }
            
            if (turboData.FalloffRPM > turboData.RPMSpoolEnd && rpm >= turboData.FalloffRPM) 
            {
                float falloffBoost = Map(rpm, turboData.FalloffRPM, 1.0f, turboData.MaxBoost, FalloffBoost);

                if (newBoost > falloffBoost)
                    newBoost = falloffBoost;
            }   

            return new Tuple<float, float> (newBoost, antilagVal);
        }
        public static Tuple<float, float> UpdateAntiLag(Vehicle vehicle, VehicleConfig vehicleData, float realRPM, float currentBoost, float newBoost, float limBoost, bool forceActivateAntiLag = false) 
        {
            float currentThrottle = GetVehicleThrottleOffset(vehicle.Handle);
            float maxVacuum = (vehicleData.Turbo.MaxVacuum / 14.5038f) * -1;
            float antilagVal = 0.0f;
            Random rnd = new Random();
            int pipeCount = 0;
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
                            pipeCount++;
                        }
                    }
                    else
                    {
                        pipeCount++;
                    }
                }  
            }
            if (Math.Abs(GetVehicleThrottleOffset(vehicle.Handle)) < 0.1f && realRPM > vehicleData.Turbo.AntiLag.MinRPM || forceActivateAntiLag) 
            {
                if (vehicleData.Turbo.AntiLag.Effects) {
                    int delayMs = LastFxTime + rnd.Next() % vehicleData.Turbo.AntiLag.RandomMs + (vehicleData.Turbo.AntiLag.PeriodMs < 20 ? 20 * pipeCount : vehicleData.Turbo.AntiLag.PeriodMs * pipeCount);
                    int gameTime = GetGameTimer();
                    if (gameTime > delayMs) {
                        bool loud = false;
                        int loudDelayMs = LastLoudTime + rnd.Next() % vehicleData.Turbo.AntiLag.RandomMs + vehicleData.Turbo.AntiLag.LoudOffThrottleIntervalMs;
                        
                        if ((LastThrottle - currentThrottle) / 0.0166667f > 1000.0f / 200.0f ||
                            vehicleData.Turbo.AntiLag.LoudOffThrottle && gameTime > loudDelayMs) {
                            loud = true;
                            LastLoudTime = gameTime;
                        }
                        RunFx(vehicle, vehicleData, realRPM, loud);
                        LastFxTime = gameTime;
                    }
                }
        
                float randMult = Map((float)(rnd.Next() % 101), 0.0f, 100.0f, 0.005f, 0.015f);
                float randMult2 = Map((float)(rnd.Next() % 101), 0.0f, 100.0f, 0.990f, 1.025f);
                antilagVal = (1.0f * randMult2) + (1.0f * randMult);
                float alBoost = Clamp((currentBoost * randMult2) + ((limBoost - currentBoost) * randMult), maxVacuum, limBoost);
                newBoost = alBoost;
            }
        
            LastThrottle = currentThrottle;
            return new Tuple<float, float> (newBoost, antilagVal);
        }
        public static void RunFx(Vehicle vehicle, VehicleConfig vehicleData, float realRPM, bool loud) 
        {
            Random rnd = new Random();
            float explSz;
            if (loud) {
                explSz = 1.50f;
            }
            else {
                explSz = Map(realRPM, vehicleData.Turbo.RPMSpoolStart, vehicleData.Turbo.RPMSpoolEnd, 0.75f, 1.25f);
                explSz = Clamp(explSz, 0.75f, 1.25f);
            }
            FixCuntStarsEdgeCaseBug(vehicle, rnd, explSz, vehicleData);
        }
    }
}