using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ObstacleCollisionSystem))]
public class PheromoneDropSystem : JobComponentSystem
{
    EntityQuery m_PheromoneMapQuery;
    EntityQuery m_MapQuery;
    EntityQuery m_AntSteeringQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PheromoneMapQuery = GetEntityQuery(ComponentType.ReadWrite<PheromoneBuffer>());
        m_MapQuery = GetEntityQuery(ComponentType.ReadOnly<MapSettingsComponent>());
        m_AntSteeringQuery = GetEntityQuery(ComponentType.ReadOnly<AntSteeringSettingsComponent>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var map = m_MapQuery.GetSingleton<MapSettingsComponent>();
        var pheromoneMap = GetBufferFromEntity<PheromoneBuffer>()[m_PheromoneMapQuery.GetSingletonEntity()];
        var antSteering = m_AntSteeringQuery.GetSingleton<AntSteeringSettingsComponent>();
        new Job
        {
            CarrierExcitement = 1f,
            SearcherExcitement = .3f,
            TrailAdd = map.TrailAdd,
            MaxSpeed = antSteering.MaxSpeed,
            MapSize = map.MapSize,
            Pheromones = pheromoneMap,
        }.Run(this, inputDeps);
        return default;
    }

    struct Job : IJobForEach<PositionComponent, SpeedComponent, HasResourcesComponent>
    {
        public DynamicBuffer<PheromoneBuffer> Pheromones;
        public float SearcherExcitement;
        public float CarrierExcitement;
        public float MaxSpeed;

        public float TrailAdd;

        public int MapSize;

        public void Execute(
            [ReadOnly] ref PositionComponent position,
            [ReadOnly] ref SpeedComponent speed,
            [ReadOnly] ref HasResourcesComponent hasResources)
        {
            var p = (int2)math.floor(position.Value);
            if (math.any(p < 0) || math.any(p >= MapSize))
                return;
            float e = hasResources.Value ? CarrierExcitement : SearcherExcitement;
            float strength = e * speed.Value / MaxSpeed;
            float pheromone = Pheromones[p.y * MapSize + p.x];
            Pheromones[p.y * MapSize + p.x] = math.min(1, pheromone + TrailAdd * strength * (1 - pheromone));
        }
    }
}