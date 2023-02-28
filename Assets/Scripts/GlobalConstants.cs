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
}

[System.Flags]
public enum EntityType
{
    None = 0,
    Being = 1,
    Soldier = Being << 1,
    Player = Soldier << 1,
    Mount = Player << 1,
    Surface = Mount << 1, // All building surfaces; walls, floors, roofs
    Wall = Surface << 1,
    WallTerrain = Wall << 1,
    Floor = WallTerrain << 1,
    Platform = Floor << 1,
    Projectile = Platform << 1,
    ProjectileSphere = Projectile << 1,
    MeleeAttack = ProjectileSphere << 1,
    Vertex = MeleeAttack << 1,
}