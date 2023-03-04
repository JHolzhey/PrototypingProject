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

    public int[] GetCellEntities(int2 cellCoords) { // TODO: Add clones for these functions that take float3's and do worldToCell
        return this.grid[cellCoords.x, cellCoords.y].GetAllEntities();
    }

    public byte AddEntityToCell(float3 position, int entity) {
        int2 cellCoords = WorldToCellCoords(position);
        return AddEntityToCell(cellCoords, entity);
    }

    public byte AddEntityToCell(int2 cellCoords, int entity) {
        byte index = this.grid[cellCoords.x, cellCoords.y].AddEntity(entity);
        //printf("Adding entity [%d] to cell: [%d | %d] gives index: [%d]\n", (int)entity, cell_coords.x, cell_coords.y, index);
        return index;
    }
    public void RemoveEntityFromCell(int2 cellCoords, byte indexOfEntity, int entity) {
        int entityToBeRemoved = this.grid[cellCoords.x, cellCoords.y].RemoveEntity(indexOfEntity);
        Debug.Assert(entity == entityToBeRemoved);
    }

    public void ClearAllCells() {
        //printf("Clearing all cells\n");
        for (uint X = 0; X < maxEntitiesPerCell; X++) {
            for (uint Y = 0; Y < maxEntitiesPerCell; Y++) {
                this.grid[X,Y].Clear();
            }
        }
    }

    public float3 CellCoordsToWorld(int2 cellCoords) { // Returns the center of cell, not bottom left
        return bottomLeftWorld + (new float3(cellCoords.x+0.5f, 0, cellCoords.y+0.5f) * cellSize);
    }

    public int2 WorldToCellCoords(float3 position) {
        return MathLib.WorldToCellCoords(position, bottomLeftWorld, cellSize);
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

    public List<int2> RasterCapsule(float3 rayStart, float3 rayEnd) {
        List<int2> rasterCellCoords = new List<int2>();
        RasterRay(rayStart, rayEnd, ref rasterCellCoords);
        return rasterCellCoords;
    }

    public List<int2> RasterRay(RayInput ray) { return RasterRay(ray.start, ray.end); }
    public List<int2> RasterRay(float3 rayStart, float3 rayEnd) {
        List<int2> rasterCellCoords = new List<int2>();
        RasterRay(rayStart, rayEnd, ref rasterCellCoords);
        return rasterCellCoords;
    }

    public void RasterRay(float3 rayStart, float3 rayEnd, ref List<int2> rasterCellCoords)
    {
        float3 rayVector = rayEnd - rayStart;
        float3 startRelativeToBottomLeftPos = (rayStart - bottomLeftWorld) / cellSize;

        int2 startCoords = WorldToCellCoords(rayStart);
        int2 endCoords = WorldToCellCoords(rayEnd);

        int signY = (rayVector.z > 0) ? 1 : -1;
        int signX = (rayVector.x > 0) ? 1 : -1;
        float slopeM = rayVector.z / rayVector.x; // TODO: rayVector.x could be 0
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
        for (int Y0 = currentY; Y0 != endCoords.y + signY; Y0 += signY)
            rasterCellCoords.Add(new int2(currentX - (1 - boolX), Y0));
    }

    public List<int2> RasterEdge(float3 rayStart, float3 rayEnd, bool isReverse = false) { // Only for testing right now
        List<int2> rasterCellCoords = new List<int2>();
        RasterEdge(rayStart, rayEnd, ref rasterCellCoords, isReverse);
        return rasterCellCoords;
    }

    public void RasterEdge(float3 rayStart, float3 rayEnd, ref List<int2> rasterCellCoords, bool isReverse = false)
    {
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

            int YUsing = isReverse ? currentY : Y;
            
            rasterCellCoords.Add(new int2(currentX - (1 - boolX), YUsing));
            currentX = X;  currentY = Y;
        }
        int YUsing2 = isReverse ? currentY : endCoords.y;
        rasterCellCoords.Add(new int2(currentX - (1 - boolX), YUsing2));
    }

    public List<int2> RasterPolygon(Polygon polygon, out List<int2> bottomEdgeCellCoords, out List<int2> topEdgeCellCoords) // Add check if ray is out of bounds
    {
        // Debug.Log("polygon.numVertices: " + polygon.numVertices);
        List<int2> rasterCellCoords = new List<int2>(); // All cell coords of fully rasterized polygon (if a cell is barely clipped, it will still be included)

        bottomEdgeCellCoords = new List<int2>(); // All cell coords of only rasterized bottom edge of polygon, and only one cell coord per X
        topEdgeCellCoords = new List<int2>();
        List<int2> currentEdgeCellCoords = default;
        int startingEdgeIndex = -1;
        bool isCurrentEdgeTop = polygon.GetEdge(0).vector.x > 0;
        for (int i = 1; i < polygon.numVertices; i++) { // Finds the first instance around the polygon that edges switch from top to bottom (end) or vice versa
            bool isTopEdge = polygon.GetEdge(i).vector.x > 0;
            if (isTopEdge ^ isCurrentEdgeTop) {
                startingEdgeIndex = i;
                isCurrentEdgeTop = isTopEdge;
                currentEdgeCellCoords = isTopEdge ? topEdgeCellCoords : bottomEdgeCellCoords;
                currentEdgeCellCoords.Add(new int2(-1, -1)); // To make the RemoveAt last index below work for the first edge iteration below
                // Debug.Log("startingEdgeIndex: " + startingEdgeIndex);
                // Debug.Log("isTopEdge: " + isTopEdge);
                break;
            }
        }
        int colorIndex = 0;
        for (int i0 = startingEdgeIndex; i0 < polygon.numVertices + startingEdgeIndex; i0++) { // Now goes around polygon edges and adds to current end Coords
            int index = i0;
            if (i0 >= polygon.numVertices) {
                index = i0 - polygon.numVertices;
            }
            // Debug.Log("index: " + index);
            Edge edge = polygon.GetEdge(index);
            CommonLib.CreatePrimitive(PrimitiveType.Cube, edge.CalcMidpoint(), new float3(0.05f), CommonLib.CycleColors[colorIndex++]);

            bool isTopEdge = edge.vector.x > 0;
            if (isTopEdge ^ isCurrentEdgeTop) { // If detect a switch of ends based on edge direction, switch current Coords and don't remove last element
                // Debug.Log("Switching ends");
                isCurrentEdgeTop = isTopEdge; // Update our variable that tells us which end we are currently on
                currentEdgeCellCoords = isTopEdge ? topEdgeCellCoords : bottomEdgeCellCoords; // Switch to correct end Coords
            } else {
                // Debug.Log("Removing last");
                currentEdgeCellCoords.RemoveAt(currentEdgeCellCoords.Count - 1); // Remove last element so no duplicates because next iteration will write its coords too
            }
            bool isZPositive = edge.vector.z > 0;
            bool isReverseRay = (!isTopEdge && isZPositive) || (isTopEdge && !isZPositive); // Make RasterRayOneX work properly (enforce top is top raster and vice versa)
            RasterEdge(edge.vertex1.position, edge.vertex2.position, ref currentEdgeCellCoords, isReverseRay); // Raster so only one cell coord per X cell (for each end)
        }

        int numXCoords = bottomEdgeCellCoords.Count;
        for (int i = 0; i < numXCoords; i++) { // Now iterate from bottomY to topY for each X and add (X, Y) to final raster coords
            int X = bottomEdgeCellCoords[i].x;
            int bottomY = bottomEdgeCellCoords[i].y;
            int topY = topEdgeCellCoords[(numXCoords - 1) - i].y;
            for (int Y = bottomY; Y <= topY; Y++) {
                rasterCellCoords.Add(new int2(X, Y));
            }
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
                // for (int Y0 = currentY + lineYSign; Y0 != Y + lineYSign; Y0 += lineYSign) {
                //      AddRadiusCells(new int2(X, Y), rasterCellCoords, cellRadius, startCoords, usedCellGrid); // Y0 instead Y
                    //if (Y0 == Y) { break; } // Instead of Y0 != Y + lineYSign
                // }
                currentY = Y;
            }
            AddRadiusCells(new int2(X+1, Y), rasterCellCoords, cellRadius, startCoords, usedCellGrid);
        }
        if (currentY != endCoords.y) {
            // for (int Y0 = currentY + lineYSign; Y0 != endCoords.y + lineYSign; Y0 += lineYSign) {
                AddRadiusCells(new int2(endCoords.x, endCoords.y), rasterCellCoords, cellRadius, startCoords, usedCellGrid); // Y0 instead endCoords.y
                //if (Y0 == endCoords.y) { break; }
            // }
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

    public bool SphereCastAll(TestEntity[] entities, RayInput ray, float radius, out RayCastResult hit) { // TODO: Ray has to be extended on both ends by radius
        Debug.Assert(radius*2 <= cellSize);
        float size = cellSize - 0.5f;

        float3 directionOffset = ray.direction*(radius*2);
        float3 tangentOffset = MathLib.CalcTangentToNormal(ray.direction)*radius;
        float3 startUpper = ray.start + tangentOffset - directionOffset;   float3 endUpper = ray.end + tangentOffset + directionOffset;
        float3 startLower = ray.start - tangentOffset - directionOffset;   float3 endLower = ray.end - tangentOffset + directionOffset;

        List<int2> upperCellCoords = RasterRay(startUpper, endUpper);
        List<int2> lowerCellCoords = RasterRay(startLower, endLower);

        List<RayCastResult> hits = new List<RayCastResult>();

        int maxCells = math.max(lowerCellCoords.Count, upperCellCoords.Count);
        int upperIndex = 0;   int lowerIndex = 0; 
        for (int i = 0; i < maxCells; i++) {
            float3 upShift = new float3(0,0.1f*i,0);
            bool isLowerContinue = lowerIndex < lowerCellCoords.Count;
            bool isUpperContinue = upperIndex < upperCellCoords.Count;

            if (isLowerContinue && isUpperContinue && math.all(upperCellCoords[upperIndex] == lowerCellCoords[lowerIndex])) {
                    // CommonLib.CreatePrimitive(PrimitiveType.Cube, CellCoordsToWorld(upperCellCoords[upperIndex]) + upShift, new float3(size, 0.2f, size), Color.black, new Quaternion(), 1.0f);
                // Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoordsUpper[upperIndex]) + upShift, new float3(size, 0.2f, size));
                // Check
                
                upperIndex++;
                lowerIndex++;
            } else {
                // Lower work:
                if (isLowerContinue) {
                        // CommonLib.CreatePrimitive(PrimitiveType.Cube, CellCoordsToWorld(lowerCellCoords[lowerIndex]) + upShift, new float3(size, 0.2f, size), Color.blue, new Quaternion(), 1.0f);
                    // Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoordsLower[lowerIndex]) + upShift, new float3(size, 0.2f, size));
                    // Check
                    
                    if (isUpperContinue && ((lowerIndex + 1) < lowerCellCoords.Count) && math.all(lowerCellCoords[lowerIndex + 1] == upperCellCoords[upperIndex])) {
                        lowerIndex++; // Don't check sphere cast colliding here
                            // CommonLib.CreatePrimitive(PrimitiveType.Cube, CellCoordsToWorld(lowerCellCoords[lowerIndex]) + upShift, new float3(size-0.3f, 0.4f, size-0.3f), Color.blue, new Quaternion(), 1.0f);
                        // Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoordsLower[lowerIndex]) + upShift, new float3(size-0.3f, 0.2f, size-0.3f));
                    }
                    lowerIndex++; // TODO: Put in if part
                }
                // Upper work:
                if (isUpperContinue) {
                        // CommonLib.CreatePrimitive(PrimitiveType.Cube, CellCoordsToWorld(upperCellCoords[upperIndex]) + upShift, new float3(size, 0.2f, size), Color.red, new Quaternion(), 1.0f);
                    // Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoordsUpper[upperIndex]) + upShift, new float3(size, 0.2f, size));
                    // Check
                    
                    if (isLowerContinue && ((upperIndex + 1) < upperCellCoords.Count) && math.all(upperCellCoords[upperIndex + 1] == lowerCellCoords[lowerIndex - 1])) { // lowerIndex - 1 because incremented earlier
                        upperIndex++; // Don't check sphere cast colliding here
                            // CommonLib.CreatePrimitive(PrimitiveType.Cube, CellCoordsToWorld(upperCellCoords[upperIndex]) + upShift, new float3(size-0.3f, 0.4f, size-0.3f), Color.red, new Quaternion(), 1.0f);
                        // Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoordsUpper[upperIndex]) + upShift, new float3(size-0.3f, 0.2f, size-0.3f));   
                    }
                    upperIndex++;
                }
            }
        }

        
        hit = new RayCastResult();
        return false;
    }

    /* public bool RayCastAll(TestEntity[] entitiesHack, RayInput ray, out List<RayCastResult> hits) {
        hits = new List<RayCastResult>();
        List<int2> cellCoords = RasterRay(ray.start, ray.end);
        for (int i = 0; i < cellCoords.Count; i++) {
            int[] cellEntities = GetCellEntities(cellCoords[i]);
            int closestHitIndex = -1;  float closestDistanceAlongRay = math.INFINITY;

            for (int j = 0; j < cellEntities.Length; j++) { // Must go through all entities in cell and choose hit that has smallest distanceAlongRay
                int entityIndex = cellEntities[j];

                if (entitiesHack[entityIndex].collider.IsRayCastColliding(ray, out float distanceAlongRay) && distanceAlongRay < closestDistanceAlongRay) {
                    closestHitIndex = entityIndex;  closestDistanceAlongRay = distanceAlongRay;
                }
            }
            if (closestHitIndex != -1) { // Means we hit something
                hit = new RayCastResult(closestHitIndex, float3.zero, closestDistanceAlongRay, ray.start + ray.direction * closestDistanceAlongRay);
                return true; // No need to check next cells since this must be the closest hit
            }
        }
        hit = new RayCastResult();
        return false;
    } */

    public void RayCastCell(TestEntity[] entitiesHack, RayInput ray, int2 cellCoords, ref List<RayCastResult> hits) {
        int[] cellEntities = GetCellEntities(cellCoords);
        for (int j = 0; j < cellEntities.Length; j++) {
            int entityIndex = cellEntities[j];

            if (entitiesHack[entityIndex].collider.IsRayCastColliding(ray, out RayCastResult hit)) {
                hits.Add(hit);
            }
        }
    }

    public bool RayCast(TestEntity[] entitiesHack, RayInput ray, out RayCastResult nearestHit, bool collectAllHits = false) { // Make RayCastResult an array
        nearestHit = new RayCastResult(-1, float3.zero, math.INFINITY, float3.zero);

        List<int2> cellCoords = RasterRay(ray.start, ray.end);
        for (int i = 0; i < cellCoords.Count; i++) {
            int[] cellEntities = GetCellEntities(cellCoords[i]);

            int closestHitIndex = -1;
            float closestDistanceAlongRay = nearestHit.distance;

            // List<RayCastResult> hits = new List<RayCastResult> // TODO
            for (int j = 0; j < cellEntities.Length; j++) { // Must go through all entities in cell and choose hit that has smallest distanceAlongRay
                int entityIndex = cellEntities[j];

                if (entitiesHack[entityIndex].collider.IsRayCastColliding(ray, out nearestHit) && nearestHit.distance < closestDistanceAlongRay) {
                    closestHitIndex = entityIndex;
                    closestDistanceAlongRay = nearestHit.distance;
                }
            }
            if (closestHitIndex != -1) { // Means we hit something
                nearestHit.hitEntity = closestHitIndex;
                return true; // No need to check next cells since this must be the closest hit
            }
        }
        return false;
    }
}

struct Cell
{
	private int []entities;
	public byte numEntities { get; private set; } // How many entities are currently held in the array above

    public Cell(int maxEntities) {
        entities = new int[maxEntities];
        numEntities = 0;
    }

    public int GetEntity(int entityIndex) {
        return entities[entityIndex];
    }

    public int[] GetAllEntities() {
        return entities.SubArray(0, numEntities);
    }

	public int RemoveEntity(int index) { // Moves entity at the end of entity list into removed index
        Assert.IsTrue((index >= 0) && (index < entities.Length) && (this.numEntities - 1 >= 0));
        //printf("Entity given: %d. At index: %d.    entity removed: %d.   entity at end: %d\n", e, entity_index, this->entities[entity_index], this->entities[this->num_entities - 1]);
        //printf("Num_entities before removing: %d. Array before removing:\n", this->num_entities);
        int entityToBeRemoved = this.entities[index];
        int entityAtEnd = this.entities[this.numEntities - 1];
        this.entities[index] = entityAtEnd;
        //Motion& motion = registry.motions.get(entity_at_end);
        //motion.cell_index = entityIndex;

        this.numEntities--;
        //printf("Num_entities after removing: %d\n", this->num_entities);
        return entityToBeRemoved;
    }
	public byte AddEntity(int entity) { // Returns newly placed entity's index
        Assert.IsTrue((this.numEntities >= 0) && (this.numEntities < entities.Length)); // "Error or need to add more space to entity list"
        this.entities[this.numEntities] = entity;
        this.numEntities++;
        //printf("Num_entities after adding = %d\n", this->num_entities);
        return (byte)(this.numEntities - 1);
    }
    public void Clear() { // TODO: Should remove entities's index too?
        this.numEntities = 0;
    }
}

// Or have namespace called Cast and just do Cast.Ray, Cast.Sphere
public struct RayInput // Future: Call it CastInput and hold a ray or capsule collider
{
    readonly public float3 start;
    readonly public float3 end;
    readonly public float3 direction;
    readonly public float length;

    readonly public float3 minPosition;
    readonly public float3 maxPosition;

    public RayInput(float3 start, float3 end, float3 direction, float length = 100) {
        this.start = start;
        this.end = end;
        this.direction = direction;
        this.length = length;
        MathLib.RayToAABB(start, end, out minPosition, out maxPosition);
    }

    public RayInput(float3 start, float3 end) : this(start, end, math.normalize(end - start), math.length(end - start)) {}

    public RayInput(float3 start, float3 direction, float length = 100) : this(start, start + direction*length, direction, length) {}
}

public struct RayInput2
{
    CapsuleCollider capsuleCollider;

    public RayInput2(float3 start, float3 end, float radius = 0) {
        capsuleCollider = new CapsuleCollider(start, end, radius);
    }
    public RayInput2(float3 start, float3 direction, float length = 100, float radius = 0) {
        capsuleCollider = new CapsuleCollider(start, direction, length, radius);
    }
}

public struct CapsuleCollider : ICollider // Future: Call it CastInput and hold a ray or capsule collider
{
    readonly public float3 start;
    readonly public float3 end;
    readonly public float3 direction;
    readonly public float length;
    public float radius;

    readonly public float3 minPosition;
    readonly public float3 maxPosition;

    public CapsuleCollider(float3 start, float3 end, float radius = 0) : this(start, end, math.normalize(end - start), math.length(end - start), radius) {}
    public CapsuleCollider(float3 start, float3 direction, float length = 100, float radius = 0) : this(start, start + direction*length, direction, length, radius) {}
    public CapsuleCollider(float3 start, float3 end, float3 direction, float length, float radius) {
        this.start = start;
        this.end = end;
        this.direction = direction;
        this.length = length;
        this.radius = radius;
        MathLib.CapsuleToAABB(start, end, radius, out minPosition, out maxPosition);
        // MathLib.RayToAABB(start, end, out minPosition, out maxPosition);
    }

    public void AddToGrid(BuildingGrid grid, int entityIndex) {
        // grid.AddEntityToCell(center, entityIndex);
    }

    public void ToAABB(out float3 minPosition, out float3 maxPosition) {
        MathLib.CapsuleToAABB(start, end, radius, out minPosition, out maxPosition);
    }

    public bool IsRayCastColliding(RayInput ray, out RayCastResult hit) {
        hit = new RayCastResult();
        return false;
    }
}

public struct RayCastResult
{
    public int hitEntity { get; set; } // Should be private set
    public float3 normal { get; private set; }
    public float distance { get; private set; }
    public float3 hitPoint { get; private set; }

    public RayCastResult(int hitEntity, float3 normal, float distance, float3 nearestPoint) {
        this.hitEntity = hitEntity;
        this.normal = normal;
        this.distance = distance;
        this.hitPoint = nearestPoint;
    }
}