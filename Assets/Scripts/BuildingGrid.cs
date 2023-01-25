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
        for (int x = 0; x < dimensions.x; x++) {
            for (int y = 0; y < dimensions.y; y++) {
                grid[x,y] = new Cell(maxEntitiesPerCell);
            }
        }
    }

    void Update() {}

    public int AddEntityToCell(int2 cellCoords, int entity) {
        int index = this.grid[cellCoords.x, cellCoords.y].AddEntity(entity);
        //printf("Adding entity [%d] to cell: [%d | %d] gives index: [%d]\n", (int)entity, cell_coords.x, cell_coords.y, index);
        return index;
    }
    public void RemoveEntityFromCell(int2 cellCoords, int entityIndex, int entity) {
        this.grid[cellCoords.x, cellCoords.y].RemoveEntity(entityIndex);
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

    public void RayCast(Ray ray, float3 rayEnd, out RayCastResult hitResultInfo, float maxDistance = math.INFINITY) {
        RayCast(ray.origin, ray.origin + ray.direction * maxDistance, out hitResultInfo);
    }

    public void RayCast(float3 rayStart, float3 rayEnd, out RayCastResult hitResultInfo) {
        List<int2> cellCoords = RasterRay(rayStart, rayEnd);
        hitResultInfo = new RayCastResult();
        for (int i = 0; i < cellCoords.Count; i++) {
            Cell cell = grid[cellCoords[i].x, cellCoords[i].y];
            for (int j = 0; j < cell.numEntities; j++) {
                int Entity = cell.GetEntity(j);
            }
        }
    }

    public List<int2> RasterRay(float3 rayStart, float3 rayEnd) {
        List<int2> rasterCellCoords = new List<int2>();
        RasterRay(rayStart, rayEnd, ref rasterCellCoords);
        return rasterCellCoords;
    }

    public void RasterRay(float3 rayStart, float3 rayEnd, ref List<int2> rasterCellCoords)
    {
        //List<int2> rasterCellCoords = new List<int2>();

        float3 rayVector = rayEnd - rayStart;
        float3 startRelativeToBottomLeftPos = (rayStart - bottomLeftWorld) / cellSize;

        int2 startCoords = WorldToCellCoords(rayStart);
        int2 endCoords = WorldToCellCoords(rayEnd);

        int signY = (rayVector.z > 0) ? 1 : -1;
        int signX = (rayVector.x > 0) ? 1 : -1;
        float slopeM = rayVector.z / rayVector.x;
        float interceptB = (-slopeM * startRelativeToBottomLeftPos.x) + startRelativeToBottomLeftPos.z;

        int numXGridLinesBtw = math.abs(endCoords.x - startCoords.x);
        int boolX = (signX > 0) ? 1 : 0; // For offsetting x axis values when ray is negative x

        int currentX = startCoords.x + (1 - boolX);  int currentY = startCoords.y;
        for (int I = 0; I < numXGridLinesBtw; I++) {
            int X = startCoords.x + (I * signX) + boolX;
            int Y = (int)(slopeM * X + interceptB); // y = m * x + b (where b is initial z position)
            
            for (int Y0 = currentY; Y0 != Y + signY; Y0 += signY) 
                rasterCellCoords.Add(new int2(currentX - (1 - boolX), Y0));

            currentX = X;  currentY = Y;
        }
        for (int Y0 = currentY; Y0 != endCoords.y; Y0 += signY) // Y0 != endCoords.y + signY;
            rasterCellCoords.Add(new int2(currentX - (1 - boolX), Y0));
        //return rasterCellCoords;
    }

    public List<int2> RasterPolygon(Polygon polygon) // Separate function for radius > 0?
    {
        List<int2> rasterCellCoords = new List<int2>();

        List<int2> bottomEdgeCellCoords = new List<int2>();
        List<int2> topEdgeCellCoords = new List<int2>();
        List<int2> currentEdgeCellCoords = new List<int2>();
        int startingEdgeIndex = -1;
        bool isCurrentedgeTop = polygon.GetEdge(0).vector.x > 0;
        for (int i = 1; i < polygon.numVertices; i++) {
            bool isTopEdge = polygon.GetEdge(i).vector.x > 0;
            if (isTopEdge ^ isCurrentedgeTop) {
                startingEdgeIndex = i;
                currentEdgeCellCoords = isTopEdge ? topEdgeCellCoords : bottomEdgeCellCoords;
            }
        }
        for (int i = startingEdgeIndex; i < polygon.numVertices + startingEdgeIndex; i++) { // 5 and 3
            int index = i;
            if (i >= polygon.numVertices) { // i == 5, numVertices == 5
                index = i - polygon.numVertices;
            }
            Edge edge = polygon.GetEdge(index);
            RasterRay(edge.vertex1.position, edge.vertex2.position, ref currentEdgeCellCoords);

            
        }

        
        return rasterCellCoords;
    }
    

    public List<int2> RasterRayOld(float3 rayStart, float3 rayEnd, float radius = 0) // Separate function for radius > 0?
    {
        List<int2> rasterCellCoords = new List<int2>();

        float3 rayVector = rayEnd - rayStart;
        if (rayVector.x <= 0) {
            rayVector = -rayVector;
            float3 holder = rayStart; rayStart = rayEnd; rayEnd = holder;
        }

        float3 startRelativeToBottomLeftPos = (rayStart - bottomLeftWorld) / cellSize;

        int lineYSign = (rayVector.z > 0) ? 1 : -1;
        float slopeM = rayVector.z / rayVector.x;

        int2 startCoords = WorldToCellCoords(rayStart);
        int2 endCoords = WorldToCellCoords(rayEnd);
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
}

struct Cell
{
	// High, hard coded upper limit
	private int []entities;
	public int numEntities { get; private set; } // How many entities are currently held in the array above

    public Cell(int maxEntities) {
        entities = new int[maxEntities];
        numEntities = 0;
    }

    public int GetEntity(int entityIndex) {
        return entities[entityIndex];
    }

	public void RemoveEntity(int entityIndex) { // Moves entity at the end of entity list into removed index
        Assert.IsTrue((entityIndex >= 0) && (entityIndex < entities.Length) && (this.numEntities - 1 >= 0));
        //printf("Entity given: %d. At index: %d.    entity removed: %d.   entity at end: %d\n", e, entity_index, this->entities[entity_index], this->entities[this->num_entities - 1]);
        //printf("Num_entities before removing: %d. Array before removing:\n", this->num_entities);

        int entityAtEnd = this.entities[this.numEntities - 1];
        this.entities[entityIndex] = entityAtEnd;
        //Motion& motion = registry.motions.get(entity_at_end);
        //motion.cell_index = entityIndex;

        this.numEntities--;
        //printf("Num_entities after removing: %d\n", this->num_entities);
    }
	public int AddEntity(int entity) { // Returns newly placed entity's index
        Assert.IsTrue((this.numEntities >= 0) && (this.numEntities < entities.Length)); // "Error or need to add more space to entity list"
        this.entities[this.numEntities] = entity;
        this.numEntities++;
        //printf("Num_entities after adding = %d\n", this->num_entities);
        return this.numEntities - 1;
    }
    public void Clear() { // TODO: Should remove entities's index too?
        this.numEntities = 0;
    }
}

public struct RayCastResult
{
    public int hitEntity { get; private set; }
    public float3 normal { get; private set; }
    public float distance { get; private set; }

    public RayCastResult(int hitEntity, float3 normal, float distance) {
        this.hitEntity = hitEntity;
        this.normal = normal;
        this.distance = distance;
    }
}