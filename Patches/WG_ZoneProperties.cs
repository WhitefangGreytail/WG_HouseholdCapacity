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
        static HashSet<float> uniqueCalcString = new HashSet<float>();

        // This is the function I'm actually after
        public static BuildingPropertyData GetBuildingPropertyData(ZoneProperties __instance, BuildingPrefab buildingPrefab, byte level)
        {
            // Buildings don't change every level, so only make it different at 1, 3 and 5.
            // And reduce the increase until I figure out how to get the building height
            float num = 1f;
            float baseNum = 1f;
            float residentialProperties = __instance.m_ResidentialProperties;

            if (__instance.m_ScaleResidentials)
            {
                // m_ResidentialProperties appears to signify the type of residential
                // 1, 1.5, 2, 4, 6
                if (residentialProperties == 2)
                {
                    // Mixed use should have a small penalty from medium density
                    residentialProperties = 1.4f;
                }
                else if (residentialProperties == 6)
                {
                    // Modify residentialProperties to lower if constrained to a small 3 length on any side
                    if (math.min(buildingPrefab.m_LotWidth, buildingPrefab.m_LotDepth) <= 3)
                    {
                        residentialProperties /= 3;
                    }
                    // Change formula to 2 + .25
                    baseNum = 2f;
                }

                if (buildingPrefab.m_LotWidth == 1) {
                    // Row house
                    // We have 1x2 and 1x3 row house buildings, cap it at 3
                    num = (1f + 0.5f * Mathf.Floor((level - 1) / 2f)) * math.min(buildingPrefab.m_LotDepth, 3);
                }
                else
                {
                    num = (baseNum + 0.5f * Mathf.Floor((level - 1) / 2f)) * (float)buildingPrefab.lotSize;
                }

                // TODO
                string value = $"GetBuildingPropertyData {buildingPrefab.m_LotWidth}x{buildingPrefab.m_LotDepth} -> {__instance.m_ResidentialProperties} x {num}";
                if (!uniqueCalcString.Contains(__instance.m_ResidentialProperties))
                {
                    System.Console.WriteLine(__instance.m_ResidentialProperties);
                    uniqueCalcString.Add(__instance.m_ResidentialProperties);
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