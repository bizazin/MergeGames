using System.Collections.Generic;
using System.Linq;
using Acme.InstallerGenerator;
using Ecs.Skills.Models;
using Ecs.Skills.Services;
using Ecs.Skills.Services.Strategies;
using JCMG.EntitasRedux;

namespace Ecs.BossFight.System
{
    [Install(ExecutionType.Game, ExecutionPriority.Normal)]
    public class BossFightFinishedSystem : ReactiveSystem<BossFightEntity>
    {
        private readonly AutoMergeStrategy _autoMergeStrategy;

        public BossFightFinishedSystem
        (
            BossFightContext bossFightContext,
            List<ISkillStrategy> skillStrategies
        ) : base(bossFightContext) =>
            _autoMergeStrategy =
                skillStrategies.First(skill => skill.Strategy == ESkillStrategy.AutoMerge) as AutoMergeStrategy;


        protected override ICollector<BossFightEntity> GetTrigger(IContext<BossFightEntity> context)
            => context.CreateCollector(BossFightMatcher.Finished.Added());

        protected override bool Filter(BossFightEntity entity)
            => entity.IsFinished && !entity.IsDestroyed;

        protected override void Execute(List<BossFightEntity> entities) => _autoMergeStrategy.StopAutoMerge();
    }
}