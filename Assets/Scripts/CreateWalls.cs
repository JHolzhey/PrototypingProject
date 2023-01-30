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
    LineRenderer polygonRenderer;
    GameObject[] polygonVertexHandles;
    BuildingGrid buildingGrid;
    Polygon selectedPolygon;

    Projectile[] projectiles;
    GameObject[] projectileObjects;
    GameObject[] projectilesPointers;
    int numProjectiles;
    public float maxRange = 40;
    float initialSpeed;
    public float projectileRadius = 0.5f;
    public float mass = 1;
    public float friction = 0.05f;

    public BuildingScriptableObject buildingScriptableObject;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 10+1;
        lineRenderer.widthMultiplier = 0.02f;

        float3[] vertexPositions = new float3[] {new float3(1, 0.2f, 0), new float3(1.5f, 2, 0), new float3(-1.5f, 2, 0), new float3(-1, 0.2f, 0)};
        testPolygon = new Polygon(vertexPositions);
        polygonRenderer = polygonObject.GetComponent<LineRenderer>();
        polygonRenderer.positionCount = (vertexPositions.Length + 1) * 2;
        polygonRenderer.widthMultiplier = 0.01f;
        polygonVertexHandles = new GameObject[10];
        for (int i = 0; i < 10; i++) {
            polygonVertexHandles[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            polygonVertexHandles[i].transform.localScale = new float3(0.2f);
        }

        float3[] vertexPositions2 = new float3[] {new float3(1, 0.2f, -10), new float3(1.5f, 2, -12), new float3(-1.5f, 2, -12), new float3(-1, 0.2f, -10)};
        testPolygon2 = new Polygon(vertexPositions);

        buildingGrid = new BuildingGrid();
        testPolygon.AddToGrid(buildingGrid);
        print(testPolygon.colliderSections.Length);
        
        TestDrawPolygon(new GameObject("TestPolygon"), testPolygon);
        TestDrawPolygon(new GameObject("TestPolygon2"), testPolygon2);

        List<int2> cellCoords = buildingGrid.RasterPolygon(testPolygon2);
        print("Polygon Count: " + cellCoords.Count);
        for (int i = 0; i < cellCoords.Count; i++) {
            GameObject sphere = CommonLib.CreatePrimitive(PrimitiveType.Sphere, buildingGrid.CellCoordsToWorld(cellCoords[i]) + new float3(0,0.2f,0), new float3(0.2f), Color.yellow);
        }
        

        projectilesPointers = new GameObject[100];
        projectileObjects = new GameObject[100];
        projectiles = new Projectile[100];
        for (int i = 0; i < 100; i++) {
            projectileObjects[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObjects[i].transform.localScale = new float3(projectileRadius*2);
            projectilesPointers[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            projectilesPointers[i].transform.localScale = new float3(0.01f);
            projectilesPointers[i].GetComponent<Renderer>().material.color = Color.red;

            projectiles[i] = new Projectile(maxRange, projectileRadius, mass, friction);
        }
        initialSpeed = math.sqrt(maxRange * GlobalConstants.GRAVITY);
    }

    // Update is called once per frame
    void Update()
    {
        frameNum++;
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit)) {
                float3 rayHitPosition = hit.point;
                // lineRenderer.SetPosition(0, transform.position - new Vector3(0, 0.01f, 0));
                // lineRenderer.SetPosition(1, rayHitPosition);

                /* TerrainData terrainData = Terrain.activeTerrain.terrainData; // Testing terrain normals
                float3 terrainBottomLeft = Terrain.activeTerrain.GetPosition();
                float3 normalizedPositon = (rayHitPosition - terrainBottomLeft) / terrainData.size;
                float3 terrainNormal = terrainData.GetInterpolatedNormal(normalizedPositon.x, normalizedPositon.z);
                GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stick.transform.localScale = new float3(0.05f, 1, 0.05f);
                stick.transform.position = rayHitPosition + terrainNormal * stick.transform.localScale.y/2;
                Quaternion rotation = Quaternion.LookRotation(math.cross(terrainNormal, math.up()), terrainNormal);
                stick.transform.rotation = rotation; */

                
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
            //Projectile projectile = projectiles[i]; // copy not by reference
            float radius = projectileRadius;

            float3 oldPosition = projectiles[i].position;
            projectiles[i].Step(deltaTime);
            float3 newPosition = projectiles[i].position;

            projectileObjects[i].transform.position = newPosition; // May be overwritten in below

            float terrainY = Terrain.activeTerrain.SampleHeight(newPosition);

            TerrainData terrainData = Terrain.activeTerrain.terrainData;
            float3 terrainBottomLeft = Terrain.activeTerrain.GetPosition();
            float3 normalizedPositon = (newPosition - terrainBottomLeft) / terrainData.size;
            float3 terrainNormal = terrainData.GetInterpolatedNormal(normalizedPositon.x, normalizedPositon.z);

            float3 arbitraryPointOnPlane = new float3(newPosition.x, terrainY, newPosition.z);
            float3 pointOnPlaneToProjectile = newPosition - arbitraryPointOnPlane;
            // if (newPosition.y - radius <= terrainY) { // basic quick method, better method below
            float penetration = -(math.dot(pointOnPlaneToProjectile, terrainNormal) - radius); // positive if penetrating
            if (penetration > 0)
            {
                float currentSpeed = math.length(projectiles[i].velocity);
                if (projectiles[i].isRolling) {
                    float3 fixPenetrationVector = (penetration-0.01f) * terrainNormal; // Pushes into the ground so next iteration will still be in ground and rolling
                    newPosition += fixPenetrationVector;
                    /* projectiles[numTrajectories - 1].transform.position = projectiles[i].transform.position; // setting this one because it's at the end
                    trajectories[i] = trajectories[numTrajectories - 1]; // replace with trajectory at the end
                    numTrajectories--; */

                    float3 perpendicularVelocity = terrainNormal * math.dot(projectiles[i].velocity, terrainNormal);
                    float3 parallelVelocity = projectiles[i].velocity - perpendicularVelocity;

                    float normalForce = mass * GlobalConstants.GRAVITY * math.dot(math.up(), terrainNormal);

                    projectileObjects[i].transform.position = newPosition;
                    projectiles[i].position = newPosition; // remove perpendicular and add parallel friction
                    projectiles[i].velocity -= (perpendicularVelocity + (parallelVelocity * (deltaTime * normalForce * friction)));

                } else {
                    float3 fixPenetrationVector = (penetration) * terrainNormal;
                    newPosition += fixPenetrationVector;

                    float3 velocityDirection = math.normalize(newPosition - oldPosition);
                    float3 reflectDirection = math.reflect(velocityDirection, terrainNormal);
                    
                    /* float dotDirectionNormal = math.dot(-velocityDirection, terrainNormal); // already in Polygon SphereCasting function
                    float dotCoeff = 1/dotDirectionNormal;
                    float underTerrainY = terrainY - newPosition.y;
                    float totalYArrow = underTerrainY + radius * dotDirectionNormal;
                    float totalYSphere = underTerrainY + radius;
                    float distanceBackwards = totalYSphere * dotCoeff; // replace penetration
                    float3 sphereOnTerrainPosition = newPosition - velocityDirection * distanceBackwards;
                    if (math.abs((sphereOnTerrainPosition.y - terrainY) - radius) > 0.001f) print("Error"); */

                    float restitution = 0.5f;
                    //float normalForce = mass * GlobalConstants.GRAVITY * math.dot(math.up(), terrainNormal);
                    Vector3 perpendicularVelocity = Vector3.Project(projectiles[i].velocity, terrainNormal);
                    Vector3 parallelVelocity = (Vector3)projectiles[i].velocity - perpendicularVelocity;
                    float3 result = (parallelVelocity) - restitution * perpendicularVelocity;
                    float3 reflectedVelocity = MathLib.Reflect(projectiles[i].velocity, terrainNormal, 0.4f);
                    
                    projectileObjects[i].transform.position = newPosition; // sphereOnTerrainPosition;
                    projectiles[i].position = newPosition;
                    projectiles[i].velocity = reflectedVelocity; // result;
                    projectiles[i].isRolling = true;
                }
            } else {
                projectiles[i].isRolling = false;
            }
            //CommonLib.CubeBetween2Points(newPosition, newPosition + projectiles[i].velocity, projectilesPointers[i]); // model velocity
        }
    }

    void TestRayCast(Ray ray)
    {
        for (int i = 0; i < selectedPolygon.numVertices; i++) {
            polygonVertexHandles[i].transform.position = float3.zero; // Deselect polygon
        }

        //float rayRadius = 0.3f;
        if (testPolygon.RayCastConvex(ray, out float3 hitPoint, 10)) {
            CommonLib.CreatePrimitive(PrimitiveType.Sphere, hitPoint, new float3(0.05f), Color.white, new Quaternion(), 5.0f);

            selectedPolygon = testPolygon;
            for (int i = 0; i < selectedPolygon.numVertices; i++) {
                polygonVertexHandles[i].transform.position = selectedPolygon.GetVertexPosition(i); // Select polygon
            }
        } else {
            selectedPolygon = new Polygon();
        }

        /* float3 cylinderCenter = GameObject.Find("Cylinder").transform.position;
        float cylinderHeight = GameObject.Find("Cylinder").transform.localScale.y;
        float cylinderRadius = GameObject.Find("Cylinder").transform.localScale.x/2; */

        GameObject capsule = GameObject.Find("Capsule");
        float capsuleRadius = capsule.GetComponent<Renderer>().bounds.size.x/2;
        float capsuleHeight = capsule.GetComponent<Renderer>().bounds.size.y - capsuleRadius*2;
        float3 capsuleSphere1 = capsule.transform.position - new Vector3(0, capsuleHeight/2, 0);
        float3 capsuleSphere2 = capsule.transform.position + new Vector3(0, capsuleHeight/2, 0);

        if (MathLib.IsRayAACapsuleIntersecting(ray.origin, ray.origin + ray.direction*10, capsuleSphere1, capsuleSphere2, capsuleHeight, capsuleRadius)) {
            print("Woohoo capsule");
        } else {
            print("Missed capsule");
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
            
            List<int2> cellCoords = buildingGrid.RasterRay(lineStart, lineEnd);
            print("Count: " + cellCoords.Count);
            for (int i = 0; i < cellCoords.Count; i++) {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoords[i]) + new float3(0,0.2f,0), new float3(buildingGrid.cellSize, 0.2f, buildingGrid.cellSize));
            }

            List<int2> cellCoords2 = buildingGrid.RasterRayOld(lineStart, lineEnd, 0);
            print("Count2: " + cellCoords2.Count);
            for (int i = 0; i < cellCoords2.Count; i++) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoords2[i]) + new float3(0,0.2f,0), new float3(buildingGrid.cellSize-0.1f, 0.2f, buildingGrid.cellSize-0.1f));
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

        mesh.vertices = polygon.GetFaceVertices();

        // int[] vertexIndices = polygon.GetFrontIndicesFan();
        // for (int i = 0; i < vertexIndices.Length; i++) {
        //     Debug.Log(vertexIndices[i]);
        // }
        mesh.triangles = polygon.GetFaceTriIndicesFan(0);

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