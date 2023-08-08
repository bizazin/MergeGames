using Game.Services.BossFight;
using SimpleUi.Abstracts;
using UniRx;
using Zenject;

namespace Ui.Controllers.BossFight.Impls
{
    public class BossDefeatController : UiController<BossDefeatView>, IInitializable
    {
        private readonly IBossFightService _bossFightService;
        public override int Order => 4;

        public BossDefeatController
        (
            IBossFightService bossFightService
        ) 
        {
            _bossFightService = bossFightService;
        }

        public void Initialize() => 
            View.RepeatButton.OnClickAsObservable().Subscribe(_ => OnRepeatButton()).AddTo(View);

        private void OnRepeatButton() =>
            _bossFightService.RestartBossFight();
    }
}