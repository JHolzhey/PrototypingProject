using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using UnityEngine;

public static class CommonLib
{
    public static GameObject ObjectBetween2Points(float3 point1, float3 point2, PrimitiveType primitiveType) {
        return ObjectBetween2Points(point1, point2, GameObject.CreatePrimitive(primitiveType));
    }

    public static GameObject ObjectBetween2Points(float3 point1, float3 point2, GameObject obj) {
        float3 vector = point2 - point1;
        float3 vectorUnit = math.normalize(vector);
        float3 rightVector = math.cross(vectorUnit, math.up());
        float3 upVector = math.cross(rightVector, vectorUnit);
        Quaternion rotation = Quaternion.LookRotation(upVector, vectorUnit);
        
        obj.transform.rotation = rotation;
        obj.transform.position = point1 + (vector/2);
        obj.Resize(new float3(obj.transform.localScale.x, math.length(vector), obj.transform.localScale.z));
        // obj.transform.localScale = new float3(obj.transform.localScale.x, math.length(vector), obj.transform.localScale.z);
        return obj;
    }

    public static GameObject CreatePrimitive(PrimitiveType primitiveType, float3 position, float3 localScale, Color color, Quaternion localRotation = new Quaternion(), float destroyTime = math.INFINITY) {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.transform.position = position;
        primitive.transform.localScale = localScale;
        primitive.transform.localRotation = localRotation;
        primitive.GetComponent<Renderer>().material.color = color;
        if (destroyTime != math.INFINITY) {
            Object.Destroy(primitive, destroyTime);
        }
        return primitive;
    }

    public static void Resize(this GameObject obj, float3 size) {
        Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
        Bounds meshBounds = mesh.bounds;
        float3 localScale = size / meshBounds.size;

        obj.transform.localScale = localScale;
    }

    public static T[] SubArray<T>(this T[] data, int startIndex, int length) { // Shallow copy
        T[] result = new T[length];
        System.Array.Copy(data, startIndex, result, 0, length); // keep System. since using System causes GameObject conflict
        return result;
    }

    public static T[] Combine<T>(this T[] frontData, T[] backData) { // Shallow copy
        T[] combined = new T[frontData.Length + backData.Length];
        System.Array.Copy(frontData, combined, frontData.Length);
        System.Array.Copy(backData, 0, combined, frontData.Length, backData.Length);
        return combined;
    }

    public static Color[] CycleColors = { Color.blue, Color.cyan, Color.green, Color.yellow, Color.red };
}

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
}