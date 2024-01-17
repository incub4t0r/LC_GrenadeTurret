using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using System.Collections;
using Unity.Netcode;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;


namespace GrenadeTurret.Patches
{


    internal class GrenadeTurretPatch
    {
        [HarmonyPatch(typeof(Turret), "Update")]
        [HarmonyPostfix]
        private static void UpdatePatch(Turret __instance)
        {
            FieldInfo turretIntervalField = typeof(Turret).GetField("turretInterval", BindingFlags.NonPublic | BindingFlags.Instance);
            float turretInterval = (float)turretIntervalField.GetValue(__instance);

            if (__instance.turretMode == TurretMode.Firing && 0f == turretInterval)
            {
                GrenadeTurretBase.GetLogger().LogInfo("Turret is firing!!!");

                Vector3 position = __instance.aimPoint.position;
                Vector3 direction = __instance.aimPoint.forward;
                float maxDistance = 50f;
                Ray ray = new Ray(position, direction);
                RaycastHit hitInfo;

                if (Physics.Raycast(ray, out hitInfo, maxDistance, StartOfRound.Instance.collidersAndRoomMask))
                {
                    GrenadeTurretBase.GetLogger().LogInfo("Turret bullet hit at " + hitInfo.point.ToString());
                    GrenadeTurretBase.GetLogger().LogInfo("Turret bullet hit on " + hitInfo.collider.gameObject.name);
                    GrenadeTurretBase.GetLogger().LogInfo("Turret bullet hit on " + hitInfo.collider.gameObject.tag);
                    GrenadeTurretBase.GetLogger().LogInfo("Turret bullet hit on " + hitInfo.collider.gameObject.layer.ToString());
                    Vector3 hitSpawnLocation = hitInfo.point;
                    Landmine.SpawnExplosion(hitSpawnLocation + Vector3.up, spawnExplosionEffect: true, 5.7f, 6.4f);
                    GrenadeTurretBase.GetLogger().LogInfo("Spawning explosion");
                }
            }
        }

        [HarmonyPatch(typeof(Turret), "Update")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            for (int i = 0; i < instructionList.Count; i++)
            {
                if (instructionList[i].opcode == OpCodes.Call &&
                    instructionList[i].operand is MethodInfo method &&
                    method.DeclaringType == typeof(GameNetworkManager) &&
                    method.Name == "get_Instance")
                {
                    if (instructionList[i + 1].opcode == OpCodes.Ldfld &&
                        instructionList[i + 2].opcode == OpCodes.Ldc_I4_S &&
                        instructionList[i + 3].opcode == OpCodes.Ldc_I4_1 &&
                        instructionList[i + 4].opcode == OpCodes.Ldc_I4_1 &&
                        instructionList[i + 5].opcode == OpCodes.Ldc_I4_7 &&
                        instructionList[i + 6].opcode == OpCodes.Ldc_I4_0 &&
                        instructionList[i + 7].opcode == OpCodes.Ldc_I4_0 &&
                        instructionList[i + 8].opcode == OpCodes.Ldloca_S &&
                        instructionList[i + 9].opcode == OpCodes.Initobj &&
                        instructionList[i + 10].opcode == OpCodes.Ldloc_3 &&
                        instructionList[i + 11].opcode == OpCodes.Callvirt
                        )
                    {
                        GrenadeTurretBase.GetLogger().LogInfo("Found damageplayer");
                        for (int j = 0; j < 12; j++)
                        {
                            instructionList[i + j].opcode = OpCodes.Nop;
                            GrenadeTurretBase.GetLogger().LogInfo("Nopped");

                        }
                    }
                }
            }

            return instructionList.AsEnumerable();
        }

        private static ArrayList customTurrets = new ArrayList();

        [HarmonyPatch(typeof(StartOfRound), "ShipLeave")]
        [HarmonyPostfix]
        private static void CleanupMines()
        {
            foreach (GameObject turret in customTurrets)
            {
                GrenadeTurretBase.GetLogger().LogInfo("Removing turret");
                //GameObject val = turret.gameObject;
                GameObject val = turret;
                UnityEngine.Object.Destroy((UnityEngine.Object)(object)val);
            }
        }

        internal static GameObject mapPropsContainer;
        internal static SpawnableMapObject turretSMO;

        [HarmonyPatch(typeof(RoundManager), "SpawnMapObjects")]
        [HarmonyPostfix]

        private static void RoundManagerPatch(ref RoundManager __instance)
        {
            mapPropsContainer = GameObject.FindGameObjectWithTag("MapPropsContainer");
            foreach (SpawnableMapObject obj in __instance.currentLevel.spawnableMapObjects)
            {
                if (obj.prefabToSpawn.GetComponentInChildren<Turret>() == null)
                {
                    continue;
                }

                turretSMO = obj;

                if (turretSMO == null)
                {
                    GrenadeTurretBase.GetLogger().LogInfo("Turret map object is null");
                }

                GrenadeTurretBase.GetLogger().LogInfo("Turret map object found");
            }
        }


        [HarmonyPatch(typeof(StartOfRound), "OnShipLandedMiscEvents")]
        [HarmonyPostfix]
        private static void ExtraLandedEvents()
        {
            GrenadeTurretBase.GetLogger().LogInfo("------------------- Ship Landed! -------------------");
            GrenadeTurretBase.GetLogger().LogInfo("Searching for main entrance location...");
            EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(false);
            Vector3 spawnLocation = new Vector3(0f, 0f, 0f);

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].entranceId == 0)
                {
                    if (array[i].isEntranceToBuilding)
                    {
                        GrenadeTurretBase.GetLogger().LogInfo("Found main entrance location");
                        spawnLocation = array[i].entrancePoint.position;
                    }
                }
            }

            GrenadeTurretBase.GetLogger().LogInfo("------------------- Adding 1 Turret at " + spawnLocation.ToString() + " -------------------");
            GameObject newTurret = UnityEngine.Object.Instantiate<GameObject>(turretSMO.prefabToSpawn, spawnLocation, Quaternion.identity, mapPropsContainer.transform);
            newTurret.GetComponent<NetworkObject>().Spawn(true);
            customTurrets.Add(newTurret);
            GrenadeTurretBase.GetLogger().LogInfo("------------------- Turret added -------------------");
        }
    }
}
