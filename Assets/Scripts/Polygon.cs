using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

public struct Polygon : ICollider // Clockwise. low-level struct
{
    Vertex[] vertices;
    Edge[] edges; // Edges by vertices in a triangle are: (0 - 1), (1 - 2), (2 - 0)
    public ColliderSection[] colliderSections;
    public int numVertices { get; private set; }
    public float3 normal { get; private set; }
    public float originDistance { get; private set; }
    public float thickness { get; private set; }

    // maxExtents
    

    public Polygon(float3[] vertexPositions, float thickness = 0.2f) : this()
    {
        this.thickness = thickness;
        numVertices = vertexPositions.Length;

        vertices = new Vertex[numVertices];
        for (int i = 0; i < numVertices; i++) {
            vertices[i].position = vertexPositions[i];
        }

        edges = new Edge[numVertices];
        for (int i = 0; i < numVertices; i++) {
            edges[i] = new Edge(vertices[i], vertices[i + 1 == numVertices ? 0 : i + 1]); // Edges in triangle are: (0 - 1), (1 - 2), (2 - 0)
        }
        UpdatePlaneEquation();
        UpdateEdgeNormals();
    }

    public float2[] GetVertexUVPositions() {
        float2[] vertexUVPositions = new float2[numVertices];
        
        float3 arbitraryVertexPos = GetVertexPosition(0);
        quaternion rotation = MathLib.CalcRotationFromNormal(normal);

        float4x4 polygonMatrix = new float4x4(rotation, arbitraryVertexPos);
        float4x4 invPolygonMatrix = math.inverse(polygonMatrix);

        float2 maxUV = new float2(float.MinValue);
        float2 minUV = new float2(float.MaxValue);
        for (int i = 0; i < numVertices; i++) {
            float3 vertexPosInLocal = MathLib.PointToLocal(GetVertexPosition(i), invPolygonMatrix);
            // Debug.Log("vertexPosInLocal: " + vertexPosInLocal);
            vertexUVPositions[i] = vertexPosInLocal.xy/2;

            maxUV = math.max(maxUV, vertexUVPositions[i]);
            minUV = math.min(minUV, vertexUVPositions[i]);
        }
        // vertexUVPositions.Select
        return vertexUVPositions;
    }

    public Vector3[] GetFaceVertices(int isFrontFace = 1) {
        Vector3[] faceVertices = new Vector3[numVertices];
        for (int i = 0; i < numVertices; i++) {
            faceVertices[i] = vertices[i].position;
        }
        return faceVertices;
    }
    public int[] GetFaceTriIndicesFan(int isFrontFace = 1) {
        int numTris = (numVertices - 3) + 1;
        int[] vertexIndices = new int[numTris*3];
        // Debug.Log(numTris);
        int stationaryIndex1 = 0;
        for (int i = 0; i < numTris; i++) {
            int index2 = i+1 + isFrontFace; // 1, 2, 3
            int index3 = i+2 - isFrontFace; // 2, 3, 4
            vertexIndices[i*3] = stationaryIndex1;  // 0, 3, 6
            vertexIndices[(i*3)+1] = index2;        // 1, 4, 7
            vertexIndices[(i*3)+2] = index3;        // 2, 5, 8
        }
        return vertexIndices;
    }

    public float3 GetVertexPosition(int index) { // Good enough?
        return vertices[index].position;
    }

    public Edge GetEdge(int index) { // Good?
        return edges[index];
    }

    public void UpdatePlaneEquation() { // ax + by + cz = d -> Normal = (a,b,c), Distance = d
        normal =  math.normalize(math.cross(edges[0].vector, edges[1].vector));
        originDistance = math.dot(normal, vertices[0].position);
    }

    public void UpdateEdgeNormals() {
        for (int i = 0; i < numVertices; i++)
            edges[i].UpdateNormal(normal);
    }

    public void ToAABB(out float3 minPosition, out float3 maxPosition) {
        maxPosition = new float3(float.MinValue);
        minPosition = new float3(float.MaxValue);
        for (int i = 0; i < numVertices; i++) {
            float3 vertexPos = GetVertexPosition(i);
            maxPosition = math.max(maxPosition, vertexPos);
            minPosition = math.min(minPosition, vertexPos);
        }
    }

    public void AddToGrid(BuildingGrid grid, int entityIndexHack) {
        if (colliderSections != null) { // If it exists in the grid first remove it. TODO: Maybe do error instead
            RemoveFromGrid(grid, entityIndexHack);
        }
        int Entity = entityIndexHack;
        List<int2> rasterCellCoords;
        if (normal.y < 0.1) { // If this Polygon is mostly vertical we don't have to raster it as a polygon
            ToAABB(out float3 minPosition, out float3 maxPosition); // TODO: Totally wrong
            rasterCellCoords = grid.RasterRay(minPosition, maxPosition);
            colliderSections = new ColliderSection[rasterCellCoords.Count];
            for (int i = 0; i < rasterCellCoords.Count; i++) {
                CommonLib.CreatePrimitive(PrimitiveType.Cube, grid.CellCoordsToWorld(rasterCellCoords[i]), new float3(grid.cellSize - 0.2f, 0.1f, grid.cellSize - 0.2f), Color.red);
                uint typeMask = GlobalConstants.BUILDING_MASK + GlobalConstants.WALL_MASK + ((i == 0 || i == rasterCellCoords.Count - 1) ? GlobalConstants.WALL_END_MASK : 0);
                colliderSections[i] = new ColliderSection(typeMask, this, Entity, grid.AddEntityToCell(rasterCellCoords[i], Entity), rasterCellCoords[i]);
            }
        } else {
            rasterCellCoords = grid.RasterPolygon(this, out List<int2> bottomEdgeCellCoords, out List<int2> topEdgeCellCoords);
            colliderSections = new ColliderSection[rasterCellCoords.Count];
            int numXCoords = bottomEdgeCellCoords.Count;
            for (int i = 0; i < numXCoords; i++) { // Only for debug
                CommonLib.CreatePrimitive(PrimitiveType.Sphere, grid.CellCoordsToWorld(bottomEdgeCellCoords[i]) + new float3(0,0.1f*i,0), new float3(0.2f), Color.green);
                CommonLib.CreatePrimitive(PrimitiveType.Sphere, grid.CellCoordsToWorld(topEdgeCellCoords[(numXCoords - 1) - i]) + new float3(0,0.1f*i,0), new float3(0.2f), Color.red);
            }
            for (int i = 0; i < rasterCellCoords.Count; i++) {
            CommonLib.CreatePrimitive(PrimitiveType.Cube, grid.CellCoordsToWorld(rasterCellCoords[i]), new float3(grid.cellSize - 0.2f, 0.1f, grid.cellSize - 0.2f), Color.blue);
            colliderSections[i] = new ColliderSection(GlobalConstants.BUILDING_MASK, this, Entity, grid.AddEntityToCell(rasterCellCoords[i], Entity), rasterCellCoords[i]);
        }
        }
    }

    public void RemoveFromGrid(BuildingGrid grid, int entityIndexHack) {
        for (int i = 0; i < colliderSections.Length; i++) {
            grid.RemoveEntityFromCell(colliderSections[i].cellCoords, colliderSections[i].cellIndex, 0);
        }
        colliderSections = null;
    }

    public bool IsRayCastColliding(RayInput ray, out float distanceAlongRay) {
        return RayCastConvex(ray, out distanceAlongRay, out float3 nearestPointToPlane);
    }

    public bool RayCastConvex(RayInput ray, out float distanceAlongRay, out float3 nearestPointToPlane) { // Old: float maxDistance = math.INFINITY
        if (MathLib.IsRayPlaneIntersecting(ray.start, ray.direction, ray.length, normal, originDistance, out distanceAlongRay, out nearestPointToPlane)) {
            return IsPointInConvex(nearestPointToPlane, ray.direction);
        }
        return false;
    }

    public bool IsPointInConvex(float3 pointOnPlane, float3 rayDirection, float amountOutsideEdges = 0) { // TODO: Hasn't been tested with enough planes yet. pointOnPlane is assumed to actually be on plane
        for (int i = 0; i < numVertices; i++) {
            float3 pointToVertex1 = pointOnPlane - edges[i].vertex1.position;
            float distanceFromEdge = math.dot(edges[i].normal, pointToVertex1);
            if (distanceFromEdge > 0) // Negative if behind the edge // TODO: Use amountOutsideEdges here
                return false;
        }
        return true;
    }

    public bool SphereCastConvex(Ray ray, float radius, out float3 hitPoint, float maxDistance = math.INFINITY) { // TODO: Gotta check if line and plane are parallel
        float constants = math.dot(ray.origin, normal); // Solve line plane intersection
        float coefficients = math.dot(ray.direction, normal);
        float distanceAlongRay = (originDistance - constants) / coefficients;

        float3 hitPointOnPlane = ray.origin + (distanceAlongRay * ray.direction); // ray.direction comes normalized
        float penetration = radius;
        float3 sphereTouchingPlane = MathLib.ResolveSpherePlanePenetration(hitPointOnPlane, ray.direction, normal, penetration);

        hitPoint = sphereTouchingPlane; // hitPointOnPlane;

        if (distanceAlongRay < 0 || distanceAlongRay > maxDistance) return false;

        hitPoint = IsSphereInConvex(sphereTouchingPlane, radius, ray.direction);

        return true;
    }

    public float3 IsSphereInConvex(float3 point, float radius, float3 rayDirection) { // TODO: Hasn't been tested with enough planes yet. pointOnPlane is assumed to actually be on plane
        
        float3 rayRight = math.cross(math.up(), rayDirection);
        float3 rayUp = math.cross(rayRight, rayDirection);
        
        for (int i = 0; i < numVertices; i++) {
            float3 vertex1ToPoint = point - edges[i].vertex1.position;

            if (math.dot(edges[i].normal, vertex1ToPoint) > 0) {
                float3 edgeDirection = math.normalize(edges[i].vector);
                float shortestDistanceBtwLines = MathLib.ShortestDistanceBtwLines(edgeDirection, rayDirection, vertex1ToPoint);

                if (shortestDistanceBtwLines <= radius) {
                    float3 nearestPointOnRay = MathLib.NearestPointOnLine1ToLine2(point, rayDirection, edges[i].vertex1.position, edgeDirection);
                    float3 spherePosition = nearestPointOnRay - rayDirection * math.sqrt(radius*radius - shortestDistanceBtwLines*shortestDistanceBtwLines);

                    // float radiusSqrd = radius*radius;
                    // float3 sphereToVertex1 = edges[i].vertex1.position - spherePosition;

                    if (MathLib.IsRaySphereIntersecting(edges[i].vertex1.position, edges[i].direction, edges[i].length, spherePosition, radius, out float3 _)) {
                        return spherePosition;
                    }

                    // float3 sphereToVertex2 = edges[falseEdgeIndex].vertex2.position - spherePosition;
                    // if (!(math.dot(sphereToVertex1, sphereToVertex1) > radiusSqrd && math.dot(sphereToVertex2, sphereToVertex2) > radiusSqrd)) {
                    //     return spherePosition;
                    // }
                }
                return new float3(100,100,100); // false;
            }
        }
        return point; // numFalse == 0; // return true;
    }
}

public struct Vertex {
    public float3 position { get; set; }
}

public struct Edge {
    //private bool isCachedValid;
    public Vertex vertex1;
    public Vertex vertex2;
    public float3 vector { get; private set; }
    public float3 normal { get; private set; }
    readonly public float3 direction { get; }
    public float length { get; private set; }

    public Edge(Vertex vertex1, Vertex vertex2) {
        this.vertex1 = vertex1;
        this.vertex2 = vertex2;

        vector = vertex2.position - vertex1.position;
        normal = math.cross(math.up(), vector);
        direction = math.normalize(vector);
        length = math.length(vector);
    }

    public void UpdateNormal(float3 planeNormal) {
        normal = math.normalize(math.cross(vector, planeNormal));
    }

    public float3 CalcMidpoint() {
        return (vertex1.position + vertex2.position) / 2;
    }
}


public interface ICollider
{
    void AddToGrid(BuildingGrid grid, int entityIndex);
    bool IsRayCastColliding(RayInput ray, out float distanceAlongRay);
    void ToAABB(out float3 minPosition, out float3 maxPosition);

    // bool SphereCast(RayInput ray, out float distanceAlongRay);
}

// public struct WallCollider {
//     public WallCollider() {
//     }
// }

public struct SphereCollider : ICollider {
    public float3 center;
    public float radius;

    public SphereCollider(float3 center, float radius) {
        this.center = center;
        this.radius = radius;
    }

    public void AddToGrid(BuildingGrid grid, int entityIndex) {
        grid.AddEntityToCell(center, entityIndex);
    }

    public void ToAABB(out float3 minPosition, out float3 maxPosition) {
        MathLib.SphereToAABB(center, radius, out minPosition, out maxPosition);
    }

    public bool IsRayCastColliding(RayInput ray, out float distanceAlongRay) {
        ToAABB(out float3 minSpherePosition, out float3 maxSpherePosition);
        if (MathLib.IsAABBsIntersecting(ray.minPosition, ray.maxPosition, minSpherePosition, maxSpherePosition)) {
            return MathLib.IsRaySphereIntersecting(ray.start, ray.direction, ray.length, center, radius, out distanceAlongRay);
        }
        distanceAlongRay = 0;
        return false;
    }
}

public struct ColliderSection {
    internal ICollider collider; // Probably would need a pointer so maybe just use Entity

    uint typeMask; // To speed up physics since we probably won't have access to collider in this struct
    internal int Entity;
    internal byte cellIndex;
    internal int2 cellCoords;

    // bool isWallEnd; // Can just check if the collider 

    public ColliderSection(uint typeMask, ICollider collider, int Entity, byte cellIndex, int2 cellCoords) {
        this.collider = collider;
        this.Entity = Entity;
        this.cellIndex = cellIndex;
        this.cellCoords = cellCoords;
        this.typeMask = typeMask;
    }
}