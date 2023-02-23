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
    public Polygon testPolygon1;
    public GameObject polygonObject;
    public Polygon testPolygon2;
    public Polygon testPolygon3;
    LineRenderer polygonRenderer;
    GameObject[] polygonVertexHandles;
    BuildingGrid buildingGrid;
    Polygon selectedPolygon;
    public Material polygonMaterial;

    Projectile[] projectiles;
    GameObject[] projectileObjects;
    GameObject[] projectilesVelPointers;
    int numProjectiles;
    int maxProjectiles = 1000;
    public float maxRange = 40;
    float initialSpeed;
    public float projectileSphereRadius = 0.5f;
    public float mass = 1;
    public float friction = 0.05f;
    public int startArrowsIndex = 3; 

    public BuildingScriptableObject buildingScriptableObject;

    TestEntity[] entities;
    float3[] verticesTest;
    int vertexSphereHoveringIndex = 0;
    int polygonHoveringIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 10+1;
        lineRenderer.widthMultiplier = 0.02f;

        float3[] vertexPositions1 = new float3[] {new float3(0.5f, 0.2f, 0.5f), new float3(1.5f, 2, 0.5f), new float3(-1.5f, 2, 0.5f), new float3(-0.5f, 0.2f, 0.5f)};
        testPolygon1 = new Polygon(vertexPositions1);
        polygonRenderer = polygonObject.GetComponent<LineRenderer>();
        polygonRenderer.positionCount = (vertexPositions1.Length + 1) * 2;
        polygonRenderer.widthMultiplier = 0.01f;
        polygonVertexHandles = new GameObject[10];
        for (int i = 0; i < 10; i++) {
            polygonVertexHandles[i] = CommonLib.CreatePrimitive(PrimitiveType.Sphere, float3.zero, new float3(0.2f), Color.black);
        }

        buildingGrid = new BuildingGrid();

        float3[] vertexPositions2 = new float3[] {new float3(1.5f, 0.6f, -10.5f), new float3(6.5f, 0.6f, -14.5f), new float3(0.5f, 0.6f, -18.5f), new float3(-6.5f, 0.6f, -14.5f), new float3(-1.5f, 0.6f, -10.5f)};
        testPolygon2 = new Polygon(vertexPositions2);

        float3[] vertexPositions3 = new float3[] {new float3(1, 0.2f, 5), new float3(1.5f, 2, 5), new float3(-1.5f, 2, 5), new float3(-1, 0.2f, 5)};
        testPolygon3 = new Polygon(vertexPositions3);
        
        // TestDrawPolygon(new GameObject("TestPolygon"), testPolygon);
        // TestDrawPolygon(new GameObject("TestPolygonSide2"), testPolygon, 0);
        // TestDrawPolygon(new GameObject("TestPolygon2"), testPolygon2, 0);
        // TestDrawPolygon(new GameObject("TestPolygon3"), testPolygon3);
        
        projectilesVelPointers = new GameObject[maxProjectiles];
        projectileObjects = new GameObject[maxProjectiles];
        projectiles = new Projectile[maxProjectiles];
        for (int i = 0; i < maxProjectiles; i++) {
            PrimitiveType primitiveType = PrimitiveType.Sphere; 
            float radius = projectileSphereRadius;
            float3 size = new float3(projectileSphereRadius*2);
            if (i >= startArrowsIndex) {
                primitiveType = PrimitiveType.Cube;
                radius = 0.01f;
                size = new float3(0.1f, 0.1f, projectileSphereRadius*2);
            }
            projectileObjects[i] = CommonLib.CreatePrimitive(primitiveType, float3.zero, size, Color.gray);
            projectilesVelPointers[i] = CommonLib.CreatePrimitive(PrimitiveType.Cube, float3.zero, new float3(0.01f), Color.red);
            projectiles[i] = new Projectile(maxRange, radius, mass, friction);
        }
        initialSpeed = math.sqrt(maxRange * GlobalConstants.GRAVITY);
        

        int totalVertices = vertexPositions1.Length + vertexPositions2.Length + vertexPositions3.Length;
        verticesTest = new float3[totalVertices];
        for (int i = 0; i < vertexPositions1.Length; i++) verticesTest[i] = vertexPositions1[i];
        for (int i = 0; i < vertexPositions2.Length; i++) verticesTest[i + vertexPositions1.Length] = vertexPositions2[i];
        for (int i = 0; i < vertexPositions3.Length; i++) verticesTest[i + vertexPositions1.Length + vertexPositions2.Length] = vertexPositions3[i];
        entities = new TestEntity[totalVertices + 3];
        for (int i = 0; i < totalVertices; i++) {
            GameObject vertexSphere = CommonLib.CreatePrimitive(PrimitiveType.Sphere, verticesTest[i], new float3(0.1f), Color.white);
            SphereCollider sphereCollider = new SphereCollider(verticesTest[i], 0.1f);
            entities[i] = new TestEntity(EntityType.Vertex, sphereCollider, vertexSphere);
        }
        entities[totalVertices] = new TestEntity(EntityType.Polygon, testPolygon1, TestDrawPolygon(testPolygon1));
        entities[totalVertices + 1] = new TestEntity(EntityType.Polygon, testPolygon2, TestDrawPolygon(testPolygon2));
        entities[totalVertices + 2] = new TestEntity(EntityType.Polygon, testPolygon3, TestDrawPolygon(testPolygon3));
        float2[] uvPosition = testPolygon1.GetVertexUVPositions();

        for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++) {
            entities[entityIndex].collider.AddToGrid(buildingGrid, entityIndex);
            // if (entities[entityIndex].type == EntityType.Polygon) {
            //     entities[entityIndex].collider.AddToGrid(buildingGrid, entityIndex);
            // } else {
            //     buildingGrid.AddEntityToCell(verticesTest[entityIndex], entityIndex);
            // }
        }
        Terrain.activeTerrain.InitLayerToMaterialIndices();
    }

    // Update is called once per frame
    void Update()
    {
        frameNum++;

        RayInput ray = cam.ScreenPointToRay(Input.mousePosition, 30);
        TestEntitiesRayCast(ray);

        if (Input.GetMouseButtonDown(0)) {

            // float sphereCastRadius = 0.1f;
            // CommonLib.ObjectBetween2Points(ray.origin, ray.origin + ray.direction*20, CommonLib.CreatePrimitive(PrimitiveType.Cylinder, float3.zero, new float3(sphereCastRadius*2), Color.grey));
            
            TerrainCollider terrainCollider = Terrain.activeTerrain.GetComponent<TerrainCollider>();
            if (terrainCollider.Raycast(new Ray(ray.start, ray.direction), out RaycastHit hit, 40)) {
                float3 rayHitPosition = hit.point;
                // lineRenderer.SetPosition(0, transform.position - new Vector3(0, 0.01f, 0));
                // lineRenderer.SetPosition(1, rayHitPosition);
                Materials.Type materialHit = Terrain.activeTerrain.SampleMaterial(rayHitPosition);
                
                for (int i = 0; i < testPolygon1.numVertices; i++) {
                    polygonRenderer.SetPosition(i, testPolygon1.GetVertexPosition(i));// - testPolygon.Normal * testPolygon.Thickness/2);
                }
                polygonRenderer.SetPosition(testPolygon1.numVertices, testPolygon1.GetVertexPosition(0));// - testPolygon.Normal * testPolygon.Thickness/2);

                // for (int i = testPolygon.NumVertices; i < testPolygon.NumVertices*2; i++) { // double sided polygon
                //     polygonRenderer.SetPosition(i, testPolygon.GetVertexPosition(i - testPolygon.NumVertices) + testPolygon.Normal * testPolygon.Thickness/2);
                // }
                // polygonRenderer.SetPosition(testPolygon.NumVertices*2, testPolygon.GetVertexPosition(0) + testPolygon.Normal * testPolygon.Thickness/2);

                TestRayCast(ray);
                
                int numArchers = 10;
                float3 startPos = new float3(-15.4f, 0.8f, -15.5f);
                for (int i = 0; i < numArchers; i++) {
                    float3 archerPos = startPos + new float3(i/2, 0, 0);
                    if (projectiles[numProjectiles].ComputeTrajectory(archerPos, rayHitPosition, initialSpeed, false)) {

                        if (i == 1) {
                            float3[] arcPositions = projectiles[numProjectiles].trajectory.GetPositionsOnArc(10);
                            for (int j = 0; j < arcPositions.Length; j++) {
                                lineRenderer.SetPosition(i, arcPositions[i]);
                            }
                        }

                        numProjectiles++;
                    }
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
                projectiles[i].Update(deltaTime, buildingGrid, entities);

                if (i >= startArrowsIndex) {
                    float3 velocityDirection = math.normalize(projectiles[i].velocity);

                    projectileObjects[i].transform.position = projectiles[i].position - velocityDirection * (projectileObjects[i].transform.localScale.z/2);
                    projectileObjects[i].transform.localRotation = Quaternion.LookRotation(velocityDirection, math.up());
                } else {
                    projectileObjects[i].transform.position = projectiles[i].position;
                }
                //CommonLib.ObjectBetween2Points(projectiles[i].position, projectiles[i].position + projectiles[i].velocity, projectilesVelPointers[i]); // model velocity
            } else {
                // projectiles[i] = projectiles[numProjectiles - 1]; // replace with trajectory at the end
                // numProjectiles--;
            }
        }
    }

    void TestRayCast(RayInput ray)
    {
        for (int i = 0; i < selectedPolygon.numVertices; i++) {
            polygonVertexHandles[i].transform.position = float3.zero; // Deselect polygon
        }

        //float rayRadius = 0.3f;
        if (testPolygon1.RayCastConvex(ray, out float _, out float3 hitPoint)) {
            CommonLib.CreatePrimitive(PrimitiveType.Sphere, hitPoint, new float3(0.05f), Color.white, new Quaternion(), 5.0f);

            selectedPolygon = testPolygon1;
            for (int i = 0; i < selectedPolygon.numVertices; i++) {
                polygonVertexHandles[i].transform.position = selectedPolygon.GetVertexPosition(i); // Select polygon
            }
        } else {
            selectedPolygon = new Polygon();
        }

        GameObject capsule = GameObject.Find("Capsule");
        float capsuleRadius = capsule.GetComponent<Renderer>().bounds.size.x/2;
        float capsuleHeight = capsule.GetComponent<Renderer>().bounds.size.y - capsuleRadius*2;
        float3 capsuleSphereStart = capsule.transform.position - new Vector3(0, capsuleHeight/2, 0);
        float3 capsuleSphereEnd = capsule.transform.position + new Vector3(0, capsuleHeight/2, 0);

        if (MathLib.IsRayAACapsuleIntersecting(ray.start, ray.end, capsuleSphereStart, capsuleSphereEnd, capsuleHeight, capsuleRadius)) {
            // print("Hit capsule");
        } else {
            // print("Missed capsule");
        }
    }

    void TestEntitiesRayCast(RayInput ray) {
        entities[vertexSphereHoveringIndex].obj.transform.localScale = new float3(0.1);
        entities[vertexSphereHoveringIndex].obj.GetComponent<Renderer>().material.color = Color.white;
        entities[polygonHoveringIndex].obj.GetComponent<Renderer>().material.color = Color.white;

        List<int2> cellCoords = buildingGrid.RasterRay(ray);
        for (int i = 0; i < cellCoords.Count; i++) {
            int[] cellEntities = buildingGrid.GetCellEntities(cellCoords[i]);
            int closestHitIndex = -1;  float closestHitAlongRay = math.INFINITY;

            for (int j = 0; j < cellEntities.Length; j++) { // Must go through all entities in cell and choose hit that has smallest distanceAlongRay
                int entityIndex = cellEntities[j];

                float distanceAlongRay;
                bool isHit = entities[entityIndex].collider.IsRayCastColliding(ray, out distanceAlongRay);
                // if (entities[entityIndex].type == EntityType.Polygon) {
                //     isHit = entities[entityIndex].collider.RayCast(ray, out distanceAlongRay);
                // } else {
                //     float3 sphereCenter = entities[entityIndex].vertexPosition;
                //     isHit = MathLib.IsRaySphereIntersecting(ray.start, ray.direction, rayLength, sphereCenter, sphereRadius, out distanceAlongRay);
                // }

                if (isHit && distanceAlongRay < closestHitAlongRay) {
                    closestHitIndex = entityIndex;  closestHitAlongRay = distanceAlongRay;
                }
            }
            if (closestHitIndex != -1) { // Means we hit something
                if (entities[closestHitIndex].type == EntityType.Polygon) {
                    polygonHoveringIndex = closestHitIndex;
                    entities[polygonHoveringIndex].obj.GetComponent<Renderer>().material.color = Color.red;
                } else {
                    vertexSphereHoveringIndex = closestHitIndex;
                    entities[vertexSphereHoveringIndex].obj.GetComponent<Renderer>().material.color = Color.red;
                    entities[vertexSphereHoveringIndex].obj.transform.localScale = new float3(0.2);
                }
                break; // No need to check next cells since this must be the closest hit
            }
        }
    }

    GameObject TestDrawPolygon(Polygon polygon, int isFrontFace = 1) {
        return TestDrawPolygon(new GameObject("TestPolygon"), polygon, isFrontFace);
    }

    GameObject TestDrawPolygon(GameObject obj, Polygon polygon, int isFrontFace = 1) {
        QuadCreator quadCreator = new QuadCreator(obj, polygon, polygonMaterial, 0);
        QuadCreator quadCreator2 = new QuadCreator(obj, polygon, polygonMaterial, 1);
        return obj;
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

            float cellSize = buildingGrid.cellSize;
            float size = cellSize - 0.5f;
            float radius = 0.5f;
            RayInput ray = new RayInput(lineStart, lineEnd);
            float3 tangent = MathLib.CalcTangentToNormal(ray.direction);
            float3 directionOffset = ray.direction*radius;
            float3 tangentOffset = tangent*radius;
            float3 startUpper = ray.start + tangentOffset - directionOffset;
            float3 endUpper = ray.end + tangentOffset + directionOffset;
            float3 startLower = ray.start - tangentOffset - directionOffset;
            float3 endLower = ray.end - tangentOffset + directionOffset;

            List<int2> cellCoordsUpper = buildingGrid.RasterRay(startUpper, endUpper);
            List<int2> cellCoordsLower = buildingGrid.RasterRay(startLower, endLower);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(startUpper, endUpper);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(startLower, endLower);

            int maxCells = math.max(cellCoordsLower.Count, cellCoordsUpper.Count);
            int upperIndex = 0;   int lowerIndex = 0; 
            for (int i = 0; i < maxCells; i++) {
                float3 upShift = new float3(0,0.1f*i,0);
                bool isContinueLower = lowerIndex < cellCoordsLower.Count;
                bool isContinueUpper = upperIndex < cellCoordsUpper.Count;

                if (isContinueLower && isContinueUpper && math.all(cellCoordsUpper[upperIndex] == cellCoordsLower[lowerIndex])) {
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoordsUpper[upperIndex]) + upShift, new float3(size, 0.2f, size));
                    upperIndex++;
                    lowerIndex++;
                } else {
                    // Lower work:
                    if (isContinueLower) { // TODO: Abstract to while loop
                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoordsLower[lowerIndex]) + upShift, new float3(size, 0.2f, size));
                        if (isContinueUpper) {
                            if ((lowerIndex + 1) < cellCoordsLower.Count && math.all(cellCoordsLower[lowerIndex + 1] == cellCoordsUpper[upperIndex])) {
                                lowerIndex++;
                                Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoordsLower[lowerIndex]) + upShift, new float3(size-0.3f, 0.2f, size-0.3f));
                            }
                        }
                        lowerIndex++; // TODO: Put in if part
                    }
                    // Upper work:
                    if (isContinueUpper) {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoordsUpper[upperIndex]) + upShift, new float3(size, 0.2f, size));
                        if (isContinueLower) {
                            if ((upperIndex + 1) < cellCoordsUpper.Count && math.all(cellCoordsUpper[upperIndex + 1] == cellCoordsLower[lowerIndex - 1])) {
                                upperIndex++;
                                Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoordsUpper[upperIndex]) + upShift, new float3(size-0.3f, 0.2f, size-0.3f));
                            }
                        }
                        upperIndex++;
                    }
                }
            }
            
            /* Gizmos.color = Color.blue;
            for (int i = 0; i < cellCoordsUpper.Count; i++) {
                Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoordsUpper[i]) + new float3(0,0.2f,0), new float3(cellSize, 0.2f, cellSize));
            }
            Gizmos.color = Color.red;
            for (int i = 0; i < cellCoordsLower.Count; i++) {
                Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoordsLower[i]) + new float3(0,0.2f,0), new float3(cellSize-0.5f, 0.2f, cellSize-0.5f));
            } */

            
            /* List<int2> cellCoords = buildingGrid.RasterEdge(lineStart, lineEnd, true);
            // print("Count: " + cellCoords.Count);
            for (int i = 0; i < cellCoords.Count; i++) {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoords[i]) + new float3(0,0.2f,0), new float3(buildingGrid.cellSize, 0.2f, buildingGrid.cellSize));
            } */

            /* List<int2> cellCoords2 = buildingGrid.RasterRayOld(lineStart, lineEnd);
            // print("Count2: " + cellCoords2.Count);
            for (int i = 0; i < cellCoords2.Count; i++) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(buildingGrid.CellCoordsToWorld(cellCoords2[i]) + new float3(0,0.2f,0), new float3(buildingGrid.cellSize-0.5f, 0.2f, buildingGrid.cellSize-0.5f));
            } */
        }
    }
}


public enum EntityType
{
    // items of the enum
    Polygon,
    Vertex,
}

public struct TestEntity
{
    public ICollider collider;
    public GameObject obj;
    public EntityType type;

    public TestEntity(EntityType type, ICollider collider, GameObject obj) {
        this.type = type;
        this.collider = collider;
        this.obj = obj;
    }
}

public class QuadCreator
{
    public QuadCreator(GameObject gameObject, Polygon polygon, Material polygonMaterial, int isFrontFace = 1)
    {
        
        MeshRenderer meshRenderer;
        if (!gameObject.TryGetComponent<MeshRenderer>(out meshRenderer)) {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        // meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
        meshRenderer.sharedMaterial = polygonMaterial;

        MeshFilter meshFilter;
        if (!gameObject.TryGetComponent<MeshFilter>(out meshFilter)) {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        Mesh mesh = meshFilter.mesh;

        Vector3[] existingVertices = mesh.vertices;
        Vector3[] addVertices = polygon.GetFaceVertices(isFrontFace);
        Vector3[] vertices = existingVertices.Concat(addVertices);
        int numVertices = vertices.Length;

        int[] existingTriIndices = mesh.triangles;
        int[] addTriIndices = polygon.GetFaceTriIndicesFan(isFrontFace);
        int[] triIndices = existingTriIndices.Concat(addTriIndices);
        for (int i = existingTriIndices.Length; i < existingTriIndices.Length + addTriIndices.Length; i++) {
            triIndices[i] = triIndices[i] + existingVertices.Length;
        }

        // Vector3[] existingNormals = mesh.normals;
        // Vector3[] addNormals = polygon.GetFaceVertices(isFrontFace);
        // Vector3[] normals = existingVertices.Concat(addVertices);
        Vector3[] normals = new Vector3[numVertices].Populate(-polygon.normal * (isFrontFace*2 - 1));

        float2[] existingUVs = mesh.uv.ConvertToFloat2Array();
        float2[] addUVs = polygon.GetVertexUVPositions();
        float2[] uvs = existingUVs.Concat(addUVs);

        // Color[] colors = new Color[numVertices];
        // for (int i = 0; i < numVertices; i++) {
        //     colors[i] = Color.Lerp(Color.red, Color.green, vertices[i].y);
        // }

        // new float2[5] 
        // {
        //     new float2(0, 0),
        //     new float2(1, 0),
        //     new float2(0, 1),
        //     new float2(1, 1),
        //     new float2(0.5f, 0.5f)
        // };

        mesh.vertices = vertices;
        mesh.triangles = triIndices;
        mesh.normals = normals;
        mesh.uv = uvs.ConvertToVector2Array(); // .SubArray(0, vertices.Length)
        // mesh.colors = colors;

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