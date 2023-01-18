using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class CreateWalls : MonoBehaviour
{
    int frameNum = 0;
    float timeDeltaBig = 0;
    Camera cam;
    LineRenderer lineRenderer;
    public Polygon testPolygon;
    public GameObject polygonObject;
    LineRenderer polygonRenderer;
    BuildingGrid buildingGrid;

    Trajectory[] trajectories;
    GameObject[] projectiles;
    GameObject[] projectilesPointers;
    int numTrajectories;
    public float maxRange = 40;
    float initialSpeed;
    float projectileRadius = 0.1f;
    public float mass = 1;
    public float friction = 0.05f;

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

        buildingGrid = new BuildingGrid();

        TestDrawPolygon(new GameObject("TestPolygon"), testPolygon);

        projectilesPointers = new GameObject[100];
        projectiles = new GameObject[100];
        for (int i = 0; i < 100; i++) {
            projectiles[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectiles[i].transform.localScale = new float3(projectileRadius*2);
            projectilesPointers[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            projectilesPointers[i].transform.localScale = new float3(0.01f);
            projectilesPointers[i].GetComponent<Renderer>().material.color = Color.red;
        }
        trajectories = new Trajectory[100];
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

                TerrainData terrainData = Terrain.activeTerrain.terrainData;
                float3 terrainBottomLeft = Terrain.activeTerrain.GetPosition();
                float3 normalizedPositon = (rayHitPosition - terrainBottomLeft) / terrainData.size;
                float3 terrainNormal = terrainData.GetInterpolatedNormal(normalizedPositon.x, normalizedPositon.z);

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sphere.transform.localScale = new float3(0.05f, 1, 0.05f);
                sphere.transform.position = rayHitPosition + terrainNormal * sphere.transform.localScale.y/2;
                Quaternion rotation = Quaternion.LookRotation(math.cross(terrainNormal, math.up()), terrainNormal);
                sphere.transform.rotation = rotation;

                
                for (int i = 0; i < testPolygon.umVertices; i++) {
                    polygonRenderer.SetPosition(i, testPolygon.GetVertexPosition(i));// - testPolygon.Normal * testPolygon.Thickness/2);
                }
                polygonRenderer.SetPosition(testPolygon.umVertices, testPolygon.GetVertexPosition(0));// - testPolygon.Normal * testPolygon.Thickness/2);

                // for (int i = testPolygon.NumVertices; i < testPolygon.NumVertices*2; i++) {
                //     polygonRenderer.SetPosition(i, testPolygon.GetVertexPosition(i - testPolygon.NumVertices) + testPolygon.Normal * testPolygon.Thickness/2);
                // }
                // polygonRenderer.SetPosition(testPolygon.NumVertices*2, testPolygon.GetVertexPosition(0) + testPolygon.Normal * testPolygon.Thickness/2);

                //yield return new WaitForSeconds(10);
                TestRayCast(ray);

                // transform.position - new Vector3(0, 0.02f, 0)
                ProjectileLib.ComputeTrajectory(new float3(-5.45f,0.4f,-9.4f), rayHitPosition, initialSpeed, false, out trajectories[numTrajectories]);
                Trajectory trajectory = trajectories[numTrajectories]; // copy of struct not reference
                if (trajectory.isInRange) {
                    float3[] arcPositions = trajectory.GetPositionsOnArc(10);
                    for (int i = 0; i < arcPositions.Length; i++) {
                        lineRenderer.SetPosition(i, arcPositions[i]);
                    }

                    numTrajectories++;
                }
            }
        }
        TestUpdateProjectiles(Time.deltaTime);
        // timeDeltaBig += Time.deltaTime;
        // if (frameNum % 10 == 0) {
        //     TestUpdateProjectiles(timeDeltaBig);
        //     timeDeltaBig = 0;
        // }
    }

    void TestUpdateProjectiles(float deltaTime)
    {
        for (int i = 0; i < numTrajectories; i++) {
            //Trajectory trajectory = trajectories[i];
            float radius = projectileRadius;

            float3 oldPosition = trajectories[i].position;
            trajectories[i].Step(deltaTime);
            float3 newPosition = trajectories[i].position;

            projectiles[i].transform.position = newPosition; // May be overwritten in below

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
                float currentSpeed = math.length(trajectories[i].velocity);
                if (trajectories[i].isRolling) {
                    float3 fixPenetrationVector = (penetration-0.01f) * terrainNormal; // Pushes into the ground so next iteration will still be in ground and rolling
                    newPosition += fixPenetrationVector;
                    /* projectiles[numTrajectories - 1].transform.position = projectiles[i].transform.position; // setting this one because it's at the end
                    trajectories[i] = trajectories[numTrajectories - 1]; // replace with trajectory at the end
                    numTrajectories--; */

                    float3 perpendicularVelocity = terrainNormal * math.dot(trajectories[i].velocity, terrainNormal);
                    float3 parallelVelocity = trajectories[i].velocity - perpendicularVelocity;

                    float normalForce = mass * GlobalConstants.GRAVITY * math.dot(math.up(), terrainNormal);

                    projectiles[i].transform.position = newPosition;
                    trajectories[i].position = newPosition; // remove perpendicular and add parallel friction
                    trajectories[i].velocity -= (perpendicularVelocity + (parallelVelocity * (deltaTime * normalForce * friction)));

                } else {
                    float3 fixPenetrationVector = (penetration) * terrainNormal;
                    newPosition += fixPenetrationVector;

                    float3 velocityDirection = math.normalize(newPosition - oldPosition);
                    float3 reflectDirection = math.reflect(velocityDirection, terrainNormal);
                    
                    /* float dotDirectionNormal = math.dot(-velocityDirection, terrainNormal);
                    float dotCoeff = 1/dotDirectionNormal;
                    float underTerrainY = terrainY - newPosition.y;
                    float totalYArrow = underTerrainY + radius * dotDirectionNormal;
                    float totalYSphere = underTerrainY + radius;
                    float distanceBackwards = totalYSphere * dotCoeff;
                    float3 sphereOnTerrainPosition = newPosition - velocityDirection * distanceBackwards;
                    if (math.abs((sphereOnTerrainPosition.y - terrainY) - radius) > 0.001f) print("Error"); */

                    float restitution = 0.5f;
                    //float normalForce = mass * GlobalConstants.GRAVITY * math.dot(math.up(), terrainNormal);
                    Vector3 perpendicularVelocity = Vector3.Project(trajectories[i].velocity, terrainNormal);
                    Vector3 parallelVelocity = (Vector3)trajectories[i].velocity - perpendicularVelocity;
                    float3 result = (restitution * parallelVelocity) - restitution * perpendicularVelocity;
                    
                    projectiles[i].transform.position = newPosition; // sphereOnTerrainPosition;
                    trajectories[i].position = newPosition;
                    trajectories[i].velocity = result;
                    trajectories[i].isRolling = true;
                }
            } else {
                trajectories[i].isRolling = false;
            }
            GlobalConstants.CubeBetween2Points(newPosition, newPosition + trajectories[i].velocity, projectilesPointers[i]);
        }
    }

    void TestRayCast(Ray ray)
    {
        float rayRadius = 0f;
        if (testPolygon.RayCastConvex(ray, rayRadius, out float3 hitPoint, 10)) {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponent<Renderer>().material.color = Color.white;
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