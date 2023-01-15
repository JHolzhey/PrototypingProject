using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct Polygon // Clockwise
{
    private Vertex[] vertices;
    private Edge[] edges;
    public int NumVertices { get; set; }

    public Polygon(float3[] vertexPositions) {
        NumVertices = vertexPositions.Length;

        vertices = new Vertex[NumVertices];
        for (int i = 0; i < NumVertices; i++) {
            vertices[i].Position = vertexPositions[i];
        }
        edges = new Edge[NumVertices];
        Vertex vertex2 = vertices[NumVertices - 1];
        for (int i = 0; i < NumVertices; i++) {
            Vertex vertex1 = vertices[i];
            edges[i] = new Edge(vertex1, vertex2);
            vertex2 = vertex1;
        }
    }
    
    public float3 GetVertexPosition(int index) {
        return vertices[index].Position;
    }
}

struct Vertex {
    public float3 Position { get; set; }
}

struct Edge {
    Vertex vertex1;
    Vertex vertex2;

    public Edge(Vertex vertex1, Vertex vertex2) {
        this.vertex1 = vertex1;
        this.vertex2 = vertex2;
    }
}