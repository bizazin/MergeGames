using System;
using Ecs.UnitShop.Utils;
using Game.Db;
using Game.Db.BossFightSettings;
using Game.Db.GameSetting;
using Game.Enums;
using Game.Services.BossFight;
using Game.Services.LevelLocations;
using Game.Services.Purchases;
using SimpleUi.Abstracts;
using Ui.Views.BossFight;
using UniRx;
using Utils.BigMath;
using Zenject;

namespace Ui.Controllers.BossFight.Impls
{
    public class BossVictoryController : UiController<BossVictoryView>, IInitializable, IDisposable,
        IBossVictoryController
    {
        private readonly CompositeDisposable _disposable = new();
        private readonly IBossFightSettingsDatabase _bossFightSettingsDatabase;
        private readonly UnitShopContext _unitShopContext;
        private readonly IBossFightService _bossFightService;
        private readonly ILevelLocationsService _levelLocationsService;
        private readonly ILevelLocationsDatabase _levelLocationsDatabase;
        private readonly ICurrentLocationHolder _locationHolder;
        private readonly IGameSettingsDatabase _gameSettingsDatabase;
        private readonly IPurchaseManager _purchaseManager;
        private readonly IItemsDatabase _itemsDatabase;

        public override int Order => 8;

        public BossVictoryController
        (
            IBossFightSettingsDatabase bossFightSettingsDatabase,
            UnitShopContext unitShopContext,
            IBossFightService bossFightService,
            ILevelLocationsService levelLocationsService,
            ILevelLocationsDatabase levelLocationsDatabase,
            ICurrentLocationHolder locationHolder,
            IGameSettingsDatabase gameSettingsDatabase,
            IPurchaseManager purchaseManager,
            IItemsDatabase itemsDatabase
        )
        {
            _bossFightSettingsDatabase = bossFightSettingsDatabase;
            _unitShopContext = unitShopContext;
            _bossFightService = bossFightService;
            _levelLocationsService = levelLocationsService;
            _levelLocationsDatabase = levelLocationsDatabase;
            _locationHolder = locationHolder;
            _gameSettingsDatabase = gameSettingsDatabase;
            _purchaseManager = purchaseManager;
            _itemsDatabase = itemsDatabase;
        }

        public void Initialize() => 
            View.ClaimButton.OnClickAsObservable().Subscribe(_ => OnClaimButton()).AddTo(View);

        public void Dispose() => _disposable?.Dispose();

        public void SetReward()
        {
            View.SoftReward.text = GetSoftRewardText();
            View.SoftMultiplier.text = GetMultiplierText();
        }

        private void OnClaimButton()
        {
            _bossFightService.End();
            _levelLocationsService.ConfirmLevelUpLocation();
        }

        private string GetSoftRewardText()
        {
            var upperUnitLevel = _levelLocationsDatabase.GetLocationById(_locationHolder.Level.Value + 1).Condition;
            var lowerUnitLevelIfHide = upperUnitLevel - _gameSettingsDatabase.GameSetting.LowerCatLevelIfHide;
            var lowerUnitLevel = lowerUnitLevelIfHide < 1 ? 1 : lowerUnitLevelIfHide;
            var softReward = _purchaseManager.GetPrice(_itemsDatabase.GetByLevel(lowerUnitLevel).Id, ECurrencyType.Soft).Value;
            return SIUtils.GetValueWithSuffix(softReward * _bossFightSettingsDatabase.BossFightSetting.SoftRewardCoefficient);
        }

        private string GetMultiplierText()
        {
            var currentLocationMultiply = _levelLocationsDatabase.GetLocationById(_locationHolder.Level.Value + 1).Multiply;
            var valueWithSuffix = SIUtils.GetValueWithSuffix(currentLocationMultiply);
            return "x" + valueWithSuffix;
        }
    }
}