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
    private BuildingGrid buildingGrid;

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

        buildingGrid = new BuildingGrid();

        TestDrawPolygon(new GameObject("TestPolygon"), testPolygon);
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
                
                for (int i = 0; i < testPolygon.NumVertices; i++) {
                    polygonRenderer.SetPosition(i, testPolygon.GetVertexPosition(i));// - testPolygon.Normal * testPolygon.Thickness/2);
                }
                polygonRenderer.SetPosition(testPolygon.NumVertices, testPolygon.GetVertexPosition(0));// - testPolygon.Normal * testPolygon.Thickness/2);

                // for (int i = testPolygon.NumVertices; i < testPolygon.NumVertices*2; i++) {
                //     polygonRenderer.SetPosition(i, testPolygon.GetVertexPosition(i - testPolygon.NumVertices) + testPolygon.Normal * testPolygon.Thickness/2);
                // }
                // polygonRenderer.SetPosition(testPolygon.NumVertices*2, testPolygon.GetVertexPosition(0) + testPolygon.Normal * testPolygon.Thickness/2);

                //yield return new WaitForSeconds(10);
                TestRayCast(ray);
            }
        }
    }

    void TestRayCast(Ray ray)
    {
        float rayRadius = 0f;
        if (testPolygon.RayCastConvex(ray, rayRadius, out float3 hitPoint, 10)) {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = hitPoint;
            sphere.transform.localScale = new float3(0.05f);
            Object.Destroy(sphere, 2.0f);
        }
    }

     void TestDrawPolygon(GameObject obj, Polygon polygon) {
        QuadCreator quadCreator = new QuadCreator(obj, polygon);
     }

    void OnDrawGizmos() {
        if (buildingGrid != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(new float3(GlobalConstants.MAP_BOTTOM_LEFT), 1);
            for (int x = 0; x < buildingGrid.dimensions.x; x++) {
                for (int y = 0; y < buildingGrid.dimensions.y; y++) {
                    Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(new int2(x, y)), new float3(buildingGrid.cellSize, 0.2f, buildingGrid.cellSize));
                }
            }
            float3 lineStart = GameObject.Find("RayStart").transform.position;
            float3 lineEnd = GameObject.Find("RayEnd").transform.position;
            Gizmos.DrawLine(lineStart, lineEnd);
            
            List<int2> cellCoords = buildingGrid.RasterLine(lineStart, lineEnd, 0);
            for (int i = 0; i < cellCoords.Count; i++) {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoords[i]) + new float3(0,0.2f,0), new float3(buildingGrid.cellSize, 0.2f, buildingGrid.cellSize));
            }
        }
    }

}

public class QuadCreator
{
    public QuadCreator(GameObject gameObject, Polygon polygon)
    {
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();

        mesh.vertices = polygon.GetFrontVertices();

        // int[] vertexIndices = polygon.GetFrontIndicesFan();
        // for (int i = 0; i < vertexIndices.Length; i++) {
        //     Debug.Log(vertexIndices[i]);
        // }
        mesh.triangles = polygon.GetFrontIndicesFan();

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