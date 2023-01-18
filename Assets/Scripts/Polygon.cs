using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

public struct Polygon // Clockwise. low-level
{
    private Vertex[] vertices;
    private Edge[] edges; // Edges by vertices in triangle are: (0 - 1), (1 - 2), (2 - 0)
    public int umVertices { get; private set; }
    public float3 normal { get; private set; }
    public float distance { get; private set; }
    public float thickness { get; private set; }

    public Polygon(float3[] vertexPositions, float thickness = 0.2f) : this()
    {
        this.thickness = thickness;
        umVertices = vertexPositions.Length;

        vertices = new Vertex[umVertices];
        for (int i = 0; i < umVertices; i++) {
            vertices[i].position = vertexPositions[i];
        }

        edges = new Edge[umVertices];
        for (int i = 0; i < umVertices; i++) {
            edges[i] = new Edge(vertices[i], vertices[i + 1 == umVertices ? 0 : i + 1]); // Edges in triangle are: (0 - 1), (1 - 2), (2 - 0)
        }
        UpdatePlaneEquation();
        UpdateEdgeNormals();
    }

    public Vector3[] GetFrontVertices() {
        Vector3[] frontVertices = new Vector3[umVertices];
        for (int i = 0; i < umVertices; i++) {
            frontVertices[i] = vertices[i].position;
        }
        return frontVertices;
    }
    public int[] GetFrontIndicesFan(int side = 0) {
        int numTris = (umVertices - 3) + 1;
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
        return vertices[index].position;
    }

    public void UpdatePlaneEquation() { // ax + by + cz = d -> Normal = (a,b,c), Distance = d
        normal =  math.normalize(math.cross(edges[0].vector, edges[1].vector));
        distance = math.dot(normal, vertices[0].position);
    }

    public void UpdateEdgeNormals() {
        for (int i = 0; i < umVertices; i++)
            edges[i].UpdateNormal(normal);
    }

    public bool RayCastConvex(Ray ray, float radius, out float3 hitPoint, float maxDistance = math.INFINITY) { // TODO: Hit can be behind ray origin. Also gotta check if line and plane are parallel
        float constants = math.dot(ray.origin, normal);
        float coefficients = math.dot(ray.direction, normal);
        float distanceAlongRay = (distance - constants) / coefficients;

        float3 hitPointOnPlane = ray.origin + (distanceAlongRay * ray.direction); // ray.direction comes normalized
        hitPoint = hitPointOnPlane;

        if (distanceAlongRay < 0 || distanceAlongRay > maxDistance) return false;

        //float angleCoeff = 1/math.dot(Normal, ray.direction);
        return IsPointInConvex(hitPointOnPlane, radius, ray.direction);
    }
    // Raycasting not fully working with spheres, going off the corners and high angle not working
    public bool IsPointInConvex(float3 pointOnPlane, float radius, float3 rayDirection) { // TODO: Hasn't been tested with enough planes yet. pointOnPlane is assumed to actually be on plane
        float dotPlaneNormalRay = math.abs(1/math.dot(normal, math.normalize(rayDirection)));
        for (int i = 0; i < umVertices; i++) {
            float3 pointToVertex1 = pointOnPlane - edges[i].vertex1.position;
            
            float dotEdgeNormalRay = math.abs(math.dot(edges[i].normal, math.normalize(rayDirection)));

            if (math.dot(edges[i].normal, pointToVertex1) > radius + radius * dotEdgeNormalRay * dotPlaneNormalRay) // Not working: + Thickness*math.abs(dotEdgeNormalRay))
                return false;
        }
        return true;
    }

    public bool IsPointInConvexSimple(float3 pointOnPlane, float radius, float3 rayDirection) { // TODO: Hasn't been tested with enough planes yet. pointOnPlane is assumed to actually be on plane
        for (int i = 0; i < umVertices; i++) {
            float3 pointToVertex1 = pointOnPlane - edges[i].vertex1.position;
            
            if (math.dot(edges[i].normal, pointToVertex1) > radius * math.dot(edges[i].normal, math.normalize(rayDirection))) // radius * 1/math.dot(Normal, math.normalize(rayDirection)))
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

    public Edge(Vertex vertex1, Vertex vertex2) {
        this.vertex1 = vertex1;
        this.vertex2 = vertex2;

        vector = vertex2.position - vertex1.position;
        normal = math.cross(math.up(), vector);
    }

    public void UpdateNormal(float3 planeNormal) {
        normal = math.normalize(math.cross(vector, planeNormal));
    }
}