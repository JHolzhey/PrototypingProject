using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

public class BuildingGrid : MonoBehaviour
{
    private Cell[,] grid;
    private int cellSize;
    private int2 dimensions;
    private int maxEntitiesPerCell;

    void Start()
    {
        cellSize = GlobalConstants.BUILDING_CELL_SIZE;
        dimensions = GlobalConstants.BUILDING_CELL_DIMENSIONS;
        maxEntitiesPerCell = GlobalConstants.MAX_ENTITIES_PER_BUILDING_CELL;

        grid = new Cell[dimensions.x, dimensions.y];
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                //Debug.Log(grid[x,y].numEntities);
                //buildingGrid[i,j] = (GameObject)Instantiate(TestMeteor, new Vector3(i, j, 0), Quaternion.identity);
            }
        }
    }

    void Update() {}

     void OnDrawGizmos() {
        if (grid != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(new float3(GlobalConstants.MAP_BOTTOM_LEFT), 1);
            int sum = 0;
            for (int x = 0; x < dimensions.x; x++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    float3 position = GlobalConstants.MAP_BOTTOM_LEFT + new float3(x+0.5f, 0, y+0.5f) * cellSize;
                    Gizmos.DrawWireCube(position, new float3(cellSize, 0.2f, cellSize));
                    sum++;
                }
            }
        }
    }

    public int AddEntityToCell(int2 cellCoords, int entity) {
        int index = this.grid[cellCoords.x, cellCoords.y].AddEntity(entity);
        //printf("Adding entity [%d] to cell: [%d | %d] gives index: [%d]\n", (int)entity, cell_coords.x, cell_coords.y, index);
        return index;
    }
    public void RemoveEntityFromCell(int2 cellCoords, int entity_index, int e) {
        this.grid[cellCoords.x, cellCoords.y].RemoveEntity(entity_index);
    }

    public void ClearAllCells() {
        //printf("Clearing all cells\n");
        for (uint X = 0; X < maxEntitiesPerCell; X++) {
            for (uint Y = 0; Y < maxEntitiesPerCell; Y++) {
                this.grid[X,Y].Clear();
            }
        }
    }
}

struct Cell
{
	// High, hard coded upper limit
	private int []entities;
	private int numEntities; // How many entities are currently held in the array above

    public Cell(int[] b1, int numEntities) {
        entities = b1;
        this.numEntities = numEntities;
    }

	public void RemoveEntity(int entityIndex) { // Moves entity at the end of entity list into removed index
        Assert.IsTrue((entityIndex >= 0) && (entityIndex < entities.Length) && (this.numEntities - 1 >= 0));
        //printf("Entity given: %d. At index: %d.    entity removed: %d.   entity at end: %d\n", e, entity_index, this->entities[entity_index], this->entities[this->num_entities - 1]);
        //printf("Num_entities before removing: %d. Array before removing:\n", this->num_entities);

        int entityAtEnd = this.entities[this.numEntities - 1];
        this.entities[entityIndex] = entityAtEnd;
        //Motion& motion = registry.motions.get(entity_at_end); // Kinda hacky
        //motion.cell_index = entityIndex;

        this.numEntities--;
        //printf("Num_entities after removing: %d\n", this->num_entities);
    }
	public int AddEntity(int entity) {
        Assert.IsTrue((this.numEntities >= 0) && (this.numEntities < entities.Length)); // "Error or need to add more space to entity list"
        this.entities[this.numEntities] = entity;
        this.numEntities++;
        //printf("Num_entities after adding = %d\n", this->num_entities);
        return this.numEntities - 1;
    }
    public void Clear() { // Might remove entity's index too
        this.numEntities = 0;
    }
};