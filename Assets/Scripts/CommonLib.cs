using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class CommonLib
{
    public static GameObject CubeBetween2Points(float3 point1, float3 point2) {
        return CubeBetween2Points(point1, point2, GameObject.CreatePrimitive(PrimitiveType.Cube));
    }

    public static GameObject CubeBetween2Points(float3 point1, float3 point2, GameObject cube) {
        float3 vector = point2 - point1;
        float3 vectorUnit = math.normalize(vector);
        float3 rightVector = math.cross(vectorUnit, math.up());
        float3 upVector = math.cross(rightVector, vectorUnit);

        Quaternion rotation = Quaternion.LookRotation(vectorUnit, upVector);
        cube.transform.rotation = rotation;
        cube.transform.position = point1 + (vector/2);
        cube.transform.localScale = new float3(cube.transform.localScale.x, cube.transform.localScale.y, math.length(vector));
        return cube;
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
}
