using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoxVehicle_Lite.Client.Structs
{
    public class VehicleStructs
    {
        public struct Config
        {
            public bool ElevationLoss;
            public bool ElevationLossOtherVehicles;
            public float ElevationLossPercentage;
            public bool ElevationLossDisplay;
            public bool EngineDisplay;
            public bool DisableMuscleCarWheelie;
            public bool EnableEngineEMSMods;
            public bool EnableEngineDamageEffectsPower;
            public float EnableEngineDamageMinimumPercent;
            public InversePower InversePower;
            public Dictionary<string, VehicleConfig> Vehicles;
        }
        public struct InversePower
        { 
            public bool enabled;
            public float speedStart;
            public float speedEnd;
            public float speedStartTorqueMod;
            public float angleStart;
            public float angleEnd;
            public float angleStartTorqueMod;
            public float angleEndTorqueMod;
            public bool carsOnly;
            public List<int> classBlacklist;

            public InversePower(bool enabled, float speedStart, float speedEnd, float speedStartTorqueMod, float angleStart, float angleEnd, float angleStartTorqueMod, float angleEndTorqueMod, bool carsOnly, List<int> classBlacklist)
            {
                this.enabled = enabled;
                this.speedStart = speedStart;
                this.speedEnd = speedEnd;
                this.speedStartTorqueMod = speedStartTorqueMod;
                this.angleStart = angleStart;
                this.angleEnd = angleEnd;
                this.angleStartTorqueMod = angleStartTorqueMod;
                this.angleEndTorqueMod = angleEndTorqueMod;
                this.carsOnly = carsOnly;
                this.classBlacklist = classBlacklist;
            } 
        }
        public struct VehicleConfig
        { 
            public Transmission Transmission;
            public Engine Engine;
            public Turbo Turbo;

            public VehicleConfig(Transmission Transmission, Turbo Turbo, Engine Engine)
            {
                this.Transmission = Transmission;
                this.Engine = Engine;
                this.Turbo = Turbo;
            } 
        }
        public struct Transmission
        { 
            public List<float> GearRatios;
            public float TopSpeedMPH;

            public Transmission(List<float> GearRatios, float TopSpeedMPH)
            {
                this.GearRatios = GearRatios;
                this.TopSpeedMPH = TopSpeedMPH;
            }
        }
        public struct Engine
        { 
            public Dictionary<float, float> TorqueCurve;
            public EngineConfig EngineConfig;

            public Engine(Dictionary<float, float> TorqueCurve, EngineConfig EngineConfig)
            {
                this.TorqueCurve = TorqueCurve;
                this.EngineConfig = EngineConfig;
            }
        }
        public struct EngineConfig
        { 
            public float EngineModScale;
            public float MaxRPM;
            public float IdleRPM;

            public EngineConfig(float EngineModScale, float MaxRPM, float IdleRPM)
            {
                this.EngineModScale = EngineModScale;
                this.MaxRPM = MaxRPM;
                this.IdleRPM = IdleRPM;
            }
        }
        public struct Turbo
        { 
            public float MaxBoost;
            public float MaxVacuum;
            public float BoostRate;
            public float VacuumRate;

            public Turbo(float MaxBoost, float MaxVacuum, float BoostRate, float VacuumRate)
            {
                this.MaxBoost = MaxBoost;
                this.MaxVacuum = MaxVacuum;
                this.BoostRate = BoostRate;
                this.VacuumRate = VacuumRate;
            }
        }
    }
}
