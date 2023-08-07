using System;
using Game.Db.BossFight;
using Game.Db.BossFight.Impls;
using SimpleUi.Abstracts;
using Ui.Views.BossFight;
using UniRx;
using UnityEngine;

namespace Ui.Controllers.BossFight.Impls
{
    public class BossFightController : UiController<BossFightView>, IBossFightController 
    {
        private readonly BossFightContext _bossFightContext;
        private readonly IBossSpriteDatabase _bossSpriteDatabase;
        private readonly BossFightPlatformsDatabase _bossFightPlatformsDatabase;
        private readonly IBossFightAnimationController _bossFightAnimationController;

        private BossFightEntity _bossFightEntity;
        private CompositeDisposable _disposable;
        private Canvas _canvas;

        public override int Order => 7;

        public BossFightController
        (
            BossFightContext bossFightContext,
            IBossSpriteDatabase bossSpriteDatabase,
            BossFightPlatformsDatabase bossFightPlatformsDatabase,
            IBossFightAnimationController bossFightAnimationController
        )
        {
            _bossFightContext = bossFightContext;
            _bossSpriteDatabase = bossSpriteDatabase;
            _bossFightPlatformsDatabase = bossFightPlatformsDatabase;
            _bossFightAnimationController = bossFightAnimationController;
        }

        public void Init()
        {
            _disposable = new CompositeDisposable();
            _bossFightEntity = _bossFightContext.BossIdEntity;
            View.Link(_bossFightEntity, _bossFightContext);
            View.SetBossParameters(_bossFightEntity, _bossSpriteDatabase.GetSprite(_bossFightEntity.BossId.Value),
                _bossFightPlatformsDatabase.BattlePlatformSprite);
            _bossFightAnimationController.InitController();
            _bossFightAnimationController.PlayIntroAnimation();
            Observable.Timer(TimeSpan.FromSeconds(1)).Repeat().Subscribe(_ => TimerTick()).AddTo(_disposable);
            UpdateTimer();
        }

        public void Cleanup()
        {
            View.Unlink();
            _disposable?.Dispose();
        }

        public void ResetCamera() => View.ResetCamera();

        private void TimerTick()
        {
            if (!_bossFightAnimationController.IntroAnimationIsFinished || _bossFightEntity.BossHealth.Current <= 0)
                return;
            var time = _bossFightEntity.BossTimeSeconds.Value;
            time--;
            _bossFightEntity.ReplaceBossTimeSeconds(time);
            if (time <= 0)
            {
                time = 0;
                _bossFightEntity.ReplaceBossTimeSeconds(time);
                UpdateTimer();
                return;
            }

            UpdateTimer();
        }

        private void UpdateTimer()
            => View.TimerText.text = TimeSpan.FromSeconds(_bossFightEntity.BossTimeSeconds.Value).ToString("mm':'ss");
    }
}