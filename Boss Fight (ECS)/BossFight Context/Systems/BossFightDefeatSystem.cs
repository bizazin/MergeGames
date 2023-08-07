using System.Collections.Generic;
using Acme.InstallerGenerator;
using Game.Services.BossFight;
using Game.Sound.Interfaces;
using JCMG.EntitasRedux;
using SimpleUi.Signals;
using Ui.Controllers;
using Zenject;

namespace Ecs.BossFight.System
{
    [Install(ExecutionType.Game, ExecutionPriority.Normal)]
    public class BossFightDefeatSystem : ReactiveSystem<BossFightEntity>
    {
        private readonly BossFightContext _bossFightContext;
        private readonly SignalBus _signalBus;
        private readonly ISoundsPoolManager _soundsPoolManager;
        private readonly IBossFightService _bossFightService;
        private readonly IBossHangarsAnimationController _bossHangarsAnimationController;

        public BossFightDefeatSystem
        (
            BossFightContext bossFightContext,
            SignalBus signalBus,
            ISoundsPoolManager soundsPoolManager,
            IBossFightService bossFightService,
            IBossHangarsAnimationController bossHangarsAnimationController
        ) : base(bossFightContext)
        {
            _bossFightContext = bossFightContext;
            _signalBus = signalBus;
            _soundsPoolManager = soundsPoolManager;
            _bossFightService = bossFightService;
            _bossHangarsAnimationController = bossHangarsAnimationController;
        }

        protected override ICollector<BossFightEntity> GetTrigger(IContext<BossFightEntity> context)
            => context.CreateCollector(BossFightMatcher.BossTimeSeconds.Added());

        protected override bool Filter(BossFightEntity entity)
            => !entity.IsFinished && !entity.IsDestroyed && entity.HasBossTimeSeconds && entity.BossTimeSeconds.Value <= 0;

        protected override void Execute(List<BossFightEntity> entities)
        {
            _bossFightContext.BossIdEntity.IsFinished = true;
            _bossHangarsAnimationController.DisableAllAttackParticles();
            _soundsPoolManager.PlaySound("BossDefeatSound");
            _soundsPoolManager.StopMusic("BossSound");
            _signalBus.Fire(new SignalWindowOpen(typeof(BossDefeatWindow)));
            _bossFightService.ResetHangars();
        }
    }
}