using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct SoldierSpawnPoints : IComponentData
{
    public NativeArray<float3> Value;
}
