using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class MathLib
{
    public static float3 Reflect(float3 vector, float3 normal, float restitution) { // restitution 0 is most bouncy, 1 is least bouncy
        return vector - (2.0f - restitution) * math.project(vector, normal);
    }

    public static bool IsSphereRayIntersecting(float3 point, float radius, float3 rayStart, float3 rayDirection, float rayLength, out float3 rayToSphere) {
        float3 nearestPointOnRay = NearestPointOnRayToPoint(point, rayStart, rayDirection, rayLength);
        rayToSphere = point - nearestPointOnRay; // * vec3Mask // Mask is to make it only 2D
        float distanceToRaySqrd = math.dot(rayToSphere, rayToSphere);
        if (distanceToRaySqrd <= radius*radius) {
            return true;
        }
        return false;
    }

    public static float3 NearestPointOnRayToPoint(float3 point, float3 rayStart, float3 rayDirection, float rayLength) {
        float3 rayStartToPoint = point - rayStart;
        float distanceOnRay = math.clamp(math.dot(rayStartToPoint, rayDirection), 0, rayLength);
        return rayStart + (rayDirection * distanceOnRay);
    }

    // https://math.stackexchange.com/q/3436386
    public static float3 NearestPointOnLine1ToLine2(float3 line1Point, float3 line1Direction, float3 line2Point, float3 line2Direction)
    {
        float3 posDiff = line1Point - line2Point;
        float3 crossNormal = math.normalize(math.cross(line1Direction, line2Direction));
        // float3 projection = math.project(pos_diff, a);
        float3 rejection = posDiff - math.project(posDiff, line2Direction) - math.project(posDiff, crossNormal);
        float3 distanceToLinePos = math.length(rejection) / math.dot(line1Direction, math.normalize(rejection));
        return line1Point - line1Direction * distanceToLinePos;
    }

    public static float ShortestDistanceBtwLines(float3 line1Direction, float3 line2Direction, float3 arbitraryPointToPointVector) {
        float3 crossNormal = math.normalize(math.cross(line1Direction, line2Direction));
        return math.dot(crossNormal, -arbitraryPointToPointVector);
    }
}
