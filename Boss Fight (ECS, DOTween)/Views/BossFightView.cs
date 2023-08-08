using System;
using Ecs.Utils.View.Impls;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Ui.Views.BossFight
{
    public class BossFightView : LinkedUiView<BossFightEntity>, IBossHealthAddedListener,
        IDamagePerSecondAddedListener
    {
        [SerializeField] private Text damageAmountText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text bossText;
        [SerializeField] private Text bossNameText;
        [SerializeField] private Text mergeToWinText;
        [SerializeField] private Image bossImage;
        [SerializeField] private Transform defaultBossPosition;
        [SerializeField] private Image bossPlatformImage;
        [SerializeField] private Image darkBackground;

        [Header("HealthBar")] 
        [SerializeField] private Slider bossHealthBar;
        [SerializeField] private Text currentBossHealthText;

        private (float, Vector3) _defaultCameraFarAndPos;
        
        public Text TimerText => timerText;
        public Text BossText => bossText;
        public Text BossNameText => bossNameText;
        public Text MergeToWinText => mergeToWinText;
        public Image BossImage => bossImage;
        public Transform DefaultBossPosition => defaultBossPosition;
        public Image DarkBackground => darkBackground;
        public Canvas Canvas { get; private set; }

        [Inject]
        private void Construct
        (
            CanvasScaler canvasScaler
        ) 
        {
            Canvas = canvasScaler.GetComponent<Canvas>();
        }
        
        public void SetBossParameters(BossFightEntity bossFightEntity, Sprite bossImageSprite, Sprite bossPlatform)
        {
            if (bossFightEntity.HasBossName)
                bossNameText.text = bossFightEntity.BossName.Value;
            bossImage.sprite = bossImageSprite;
            bossPlatformImage.sprite = bossPlatform;
        }

        public void OnBossHealthAdded(BossFightEntity entity, int current, int max)
        {
            if (current < 0)
                current = 0;
            currentBossHealthText.text = $"{current}k / {max}k";
            bossHealthBar.value = (float)current / max;
        }

        public void OnDamagePerSecondAdded(BossFightEntity entity, int value) => 
            damageAmountText.text = $"{value}k/s";

        public void ResetCamera() => 
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        public void SetupCamera(float cameraDist)
        {
            Canvas.renderMode = RenderMode.WorldSpace;
            var mainCamera = Camera.main;
            if (mainCamera == null)
                throw new Exception($"[BossFightView] Main camera is null");
            Canvas.worldCamera = mainCamera;
            _defaultCameraFarAndPos.Item1 = mainCamera.farClipPlane;
            var mainCameraTransform = mainCamera.transform;
            _defaultCameraFarAndPos.Item2 = mainCameraTransform.position;

            var position = Canvas.transform.position;
            mainCameraTransform.position = new Vector3(position.x, position.y, cameraDist);
            mainCamera.farClipPlane = Math.Abs(cameraDist) + 1;
            mainCamera.orthographic = false;
        }

        public void ResetCameras()
        {
            Canvas.renderMode = RenderMode.ScreenSpaceCamera;

            var mainCamera = Camera.main;
            if (mainCamera == null)
                throw new Exception($"[BossFightView] Main camera is null");

            mainCamera.farClipPlane = _defaultCameraFarAndPos.Item1;
            mainCamera.transform.position = _defaultCameraFarAndPos.Item2;
            mainCamera.orthographic = true;
        }

        protected override void Listen(BossFightEntity entity)
        {
            entity.AddBossHealthAddedListener(this);
            entity.AddDamagePerSecondAddedListener(this);
            
            if (entity.HasDamagePerSecond)
                OnDamagePerSecondAdded(entity, entity.DamagePerSecond.Value);
            if (entity.HasBossHealth)
                OnBossHealthAdded(entity, entity.BossHealth.Current, entity.BossHealth.Max);
        }

        protected override void Unlisten(BossFightEntity entity)
        {
            entity.RemoveBossHealthAddedListener(this);
            entity.RemoveDamagePerSecondAddedListener(this);
        }

        protected override void InternalClear()
        {
        }
    }
}