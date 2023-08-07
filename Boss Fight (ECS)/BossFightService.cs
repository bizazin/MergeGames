using System;
using System.Collections.Generic;
using System.Linq;
using Core.Interfaces;
using Core.PlayerProfileProgress.Player;
using DG.Tweening;
using Ecs.BossFight.Utils;
using Ecs.Skills.Models;
using Ecs.Skills.Services;
using Ecs.Skills.Services.Strategies;
using Game.Behaviors;
using Game.Db;
using Game.Db.BossFight.Impls;
using Game.Db.BossFightSettings;
using Game.Db.UnitSprites.BossFight;
using Game.Enums;
using Game.Factories.Units;
using Game.Models;
using Game.Models.Hangars;
using Game.Services.Hangars.Impls;
using Game.Services.LevelLocations;
using Game.Services.Profile;
using Game.Signals.Quests.QuestEvents;
using Game.Sound.Interfaces;
using SimpleUi.Signals;
using Ui.Controllers.BossFight;
using Ui.Controllers.BossFight.Impls;
using Ui.Controllers.Hangars;
using Ui.Enums;
using Ui.Signals;
using Ui.Views.Background;
using Ui.Windows;
using UniRx;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;
using Unit = Game.Models.ItemImplementations.Unit.Unit;

namespace Game.Services.BossFight.Impls
{
    public class BossFightService : IBossFightService, IInitializable, IDisposable
    {
        private readonly PlayerProfile _playerProfile;
        private readonly BossFightContext _bossFightContext;
        private readonly IBossFightSettingsDatabase _bossFightSettingsDatabase;
        private readonly SignalBus _signalBus;
        private readonly IPlayerProfileService _playerProfileService;
        private readonly HangarsManager _hangarsManager;
        private readonly BossFightController _bossFightController;
        private readonly IUnitsDamageDatabase _unitsDamageDatabase;
        private readonly HangarsController _hangarsController;
        private readonly IUnitFactory _unitFactory;
        private readonly IItemsDatabase _itemsDatabase;
        private readonly IBossInfoDatabase _bossInfoDatabase;
        private readonly IBossFightAnimationController _bossFightAnimationController;
        private readonly ISoundsPoolManager _soundsPoolManager;
        private readonly ICurrentLocationHolder _currentLocationHolder;
        private readonly CurveTrackBehavior _curveTrackBehavior;
        private readonly ILevelLocationsDatabase _levelLocationsDatabase;
        private readonly IBossFightAnimationDatabase _bossFightAnimationDatabase;
        private readonly BackgroundView _backgroundView;
        private readonly ISettingsService _settingsService;
        private readonly AutoMergeStrategy _autoMergeStrategy;
        private int _currentDamageSum;
        private CompositeDisposable _disposable;

        private BossFightEntity _bossFightEntity;
        private PlayerProfile _bossPlayerProfile;
        private int _unitBoxId;
        private int _battlePlatformCount;
        public bool IsStarted { get; private set; }
        private IDisposable _openBoxDisposable;
        private bool _isAutoMergeActive;

        public BossFightService
        (
            PlayerProfile playerProfile,
            BossFightContext bossFightContext,
            IBossFightSettingsDatabase bossFightSettingsDatabase,
            SignalBus signalBus,
            IPlayerProfileService playerProfileService,
            HangarsManager hangarsManager,
            BossFightController bossFightController,
            IUnitsDamageDatabase unitsDamageDatabase,
            HangarsController hangarsController,
            IUnitFactory unitFactory,
            IItemsDatabase itemsDatabase,
            IBossInfoDatabase bossInfoDatabase,
            IBossFightAnimationController bossFightAnimationController,
            ISoundsPoolManager soundsPoolManager,
            ICurrentLocationHolder currentLocationHolder,
            CurveTrackBehavior curveTrackBehavior,
            ILevelLocationsDatabase levelLocationsDatabase,
            IBossFightAnimationDatabase bossFightAnimationDatabase,
            BackgroundView backgroundView,
            ISettingsService settingsService,
            List<ISkillStrategy> skillStrategies
        )
        {
            _playerProfile = playerProfile;
            _bossFightContext = bossFightContext;
            _bossFightSettingsDatabase = bossFightSettingsDatabase;
            _signalBus = signalBus;
            _playerProfileService = playerProfileService;
            _hangarsManager = hangarsManager;
            _bossFightController = bossFightController;
            _unitsDamageDatabase = unitsDamageDatabase;
            _hangarsController = hangarsController;
            _unitFactory = unitFactory;
            _itemsDatabase = itemsDatabase;
            _bossInfoDatabase = bossInfoDatabase;
            _bossFightAnimationController = bossFightAnimationController;
            _soundsPoolManager = soundsPoolManager;
            _currentLocationHolder = currentLocationHolder;
            _curveTrackBehavior = curveTrackBehavior;
            _levelLocationsDatabase = levelLocationsDatabase;
            _bossFightAnimationDatabase = bossFightAnimationDatabase;
            _backgroundView = backgroundView;
            _settingsService = settingsService;
            _autoMergeStrategy =
                skillStrategies.First(skill => skill.Strategy == ESkillStrategy.AutoMerge) as AutoMergeStrategy;
        }

        public void Initialize() =>
            _battlePlatformCount = _bossFightSettingsDatabase.BossFightSetting.BattlePlatformsCount;

        public void Dispose()
        {
            _bossFightEntity?.Destroy();
            _hangarsController?.Dispose();
            _disposable?.Dispose();
            _openBoxDisposable?.Dispose();
            _bossFightController.Cleanup();
        }

        public void Start()
        {
            _soundsPoolManager.StopMusic("MainSoundtrack");
            _soundsPoolManager.PlayMusic("BossSound");
            _isAutoMergeActive = _settingsService.GetSetting.AutoMerge;
            IsStarted = true;
            _backgroundView.IncomeParticle(false);
            _bossPlayerProfile = _playerProfileService.CreateCurrentPlayerProfile();

            var maxLevel = _levelLocationsDatabase.GetLocationById(_currentLocationHolder.Level.Value + 1).Condition - 1;
            _bossPlayerProfile.MaxUnitLevel = maxLevel;

            _disposable = new CompositeDisposable();
            _signalBus.GetStream<SignalHangarDrag>().Subscribe(OnHangarDrag).AddTo(_disposable);
            _signalBus.GetStream<SignalMergeUnit>().Subscribe(OnMergeUnit).AddTo(_disposable);

            _hangarsController.SetBossFightActive(true);

            for (int i = 0; i < _battlePlatformCount; i++)
                _hangarsManager.Add(_bossPlayerProfile, EHangarType.Battle);

            for (int i = 0; i < _bossFightSettingsDatabase.BossFightSetting.CommonPlatformsCount; i++)
                _hangarsManager.Add(_bossPlayerProfile, EHangarType.BossCommon);

            var maxLevelUnit = _unitFactory.Create(maxLevel);
            _bossPlayerProfile.AddItem(maxLevelUnit);
            _hangarsManager.Occupy(_bossPlayerProfile, 0, maxLevelUnit, HangarItemAddType.Add);

            var bossLevel = _bossInfoDatabase.GetById(_currentLocationHolder.Level.Value);
            var fightDuration = _bossFightSettingsDatabase.BossFightSetting.BossFightDurationTimeS;
            var fightDamage = _unitsDamageDatabase.UnitsDamageDictionary[maxLevel]
                .damagePerSecond;
            _bossFightEntity = _bossFightContext.CreateBossFight(bossLevel, fightDuration, fightDamage);

            var unitMinLevel = maxLevel -
                               _bossFightSettingsDatabase.BossFightSetting.LevelDifference;
            var unitLevel = unitMinLevel <= 0 ? 1 : unitMinLevel;

            _unitBoxId = _itemsDatabase.GetByLevel(unitLevel).Id;
            for (var i = 0; i < _battlePlatformCount; i++)
            {
                var index = i;
                var rnd = Random.Range(_bossFightAnimationDatabase.KickRandom,
                    _bossFightAnimationDatabase.MaxKickFrequency);
                Observable.Timer(TimeSpan.FromSeconds(rnd * _bossFightAnimationDatabase.DelayBetweenTicks))
                    .Repeat()
                    .Subscribe(_ =>
                    {
                        if (!_bossFightAnimationController.IntroAnimationIsFinished ||
                            _bossFightEntity.BossHealth.Current < 0 || _bossFightEntity.BossTimeSeconds.Value <= 0)
                            return;
                        _bossFightAnimationController.KickBossAnimation(index,
                            _bossFightAnimationDatabase.DelayBetweenTicks,
                            Random.Range(_bossFightAnimationDatabase.MinKickFrequency, rnd)).Play();
                    })
                    .AddTo(_disposable);
            }

            for (var i = 0; i < _bossPlayerProfile.Hangars.Count; i++)
            {
                if (i == 0)
                    continue;
                SpawnBattleUnit(i);
            }

            ResetUnitsDamageSum();

            Observable.Timer(TimeSpan.FromSeconds(1))
                .Repeat()
                .Subscribe(_ => ReduceBossHp())
                .AddTo(_disposable);

            _bossFightController.Init();
            _curveTrackBehavior.SetFinishImageActive(false);
        }

        public void End()
        {
            if (_isAutoMergeActive)
                _autoMergeStrategy.ActiveAutoMerge();
            IsStarted = false;
            _currentDamageSum = 0;
            _hangarsController.ClearBossFightHangars();
            _playerProfileService.ResetCurrentPlayerProfile();
            _bossFightEntity.Destroy();
            _disposable.Dispose();
            _bossFightController.ResetCamera();
            _bossFightController.Cleanup();
            _hangarsController.SetBossFightActive(false);
            _curveTrackBehavior.SetFinishImageActive(true);
            _soundsPoolManager.PlayMusic("MainSoundtrack");
        }

        public void ResetHangars()
        {
            IsStarted = false;
            _currentDamageSum = 0;
            _hangarsController.ClearBossFightHangars();
            _bossFightController.Cleanup();
            _disposable.Dispose();
        }

        public void RestartBossFight()
        {
            if (_isAutoMergeActive)
                _autoMergeStrategy.ActiveAutoMerge();
            _bossFightEntity.Destroy();
            _playerProfileService.ResetCurrentPlayerProfile();
            _bossFightController.ResetCamera();
            _hangarsController.SetBossFightActive(false);
            _signalBus.Fire(new SignalWindowOpen(typeof(BossFightWindow)));
            Start();
        }

        private void ReduceBossHp()
        {
            if (!_bossFightAnimationController.IntroAnimationIsFinished)
                return;
            if (_bossFightEntity.BossHealth.Current < 0 || _bossFightEntity.BossTimeSeconds.Value <= 0)
                return;
            _bossFightEntity.ReplaceBossCurrentHealth(_bossFightEntity.BossHealth.Current -
                                                      _currentDamageSum);
        }

        private void OnHangarDrag(SignalHangarDrag signal)
        {
            if (!_bossFightEntity.IsDestroyed && signal.DragState == DragState.End)
                ResetUnitsDamageSum();
        }

        private void OnMergeUnit(SignalMergeUnit signal)
        {
            if (!_bossFightEntity.IsDestroyed)
            {
                var hangarIndex = _hangarsManager.GetFree(_bossPlayerProfile);
                if (hangarIndex != -1) 
                    SpawnBattleUnit(hangarIndex);

                ResetUnitsDamageSum();
            }
        }

        private void SpawnBattleUnit(int hangarIndex)
        {
            var unit = _unitFactory.Create(_unitBoxId);
            _bossPlayerProfile.AddItem(unit);
            _hangarsManager.Occupy(_bossPlayerProfile, hangarIndex, unit, HangarItemAddType.Add);
            _signalBus.Fire(new SignalGetUnit(unit.Model.Level));
        }

        private void ResetUnitsDamageSum()
        {
            _currentDamageSum = 0;
            foreach (var hangar in _bossPlayerProfile.Hangars)
                if (hangar.GetItem() is Unit unit)
                    _currentDamageSum += _unitsDamageDatabase.UnitsDamageDictionary[unit.Model.Level].damagePerSecond;

            SetOverallDamage();
        }

        private void SetOverallDamage() =>
            _bossFightEntity.ReplaceDamagePerSecond(_currentDamageSum);
    }
}