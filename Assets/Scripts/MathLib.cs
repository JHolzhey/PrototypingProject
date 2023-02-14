using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class MathLib
{
    public static float Square(float number) {
        return number*number;
    }

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

    public static bool IsRaySphereIntersecting(float3 rayStart, float3 rayDirection, float rayLength, float3 sphereCenter, float sphereRadius, out float distanceAlongRay) {
        float3 nearestPointOnRay = NearestPointOnRayToPoint(sphereCenter, rayStart, rayDirection, rayLength, out distanceAlongRay);
        return IsSpheresIntersecting(sphereCenter, sphereRadius, nearestPointOnRay);
    }

    public static bool IsRaySphereIntersecting(float3 rayStart, float3 rayDirection, float rayLength, float3 sphereCenter, float sphereRadius, out float3 nearestPointOnRay) {
        nearestPointOnRay = NearestPointOnRayToPoint(sphereCenter, rayStart, rayDirection, rayLength, out float distanceAlongRay);
        return IsSpheresIntersecting(sphereCenter, sphereRadius, nearestPointOnRay);
    }
    // Untested:
    public static bool IsPointCapsuleIntersecting(float3 point, float3 capsuleSphereBottom, float3 capsuleSphereTop, float capsuleLength, float capsuleRadius, out float3 nearestPointOnCapsuleRay) {
        return IsRaySphereIntersecting(capsuleSphereBottom, math.normalize(capsuleSphereTop - capsuleSphereBottom), capsuleLength, point, capsuleRadius, out nearestPointOnCapsuleRay);
    }

    public static bool IsSpherePlaneIntersecting(float3 sphereCenter, float sphereRadius, float3 planeNormal, float3 pointOnPlane, out float penetration) {
        float3 pointOnPlaneToSphere = sphereCenter - pointOnPlane;

        penetration = -(math.dot(pointOnPlaneToSphere, planeNormal) - sphereRadius);
        return penetration > 0;
    }

    public static bool IsRayPlaneIntersecting(float3 rayOrigin, float3 rayDirection, float rayLength, float3 planeNormal, float planeDistance, out float distanceAlongRay) {
        return IsRayPlaneIntersecting(rayOrigin, rayDirection, rayLength, planeNormal, planeDistance, out distanceAlongRay, out float3 _);
    }
    public static bool IsRayPlaneIntersecting(float3 rayOrigin, float3 rayDirection, float rayLength, float3 planeNormal, float planeDistance, out float3 nearestPointToPlane) {
        return IsRayPlaneIntersecting(rayOrigin, rayDirection, rayLength, planeNormal, planeDistance, out float _, out nearestPointToPlane);
    }
    public static bool IsRayPlaneIntersecting(float3 rayOrigin, float3 rayDirection, float rayLength, float3 planeNormal, float planeDistance, out float distanceAlongRay, out float3 nearestPointToPlane) {
        float constants = math.dot(rayOrigin, planeNormal);
        float coefficients = math.dot(rayDirection, planeNormal);
        distanceAlongRay = (planeDistance - constants) / coefficients;

        bool isIntersecting = true;
        if (distanceAlongRay < 0) { // This is essentially the clamp function written so we can set isIntersecting
            distanceAlongRay = 0; isIntersecting = false;
        } else if (distanceAlongRay > rayLength) {
            distanceAlongRay = rayLength; isIntersecting = false;
        }
        nearestPointToPlane = rayOrigin + (rayDirection * distanceAlongRay);

        return isIntersecting;
    }

    public static float3 ResolvePointPlanePenetration(float3 currentSphereCenter, float3 sphereVelocityDirection, float3 planeNormal, float penetration) { // penetration includes radius
        float dotDirectionNormal = math.dot(-sphereVelocityDirection, planeNormal);
        float dotCoeff = 1/dotDirectionNormal;
        float distanceBackwards = penetration * dotCoeff;
        return currentSphereCenter - sphereVelocityDirection * distanceBackwards; // returns resolved position so sphere is barely touching plane
    }

    public static float3 NearestPointOnRayToPoint(float3 point, float3 rayStart, float3 rayDirection, float rayLength, out float distanceAlongRay) {
        float3 rayStartToPoint = point - rayStart;
        distanceAlongRay = math.clamp(math.dot(rayStartToPoint, rayDirection), 0, rayLength);
        return rayStart + (rayDirection * distanceAlongRay);
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
        return rayStart + rayDirection * distanceToLinePos;
    }

    public static float ShortestDistanceBtwLines(float3 line1Direction, float3 line2Direction, float3 line1Point, float3 line2Point) {
        return ShortestDistanceBtwLines(line1Direction, line2Direction, line2Point - line1Point);
    }

    public static float ShortestDistanceBtwLines(float3 line1Direction, float3 line2Direction, float3 arbitraryPoint1ToPoint2Vector) {
        float3 crossNormal = math.normalize(math.cross(line1Direction, line2Direction));
        return math.dot(crossNormal, -arbitraryPoint1ToPoint2Vector);
    }

    // Untested but this might work for any capsule orientation:
    public static bool IsRayAACapsuleIntersecting(float3 rayStart, float3 rayEnd, float3 capsuleSphereBottom, float3 capsuleSphereTop, float capsuleLength, float capsuleRadius) {
        RayToAABB(rayStart, rayEnd, out float3 rayMinPosition, out float3 rayMaxPosition);
        CapsuleToAABB(capsuleSphereBottom, capsuleSphereTop, capsuleRadius, out float3 capsuleMinPosition, out float3 capsuleMaxPosition);

        if (IsAABBsIntersecting(rayMinPosition, rayMaxPosition, capsuleMinPosition, capsuleMaxPosition)) { // This all probably won't work in some cases
            float3 rayVector = rayEnd - rayStart;
            float3 rayDirection = math.normalize(rayVector);
            float rayLength = math.length(rayVector);

            float3 capsuleVector = capsuleSphereTop - capsuleSphereBottom;
            float3 capsuleVectorDirection = math.normalize(capsuleVector);

            // float3 nearestPointOnRay = NearestPointOnRayToLine(rayStart, rayDirection, rayLength, capsuleSphereBottom, capsuleVectorDirection);

            float3 nearestPointOnCapsuleRay = NearestPointOnRayToLine(capsuleSphereBottom, capsuleVectorDirection, capsuleLength, rayStart, rayDirection);
            // Capsule ray is from sphereBottom to sphereTop positions

            bool isIntersecting = IsRaySphereIntersecting(rayStart, rayDirection, rayLength, nearestPointOnCapsuleRay, capsuleRadius, out float3 _);
            // IsPointCapsuleIntersecting

            /* CommonLib.CreatePrimitive(PrimitiveType.Sphere, nearestPointOnCapsuleRay, new float3(0.1f), Color.red, new Quaternion(), 5.0f);
            CommonLib.CreatePrimitive(PrimitiveType.Sphere, _, new float3(0.1f), Color.yellow, new Quaternion(), 5.0f); */
            return isIntersecting;
        }
        return false;
    }

    public static bool IsAABBsIntersecting(float3 minPosBox1, float3 maxPosBox1, float3 minPosBox2, float3 maxPosBox2) {
        /* CommonLib.CreatePrimitive(PrimitiveType.Cube, minPosBox1, new float3(0.05f), Color.blue, new Quaternion(), 5.0f);
        CommonLib.CreatePrimitive(PrimitiveType.Cube, maxPosBox1, new float3(0.05f), Color.black, new Quaternion(), 5.0f);

        CommonLib.CreatePrimitive(PrimitiveType.Cube, minPosBox2, new float3(0.05f), Color.green, new Quaternion(), 5.0f);
        CommonLib.CreatePrimitive(PrimitiveType.Cube, maxPosBox2, new float3(0.05f), Color.magenta, new Quaternion(), 5.0f); */

        return ((minPosBox1.y <= maxPosBox2.y && minPosBox2.y <= maxPosBox1.y)
            && (minPosBox1.x <= maxPosBox2.x && minPosBox2.x <= maxPosBox1.x)
            && (minPosBox1.z <= maxPosBox2.z && minPosBox2.z <= maxPosBox1.z));
    }

    static void CapsuleToAABB(float3 capsuleSphereBottom, float3 capsuleSphereTop, float capsuleRadius, out float3 minPosition, out float3 maxPosition) {
        float3 minBtwSpheres = math.min(capsuleSphereBottom, capsuleSphereTop);
        float3 maxBtwSpheres = math.max(capsuleSphereBottom, capsuleSphereTop);
        
        float3 toCapsuleMaxPos = new float3(capsuleRadius);
        minPosition = minBtwSpheres - toCapsuleMaxPos;
        maxPosition = maxBtwSpheres + toCapsuleMaxPos;
    }

    static void RayToAABB(float3 rayStart, float3 rayEnd, out float3 minPosition, out float3 maxPosition) {
        minPosition = math.min(rayStart, rayEnd);
        maxPosition = math.max(rayStart, rayEnd);
    }
}
