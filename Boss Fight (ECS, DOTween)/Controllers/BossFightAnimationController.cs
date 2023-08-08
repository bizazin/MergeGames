using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Db.UnitSprites;
using Game.Sound.Interfaces;
using Ui.Controllers.BossFight;
using Ui.Controllers.Hangars;
using Ui.Views.BossFight;
using UnityEngine;

namespace Ui.Controllers.Impls
{
    public class BossFightAnimationController : IBossFightAnimationController
    {
        private readonly HangarsController _hangarsController;
        private readonly IBossHangarsAnimationController _bossHangarsAnimationController;
        private readonly ISoundsPoolManager _soundsPoolManager;
        private readonly BossFightView _bossFightView;
        private readonly IBossFightAnimationDatabase _bossFightAnimationDatabase;
        private Transform _defaultBossTransform;

        public bool IntroAnimationIsFinished { get; private set; }

        public BossFightAnimationController
        (
            HangarsController hangarsController,
            IBossHangarsAnimationController bossHangarsAnimationController,
            ISoundsPoolManager soundsPoolManager,
            BossFightView bossFightView,
            IBossFightAnimationDatabase bossFightAnimationDatabase
        )
        {
            _hangarsController = hangarsController;
            _bossHangarsAnimationController = bossHangarsAnimationController;
            _soundsPoolManager = soundsPoolManager;
            _bossFightView = bossFightView;
            _bossFightAnimationDatabase = bossFightAnimationDatabase;
        }

        public void InitController()
        {
            IntroAnimationIsFinished = false;
            _defaultBossTransform = _bossFightView.DefaultBossPosition;
        }

        public void PlayIntroAnimation()
        {
	        var mainCamera = Camera.main;
	        if (mainCamera == null)
		        throw new Exception($"[BossFightAnimationController] Main camera is null");
	        var cameraTransform = mainCamera.transform;
            var canvasPos = _bossFightView.Canvas.transform.position;
            var cameraDist = (float)(-canvasPos.y / Math.Tan(mainCamera.fieldOfView * Math.PI / 360));
            _bossFightView.SetupCamera(cameraDist);
            SetupBossPosition();

            var defaultBossPosition = _defaultBossTransform.position;
            DOTween.Sequence()
                //Camera approach
                .Append(cameraTransform
                    .DOMove(
                        new Vector3(defaultBossPosition.x,
                            defaultBossPosition.y - _bossFightAnimationDatabase.DeltaCameraScaleY,
                            _bossFightAnimationDatabase.ScaleCameraPosZ),
                        _bossFightAnimationDatabase.MoveCameraDuration)
                    .SetEase(Ease.InOutQuart))
                .Join(_bossFightView.DarkBackground.DOFade(_bossFightAnimationDatabase.BackgroundBlackout,
                    _bossFightAnimationDatabase.MoveCameraDuration))
                .Join(_hangarsController.FadeHangars(_bossFightAnimationDatabase.HangarsFadeColor,
                    _bossFightAnimationDatabase.MoveCameraDuration))
                .Join(_bossFightView.BossText.transform.DOMoveX(canvasPos.x, _bossFightAnimationDatabase.MoveCameraDuration))
                .Join(_bossFightView.BossNameText.transform.DOMoveX(canvasPos.x, _bossFightAnimationDatabase.MoveCameraDuration))

                //Delay between approach and distance
                .Append(cameraTransform.DOShakePosition(_bossFightAnimationDatabase.ShakeDuration,
                    _bossFightAnimationDatabase.ShakeStrength))
                .AppendInterval(_bossFightAnimationDatabase.DelayBetweenCameraApproachAndDistance)

                //Camera distance
                .Append(cameraTransform
                    .DOMove(new Vector3(canvasPos.x, canvasPos.y, cameraDist),
                        _bossFightAnimationDatabase.MoveCameraDuration)
                    .SetEase(Ease.InOutQuart))
                .Join(_bossFightView.DarkBackground.DOFade(_bossFightAnimationDatabase.DarkBackgroundFadeDepth,
                    _bossFightAnimationDatabase.MoveCameraDuration))
                .Join(_hangarsController.FadeHangars(Color.white, _bossFightAnimationDatabase.MoveCameraDuration))
                .Join(_bossFightView.BossText.transform.DOMoveX(canvasPos.x + _bossFightAnimationDatabase.DeltaBossTextShift,
                    _bossFightAnimationDatabase.MoveCameraDuration))
                .Join(_bossFightView.BossNameText.transform.DOMoveX(canvasPos.x - _bossFightAnimationDatabase.DeltaBossTextShift,
                    _bossFightAnimationDatabase.MoveCameraDuration))
                .Append(_bossFightView.MergeToWinText.DOFade(1, _bossFightAnimationDatabase.MergeToWinTextFadingDuration))
                .AppendCallback(() => _bossFightView.ResetCameras())
                .AppendInterval(_bossFightAnimationDatabase.MergeToWinTextInterval)
                .Append(_bossFightView.MergeToWinText.DOFade(0, _bossFightAnimationDatabase.MergeToWinTextFadingDuration))
                .AppendCallback(() => IntroAnimationIsFinished = true);
        }
        
		public Sequence KickBossAnimation(int unitIndex, float delayBetweenTicks, float kickFrequency) 
		{
			var bossImage = _bossFightView.BossImage;
			_hangarsController.SetCurrentBossFightUnit(unitIndex);
			var sequence = DOTween.Sequence();
			sequence
				.Append(_bossHangarsAnimationController.JumpUnit(_bossFightAnimationDatabase.UnitJumpHeight, _bossFightAnimationDatabase.UnitJumpingDuration * kickFrequency))
				.AppendCallback(() => _bossHangarsAnimationController.ClawAppear(bossImage.transform.position))
				.AppendCallback(() => _soundsPoolManager.PlayRandomSound(new List<string>
					{ "BossTakingDamageSound1", "BossTakingDamageSound2" }))
				.Join(bossImage.DOColor(Color.red, _bossFightAnimationDatabase.BossReddingDuration * kickFrequency))
				.Join(DoBossShakePosition(bossImage.transform, kickFrequency))
				.AppendCallback(() => _bossHangarsAnimationController.StopParticles())
				.Append(bossImage.DOColor(Color.white, _bossFightAnimationDatabase.BossReddingDuration * kickFrequency))
				.Join(_bossHangarsAnimationController.ResetUnitPos(_bossFightAnimationDatabase.UnitJumpingDuration * kickFrequency));


			if (sequence.Duration() > delayBetweenTicks * kickFrequency)
				throw new Exception(
					$"[{typeof(BossFightAnimationController)}] Unit hit duration cannot exceed delay between ticks!");
			return sequence;
		}

		public Sequence BossDeathAnimation()
		{
			var bossImageTransform = _bossFightView.BossImage.transform;
			var position = bossImageTransform.position;

			return DOTween.Sequence()
			              .AppendInterval(_bossFightAnimationDatabase.BossDeathInterval)
			              .Append(bossImageTransform.DOMoveY(position.y + _bossFightAnimationDatabase.BossDeathFallHeight, _bossFightAnimationDatabase.BossDeathRiseDuration)
			                                        .SetEase(Ease.OutCubic))
			              .Append(bossImageTransform.DOMoveY(position.y - _bossFightAnimationDatabase.BossDeathFallDepth, _bossFightAnimationDatabase.BossDeathFallDuration)
			                                        .SetEase(Ease.InQuart));
		}

		private void SetupBossPosition() =>
			_bossFightView.BossImage.transform.position = _defaultBossTransform.position;

		private Tweener DoBossShakePosition(Transform bossPosition, float kickFrequency) => 
			bossPosition.position == _defaultBossTransform.position
				? bossPosition.DOShakePosition(_bossFightAnimationDatabase.BossShakingDuration * kickFrequency, _bossFightAnimationDatabase.BossShakingStrength)
				: bossPosition.DOMove(_defaultBossTransform.position, 0);
    }
}