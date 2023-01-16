using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

public struct Polygon // Clockwise. low-level
{
    private Vertex[] vertices;
    private Edge[] edges; // Edges by vertices in triangle are: (0 - 1), (1 - 2), (2 - 0)
    public int NumVertices { get; private set; }
    public float3 Normal { get; private set; }
    public float Distance { get; private set; }
    public float Thickness { get; private set; }

    public Polygon(float3[] vertexPositions, float thickness = 0.2f) : this()
    {
        Thickness = thickness;
        NumVertices = vertexPositions.Length;

        vertices = new Vertex[NumVertices];
        for (int i = 0; i < NumVertices; i++) {
            vertices[i].Position = vertexPositions[i];
        }

        edges = new Edge[NumVertices];
        for (int i = 0; i < NumVertices; i++) {
            edges[i] = new Edge(vertices[i], vertices[i + 1 == NumVertices ? 0 : i + 1]); // Edges in triangle are: (0 - 1), (1 - 2), (2 - 0)
        }
        UpdatePlaneEquation();
        UpdateEdgeNormals();
    }

    public Vector3[] GetFrontVertices() {
        Vector3[] frontVertices = new Vector3[NumVertices];
        for (int i = 0; i < NumVertices; i++) {
            frontVertices[i] = vertices[i].Position;
        }
        return frontVertices;
    }
    public int[] GetFrontIndicesFan(int side = 0) {
        int numTris = (NumVertices - 3) + 1;
        int[] vertexIndices = new int[numTris*3];
        // Debug.Log(numTris);
        int stationaryIndex1 = 0;
        for (int i = 0; i < numTris; i++) {
            int index2 = i+1 + side; // 1, 2, 3
            int index3 = i+2 + side; // 2, 3, 4
            vertexIndices[i*3] = stationaryIndex1;  // 0, 3, 6
            vertexIndices[(i*3)+1] = index2;        // 1, 4, 7
            vertexIndices[(i*3)+2] = index3;        // 2, 5, 8
        }
        return vertexIndices;
    }

    public float3 GetVertexPosition(int index) {
        return vertices[index].Position;
    }

    public void UpdatePlaneEquation() { // ax + by + cz = d -> Normal = (a,b,c), Distance = d
        Normal =  math.normalize(math.cross(edges[0].Vector, edges[1].Vector));
        Distance = math.dot(Normal, vertices[0].Position);
    }

    public void UpdateEdgeNormals() {
        for (int i = 0; i < NumVertices; i++)
            edges[i].UpdateNormal(Normal);
    }

    public bool RayCastConvex(Ray ray, float radius, out float3 hitPoint, float maxDistance = math.INFINITY) { // TODO: Hit can be behind ray origin. Also gotta check if line and plane are parallel
        float constants = math.dot(ray.origin, Normal);
        float coefficients = math.dot(ray.direction, Normal);
        float distanceAlongRay = (Distance - constants) / coefficients;

        float3 hitPointOnPlane = ray.origin + (distanceAlongRay * ray.direction); // ray.direction comes normalized
        hitPoint = hitPointOnPlane;

        if (distanceAlongRay < 0 || distanceAlongRay > maxDistance) return false;

        //float angleCoeff = 1/math.dot(Normal, ray.direction);
        return IsPointInConvex(hitPointOnPlane, radius, ray.direction);
    }
    // Raycasting not fully working with spheres, going off the corners and high angle not working
    public bool IsPointInConvex(float3 pointOnPlane, float radius, float3 rayDirection) { // TODO: Hasn't been tested with enough planes yet. pointOnPlane is assumed to actually be on plane
        float dotPlaneNormalRay = math.abs(1/math.dot(Normal, math.normalize(rayDirection)));
        for (int i = 0; i < NumVertices; i++) {
            float3 pointToVertex1 = pointOnPlane - edges[i].vertex1.Position;
            
            float dotEdgeNormalRay = math.abs(math.dot(edges[i].Normal, math.normalize(rayDirection)));

            if (math.dot(edges[i].Normal, pointToVertex1) > radius + radius * dotEdgeNormalRay * dotPlaneNormalRay) // Not working: + Thickness*math.abs(dotEdgeNormalRay))
                return false;
        }
        return true;
    }

    public bool IsPointInConvexSimple(float3 pointOnPlane, float radius, float3 rayDirection) { // TODO: Hasn't been tested with enough planes yet. pointOnPlane is assumed to actually be on plane
        for (int i = 0; i < NumVertices; i++) {
            float3 pointToVertex1 = pointOnPlane - edges[i].vertex1.Position;
            
            if (math.dot(edges[i].Normal, pointToVertex1) > radius * math.dot(edges[i].Normal, math.normalize(rayDirection))) // radius * 1/math.dot(Normal, math.normalize(rayDirection)))
                return false;
        }
        return true;
    }
}

struct Vertex {
    public float3 Position { get; set; }
}

struct Edge {
    //private bool isCachedValid;
    public Vertex vertex1;
    public Vertex vertex2;
    public float3 Vector { get; private set; }
    public float3 Normal { get; private set; }

    public Edge(Vertex vertex1, Vertex vertex2) {
        this.vertex1 = vertex1;
        this.vertex2 = vertex2;

        Vector = vertex2.Position - vertex1.Position;
        Normal = math.cross(math.up(), Vector);
    }

    public void UpdateNormal(float3 planeNormal) {
        Normal = math.normalize(math.cross(Vector, planeNormal));
    }
}