using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class CreateWalls : MonoBehaviour
{
    private float3 rayHit;
    private Camera cam;
    private LineRenderer lineRenderer;
    public Polygon testPolygon;
    public GameObject polygonObject;
    private LineRenderer polygonRenderer;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.widthMultiplier = 0.02f;

        float3[] vertexPositions = new float3[] {new float3(1, 0.2f, 0), new float3(1.5f, 2, 0), new float3(-1.5f, 2, 0), new float3(-1, 0.2f, 0)};
        testPolygon = new Polygon(vertexPositions);
        polygonRenderer = polygonObject.GetComponent<LineRenderer>();
        polygonRenderer.positionCount = (vertexPositions.Length + 1) * 2;
        polygonRenderer.widthMultiplier = 0.01f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) {
                rayHit = hit.point;
                lineRenderer.SetPosition(0, transform.position - new Vector3(0, 0.01f, 0));
                lineRenderer.SetPosition(1, rayHit);
                
                int sum = 0;
                for (int i = 0; i < testPolygon.NumVertices; i++) {
                    polygonRenderer.SetPosition(i, testPolygon.GetVertexPosition(i));// - testPolygon.Normal * testPolygon.Thickness/2);
                    sum++;
                }
                polygonRenderer.SetPosition(testPolygon.NumVertices, testPolygon.GetVertexPosition(0));// - testPolygon.Normal * testPolygon.Thickness/2);

                // for (int i = testPolygon.NumVertices; i < testPolygon.NumVertices*2; i++) {
                //     polygonRenderer.SetPosition(i, testPolygon.GetVertexPosition(i - testPolygon.NumVertices) + testPolygon.Normal * testPolygon.Thickness/2);
                //     sum++;
                // }
                // polygonRenderer.SetPosition(testPolygon.NumVertices*2, testPolygon.GetVertexPosition(0) + testPolygon.Normal * testPolygon.Thickness/2);

                //print(sum);

                TestRayCast(ray);
            }
        }
    }

    void TestRayCast(Ray ray)
    {
        float rayRadius = 0.25f;
        if (testPolygon.RayCastConvex(ray, rayRadius, out float3 hitPoint)) {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = hitPoint;
            sphere.transform.localScale = new float3(rayRadius*2);
            Object.Destroy(sphere, 2.0f);
        }
    }

    void OnDrawGizmos() {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.blue;
        //Gizmos.DrawSphere(rayHit, 1);
    }

}

public class QuadCreator : MonoBehaviour
{
    public float width = 1;
    public float height = 1;

    public void Start()
    {
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(0, 0, 0),
            new Vector3(width, 0, 0),
            new Vector3(0, height, 0),
            new Vector3(width, height, 0)
        };
        mesh.vertices = vertices;

        int[] tris = new int[6]
        {
            // lower left triangle
            0, 2, 1,
            // upper right triangle
            2, 3, 1
        };
        mesh.triangles = tris;

        Vector3[] normals = new Vector3[4]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
        };
        mesh.normals = normals;

        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.uv = uv;

        meshFilter.mesh = mesh;
    }
}