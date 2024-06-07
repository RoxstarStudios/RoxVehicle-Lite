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
            public float ElevationLossPercentage;
            public bool ElevationLossDisplay;
            public bool EngineDisplay;
            public bool DisableMuscleCarWheelie;
            public bool EnableEngineEMSMods;
            public Dictionary<string, VehicleConfig> Vehicles;
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
            public float MaxRPM;
            public float IdleRPM;

            public EngineConfig(float MaxRPM, float IdleRPM)
            {
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
