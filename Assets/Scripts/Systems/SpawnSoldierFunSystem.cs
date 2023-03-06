using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct SpawnSoldierFunSystem : ISystem // ISystem is best but SystemBase can be used for managed data components
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float DeltaTime = SystemAPI.Time.DeltaTime; // TODO
        var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();

        new SpawnSoldierFunJob
        {
            deltaTime = DeltaTime,
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
        }.Run();
    }

    [BurstCompile]
    public partial struct SpawnSoldierFunJob : IJobEntity
    {
        public float deltaTime;
        public EntityCommandBuffer ECB;

        [BurstCompile]
        private void Execute(WorldAspect world)
        {
            world.SoldierFunSpawnTimer -= deltaTime;
            if (!world.timeToSpawnSoldierFun) return;
            if (world.SoldierFunSpawnPoints.Length == 0) return;
            
            world.SoldierFunSpawnTimer = world.soldierFunSpawnRate;

            Entity newSoldierFun = ECB.Instantiate(world.soldierFunPrefab);

            LocalTransform newSoldierFunTransform = world.GetSoldierFunSpawnPoint();
            ECB.SetComponent(newSoldierFun, newSoldierFunTransform);
        }
    }
}
