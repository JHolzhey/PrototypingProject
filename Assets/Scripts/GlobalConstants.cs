using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class GlobalConstants : MonoBehaviour
{ // static global variables succeeded by interface variable for changing static variables with inspector
    public static float GRAVITY;                        public float gravity = 9.8f;
    public static int3 MAP_BOTTOM_LEFT;
    public static int3 MAP_DIMENSIONS;                  public int3 mapDimensions = new int3(200,100,200); // Might make float3
    public static int BUILDING_CELL_SIZE;               public int buildingCellSize = 2;
    public static int MAX_ENTITIES_PER_BUILDING_CELL;   public static int maxEntitiesPerBuildingCell = 20;
    public static int2 BUILDING_CELL_DIMENSIONS;

    void Awake()
    {
        GRAVITY = gravity;

        MAP_DIMENSIONS = mapDimensions;
        MAP_BOTTOM_LEFT = -new int3(MAP_DIMENSIONS.x/2, 0, MAP_DIMENSIONS.z/2);

        BUILDING_CELL_SIZE = buildingCellSize;
        MAX_ENTITIES_PER_BUILDING_CELL = maxEntitiesPerBuildingCell;
        BUILDING_CELL_DIMENSIONS = new int2(MAP_DIMENSIONS.x, MAP_DIMENSIONS.z) / BUILDING_CELL_SIZE;
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

    public static GameObject CubeBetween2Points(float3 point1, float3 point2) {
        return CubeBetween2Points(point1, point2, GameObject.CreatePrimitive(PrimitiveType.Cube));
    }
}
