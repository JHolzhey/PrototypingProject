using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class WorldAuthoring : MonoBehaviour
{
    public float2 dimensions;
    public int numSoldiersToSpawn;
    public uint randomSeed = 10;
    public GameObject soldierPrefab;
    public GameObject soldierFunPrefab;
    public float soldierFunSpawnRate;

    public class WorldBaker : Baker<WorldAuthoring>
    {
        public override void Bake(WorldAuthoring authoring)
        {
            AddComponent(new WorldProperties
            {
                dimensions = authoring.dimensions,
                numSoldiersToSpawn = authoring.numSoldiersToSpawn,
                soldierPrefab = GetEntity(authoring.soldierPrefab),
                soldierFunPrefab = GetEntity(authoring.soldierFunPrefab),
                soldierFunSpawnRate = authoring.soldierFunSpawnRate,
            });
            AddComponent(new RandomTest
            {
                Value = Unity.Mathematics.Random.CreateFromIndex(authoring.randomSeed),
            });
            AddComponent<SoldierSpawnPoints>();
            AddComponent<SoldierFunSpawnTimer>();
        }
    }
}