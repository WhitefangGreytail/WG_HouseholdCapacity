using HarmonyLib;
using UnityEngine;
using Game.Prefabs;
using Game.Simulation;
using Unity.Mathematics;
using Game.Economy;
using System.Collections.Generic;
using Unity.Entities;

/*
 * Scraps
 * 
 * This code has flow on effects across the entire simulation. Maybe best to just leave it.
*/
namespace WG_HouseholdCapacityModifier.Patches
{
    [HarmonyPatch(typeof(ZoneProperties), nameof(ZoneProperties.InitializeBuilding))]
    class WG_ZoneProperties_InitializeBuilding
    {
        [HarmonyPrefix]
        static bool InitializeBuilding_Prefix(ref ZoneProperties __instance, EntityManager entityManager, Entity entity, BuildingPrefab buildingPrefab, byte level)
        {
            if (!buildingPrefab.Has<BuildingProperties>())
            {
                BuildingPropertyData buildingPropertyData = WG_ZoneProperties_Static.GetBuildingPropertyData(__instance, buildingPrefab, level);
                entityManager.SetComponentData(entity, buildingPropertyData);
            }
            return false; // Skip original
        }
    }

    [HarmonyPatch(typeof(ZoneProperties), nameof(ZoneProperties.GetBuildingArchetypeComponents))]
    class WG_ZoneProperties_GetBuildingArchetypeComponents
    {
        [HarmonyPrefix]
        static bool GetBuildingArchetypeComponents(ref ZoneProperties __instance, HashSet<ComponentType> components, BuildingPrefab buildingPrefab, byte level)
        {
            if (!buildingPrefab.Has<BuildingProperties>())
            {
                BuildingPropertyData buildingPropertyData = WG_ZoneProperties_Static.GetBuildingPropertyData(__instance, buildingPrefab, level);
                BuildingProperties.AddArchetypeComponents(components, buildingPropertyData);
            }
            return false; // Skip original
        }
    }

    class WG_ZoneProperties_Static
    {
        static HashSet<string> uniqueCalcString = new HashSet<string>();

        // This is the function I'm actually after
        public static BuildingPropertyData GetBuildingPropertyData(ZoneProperties __instance, BuildingPrefab buildingPrefab, byte level)
        {
            // Buildings don't change every level, so only make it different at 1, 3 and 5.
            // And reduce the increase until I figure out how to get the building height
            float num = 1f;
            float baseNum = 1.5f;
            float levelBooster = 0.25f;
            float residentialProperties = __instance.m_ResidentialProperties;
            float lotSize = (float)buildingPrefab.lotSize;
            List<ComponentBase> ogd = new List<ComponentBase>();
            if (buildingPrefab.GetComponents(ogd)) {
                /*
                Game.Prefabs.BuildingPrefab
                Game.Prefabs.SpawnableBuilding <-
                Game.Prefabs.ObjectSubObjects
                Game.Prefabs.ObjectSubAreas
                Game.Prefabs.ObjectSubNets
                 */
                foreach (ComponentBase item in ogd)
                {
                    switch (item.GetType())
                    {
                        case 
                        default:
                            break;
                    }
                }
            }

            if (__instance.m_ScaleResidentials)
            {
                // m_ResidentialProperties appears to signify the type of residential
                // 1 - Row housing
                // 1.5 - Medium
                // 2 - Mixed
                // 4 - Low rent
                // 6 - Tower
                switch (residentialProperties)
                {
                    case 1f:
                        //if (buildingPrefab.m_LotWidth == 1) 
                        lotSize = math.min(buildingPrefab.m_LotDepth, 3);
                        break;
                    case 2f:
                        // Mixed use should slightly more as medium density
                        residentialProperties = 1.6f;
                        break;
                    case 6f:
                        // TODO - Reduce residentialProperties to lower if constrained to a short building
                        // TODO - Find the Crane or Bounds object
                        levelBooster = 0.125f;
                        break;
                    // No default
                }

                num = (baseNum + levelBooster * Mathf.Floor((level - 1) / 2f)) * lotSize;

                // TODO
                string value = $"GetBuildingPropertyData {buildingPrefab.m_LotWidth}x{buildingPrefab.m_LotDepth} -> {__instance.m_ResidentialProperties} * {num}";
                if (!uniqueCalcString.Contains(value))
                {
                    //System.Console.WriteLine(value);
                    //uniqueCalcString.Add(value);
                }
            }


            BuildingPropertyData result = default(BuildingPropertyData);
            result.m_ResidentialProperties = (int)math.round(num * residentialProperties);
            result.m_AllowedSold = EconomyUtils.GetResources(__instance.m_AllowedSold);
            result.m_AllowedManufactured = EconomyUtils.GetResources(__instance.m_AllowedManufactured);
            result.m_AllowedStored = EconomyUtils.GetResources(__instance.m_AllowedStored);
            result.m_SpaceMultiplier = __instance.m_SpaceMultiplier;
            return result;
        }
    }
}