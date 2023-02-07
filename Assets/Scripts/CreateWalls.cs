using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class CreateWalls : MonoBehaviour
{
    int frameNum = 0;
    Camera cam;
    LineRenderer lineRenderer;
    public Polygon testPolygon;
    public GameObject polygonObject;
    public Polygon testPolygon2;
    public Polygon testPolygon3;
    LineRenderer polygonRenderer;
    GameObject[] polygonVertexHandles;
    BuildingGrid buildingGrid;
    Polygon selectedPolygon;

    Projectile[] projectiles;
    GameObject[] projectileObjects;
    GameObject[] projectilesVelPointers;
    int numProjectiles;
    public float maxRange = 40;
    float initialSpeed;
    public float projectileRadius = 0.5f;
    public float mass = 1;
    public float friction = 0.05f;
    public int startArrowsIndex = 3; 

    public BuildingScriptableObject buildingScriptableObject;

    float3[] verticesTest;
    GameObject[] verticesTestSpheres;
    int vertexSphereHoveringIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 10+1;
        lineRenderer.widthMultiplier = 0.02f;

        float3[] vertexPositions1 = new float3[] {new float3(0.5f, 0.2f, 0.5f), new float3(1.5f, 2, 0.5f), new float3(-1.5f, 2, 0.5f), new float3(-0.5f, 0.2f, 0.5f)};
        testPolygon = new Polygon(vertexPositions1);
        polygonRenderer = polygonObject.GetComponent<LineRenderer>();
        polygonRenderer.positionCount = (vertexPositions1.Length + 1) * 2;
        polygonRenderer.widthMultiplier = 0.01f;
        polygonVertexHandles = new GameObject[10];
        for (int i = 0; i < 10; i++) {
            polygonVertexHandles[i] = CommonLib.CreatePrimitive(PrimitiveType.Sphere, float3.zero, new float3(0.2f), Color.black);
        }

        buildingGrid = new BuildingGrid();
        // testPolygon.AddToGrid(buildingGrid);

        float3[] vertexPositions2 = new float3[] {new float3(1.5f, 0.2f, -10.5f), new float3(6.5f, 0.6f, -14.5f), new float3(0.5f, 0.6f, -18.5f), new float3(-6.5f, 0.6f, -14.5f), new float3(-1.5f, 0.2f, -10.5f)};
        testPolygon2 = new Polygon(vertexPositions2);

        float3[] vertexPositions3 = new float3[] {new float3(1, 0.2f, 5), new float3(1.5f, 2, 5), new float3(-1.5f, 2, 5), new float3(-1, 0.2f, 5)};
        testPolygon3 = new Polygon(vertexPositions3);
        
        TestDrawPolygon(new GameObject("TestPolygon"), testPolygon);
        TestDrawPolygon(new GameObject("TestPolygonSide2"), testPolygon, 0);
        TestDrawPolygon(new GameObject("TestPolygon2"), testPolygon2, 0);
        TestDrawPolygon(new GameObject("TestPolygon3"), testPolygon3);

        List<int2> rasterCellCoords = buildingGrid.RasterPolygon(testPolygon2, out List<int2> bottomEdgeCellCoords, out List<int2> topEdgeCellCoords);
        // print("bottomEdgeCellCoords: " + bottomEdgeCellCoords.Count + ", topEdgeCellCoords: " + topEdgeCellCoords.Count);
        int numXCoords = bottomEdgeCellCoords.Count;
        for (int i = 0; i < numXCoords; i++) {
            GameObject sphere = CommonLib.CreatePrimitive(PrimitiveType.Sphere, buildingGrid.CellCoordsToWorld(bottomEdgeCellCoords[i]) + new float3(0,0.1f*i,0), new float3(0.2f), Color.green);
            GameObject sphere2 = CommonLib.CreatePrimitive(PrimitiveType.Sphere, buildingGrid.CellCoordsToWorld(topEdgeCellCoords[(numXCoords - 1) - i]) + new float3(0,0.1f*i,0), new float3(0.2f), Color.red);
        }
        for (int i = 0; i < rasterCellCoords.Count; i++) {
            GameObject sphere = CommonLib.CreatePrimitive(PrimitiveType.Sphere, buildingGrid.CellCoordsToWorld(rasterCellCoords[i]), new float3(0.2f), Color.yellow);
        }
        
        projectilesVelPointers = new GameObject[100];
        projectileObjects = new GameObject[100];
        projectiles = new Projectile[100];
        for (int i = 0; i < 100; i++) {
            PrimitiveType primitiveType = PrimitiveType.Sphere; 
            float radius = projectileRadius;
            float3 size = new float3(projectileRadius*2);
            if (i >= startArrowsIndex) {
                primitiveType = PrimitiveType.Cube;
                radius = 0;
                size = new float3(0.1f, 0.1f, projectileRadius*2);
            }
            projectileObjects[i] = CommonLib.CreatePrimitive(primitiveType, float3.zero, size, Color.gray);
            projectilesVelPointers[i] = CommonLib.CreatePrimitive(PrimitiveType.Cube, float3.zero, new float3(0.01f), Color.red);
            projectiles[i] = new Projectile(maxRange, radius, mass, friction);
        }
        initialSpeed = math.sqrt(maxRange * GlobalConstants.GRAVITY);


        int totalVertices = vertexPositions1.Length + vertexPositions2.Length + vertexPositions3.Length;
        verticesTest = new float3[totalVertices];
        verticesTestSpheres = new GameObject[totalVertices];
        for (int i = 0; i < vertexPositions1.Length; i++) verticesTest[i] = vertexPositions1[i];
        for (int i = 0; i < vertexPositions2.Length; i++) verticesTest[i + vertexPositions1.Length] = vertexPositions2[i];
        for (int i = 0; i < vertexPositions3.Length; i++) verticesTest[i + vertexPositions1.Length + vertexPositions2.Length] = vertexPositions3[i];

        for (int i = 0; i < totalVertices; i++) {
            verticesTestSpheres[i] = CommonLib.CreatePrimitive(PrimitiveType.Sphere, verticesTest[i], new float3(0.1f), Color.white);
            buildingGrid.AddEntityToCell(buildingGrid.WorldToCellCoords(verticesTest[i]), i);
        }
    }

    // Update is called once per frame
    void Update()
    {
        frameNum++;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        TestVerticesRayCast(ray);

        if (Input.GetMouseButtonDown(0)) {

            float sphereCastRadius = 0.1f;
            CommonLib.ObjectBetween2Points(ray.origin, ray.origin + ray.direction*20, CommonLib.CreatePrimitive(PrimitiveType.Cylinder, float3.zero, new float3(sphereCastRadius*2), Color.grey));
            
            if (Physics.Raycast(ray, out RaycastHit hit)) {
                float3 rayHitPosition = hit.point;
                // lineRenderer.SetPosition(0, transform.position - new Vector3(0, 0.01f, 0));
                // lineRenderer.SetPosition(1, rayHitPosition);

                
                for (int i = 0; i < testPolygon.numVertices; i++) {
                    polygonRenderer.SetPosition(i, testPolygon.GetVertexPosition(i));// - testPolygon.Normal * testPolygon.Thickness/2);
                }
                polygonRenderer.SetPosition(testPolygon.numVertices, testPolygon.GetVertexPosition(0));// - testPolygon.Normal * testPolygon.Thickness/2);

                // for (int i = testPolygon.NumVertices; i < testPolygon.NumVertices*2; i++) { // double sided polygon
                //     polygonRenderer.SetPosition(i, testPolygon.GetVertexPosition(i - testPolygon.NumVertices) + testPolygon.Normal * testPolygon.Thickness/2);
                // }
                // polygonRenderer.SetPosition(testPolygon.NumVertices*2, testPolygon.GetVertexPosition(0) + testPolygon.Normal * testPolygon.Thickness/2);

                TestRayCast(ray);
                
                if (projectiles[numProjectiles].ComputeTrajectory(new float3(-5.45f,1.1f,-9.4f), rayHitPosition, initialSpeed, false)) {
                    float3[] arcPositions = projectiles[numProjectiles].trajectory.GetPositionsOnArc(10);
                    for (int i = 0; i < arcPositions.Length; i++) {
                        lineRenderer.SetPosition(i, arcPositions[i]);
                    }

                    numProjectiles++;
                }
            }
        }
        TestUpdateProjectiles(Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.T)) {
            NewWall();
        }
    }

    void NewWall() {

    }

    void TestUpdateProjectiles(float deltaTime) // Contains simple sphere/arrow against terrain ray cast
    {
        for (int i = 0; i < numProjectiles; i++) {
            if (projectiles[i].timeAlive != -1) {
                projectiles[i].Update(deltaTime);

                if (i >= startArrowsIndex) {
                    float3 velocityDirection = math.normalize(projectiles[i].velocity);
                    projectileObjects[i].transform.position = projectiles[i].position - velocityDirection * (projectileObjects[i].transform.localScale.z/2);
                    projectileObjects[i].transform.localRotation = Quaternion.LookRotation(velocityDirection, math.up());
                } else {
                    projectileObjects[i].transform.position = projectiles[i].position;
                }
                CommonLib.ObjectBetween2Points(projectiles[i].position, projectiles[i].position + projectiles[i].velocity, projectilesVelPointers[i]); // model velocity
            }
        }
    }

    void TestRayCast(Ray ray)
    {
        float rayLength = 10;

        for (int i = 0; i < selectedPolygon.numVertices; i++) {
            polygonVertexHandles[i].transform.position = float3.zero; // Deselect polygon
        }

        //float rayRadius = 0.3f;
        if (testPolygon.RayCastConvex(ray.origin, ray.direction, rayLength, out float3 hitPoint)) {
            CommonLib.CreatePrimitive(PrimitiveType.Sphere, hitPoint, new float3(0.05f), Color.white, new Quaternion(), 5.0f);

            selectedPolygon = testPolygon;
            for (int i = 0; i < selectedPolygon.numVertices; i++) {
                polygonVertexHandles[i].transform.position = selectedPolygon.GetVertexPosition(i); // Select polygon
            }
        } else {
            selectedPolygon = new Polygon();
        }

        GameObject capsule = GameObject.Find("Capsule");
        float capsuleRadius = capsule.GetComponent<Renderer>().bounds.size.x/2;
        float capsuleHeight = capsule.GetComponent<Renderer>().bounds.size.y - capsuleRadius*2;
        float3 capsuleSphere1 = capsule.transform.position - new Vector3(0, capsuleHeight/2, 0);
        float3 capsuleSphere2 = capsule.transform.position + new Vector3(0, capsuleHeight/2, 0);

        if (MathLib.IsRayAACapsuleIntersecting(ray.origin, ray.origin + ray.direction*10, capsuleSphere1, capsuleSphere2, capsuleHeight, capsuleRadius)) {
            // print("Hit capsule");
        } else {
            // print("Missed capsule");
        }
    }

    void TestVerticesRayCast(Ray ray) {
        verticesTestSpheres[vertexSphereHoveringIndex].transform.localScale = new float3(0.1);

        float rayLength = 10;
        float sphereRadius = 0.1f;
        List<int2> cellCoords = buildingGrid.RasterRay(ray.origin, ray.origin + ray.direction*rayLength);
        for (int i = 0; i < cellCoords.Count; i++) {
            int[] cellEntities = buildingGrid.GetCellEntities(cellCoords[i]);
            float closestHitAlongRay = math.INFINITY;
            int closestHitIndex = -1;
            for (int j = 0; j < cellEntities.Length; j++) { // Must go through all entities and choose hit that has smallest distanceAlongRay
                int vertexIndex = cellEntities[j];
                float3 sphereCenter = verticesTest[vertexIndex];
                bool isHit = MathLib.IsRaySphereIntersecting(ray.origin, ray.direction, rayLength, sphereCenter, sphereRadius, out float distanceAlongRay);
                if (isHit && distanceAlongRay < closestHitAlongRay) {
                    closestHitIndex = vertexIndex;
                    closestHitAlongRay = distanceAlongRay;
                }
            }
            if (closestHitIndex != -1) { // Means we hit something
                verticesTestSpheres[closestHitIndex].transform.localScale = new float3(0.2);
                vertexSphereHoveringIndex = closestHitIndex;
                break;
            }
        }
    }

     void TestDrawPolygon(GameObject obj, Polygon polygon, int isFrontFace = 1) {
        QuadCreator quadCreator = new QuadCreator(obj, polygon, isFrontFace);
     }

    void OnDrawGizmos() {
        if (buildingGrid != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(new float3(GlobalConstants.MAP_BOTTOM_LEFT), 1); // Debug grid:
            for (int x = 0; x < buildingGrid.dimensions.x; x++) {
                for (int y = 0; y < buildingGrid.dimensions.y; y++) {
                    Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(new int2(x, y)), new float3(buildingGrid.cellSize, 0.2f, buildingGrid.cellSize));
                }
            }

            float3 lineStart = GameObject.Find("RayStart").transform.position; // Debug ray casting:
            float3 lineEnd = GameObject.Find("RayEnd").transform.position;
            Gizmos.DrawLine(lineStart, lineEnd);
            
            List<int2> cellCoords = buildingGrid.RasterRayOneX(lineStart, lineEnd, true);
            // print("Count: " + cellCoords.Count);
            for (int i = 0; i < cellCoords.Count; i++) {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoords[i]) + new float3(0,0.2f,0), new float3(buildingGrid.cellSize, 0.2f, buildingGrid.cellSize));
            }

            List<int2> cellCoords2 = buildingGrid.RasterRayOld(lineStart, lineEnd);
            // print("Count2: " + cellCoords2.Count);
            for (int i = 0; i < cellCoords2.Count; i++) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoords2[i]) + new float3(0,0.2f,0), new float3(buildingGrid.cellSize-0.5f, 0.2f, buildingGrid.cellSize-0.5f));
            }
        }
    }

}

public class QuadCreator
{
    public QuadCreator(GameObject gameObject, Polygon polygon, int isFrontFace = 1)
    {
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();

        mesh.vertices = polygon.GetFaceVertices(isFrontFace);

        // int[] vertexIndices = polygon.GetFrontIndicesFan();
        // for (int i = 0; i < vertexIndices.Length; i++) {
        //     Debug.Log(vertexIndices[i]);
        // }
        mesh.triangles = polygon.GetFaceTriIndicesFan(isFrontFace);

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

/* TerrainData terrainData = Terrain.activeTerrain.terrainData; // Testing terrain normals by placing vertical stick
float3 terrainBottomLeft = Terrain.activeTerrain.GetPosition();
float3 normalizedPositon = (rayHitPosition - terrainBottomLeft) / terrainData.size;
float3 terrainNormal = terrainData.GetInterpolatedNormal(normalizedPositon.x, normalizedPositon.z);
GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cube);
stick.transform.localScale = new float3(0.05f, 1, 0.05f);
stick.transform.position = rayHitPosition + terrainNormal * stick.transform.localScale.y/2;
Quaternion rotation = Quaternion.LookRotation(math.cross(terrainNormal, math.up()), terrainNormal);
stick.transform.rotation = rotation; */