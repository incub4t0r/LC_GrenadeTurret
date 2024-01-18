using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using GrenadeTurret.Patches;
using System.Net;

namespace GrenadeTurret
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class GrenadeTurretBase : BaseUnityPlugin
    {
        private const string modGUID = "f0ur3y3s.GrenadeTurret";
        private const string modName = "GrenadeTurret";
        private const string modVersion = "1.0.0";
        private readonly Harmony harmony = new Harmony(modGUID);
        private static GrenadeTurretBase Instance;
        static ManualLogSource logger;
        internal static ManualLogSource GetLogger()
        {
            return logger;
        }
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            logger = BepInEx.Logging.Logger.CreateLogSource(modName);
            logger.LogInfo("GrenadeTurret Loaded!");
            logger.LogInfo("Patching GrenadeTurretBase");
            harmony.PatchAll(typeof(GrenadeTurretBase));
            logger.LogInfo("Patching GrenadeTurretPatch");
            harmony.PatchAll(typeof(GrenadeTurretPatch));
        }
    }
}
