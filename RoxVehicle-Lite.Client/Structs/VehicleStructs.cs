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
            public AntiLagSounds AntiLagSounds;
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
            public Turbo Turbo2;

            public VehicleConfig(Transmission Transmission, Turbo Turbo, Turbo Turbo2, Engine Engine)
            {
                this.Transmission = Transmission;
                this.Engine = Engine;
                this.Turbo = Turbo;
                this.Turbo2 = Turbo2;
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
            public float RPMSpoolStart;
            public float RPMSpoolEnd;
            public float FalloffRPM;
            public float FalloffBoost;
            public bool IncreaseBoostToCounteractElevationLoss;
            public TurboAntiLag AntiLag
;
            public Turbo(float MaxBoost, float MaxVacuum, float BoostRate, float VacuumRate, float RPMSpoolStart, float RPMSpoolEnd, float FalloffRPM, float FalloffBoost, bool IncreaseBoostToCounteractElevationLoss, TurboAntiLag AntiLag)
            {
                this.MaxBoost = MaxBoost;
                this.MaxVacuum = MaxVacuum;
                this.BoostRate = BoostRate;
                this.VacuumRate = VacuumRate;
                this.RPMSpoolStart = RPMSpoolStart;
                this.RPMSpoolEnd = RPMSpoolEnd;
                this.FalloffBoost = FalloffBoost;
                this.FalloffRPM = FalloffRPM;
                this.IncreaseBoostToCounteractElevationLoss = IncreaseBoostToCounteractElevationLoss;
                this.AntiLag = AntiLag;
            }
        }
        public struct TurboAntiLag
        { 
            public bool Enable;
            public float MinRPM;
            public bool Effects;
            public int RandomMs;
            public int PeriodMs;
            public int LoudOffThrottleIntervalMs;
            public bool LoudOffThrottle;
            public List<string> AntiLagSounds;

            public TurboAntiLag(bool Enable, float MinRPM, bool Effects, int RandomMs, int PeriodMs, int LoudOffThrottleIntervalMs, bool LoudOffThrottle, List<string> AntiLagSounds)
            {
                this.Enable = Enable;
                this.MinRPM = MinRPM;
                this.Effects = Effects;
                this.RandomMs = RandomMs;
                this.PeriodMs = PeriodMs;
                this.LoudOffThrottleIntervalMs = LoudOffThrottleIntervalMs;
                this.LoudOffThrottle = LoudOffThrottle;
                this.AntiLagSounds = AntiLagSounds;
            }
        }
        public struct AntiLagSounds
        { 
            public List<string> RegisterSoundBank;
            public Dictionary<string, ExaustPopSounds> ExaustPopSounds;

            public AntiLagSounds(List<string> RegisterSoundBank, Dictionary<string, ExaustPopSounds> ExaustPopSounds)
            {
                this.RegisterSoundBank = RegisterSoundBank;
                this.ExaustPopSounds = ExaustPopSounds;
            }
        }
        public struct ExaustPopSounds
        { 
            public string SoundString;
            public string Ref;

            public ExaustPopSounds(string SoundString, string Ref)
            {
                this.SoundString = SoundString;
                this.Ref = Ref;
            }
        }
    }
}
