using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SpawnSoldierSystem : ISystem // ISystem is best but SystemBase can be used for managed data components
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WorldProperties>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;
        Entity worldEntity = SystemAPI.GetSingletonEntity<WorldProperties>();
        WorldAspect world = SystemAPI.GetAspectRW<WorldAspect>(worldEntity);

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        NativeList<float3> spawnPoints = new NativeList<float3>(Allocator.Temp);
        
        for (int i = 0; i < world.numSoldiersToSpawn; i++) {
            Entity newSoldier = ecb.Instantiate(world.soldierPrefab);
            LocalTransform newSoldierTransform = world.GetRandomSoldierTransform();
            // ecb.SetComponent(newSoldier, new LocalToWorld{ Value = newSoldierTransform.ToMatrix() });
            ecb.SetComponent(newSoldier, newSoldierTransform);

            float3 newSpawnPoint = newSoldierTransform.Position + new float3(1f, 0, 0);
            spawnPoints.Add(newSpawnPoint);
        }

        world.SoldierFunSpawnPoints = spawnPoints.ToArray(Allocator.Persistent);

        ecb.Playback(state.EntityManager);
    }
}
