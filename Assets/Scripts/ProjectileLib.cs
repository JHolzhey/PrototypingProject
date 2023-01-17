using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

public static class ProjectileLib
{
    public static float ComputeLaunchAngle(float horizontalDistance, float distanceY, float intialSpeed, out bool isInRange) { // Helper method for function below
        float distanceTimesG = GlobalConstants.GRAVITY * horizontalDistance; // Micro-optimizations
        float initialSpeedSqr = intialSpeed*intialSpeed;
        float inRoot = math.pow(initialSpeedSqr, 2) - (GlobalConstants.GRAVITY * ((distanceTimesG * horizontalDistance) + (2*distanceY * initialSpeedSqr)));
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

    public static void ComputeTrajectory(float3 startPoint, float3 targetPoint, float initialSpeed, bool allowOutOfRange, out Trajectory trajectory) {
        float3 distanceVector = targetPoint - startPoint;
        float3 horizontalDistanceVector = new float3(distanceVector.x, 0, distanceVector.z);
        float horizontalDistance = math.length(horizontalDistanceVector);
        
        float launchAngle = ComputeLaunchAngle(horizontalDistance, distanceVector.y, initialSpeed, out bool isInRange);
        if (!isInRange && !allowOutOfRange) {
            trajectory = new Trajectory(isInRange); 
            return;
        }
        
        float3 horizontalDirection = math.normalize(horizontalDistanceVector);
        float3 initialDirection = (horizontalDirection * math.cos(launchAngle)) + new float3(0, math.sin(launchAngle), 0);
        float3 initialVelocity = initialDirection * initialSpeed;
        
        float horizontalRangeHalf = ((initialSpeed*initialSpeed)/GlobalConstants.GRAVITY * (math.sin(2*launchAngle))) / 2;
        
        /*
        float initialVerticalSpeed = initialDirection.y * initialSpeed; // TODO: initialVelocity.y
        float flightTime
        if horizontalRangeHalf <= horizontalDistance then
            flightTime = ((initialVerticalSpeed+(math.sqrt(initialVerticalSpeed^2+(2*-g*((distanceVector.Y))))))/g)
        else          
            flightTime = ((initialVerticalSpeed-(math.sqrt(initialVerticalSpeed^2+(2*-g*((distanceVector.Y))))))/g)
        end
        trajectory.flightTime = flightTime
        */
        trajectory = new Trajectory(isInRange, startPoint, distanceVector, launchAngle, initialVelocity, horizontalRangeHalf);
    }
}

public struct Trajectory { // holds trajectory information and can do projectile position stepping
    public bool isInRange { get; private set; }
    public float launchAngle { get; private set; }
    public float3 distanceVector { get; private set; }
    // public float initialSpeed { get; private set; }
    // public float3 initialVelocity { get; private set; }
    // public float horizontalRangeHalf { get; private set; }
    public float inverseHorizontalRangeHalf { get; private set; }
    public float horizontalSpeed { get; private set; }
    public float initialVerticalSpeed { get; private set; }
    public float3 startPoint { get; private set; }

    public float pitchAngle { get; private set; }
    private float elapsedTime;
    private float3 velocity;
    public float3 position { get; private set; }
    public float horizontalDistanceTraveled { get; private set; }

    public Trajectory(bool isInRange, float3 startPoint, float3 distanceVector, float launchAngle, float3 initialVelocity, float horizontalRangeHalf) : this() {
        this.isInRange = isInRange;
        position = startPoint;
        this.startPoint = startPoint;
        pitchAngle = launchAngle;
        this.distanceVector = distanceVector;
        // this.initialSpeed = initialSpeed;
        velocity = initialVelocity;
        // this.horizontalRangeHalf = horizontalRangeHalf;
        horizontalSpeed = math.sqrt(initialVelocity.x*initialVelocity.x + initialVelocity.z*initialVelocity.z);
        initialVerticalSpeed = initialVelocity.y;

        inverseHorizontalRangeHalf = 1/horizontalRangeHalf;
        horizontalDistanceTraveled = 0;
        elapsedTime = 0;
    }
    public Trajectory(bool isInRange) : this() {
        this.isInRange = isInRange;
    }

    public void Step(float deltaTime) { // update velocity, position, and pitchAngle
        elapsedTime += deltaTime; // 1 float add
        horizontalDistanceTraveled += horizontalSpeed * deltaTime; // 1 float add, 1 float mult

        velocity -= math.up() * (GlobalConstants.GRAVITY * deltaTime); // 1 float3 add, 1 float3-float mult
        position += velocity * deltaTime; // 1 float3 add, 1 float3-float mult

        pitchAngle = (1 - (horizontalDistanceTraveled * inverseHorizontalRangeHalf)) * launchAngle;
        //projectilePart.Orientation = Vector3.new(math.deg(arrowAngle), orientationY, orientationZ)
    } // 2 float add, 1 float mult, 2 float3 add, 2 float3-float mult

    public float3 GetPositionAtTime(float time) {
        float horizontalDistanceTraveled = horizontalSpeed * time; // 1 float mult
        float y = (initialVerticalSpeed * time) - (GlobalConstants.GRAVITY * time * time) / 2; // 3 float mult, 1 float add
        float3 horizontalDirection = math.normalize(new float3(distanceVector.x, 0, distanceVector.z)); // Maybe make this a variable to avoid this calculation

        return startPoint + (horizontalDistanceTraveled * horizontalDirection + new float3(0, y, 0)); // 1 float3-float mult, 2 float3 add
    } // 1 float add, 4 float mult, 2 float3 add, 1 float3-float mult

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