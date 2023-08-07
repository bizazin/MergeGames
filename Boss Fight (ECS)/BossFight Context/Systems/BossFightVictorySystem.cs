using System.Collections.Generic;
using Acme.InstallerGenerator;
using DG.Tweening;
using Game.Sound.Interfaces;
using JCMG.EntitasRedux;
using SimpleUi.Signals;
using Ui.Controllers;
using Ui.Controllers.BossFight;
using Ui.Controllers.BossFight.Impls;
using Ui.Windows;
using Websockets;
using Zenject;

namespace Ecs.BossFight.System
{
    [Install(ExecutionType.Game, ExecutionPriority.Normal)]
    public class BossFightVictorySystem : ReactiveSystem<BossFightEntity>
    {
        private readonly BossFightContext _bossFightContext;
        private readonly BossFightController _bossFightController;
        private readonly SignalBus _signalBus;
        private readonly ISoundsPoolManager _soundsPoolManager;
        private readonly IBossVictoryController _bossVictoryController;
        private readonly IWebSockets _webSockets;
        private readonly IBossFightAnimationController _bossFightAnimationController;
        private readonly IBossHangarsAnimationController _bossHangarsAnimationController;

        public BossFightVictorySystem
        (
            BossFightContext bossFightContext,
            BossFightController bossFightController,
            SignalBus signalBus,
            ISoundsPoolManager soundsPoolManager,
            IBossVictoryController bossVictoryController,
            IWebSockets webSockets,
            IBossFightAnimationController bossFightAnimationController,
            IBossHangarsAnimationController bossHangarsAnimationController
        ) : base(bossFightContext)
        {
            _bossFightContext = bossFightContext;
            _bossFightController = bossFightController;
            _signalBus = signalBus;
            _soundsPoolManager = soundsPoolManager;
            _bossVictoryController = bossVictoryController;
            _webSockets = webSockets;
            _bossFightAnimationController = bossFightAnimationController;
            _bossHangarsAnimationController = bossHangarsAnimationController;
        }

        protected override ICollector<BossFightEntity> GetTrigger(IContext<BossFightEntity> context)
            => context.CreateCollector(BossFightMatcher.BossHealth.Added());

        protected override bool Filter(BossFightEntity entity)
            => !entity.IsFinished && !entity.IsDestroyed && entity.HasBossHealth && entity.BossHealth.Current <= 0 &&
               entity.HasBossId;

        protected override void Execute(List<BossFightEntity> entities)
        {
            _bossFightContext.BossIdEntity.IsFinished = true;
            _webSockets.AdventureMapLevelUp();
            _bossFightAnimationController.BossDeathAnimation().OnComplete(() =>
            {
                _bossFightController.Cleanup();
                _bossHangarsAnimationController.DisableAllAttackParticles();
                _soundsPoolManager.PlaySound("BossVictorySound");
                _soundsPoolManager.StopMusic("BossSound");
                _signalBus.Fire(new SignalWindowOpen(typeof(BossVictoryWindow)));
                _bossVictoryController.SetReward();
            });
        }
    }
}