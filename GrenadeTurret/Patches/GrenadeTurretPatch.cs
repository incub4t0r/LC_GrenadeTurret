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

            //if (__instance.turretMode == TurretMode.Firing && 0.21f >= turretInterval)
            if (__instance.turretMode == TurretMode.Firing && 0f == turretInterval)
            {
                GrenadeTurretBase.GetLogger().LogInfo("Turret is firing!!!");

                Vector3 position = __instance.aimPoint.position;
                Vector3 direction = __instance.aimPoint.forward;
                float maxDistance = 50f;
                Ray ray = new Ray(position, direction);
                RaycastHit hitInfo;

                //if (Physics.Raycast(ray, out hitInfo, maxDistance, StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers))
                if (Physics.Raycast(ray, out hitInfo, maxDistance, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
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

        /// <summary>
        /// Iterates through the given instructions and checks if the opcodes match the expected opcodes starting at the given index
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="expectedOpcodes"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        static bool IsOpcodesMatch(List<CodeInstruction> instructions, OpCode[] expectedOpcodes,  int startIndex)
        {
            for (int j = 0; j < 11; j++)
            {
                if (instructions[startIndex + j].opcode != expectedOpcodes[j])
                {
                    return false;
                }
            }
            GrenadeTurretBase.GetLogger().LogInfo("Found a nopper");

            return true;
        }

        /// <summary>
        /// Checks if the instruction at the given index is a call to game network manager get instance method
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        static bool IsCallGNMGetInstance(List<CodeInstruction> instructions, int index)
        {
            if (instructions[index].opcode == OpCodes.Call &&
                instructions[index].operand is MethodInfo method &&
                method.DeclaringType == typeof(GameNetworkManager) &&
                method.Name == "get_Instance")
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// An array of OpCodes that are used to calculate the damage for the turret
        /// </summary>
        private static readonly OpCode[] DamageOpcodes = new OpCode[]
        {
            OpCodes.Ldfld,
            OpCodes.Ldc_I4_S,
            OpCodes.Ldc_I4_1,
            OpCodes.Ldc_I4_1,
            OpCodes.Ldc_I4_7,
            OpCodes.Ldc_I4_0,
            OpCodes.Ldc_I4_0,
            OpCodes.Ldloca_S,
            OpCodes.Initobj,
            OpCodes.Ldloc_3,
            OpCodes.Callvirt
        };

        /// <summary>
        /// A transpiler patch that nops the damage calculation code for the turret
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(Turret), "Update")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int startIndex = 0; startIndex < instructionList.Count; startIndex++)
            {
                if (IsCallGNMGetInstance(instructionList, startIndex) && IsOpcodesMatch(instructionList, DamageOpcodes, startIndex + 1))
                {
                    for (int j = 0; j < 12; j++)
                    {
                        instructionList[startIndex + j].opcode = OpCodes.Nop;
                        GrenadeTurretBase.GetLogger().LogInfo("Nopped");
                    }
                }
            }

            return instructionList.AsEnumerable();
        }

        internal static GameObject mapPropsContainer;
        internal static SpawnableMapObject turretSMO;

        /// <summary>
        /// Finds the turret spawnable map object and stores it in a static variable
        /// </summary>
        /// <param name="__instance"></param>
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

        //private static ArrayList customTurrets = new ArrayList();

        //[HarmonyPatch(typeof(StartOfRound), "ShipLeave")]
        //[HarmonyPostfix]
        //private static void CleanupTurrets()
        //{
        //    foreach (GameObject turret in customTurrets)
        //    {
        //        GrenadeTurretBase.GetLogger().LogInfo("Removing turret");
        //        GameObject turretGameObject = turret;
        //        UnityEngine.Object.Destroy((UnityEngine.Object)(object)turretGameObject);
        //    }
        //}

        //[HarmonyPatch(typeof(StartOfRound), "OnShipLandedMiscEvents")]
        //[HarmonyPostfix]
        //private static void ExtraLandedEvents()
        //{
        //    GrenadeTurretBase.GetLogger().LogInfo("------------------- Ship Landed! -------------------");
        //    GrenadeTurretBase.GetLogger().LogInfo("Searching for main entrance location...");
        //    EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(false);
        //    Vector3 spawnLocation = new Vector3(0f, 0f, 0f);

        //    for (int i = 0; i < array.Length; i++)
        //    {
        //        if (array[i].entranceId == 0)
        //        {
        //            if (array[i].isEntranceToBuilding)
        //            {
        //                GrenadeTurretBase.GetLogger().LogInfo("Found main entrance location");
        //                spawnLocation = array[i].entrancePoint.position;
        //            }
        //        }
        //    }

        //    GrenadeTurretBase.GetLogger().LogInfo("------------------- Adding 1 Turret at " + spawnLocation.ToString() + " -------------------");
        //    Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);
        //    GameObject newTurret = UnityEngine.Object.Instantiate<GameObject>(turretSMO.prefabToSpawn, spawnLocation, rotation, mapPropsContainer.transform);
        //    newTurret.GetComponent<NetworkObject>().Spawn(true);
        //    customTurrets.Add(newTurret);
        //    GrenadeTurretBase.GetLogger().LogInfo("------------------- Turret added -------------------");


        //    // spawn a shovel at spawnlocation
        //}
    }
}
