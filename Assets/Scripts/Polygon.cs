using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

public struct Polygon // Clockwise. low-level
{
    Vertex[] vertices;
    Edge[] edges; // Edges by vertices in triangle are: (0 - 1), (1 - 2), (2 - 0)
    public ColliderSection[] colliderSections;
    public int numVertices { get; private set; }
    public float3 normal { get; private set; }
    public float planeDistance { get; private set; }
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

    public float3 GetVertexPosition(int index) {
        return vertices[index].position;
    }

    public void UpdatePlaneEquation() { // ax + by + cz = d -> Normal = (a,b,c), Distance = d
        normal =  math.normalize(math.cross(edges[0].vector, edges[1].vector));
        normalDistance = math.dot(normal, vertices[0].position);
    }

    public void UpdateEdgeNormals() {
        for (int i = 0; i < numVertices; i++)
            edges[i].UpdateNormal(normal);
    }

    public void AddToGrid(BuildingGrid grid) {
        if (colliderSections != null) {
            RemoveFromGrid(grid);
        }
        if (normal.y < 0.1) {
            float3 maxPosition = new float3(float.MinValue);
            float3 minPosition = new float3(float.MaxValue);
            for (int i = 0; i < numVertices; i++) {
                float3 vertexPos = GetVertexPosition(i);
                maxPosition = math.max(maxPosition, vertexPos);
                minPosition = math.min(minPosition, vertexPos);
            }
            Debug.Log("maxPosition: " + maxPosition);
            Debug.Log("minPosition: " + minPosition);

            int Entity = 0;
            List<int2> cellCoords = grid.RasterRay(minPosition, maxPosition);
            colliderSections = new ColliderSection[cellCoords.Count];
            for (int i = 0; i < cellCoords.Count; i++) {
                colliderSections[i] = new ColliderSection(Entity, grid.AddEntityToCell(cellCoords[i], Entity), cellCoords[i]);
            }
        } else {
            // raster polygon
        }
    }

    public void RemoveFromGrid(BuildingGrid grid) {
        for (int i = 0; i < colliderSections.Length; i++) {
            grid.RemoveEntityFromCell(colliderSections[i].cellCoords, colliderSections[i].cellIndex, 0);
        }
        colliderSections = null;
    }

    public bool RayCastConvex(Ray ray, out float3 hitPoint, float maxDistance = math.INFINITY) {
        float constants = math.dot(ray.origin, normal);
        float coefficients = math.dot(ray.direction, normal);
        float distanceAlongRay = (normalDistance - constants) / coefficients;

        float3 hitPointOnPlane = ray.origin + (distanceAlongRay * ray.direction); // ray.direction comes normalized
        hitPoint = hitPointOnPlane;

        if (distanceAlongRay < 0 || distanceAlongRay > maxDistance) return false;

        return IsPointInConvex(hitPointOnPlane, ray.direction);
    }

    public bool SphereCastConvex(Ray ray, float radius, out float3 hitPoint, float maxDistance = math.INFINITY) { // TODO: Gotta check if line and plane are parallel
        float constants = math.dot(ray.origin, normal);
        float coefficients = math.dot(ray.direction, normal);
        float distanceAlongRay = (normalDistance - constants) / coefficients;

        float3 hitPointOnPlane = ray.origin + (distanceAlongRay * ray.direction); // ray.direction comes normalized

        float dotDirectionNormal = math.dot(-ray.direction, normal);
        float dotCoeff = 1/dotDirectionNormal;
        float penetration = radius;
        float distanceBackwards = penetration * dotCoeff;
        float3 sphereTouchingPlane = hitPointOnPlane - (float3)ray.direction * distanceBackwards;

        hitPoint = sphereTouchingPlane; // hitPointOnPlane;

        if (distanceAlongRay < 0 || distanceAlongRay > maxDistance) return false;

        hitPoint = IsSphereInConvex(sphereTouchingPlane, radius, ray.direction);

        return true;
    }

    public float3 IsSphereInConvex(float3 point, float radius, float3 rayDirection) { // TODO: Hasn't been tested with enough planes yet. pointOnPlane is assumed to actually be on plane
        
        float3 rayRight = math.cross(math.up(), rayDirection);
        float3 rayUp = math.cross(rayRight, rayDirection);

        int falseEdgeIndex = -1;
        for (int i = 0; i < numVertices; i++) {
            float3 vertex1ToPoint = point - edges[i].vertex1.position;

            if (math.dot(edges[i].normal, vertex1ToPoint) > 0) {
                falseEdgeIndex = i;
                break;
                
                /* float3 edgeDirection = math.normalize(edges[falseEdgeIndex].vector);
                float3 crossNormal = math.normalize(math.cross(edgeDirection, rayDirection));
                shortestDistanceBtwLines = math.dot(crossNormal, -vertex1ToPoint);
                
                if (shortestDistanceBtwLines > radius) {
                    return new float3(100,100,100);
                } */
            }
        }
        if (falseEdgeIndex >= 0) {
            float3 vertex1ToPoint = point - edges[falseEdgeIndex].vertex1.position;
            float3 edgeDirection = math.normalize(edges[falseEdgeIndex].vector);
            float3 crossNormal = math.normalize(math.cross(edgeDirection, rayDirection));
            float shortestDistanceBtwLines = math.dot(crossNormal, -vertex1ToPoint);

            if (shortestDistanceBtwLines <= radius) {
                float3 nearestPointOnRay = NearestPointOnLine1ToLine2(point, rayDirection, edges[falseEdgeIndex].vertex1.position, edgeDirection);
                float3 spherePosition = nearestPointOnRay - rayDirection * math.sqrt(radius*radius - shortestDistanceBtwLines*shortestDistanceBtwLines);

                float radiusSqrd = radius*radius;
                float3 sphereToVertex1 = edges[falseEdgeIndex].vertex1.position - spherePosition;

                if (IsSphereRayIntersecting(spherePosition, radius, edges[falseEdgeIndex].vertex1.position, edges[falseEdgeIndex].direction, edges[falseEdgeIndex].length, out float3 rayToSphere)) {
                    return spherePosition;
                }

                // float3 sphereToVertex2 = edges[falseEdgeIndex].vertex2.position - spherePosition;
                // if (!(math.dot(sphereToVertex1, sphereToVertex1) > radiusSqrd && math.dot(sphereToVertex2, sphereToVertex2) > radiusSqrd)) {
                //     return spherePosition;
                // }
            }
            return new float3(100,100,100); // false;
        }

        return point; // numFalse == 0; // return true;
    }

    bool IsSphereRayIntersecting(float3 point, float radius, float3 rayStart, float3 rayDirection, float rayLength, out float3 rayToSphere) {
        float3 nearestPointOnRay = NearestPointOnRayToPoint(point, rayStart, rayDirection, rayLength);
        rayToSphere = point - nearestPointOnRay; // * vec3Mask // Mask is to make it only 2D
        float distanceToRaySqrd = math.dot(rayToSphere, rayToSphere);
        if (distanceToRaySqrd <= radius*radius) {
            return true;
        }
        return false;
    }

    float3 NearestPointOnRayToPoint(float3 point, float3 rayStart, float3 rayDirection, float rayLength) {
        float3 rayStartToPoint = point - rayStart;
        float distanceOnRay = math.clamp(math.dot(rayStartToPoint, rayDirection), 0, rayLength);
        return rayStart + (rayDirection * distanceOnRay);
    }

    float3 NearestPointOnLine1ToLine2(float3 line1Point, float3 line1Direction, float3 line2Point, float3 line2Direction) // https://math.stackexchange.com/q/3436386
    {
        float3 posDiff = line1Point - line2Point;
        float3 crossNormal = math.normalize(math.cross(line1Direction, line2Direction));
        // float3 projection = math.project(pos_diff, a);
        float3 rejection = posDiff - math.project(posDiff, line2Direction) - math.project(posDiff, crossNormal);
        float3 distanceToLinePos = math.length(rejection) / math.dot(line1Direction, math.normalize(rejection));
        return line1Point - line1Direction * distanceToLinePos;
    }

    // Raycasting not fully working with spheres, going off the corners and high angle not working
    public bool IsPointInConvexOk(float3 pointOnPlane, float radius, float3 rayDirection) { // TODO: Hasn't been tested with enough planes yet. pointOnPlane is assumed to actually be on plane
        float dotPlaneNormalRay = math.abs(1/math.dot(normal, math.normalize(rayDirection))); // 1 if looking directly at plane
        int numFalse = 0;
        for (int i = 0; i < numVertices; i++) {
            float3 pointToVertex1 = pointOnPlane - edges[i].vertex1.position;
            
            float dotEdgeNormalRay = math.abs(math.dot(edges[i].normal, math.normalize(rayDirection)));

            if (math.dot(edges[i].normal, pointToVertex1) > radius + radius * (dotPlaneNormalRay * dotEdgeNormalRay)) // Not working: + Thickness*math.abs(dotEdgeNormalRay))
                numFalse++; // return false;
        }
        if (numFalse > 0) Debug.Log("Multiple sides showing false");
        return numFalse == 0;
        // return true;

        /* float dotDirectionNormal = math.dot(-velocityDirection, terrainNormal);
        float dotCoeff = 1/dotDirectionNormal;
        float underTerrainY = terrainY - newPosition.y;
        float totalYArrow = underTerrainY + radius * dotDirectionNormal;
        float totalYSphere = underTerrainY + radius;
        float distanceBackwards = totalYSphere * dotCoeff;
        float3 sphereOnTerrainPosition = newPosition - velocityDirection * distanceBackwards; */
    }

    public bool IsPointInConvex(float3 pointOnPlane, float3 rayDirection) { // TODO: Hasn't been tested with enough planes yet. pointOnPlane is assumed to actually be on plane
        for (int i = 0; i < numVertices; i++) {
            float3 pointToVertex1 = pointOnPlane - edges[i].vertex1.position;
            
            if (math.dot(edges[i].normal, pointToVertex1) > 0)
                return false;
        }
        return true;
    }
}

struct Vertex {
    public float3 position { get; set; }
}

struct Edge {
    //private bool isCachedValid;
    public Vertex vertex1;
    public Vertex vertex2;
    public float3 vector { get; private set; }
    public float3 normal { get; private set; }
    public float3 direction { get; private set; }
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
}

public struct ColliderSection {
    internal int Entity;
    internal int cellIndex;
    internal int2 cellCoords;

    public ColliderSection(int Entity, int cellIndex, int2 cellCoords) {
        this.Entity = Entity;
        this.cellIndex = cellIndex;
        this.cellCoords = cellCoords;
    }
}