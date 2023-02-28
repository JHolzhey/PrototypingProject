using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class WorldMono : MonoBehaviour
{
    public float2 dimensions;
    public int numSoldiersToSpawn;
    public GameObject soldierPrefab;
    public uint randomSeed = 10;
}

public class WorldBaker : Baker<WorldMono>
{
    public override void Bake(WorldMono authoring)
    {
        AddComponent(new WorldProperties
        {
            dimensions = authoring.dimensions,
            numSoldiersToSpawn = authoring.numSoldiersToSpawn,
            soldierPrefab = GetEntity(authoring.soldierPrefab),
        });
        AddComponent(new RandomTest
        {
            Value = Unity.Mathematics.Random.CreateFromIndex(authoring.randomSeed),
        });
    }
}