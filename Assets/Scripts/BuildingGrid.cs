using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

public class BuildingGrid
{
    private Cell[,] grid;
    public int cellSize { get; private set; } // Possibly change to float
    public int2 dimensions { get; private set; }
    private int maxEntitiesPerCell;
    private float3 bottomLeftWorld;

    public BuildingGrid()
    {
        cellSize = GlobalConstants.BUILDING_CELL_SIZE;
        dimensions = GlobalConstants.BUILDING_CELL_DIMENSIONS;
        maxEntitiesPerCell = GlobalConstants.MAX_ENTITIES_PER_BUILDING_CELL;
        bottomLeftWorld = GlobalConstants.MAP_BOTTOM_LEFT;

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

    public float3 CellCoordsToWorld(int2 cellCoords) {
        return bottomLeftWorld + (new float3(cellCoords.x+0.5f, 0, cellCoords.y+0.5f) * cellSize);
    }

    public int2 WorldToCellCoords(float3 position) {
        float3 relativeToBottomLeftPos = (position - bottomLeftWorld)/ cellSize;
	    return new int2((int)relativeToBottomLeftPos.x, (int)relativeToBottomLeftPos.z);
    }

    void AddRadiusCells(int2 center, List<int2> rasterCellCoords, float cellRadius, int2 startCoords, bool[,] usedCellGrid) // Helper for RasterLine function // TODO: Not working totally
    {
        if (cellRadius == 0) {
            rasterCellCoords.Add(center);
        } else {
            int intRadius = (int)cellRadius;
            for (int X = center.x - intRadius; X <= center.x + intRadius; X++) {
                for (int Y = center.y - intRadius; Y <= center.y + intRadius; Y++) {
                    //if (are_cell_coords_out_of_bounds({ X, Y })) { continue; }

                    int dx = X - center.x;
                    int dy = Y - center.y;
                    float distanceSquared = (dx*dx) + (dy*dy);

                    if (distanceSquared <= (cellRadius * cellRadius)) {
                        int usedX = math.abs(X - startCoords.x) + intRadius;
                        int usedY = math.abs(Y - startCoords.y) + intRadius;
                        if (!(usedCellGrid[usedX, usedY])) {
                            usedCellGrid[usedX, usedY] = true;
                            rasterCellCoords.Add(new int2(X, Y));
                        }
                    }
                }
            }
        }
    }
    public List<int2> RasterLine(float3 lineStart, float3 lineEnd, float radius = 0) // Separate function for radius > 0?
    {
        List<int2> rasterCellCoords = new List<int2>();

        float3 lineVector = lineEnd - lineStart;
        if (lineVector.x <= 0) {
            lineVector = -lineVector;
            float3 holder = lineStart; lineStart = lineEnd; lineEnd = holder;
        }

        float3 startRelativeToBottomLeftPos = (lineStart - bottomLeftWorld)/ cellSize;

        int lineYSign = (lineVector.z > 0) ? 1 : -1;
        float slopeM = lineVector.z / lineVector.x;

        int2 startCoords = WorldToCellCoords(lineStart);
        int2 endCoords = WorldToCellCoords(lineEnd);
        int currentY = startCoords.y;

        bool[,] usedCellGrid = new bool[0,0];
        float cellRadius = 0;
        if (radius > 0) {
            cellRadius = radius / cellSize;
            int sizeX = math.abs(endCoords.x - startCoords.x) + 1;
            int sizeY = math.abs(endCoords.y - startCoords.y) + 1;
            Debug.Log(startCoords);
            Debug.Log(endCoords);
            Debug.Log(sizeX);
            Debug.Log(sizeY);
            usedCellGrid = new bool[sizeX + 2*(int)cellRadius, sizeY + 2*(int)cellRadius];
        }

        AddRadiusCells(new int2(startCoords.x, currentY), rasterCellCoords, cellRadius, startCoords, usedCellGrid);

        for (int X = startCoords.x; X < endCoords.x; X++) {
            int Y = (int)(slopeM * ((float)X - startRelativeToBottomLeftPos.x + 1) + startRelativeToBottomLeftPos.z); // y = m * x + b (where b is initial z position)
            if (Y != currentY) {
                for (int Y0 = currentY + lineYSign; Y0 != Y + lineYSign; Y0 += lineYSign) {
                    AddRadiusCells(new int2(X, Y0), rasterCellCoords, cellRadius, startCoords, usedCellGrid);
                    //if (Y0 == Y) { break; } // Instead of Y0 != Y + lineYSign
                }
                currentY = Y;
            }
            AddRadiusCells(new int2(X+1, Y), rasterCellCoords, cellRadius, startCoords, usedCellGrid);
        }
        if (currentY != endCoords.y) {
            for (int Y0 = currentY + lineYSign; Y0 != endCoords.y + lineYSign; Y0 += lineYSign) {
                AddRadiusCells(new int2(endCoords.x, Y0), rasterCellCoords, cellRadius, startCoords, usedCellGrid);
                //if (Y0 == endCoords.y) { break; }
            }
        }
        return rasterCellCoords;
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