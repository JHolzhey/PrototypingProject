using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public readonly partial struct WorldAspect : IAspect // (https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/aspects-concepts.html)
{
    public readonly Entity entity;
    private readonly TransformAspect _transformAspect;
    private readonly RefRO<WorldProperties> _worldProperties;
    private readonly RefRW<RandomTest> _worldRandom;

    public int numSoldiersToSpawn => _worldProperties.ValueRO.numSoldiersToSpawn;
    public Entity soldierPrefab => _worldProperties.ValueRO.soldierPrefab;

    public LocalTransform GetRandomSoldierTransform() {
        return new LocalTransform
        {
            Position = GetRandomPosition(),
            Rotation = quaternion.identity,
            Scale = 1,
        };
    }

    float3 GetRandomPosition()
    {
        float3 randomPosition;

        randomPosition = _worldRandom.ValueRW.Value.NextFloat3(MinCorner, MaxCorner);

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
    
}
