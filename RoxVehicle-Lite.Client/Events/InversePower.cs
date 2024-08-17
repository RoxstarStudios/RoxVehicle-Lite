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

namespace RoxVehicle_Lite.Client.InversePower
{
    public class InversePower
    {
        
        public static Config scriptConfig = JsonConvert.DeserializeObject<Config>(LoadResourceFile(GetCurrentResourceName(), "config.jsonc") ?? "{}");
        private static float speedStart = scriptConfig.InversePower.speedStart;
        private static float speedEnd = scriptConfig.InversePower.speedEnd;
        private static float speedStartTorqueMod = scriptConfig.InversePower.speedStartTorqueMod;
        private static float angleStart = scriptConfig.InversePower.angleStart;
        private static float angleEnd = scriptConfig.InversePower.angleEnd;
        private static float angleStartTorqueMod = scriptConfig.InversePower.angleStartTorqueMod;
        private static float angleEndTorqueMod = scriptConfig.InversePower.angleEndTorqueMod;
        private static readonly object _padlock = new();
        private static InversePower _instance;

        internal static InversePower Instance
        {
            get
            {
                lock (_padlock)
                {
                    return _instance ??= new InversePower();
                }
            }
        }
        private InversePower()
        {
            if (scriptConfig.InversePower.enabled)
            {
                Main.Instance.AttachTick(InversePowerHandler);
            }
        }

        private async Task InversePowerHandler()
        {
            if (Game.PlayerPed.IsInVehicle())
            {
                if (GetVehicle().Driver == Game.PlayerPed)
                {
                    Vehicle vehicle = GetVehicle();
                    if (scriptConfig.InversePower.classBlacklist.Contains((int)vehicle.ClassType) || !(!IsThisModelACar(vehicle.Model) && scriptConfig.InversePower.carsOnly))
                    {
                        float vehSpeed = GetEntitySpeed(vehicle.Handle);
                        Vector3 vehicleForwardVector = GetEntitySpeedVector(vehicle.Handle, true);
                        float vehAngle = (float)(Math.Acos(Math.Abs(vehicleForwardVector.Y / vehSpeed)) * 180.0 / Math.PI);

                        float speedMod = Map(vehSpeed, speedStart, speedEnd, speedStartTorqueMod, 0.0f);
                        speedMod = Math.Min(Math.Max(speedMod, 0.0f), speedStartTorqueMod);

                        float torqueMod = Map(vehAngle, angleStart, angleEnd, angleStartTorqueMod, angleEndTorqueMod);
                        speedMod = Math.Min(Math.Max(torqueMod, angleStartTorqueMod), angleEndTorqueMod);

                        float torqueMult = 1.0f + (speedMod * torqueMod) / 1.55f;
                        if (vehAngle > angleStart)
                        {
                            SetVehicleCheatPowerIncrease(vehicle.Handle, torqueMult);
                        }
                        else
                        {
                            SetVehicleCheatPowerIncrease(vehicle.Handle, 1.0f);
                        }
                    }
                }
                await Task.FromResult(0);
            }
        }      
    }
}