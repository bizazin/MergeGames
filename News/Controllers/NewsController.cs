using System;
using System.Collections.Generic;
using System.Linq;
using Core.Interfaces;
using Core.Managers;
using Game.Models;
using Game.Services;
using Game.Services.Theme;
using Game.Signals.News;
using Game.Utils;
using SimpleUi.Signals;
using Ui.Enums;
using Ui.Signals;
using Ui.Views.News;
using UniRx;
using UniRx.Triggers;
using Websockets;
using Websockets.Models.News;
using Websockets.Settings;
using Zenject;

namespace Ui.Controllers.News
{
    public class NewsController : AThemedController<NewsView>, IInitializable, IDisposable
    {
        private readonly IThemeService _themeService;
        private readonly SignalBus _signalBus;
        private readonly IWebSockets _webSockets;
        private readonly CompositeDisposable _disposable = new();
        private readonly IConnectionSettingsDatabase _connectionSettingsDatabase;
        private readonly InteractableLinkService _interactableLinkService;
        private readonly IDataService _dataService;
        
        private NewsOption[] _gameNews;
        private List<int> _checkedNewsIds = new();
        private int[] _newsIds;

        public override int Order => 3;

        public NewsController
        (
            IThemeService themeService,
            SignalBus signalBus,
            IWebSockets webSockets,
            IConnectionSettingsDatabase connectionSettingsDatabase,
            InteractableLinkService interactableLinkService,
            IDataService dataService
        ) : base(themeService)
        {
            _themeService = themeService;
            _signalBus = signalBus;
            _webSockets = webSockets;
            _connectionSettingsDatabase = connectionSettingsDatabase;
            _interactableLinkService = interactableLinkService;
            _dataService = dataService;
        }

        protected override void Show()
        {
            base.Show();
            if (_newsIds == null || _newsIds.Length == 0)
                return;
            OnSnapPanel();
        }

        public void Initialize()
        {
            View.SimpleScrollSnap.OnSnapPanel += OnSnapPanel;
            _signalBus.GetStream<SignalNewsInfo>().Subscribe(SetCurrentNews).AddTo(_disposable);
            View.CloseButton.OnClickAsObservable().Subscribe(_ =>
                {
                    _signalBus.Fire<SignalWindowBack>();
                    SavePrefs();
                })
                .AddTo(_disposable);
            _webSockets.NewsDate();
        }

        private void SavePrefs()
        {
            _dataService.SetObject(PlayerPrefsKeys.UserAlreadyCheckedNews,
                new NewsSaveVo(_checkedNewsIds.ToArray()));
            _dataService.Save();
        }

        protected override void SwitchTheme()
        {
            base.SwitchTheme();
            if (!IsThemeSwitched)
                return;

            var newsDetailsItemViews = View.NewsCollection.GetItems();
            foreach (var news in newsDetailsItemViews)
                _themeService.SwitchElements(news.ThemeElements);
        }

        private void SetCurrentNews(SignalNewsInfo signal)
        {
            _gameNews = signal.Value.gameNews;
            InstantiateNews();
            _newsIds = _gameNews.Select(news => news.id).ToArray();
            var count = 0;
            if (_dataService.Has(PlayerPrefsKeys.UserAlreadyCheckedNews)) 
            {
                var checkedNewsIds = _dataService
                                     .GetObject<NewsSaveVo>(PlayerPrefsKeys.UserAlreadyCheckedNews)
                                     .NewsIds;
                checkedNewsIds = checkedNewsIds.Where(id => _newsIds.Contains(id)).ToArray();
                count = _newsIds
                        .Except(checkedNewsIds)
                        .Count();
                _checkedNewsIds = checkedNewsIds.ToList();
            }
            else
                count = _gameNews.Length;
            _signalBus.Fire(new SignalNotificationImage(NotificationImagePlace.News, true, count));
        }

        private void InstantiateNews()
        {
            View.SetText(1, _gameNews.Length);
            for (var i = 0; i < _gameNews.Length; i++)
            {
                var item = View.NewsCollection.AddItem();
                var value = _gameNews[i];
                var scrollSnapView = View.ScrollSnapCollection.AddItem();
                scrollSnapView.ScrollToggle.group = View.Pagination;
                item.HeaderText.text = value.title;
                item.DetailsText.text = value.content;
                SetImage(item, i);
                item.DetailsRaycastButton.OnPointerClickAsObservable()
                        .Subscribe(eventData =>
                            _interactableLinkService.OnPointerClick(eventData.position, item.DetailsText))
                        .AddTo(item);
            }
        }

        private void SetImage(NewsDetailsItemView item, int indexImage)
        {
            var image = _gameNews[indexImage].image;
            UrlConvertUtils.SetTextureFromUrl(texture => item.HeaderImage.texture = texture,
                _connectionSettingsDatabase.GetStorageDomain() + image);
        }

        private void OnSnapPanel()
        {
            var selectedPanel = View.SimpleScrollSnap.SelectedPanel;
            var checkNewsId = _newsIds[selectedPanel];
            if (!_checkedNewsIds.Contains(checkNewsId)) 
                _checkedNewsIds.Add(checkNewsId);
            _webSockets.CheckNewsId(checkNewsId);
            
            View.SetText(selectedPanel + 1, _gameNews.Length);
        }


        public void Dispose()
        {
            SavePrefs();
            View.SimpleScrollSnap.OnSnapPanel -= OnSnapPanel;
            _disposable?.Dispose();
        }
    }
}