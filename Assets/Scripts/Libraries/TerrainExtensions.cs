using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using UnityEngine;

public static class TerrainExtensions
{
    public static int[] layerToMaterialIndices;
    public static int numTerrainLayers;

    public static void InitLayerToMaterialIndices(this Terrain terrain) {
        numTerrainLayers = terrain.terrainData.GetAlphamaps(0, 0, 1, 1).Length;
        layerToMaterialIndices = new int[numTerrainLayers];
        for (int i = 0; i < numTerrainLayers; i++) {
            TerrainLayer terrainLayer = terrain.terrainData.terrainLayers[i];
            string layerName = terrainLayer.name;
            
            Regex regex = new Regex(@"^.*(?=(_Terrain))");
            Match match = regex.Match(layerName);

            bool matchSuccess = false;
            for (int j = 0; j < Materials.materialTypes.Length; j++) {
                if (Materials.materialTypes[j].name == match.Value) {
                    layerToMaterialIndices[i] = j;
                    matchSuccess = true;
                }
            }
            Debug.Assert(matchSuccess);
        }
    }

    public static Materials.Type SampleMaterial(this Terrain terrain, float3 position) {
        return Materials.materialTypes[terrain.SampleLayerIndex(position)];
    }

    public static int SampleLayerIndex(this Terrain terrain, float3 position) {
        int2 splatMapCoords = terrain.GetSplatMapCoords(position);
        float[,,] splatMapData = terrain.terrainData.GetAlphamaps(splatMapCoords.x, splatMapCoords.y, 1, 1);
        int maxBlendLayerIndex = default;
        float maxBlendLayerStrength = -1;
        for (int i = 0; i < numTerrainLayers; i++) {
            if (splatMapData[0,0,i] > maxBlendLayerStrength) {
                maxBlendLayerIndex = i;
                maxBlendLayerStrength = splatMapData[0,0,i];
            }
        }
        return layerToMaterialIndices[maxBlendLayerIndex];
    }

    public static float3 SampleNormal(this Terrain terrain, float3 position) {
        float2 normalizedCoords = terrain.GetNormalizedCoords(position);
        return terrain.terrainData.GetInterpolatedNormal(normalizedCoords.x, normalizedCoords.y);
    }

    private static int2 GetSplatMapCoords(this Terrain terrain, float3 position) {
        float2 alphamapSize = new float2(terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight);
        float2 normalizedCoords = terrain.GetNormalizedCoords(position);
        float2 splatMapCoords = normalizedCoords * alphamapSize;
        return new int2((int)splatMapCoords.x, (int)splatMapCoords.y);
    }

    public static float2 GetNormalizedCoords(this Terrain terrain, float3 position) {
        TerrainData terrainData = terrain.terrainData;
        float3 terrainBottomLeft = terrain.GetPosition();
        float3 normalizedCoords = (position - terrainBottomLeft) / terrainData.size;
        
        return new float2(normalizedCoords.x, normalizedCoords.z);
    }

    public static float3[] SampleSegHeights(this Terrain terrain, float3 start, float3 end) {
        TerrainData terrainData = terrain.terrainData;
        float3 terrainBottomLeft = terrain.GetPosition();
        float3 heightmapScale = terrainData.heightmapScale;

        Debug.Assert(heightmapScale.x == heightmapScale.z);

        List<float3> gridIntersectionPoints = MathLib.SegGridIntersections(start, end, terrainBottomLeft, heightmapScale.x);

        float3[] heights = new float3[gridIntersectionPoints.Count];

        for (int i = 0; i < gridIntersectionPoints.Count; i++) {
            heights[i] = gridIntersectionPoints[i];
            heights[i].y = Terrain.activeTerrain.SampleHeight(gridIntersectionPoints[i]);
        }

        return heights;
    }
}