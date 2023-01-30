using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class MathLib
{
    public static float3 Reflect(float3 vector, float3 normal, float restitution) { // restitution 0 is most bouncy, 1 is least bouncy
        return vector - (2.0f - restitution) * math.project(vector, normal);
    }

    public static bool IsSpheresIntersecting(float3 sphereCenter, float sphereRadius, float3 point) {
        float3 pointToSphere = sphereCenter - point; // * vec3Mask // Mask is to make it only 2D
        float distanceToSphereSqrd = math.dot(pointToSphere, pointToSphere);
        if (distanceToSphereSqrd <= sphereRadius*sphereRadius) {
            return true;
        }
        return false;
    }

    public static bool IsSphereRayIntersecting(float3 sphereCenter, float sphereRadius, float3 rayStart, float3 rayDirection, float rayLength, out float3 nearestPointOnRay) {
        nearestPointOnRay = NearestPointOnRayToPoint(sphereCenter, rayStart, rayDirection, rayLength);
        return IsSpheresIntersecting(sphereCenter, sphereRadius, nearestPointOnRay);
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
        float3 rejection = posDiff - math.project(posDiff, line2Direction) - math.project(posDiff, crossNormal);
        float3 distanceToLinePos = math.length(rejection) / math.dot(line1Direction, math.normalize(rejection));
        return line1Point - line1Direction * distanceToLinePos;
    }

    public static float3 NearestPointOnRayToLine(float3 rayStart, float3 rayDirection, float rayLength, float3 linePoint, float3 lineDirection)
    {
        float3 posDiff = rayStart - linePoint;
        float3 crossNormal = math.normalize(math.cross(rayDirection, lineDirection));
        float3 rejection = posDiff - math.project(posDiff, lineDirection) - math.project(posDiff, crossNormal);
        float3 distanceToLinePos = math.clamp( math.length(rejection) / math.dot(rayDirection, math.normalize(rejection)), 0, rayLength );
        return rayStart - rayDirection * distanceToLinePos;
    }

    public static float ShortestDistanceBtwLines(float3 line1Direction, float3 line2Direction, float3 line1Point, float3 line2Point) {
        return ShortestDistanceBtwLines(line1Direction, line2Direction, line2Point - line1Point);
    }

    public static float ShortestDistanceBtwLines(float3 line1Direction, float3 line2Direction, float3 arbitraryPoint1ToPoint2Vector) {
        float3 crossNormal = math.normalize(math.cross(line1Direction, line2Direction));
        return math.dot(crossNormal, -arbitraryPoint1ToPoint2Vector);
    }

    public static bool IsRayAACapsuleIntersecting(float3 rayStart, float3 rayEnd, float3 capsuleSphereBottom, float3 capsuleSphereTop, float capsuleLength, float capsuleRadius) {
        float3 toCapsuleMaxPos = new float3(capsuleRadius);

        RayToAABB(rayStart, rayEnd, out float3 minPosition, out float3 maxPosition);

        if (IsAABBsIntersecting(minPosition, maxPosition, capsuleSphereBottom - toCapsuleMaxPos, capsuleSphereTop + toCapsuleMaxPos)) {
            float3 rayVector = rayEnd - rayStart;
            float3 rayDirection = math.normalize(rayVector);
            float rayLength = math.length(rayVector);

            float3 capsuleVector = capsuleSphereTop - capsuleSphereBottom;
            float3 capsuleVectorDirection = math.normalize(capsuleVector);

            // float3 nearestPointOnRay = NearestPointOnRayToLine(rayStart, rayDirection, rayLength, capsuleSphereBottom, capsuleVectorDirection);
            float3 nearestPointOnCapsuleRay = NearestPointOnRayToLine(capsuleSphereBottom, -capsuleVectorDirection, capsuleLength, rayStart, rayDirection);
            CommonLib.CreatePrimitive(PrimitiveType.Sphere, nearestPointOnCapsuleRay, new float3(0.1f), Color.red, new Quaternion(), 5.0f);
            
            bool isIntersecting = (IsSphereRayIntersecting(nearestPointOnCapsuleRay, capsuleRadius, rayStart, rayDirection, rayLength, out float3 thing));
            CommonLib.CreatePrimitive(PrimitiveType.Sphere, thing, new float3(0.1f), Color.yellow, new Quaternion(), 5.0f);
            return isIntersecting;
        }
        return false;
    }

    public static bool IsAABBsIntersecting(float3 minPosBox1, float3 maxPosBox1, float3 minPosBox2, float3 maxPosBox2) {
        CommonLib.CreatePrimitive(PrimitiveType.Cube, minPosBox1, new float3(0.05f), Color.blue, new Quaternion(), 5.0f);
        CommonLib.CreatePrimitive(PrimitiveType.Cube, maxPosBox1, new float3(0.05f), Color.black, new Quaternion(), 5.0f);

        CommonLib.CreatePrimitive(PrimitiveType.Cube, minPosBox2, new float3(0.05f), Color.green, new Quaternion(), 5.0f);
        CommonLib.CreatePrimitive(PrimitiveType.Cube, maxPosBox2, new float3(0.05f), Color.magenta, new Quaternion(), 5.0f);
        return ((minPosBox1.y <= maxPosBox2.y && minPosBox2.y <= maxPosBox1.y)
            && (minPosBox1.x <= maxPosBox2.x && minPosBox2.x <= maxPosBox1.x)
            && (minPosBox1.z <= maxPosBox2.z && minPosBox2.z <= maxPosBox1.z));
    }

    public static void RayToAABB(float3 rayStart, float3 rayEnd, out float3 minPosition, out float3 maxPosition) {
        maxPosition = math.max(rayStart, rayEnd);
        minPosition = math.min(rayStart, rayEnd);
    }
}
