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
        quaternion rotation = quaternion.LookRotation(upVector, vectorUnit);
        
        obj.transform.rotation = rotation;
        obj.transform.position = point1 + (vector/2);
        obj.Resize(new float3(obj.transform.localScale.x, math.length(vector), obj.transform.localScale.z));
        // obj.transform.localScale = new float3(obj.transform.localScale.x, math.length(vector), obj.transform.localScale.z);
        return obj;
    }

    public static GameObject CreatePrimitive(PrimitiveType primitiveType, float3 position, float3 localScale, Color color, quaternion localRotation = new quaternion(), float destroyTime = math.INFINITY) {
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

    public static Color[] CycleColors = { Color.blue, Color.cyan, Color.green, Color.yellow, Color.red };
}

public static class MiscExtensions
{
    public static RayInput ScreenPointToRay(this Camera camera, float3 screenPos, float rayLength = 100) {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        return new RayInput(ray.origin, ray.direction, rayLength);
    }
}

public static class ArrayExtensions
{
    public static T[] SubArray<T>(this T[] array, int startIndex, int length) { // Shallow copy
        T[] result = new T[length];
        System.Array.Copy(array, startIndex, result, 0, length); // keep System. since using System causes GameObject conflict
        return result;
    }

    public static T[] Concat<T>(this T[] frontData, T[] backData) { // Shallow copy
        T[] combined = new T[frontData.Length + backData.Length];
        System.Array.Copy(frontData, combined, frontData.Length);
        System.Array.Copy(backData, 0, combined, frontData.Length, backData.Length);
        return combined;
    }

    public static T[] Populate<T>(this T[] array, T value) {
        for ( int i = 0; i < array.Length;i++ ) {
            array[i] = value;
        }
        return array;
    }

    public static Vector2[] ConvertToVector2Array(this float2[] data) {
        Vector2[] castData = new Vector2[data.Length];
        for (int i = 0; i < data.Length; i++) {
            castData[i] = data[i];
        }
        return castData;
    }

    public static float2[] ConvertToFloat2Array(this Vector2[] data) {
        float2[] castData = new float2[data.Length];
        for (int i = 0; i < data.Length; i++) {
            castData[i] = data[i];
        }
        return castData;
    }
}