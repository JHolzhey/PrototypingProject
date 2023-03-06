using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public readonly partial struct SoldierFunAspect : IAspect // (https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/aspects-concepts.html)
{
    public readonly Entity entity;
    private readonly TransformAspect _transformAspect;
    private readonly RefRO<WorldProperties> _worldProperties;
    private readonly RefRW<RandomTest> _worldRandom;
    private readonly RefRW<SoldierSpawnPoints> _soldierSpawnPoints;
    private readonly RefRW<SoldierFunSpawnTimer> _soldierFunSpawnTimer;

    public int numSoldiersToSpawn => _worldProperties.ValueRO.numSoldiersToSpawn;
    public Entity soldierPrefab => _worldProperties.ValueRO.soldierPrefab;

    public NativeArray<float3> SoldierFunSpawnPoints
    {
        get => _soldierSpawnPoints.ValueRO.Value;
        set => _soldierSpawnPoints.ValueRW.Value = value;
    }

    public LocalTransform GetRandomSoldierTransform() {
        return new LocalTransform
        {
            Position = GetRandomPosition(),
            Rotation = GetRandomRotation(),
            Scale = GetRandomScale(0.5f),
        };
    }

    float3 GetRandomPosition()
    {
        float3 randomPosition;

        do {
            randomPosition = _worldRandom.ValueRW.Value.NextFloat3(MinCorner, MaxCorner);
        } while (math.distancesq(_transformAspect.LocalPosition, randomPosition) <= SAFETY_RADIUS_SQ);
        
        randomPosition.y = UnityEngine.Terrain.activeTerrain.SampleHeight(randomPosition);
        return randomPosition;
    }

    float3 MinCorner => _transformAspect.LocalPosition - HalfDimensions;
    float3 MaxCorner => _transformAspect.LocalPosition + HalfDimensions;
    float3 HalfDimensions => new()
    {
        x = _worldProperties.ValueRO.dimensions.x * 0.5f,
        y = 0f,
        z = _worldProperties.ValueRO.dimensions.x * 0.5f,
    };

    const float SAFETY_RADIUS_SQ = 100;

    quaternion GetRandomRotation() => quaternion.RotateY(_worldRandom.ValueRW.Value.NextFloat(-1f, 1f));
    float GetRandomScale(float min) => _worldRandom.ValueRW.Value.NextFloat(min, 1f);
    

    public float SoldierFunSpawnTimer
    {
        get => _soldierFunSpawnTimer.ValueRO.Value;
        set => _soldierFunSpawnTimer.ValueRW.Value = value;
    }

    public bool timeToSpawnSoldierFun => SoldierFunSpawnTimer <= 0f;
    public float soldierFunSpawnRate => _worldProperties.ValueRO.soldierFunSpawnRate;
    public Entity soldierFunPrefab => _worldProperties.ValueRO.soldierFunPrefab;

    public LocalTransform GetSoldierFunSpawnPoint()
    {
        float3 position = GetRandomZombieSpawnPoint();
        return new LocalTransform
        {
            Position = position,
            Rotation = MathLib.CalcHeadingRotation(position, _transformAspect.WorldPosition),
            Scale = 1f,
        };
    }

    private float3 GetRandomZombieSpawnPoint()
    {
        return SoldierFunSpawnPoints[_worldRandom.ValueRW.Value.NextInt(SoldierFunSpawnPoints.Length)];
    }
}
