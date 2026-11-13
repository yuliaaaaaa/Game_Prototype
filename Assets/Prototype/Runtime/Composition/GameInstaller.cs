using UnityEngine;
using Zenject;

namespace ClearPath
{
    public sealed class GameInstaller : MonoInstaller
    {
        [SerializeField] LevelConfigurator configurator;
        [SerializeField] GameHud hud;
        public void Configure(LevelConfigurator world, GameHud gameHud)
        { configurator = world; hud = gameHud; }
        public override void InstallBindings()
        {
            if (!configurator || !configurator.Catalog)
                throw new System.InvalidOperationException("Assign the Level Catalog on the Level Configurator.");

            Container.BindInstance(configurator.Catalog);
            Container.BindInstance(configurator); Container.BindInstance(hud);
            Container.Bind<IChargeInput>().To<PointerInput>().AsSingle();
            Container.Bind<ResourceBudget>().AsSingle();
            Container.Bind<LevelState>().AsSingle();
            Container.Bind<SessionPresentation>().AsSingle();
            Container.Bind<CorridorSolver>().AsSingle(); Container.Bind<InfectionSolver>().AsSingle();
            Container.Bind<LevelBudgetPlanner>().AsSingle(); Container.Bind<LevelGenerator>().AsSingle();
            Container.Bind<SessionStateMachine>().AsSingle(); Container.Bind<PlayerMovement>().AsSingle();
            Container.Bind<AimPreview>().AsSingle();
            Container.Bind<ProjectileSystem>().AsSingle(); Container.Bind<InfectionSystem>().AsSingle();
            var poolRoot = new GameObject("Visual Pools").transform; poolRoot.SetParent(transform, false);
            var palette = configurator.Palette;
            Container.Bind<AudioVolumeSettings>().FromMethod(_ => new AudioVolumeSettings(palette.MusicOutput.audioMixer)).AsSingle();
            Container.Bind<GameAudio>().FromMethod(context => new GameAudio(poolRoot, palette,
                context.Container.Resolve<AudioVolumeSettings>())).AsSingle();
            Container.BindMemoryPool<ShotVisual, ShotVisual.Pool>().WithInitialSize(2)
                .FromMethod(_ => new ShotVisual(poolRoot, configurator.Palette.Charge, configurator.Palette.ShotTrail));
            Container.BindMemoryPool<PulseVisual, PulseVisual.Pool>().WithInitialSize(2)
                .FromMethod(_ => new PulseVisual(poolRoot, configurator.Palette.ShotTrail));
            Container.BindInterfacesAndSelfTo<GameSession>().AsSingle();
        }
    }
}
