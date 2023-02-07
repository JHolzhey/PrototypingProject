using System.Collections;
using System.Collections.Generic;
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
        obj.transform.localScale = new float3(obj.transform.localScale.x, math.length(vector), obj.transform.localScale.z);
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

    public static float3 SampleNormal(this Terrain terrain, float3 position) {
        // TerrainData terrainData = Terrain.activeTerrain.terrainData;
        TerrainData terrainData = terrain.terrainData;
        float3 terrainBottomLeft = terrain.GetPosition();
        float3 normalizedPositon = (position - terrainBottomLeft) / terrainData.size;
        return terrainData.GetInterpolatedNormal(normalizedPositon.x, normalizedPositon.z);
    }

    public static T[] SubArray<T>(this T[] data, int startIndex, int length) { // Shallow copy
        T[] result = new T[length];
        System.Array.Copy(data, startIndex, result, 0, length);
        return result;
    }

    public static Color[] CycleColors = { Color.blue, Color.cyan, Color.green, Color.yellow, Color.red };
}
