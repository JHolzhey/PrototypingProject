using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

public static class ProjectileLib
{
    public static float g = GlobalConstants.GRAVITY;
    public static float ComputeLaunchAngle(float horizontalDistance, float distanceY, float intialSpeed, out bool isInRange) { // Helper method for function below
        float distanceTimesG = g * horizontalDistance; // Micro-optimizations
        float initialSpeedSqr = intialSpeed*intialSpeed;
        float inRoot = math.pow(initialSpeedSqr, 2) - (g * ((distanceTimesG * horizontalDistance) + (2*distanceY * initialSpeedSqr)));
        if (inRoot <= 0) { // Out of range
            isInRange = false; return 0.25f * math.PI;
        }
        float root = math.sqrt(inRoot);
        float answerAngle1 = math.atan((initialSpeedSqr - root)/distanceTimesG); // Angle is direct
        float answerAngle2 = math.atan((initialSpeedSqr + root)/distanceTimesG); // Angle is lofted TODO: Make a request
        if (answerAngle1 < answerAngle2) { // Maybe add a isLofted bool instead?
            isInRange = true; return answerAngle1; //print("Direct")
        } else {
            isInRange = true; return answerAngle2; //print("Lofted") // TODO: Get rid of this: it's never used, never happens in original slingshot too
        }
    }
    // Trajectory is a struct that contains important projectile trajectory values used in
    public static bool ComputeTrajectory(float3 startPoint, float3 targetPoint, float initialSpeed, bool allowOutOfRange, out Trajectory trajectory) {
        float3 distanceVector = targetPoint - startPoint;
        float3 horizontalDistanceVector = new float3(distanceVector.x, 0, distanceVector.z);
        float horizontalDistance = math.length(horizontalDistanceVector);
        
        float launchAngle = ComputeLaunchAngle(horizontalDistance, distanceVector.y, initialSpeed, out bool isInRange);
        if (!isInRange && !allowOutOfRange) {
            trajectory = new Trajectory(isInRange);
            return false;
        }
        
        float3 horizontalDirection = math.normalize(horizontalDistanceVector);
        float3 initialDirection = (horizontalDirection * math.cos(launchAngle)) + new float3(0, math.sin(launchAngle), 0);
        float3 initialVelocity = initialDirection * initialSpeed;
        
        float horizontalRangeHalf = ((initialSpeed*initialSpeed)/g * (math.sin(2*launchAngle))) / 2;
        
        float initialVerticalSpeed = initialDirection.y * initialSpeed; // TODO: initialVelocity.y
        float flightTime;
        if (horizontalRangeHalf <= horizontalDistance) {
            flightTime = ((initialVerticalSpeed+(math.sqrt(math.pow(initialVerticalSpeed,2)+(2*-g*((distanceVector.y))))))/g);
        } else {
            flightTime = ((initialVerticalSpeed-(math.sqrt(math.pow(initialVerticalSpeed,2)+(2*-g*((distanceVector.y))))))/g);
        }
        float flightTime2 = (horizontalDistance / math.sqrt(initialVelocity.x*initialVelocity.x + initialVelocity.z*initialVelocity.z));
        // Debug.Log("flightTime: " + flightTime);
        // Debug.Log("flightTime2: " + flightTime2);
        // trajectory.flightTime = flightTime
        
        trajectory = new Trajectory(isInRange, startPoint, distanceVector, launchAngle, initialVelocity, horizontalRangeHalf, horizontalDistance);
        return true;
    }

    public static float CalcHorizontalFlightTime(float horizontalDistance, float initialSpeed) {
        float launchAngle = math.asin( horizontalDistance * g / MathLib.Square(initialSpeed) ) / 2;
        return 2 * initialSpeed * math.sin(launchAngle) / g;
    }

    public static float3 CalcLeadTargetPosition(float3 startPoint, float3 targetPoint, float initialSpeed, float3 targetVelocity) {
        float3 distanceVector = targetPoint - startPoint;
        float timeToTarget = CalcHorizontalFlightTime(math.length(distanceVector), initialSpeed);

        float3 approxLeadPosition = targetPoint + targetVelocity * timeToTarget;
        return approxLeadPosition;
    }

    public static float2 CalcAimSpread(float3 startPoint, float3 targetPoint, float3 normal, float initialSpeed, float launchAngle, float horizontalAngleSpread, float verticalAngleSpread) {
        float3 distanceVector = targetPoint - startPoint;
        float distance = math.length(distanceVector);

        float3 horizontalDistanceVector = new float3(distanceVector.x, 0, distanceVector.z);
        float horizontalDistance = math.length(horizontalDistanceVector);

        float horizontalSpreadRadius = distance * math.tan(horizontalAngleSpread / 2); // TanOA

        float rangeShortFromVerticalSpread = MathLib.Square(initialSpeed) * math.sin(2*launchAngle) / g;
        // Debug.Log("verticalSpreadShort: " + rangeShortFromVerticalSpread);
        Debug.Log("horizontalSpreadRadius: " + horizontalSpreadRadius);


        float shortVerticalAngle = launchAngle - verticalAngleSpread;
        float shortY = CalcYFromX(horizontalDistance, shortVerticalAngle, initialSpeed);
        float shortToTargetY = targetPoint.y - shortY;
        float shortFinalPitchAngle = CalcPitchAngleFromX(horizontalDistance, launchAngle, initialSpeed, CalcHorizontalRange(launchAngle, initialSpeed));

        float rotatedSpreadInPitchAngle = math.cos(shortFinalPitchAngle) * shortToTargetY;

        return new float2(horizontalSpreadRadius*2, (distance - rangeShortFromVerticalSpread)*2);
    }

    public static float CalcHorizontalRange(float launchAngle, float initialSpeed) {
        return ((initialSpeed*initialSpeed)/g * (math.sin(2*launchAngle)));
    }

    public static float CalcPitchAngleFromX(float X, float launchAngle, float initialSpeed, float horizontalRange) {
        return (1 - (X * (1/(horizontalRange / 2)))) * launchAngle;
    }

    public static float CalcYFromX(float X, float launchAngle, float initialSpeed) {
        return math.tan(launchAngle)*X - g / (2 * MathLib.Square(initialSpeed) * MathLib.Square(math.cos(launchAngle))) * MathLib.Square(X);
    }
}

public struct Projectile {
    public Trajectory trajectory;
    public float3 velocity { get; set; } // Should be private set for these 2
    public float3 position { get; set; }
    float3 prevPosition;
    public float pitchAngle { get; private set; }
    public float timeAlive;
    float horizontalDistanceTraveled { get; set; }
    public bool isRolling;
    public bool hasBounced;

    // public float maxRange;
    // float initialSpeed;
    float radius; // 0 for arrows, spears, and axes
    float mass; // in kg
    float friction;

    public Projectile(float maxRange, float radius, float mass, float friction) : this() {
        // this.maxRange = maxRange;
        this.radius = radius;
        this.mass = mass;
        this.friction = friction;
    }

    public bool ComputeTrajectory(float3 startPoint, float3 targetPoint, float initialSpeed, bool allowOutOfRange) {
        if (!ProjectileLib.ComputeTrajectory(startPoint, targetPoint, initialSpeed, allowOutOfRange, out trajectory)) {
            return false;
        }
        velocity = trajectory.initialVelocity;
        position = trajectory.startPoint;
        pitchAngle = trajectory.launchAngle;
        timeAlive = 0;
        horizontalDistanceTraveled = 0;
        isRolling = false;
        hasBounced = false;
        return true;
    }

    public void Update(float deltaTime, BuildingGrid grid) { // Update timeAlive before or after?
        timeAlive += deltaTime;
        prevPosition = position;
        if (!hasBounced) {
            horizontalDistanceTraveled += trajectory.horizontalSpeed * deltaTime;
            position = trajectory.GetPositionAtTime(timeAlive, horizontalDistanceTraveled);
            velocity = position - prevPosition;
            // UpdatePitchAngle();
        } else {
            Step(deltaTime);
        }
        RayCast(grid);
        CheckTerrainCollision(deltaTime);
    }

    void RayCast(BuildingGrid grid) {
        int entityHit = grid.RayCast(entities, prevPosition, position);
        if (entityHit >= 0) {
            
        }
    }

    void CheckTerrainCollision(float deltaTime) {
        // New function for projectile terrain collision:
        float terrainY = Terrain.activeTerrain.SampleHeight(position);
        float3 terrainNormal = Terrain.activeTerrain.SampleNormal(position);
        float3 pointOnTerrainPlane = new float3(position.x, terrainY, position.z);
        
        // if (position.y - radius <= terrainY) { // basic quick method, better method below
        if (MathLib.IsSpherePlaneIntersecting(position, radius, terrainNormal, pointOnTerrainPlane, out float penetration))
        {
            if (!isRolling) { // Projectile has hit the ground for the first time because it has not been rolling
                float3 hitVelocity = (position - prevPosition) / deltaTime; // Manually calculate velocity because haven't been using Euler til now

                float constant = 0.000192f; // A = pi * 0.01^2, Cd = 1, rho = 1.225
                float initialSpeed = math.length(trajectory.initialVelocity);
                float dragDeceleration = MathLib.Square(initialSpeed) * constant / mass;
                float hitSpeed = initialSpeed - dragDeceleration * timeAlive;
                
                // can possibly use velocity since its being set in Update()
                float3 velocityDirection = math.normalize(hitVelocity);

                if (radius != 0) { // Is spherical
                    float3 fixPenetrationVector = (penetration) * terrainNormal;
                    position += fixPenetrationVector;

                    // float3 reflectDirection = math.reflect(velocityDirection, terrainNormal);
                    float restitution = 0.5f;
                    //float normalForce = mass * GlobalConstants.GRAVITY * math.dot(math.up(), terrainNormal);
                    float3 perpendicularVelocity = math.project(hitVelocity, terrainNormal);
                    float3 parallelVelocity = hitVelocity - perpendicularVelocity;
                    float3 result = (parallelVelocity) - (restitution * perpendicularVelocity);
                    
                    // float3 reflectedVelocity = MathLib.Reflect(hitVelocity, terrainNormal, 0.4f);
                    
                    velocity = result; // alternative: reflectedVelocity
                    isRolling = true;
                    hasBounced = true; // will start euler stepping after landing

                } else { // Is an arrow, spear, or axe
                    // Debug.Log("initialSpeed: " + initialSpeed + "    hitSpeed: " + hitSpeed);

                    Materials.Type material = Terrain.activeTerrain.SampleMaterial(position);
                    float density = material.density;
                    float materialDynamicPressure = 0.5f * density * MathLib.Square(initialSpeed);
                    float materialForce = materialDynamicPressure * (math.PI * MathLib.Square(radius + 0.01f));
                    
                    float materialPenetration = MathLib.Square(initialSpeed) / (2 * (materialForce/mass));
                    Debug.Log("materialPenetration: " + materialPenetration + "    radius: " + radius);

                    position = MathLib.ResolvePointPlanePenetration(position, velocityDirection, terrainNormal, penetration);

                    timeAlive = -1; // It is dead
                }

            } else { // Projectile sphere is rolling
                float3 fixPenetrationVector = (penetration-0.01f) * terrainNormal; // Pushes into the ground so next iteration will still be in ground and rolling
                position += fixPenetrationVector;

                float3 perpendicularVelocity = terrainNormal * math.dot(velocity, terrainNormal);
                float3 parallelVelocity = velocity - perpendicularVelocity;

                float normalForce = mass * GlobalConstants.GRAVITY * math.dot(math.up(), terrainNormal);

                velocity -= (perpendicularVelocity + (parallelVelocity * (deltaTime * normalForce * friction)));
            }
        } else {
            isRolling = false;
        }
    }

    public void Step(float deltaTime) { // update velocity and position using Euler integration
        velocity += math.down() * (GlobalConstants.GRAVITY * deltaTime);
        position += velocity * deltaTime;
    }

    public void UpdatePitchAngle() {
        pitchAngle = (1 - (horizontalDistanceTraveled * trajectory.inverseHorizontalRangeHalf)) * trajectory.launchAngle;
        //projectilePart.Orientation = Vector3.new(math.deg(arrowAngle), orientationY, orientationZ)
    }
}

public struct Trajectory { // holds trajectory information and can do projectile position stepping
    public bool isInRange { get; private set; }
    public float launchAngle { get; private set; }
    public float3 distanceVector { get; private set; }
    public float3 horizontalDirection { get; private set; }
    // public float initialSpeed { get; private set; }
    // public float3 initialVelocity { get; private set; }
    // public float horizontalRangeHalf { get; private set; }
    public float inverseHorizontalRangeHalf { get; private set; }
    
    public float3 startPoint { get; private set; }
    public float3 initialVelocity { get; private set; }
    public float horizontalSpeed { get; private set; } // constant throughout flight
    public float initialVerticalSpeed { get; private set; }
    public float flightTime { get; private set; }

    public Trajectory(bool isInRange, float3 startPoint, float3 distanceVector, float launchAngle, float3 initialVelocity, float horizontalRangeHalf, float horizontalDistance) : this() {
        this.isInRange = isInRange;
        this.launchAngle = launchAngle;
        this.distanceVector = distanceVector;
        horizontalDirection = math.normalize(new float3(distanceVector.x, 0, distanceVector.z));
        // this.horizontalRangeHalf = horizontalRangeHalf;
        this.startPoint = startPoint;
        this.initialVelocity = initialVelocity;
        horizontalSpeed = math.sqrt(initialVelocity.x*initialVelocity.x + initialVelocity.z*initialVelocity.z);
        initialVerticalSpeed = initialVelocity.y;
        flightTime = horizontalDistance / horizontalSpeed;

        inverseHorizontalRangeHalf = 1/horizontalRangeHalf;
    }
    public Trajectory(bool isInRange) : this() {
        this.isInRange = isInRange;
    }

    public float3 GetPositionAtTime(float time) {
        return GetPositionAtTime(time, horizontalSpeed * time);
    }

    public float3 GetPositionAtTime(float time, float horizontalDistanceTraveled) {
        float y = (initialVerticalSpeed * time) - (GlobalConstants.GRAVITY * time * time) / 2;

        return startPoint + (horizontalDistanceTraveled * horizontalDirection + new float3(0, y, 0));
    }

    public float3[] GetPositionsOnArc(int divisions) {
        float3[] positions = new float3[divisions+1];

        float g = GlobalConstants.GRAVITY;
        float fullFlightTime = ((initialVerticalSpeed+(math.sqrt(math.pow(initialVerticalSpeed,2)+(2*-g*((distanceVector.y))))))/g);
        float timeStep = fullFlightTime / divisions;
        //Debug.Log(fullFlightTime);

        positions[0] = startPoint;
        for (int i = 1; i < divisions+1; i++) {
            positions[i] = GetPositionAtTime(timeStep*i);
        }
        return positions;
    }
}