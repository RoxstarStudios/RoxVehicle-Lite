using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;
using CitizenFX.Core.Native;

using Newtonsoft.Json;

using static CitizenFX.Core.Native.API;

namespace RoxVehicle_Lite.Client
{
    public class Main : BaseScript
    {
        public static Main Instance { get; private set; }
        public Main()
        {
            Instance = this;
            _ = Transmission.Transmission.Instance;
        }
        internal dynamic GetGlobalState(string stateName)
        {
            return GlobalState.Get(stateName);
        }
        internal void AddEventHandler(string eventName, Delegate @delegate)
        {
            EventHandlers[eventName] += @delegate;
        }
        internal void AttachTick(Func<Task> task)
        {
            Tick += task;
        }
        internal void DetachTick(Func<Task> task)
        {
            Tick -= task;
        }
        internal void Export(string val, Delegate obj)
        {
            Exports.Add(val, obj);
        }
    }
}