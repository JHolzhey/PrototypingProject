using Unity.Entities;
using Unity.Mathematics;

public struct WorldProperties : IComponentData
{
    public float2 dimensions;
    public int numSoldiersToSpawn;
    public Entity soldierPrefab;
    public Entity soldierFunPrefab;
    public float soldierFunSpawnRate;
}

public struct SoldierFunSpawnTimer : IComponentData
{
    public float Value;
}
