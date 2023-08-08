using System;
using System.Collections.Generic;
using Game.Services.Theme;
using Game.Signals.GameplayHud;
using SimpleUi.Signals;
using Ui.Views.Poll;
using Ui.Windows;
using UniRx;
using Websockets.Models.Poll;
using Websockets.Values.Poll;
using Zenject;

namespace Ui.Controllers.Poll
{
    public class PollHistoryController : AThemedController<PollHistoryView>, IDisposable, IInitializable
    {
        private readonly SignalBus _signalBus;
        private readonly CompositeDisposable _disposable = new();
        private readonly IThemeService _themeService;
        private readonly List<PollHistoryItemView> _historyPolls = new();

        private int _currentPollIndex;
        private PollValue[] _polls;
        private int _answerId;
        private string[] _timeLeft;
        
        public PollHistoryController
        (
            SignalBus signalBus,
            IThemeService themeService
        ) : base(themeService)
        {
            _signalBus = signalBus;
            _themeService = themeService;
        }

        public override int Order => 3;

        public void Initialize() =>
            View.CloseButton.OnClickAsObservable()
                .Subscribe(_ => _signalBus.Fire(new SignalWindowOpen(typeof(HomeWindow)))).AddTo(View);

        public void Dispose() =>
            _disposable?.Dispose();

        public void AddPoll(PollValue poll, PollOptionPercent[] sortedPercents)
        {
            var isProgress = DateTime.Parse(poll.endDate) >= DateTime.UtcNow;

            var item = isProgress ? View.HistoryProgressCollection.AddItem() : View.HistoryFinishedCollection.AddItem();
            item.ProgressEnable(isProgress);
            item.SetViewText(poll.question, poll.endDate);
            item.SetPercentText(sortedPercents);
            item.ShowButton.OnClickAsObservable().Subscribe(_ => _signalBus.Fire(new SignalShowPoll(poll))).AddTo(_disposable);
            
            _historyPolls.Add(item);
        }

        public void SetPercentsForPoll(int currentPollIndex, PollOptionPercent[] sortedPercents) => 
            _historyPolls[currentPollIndex].SetPercentText(sortedPercents);

        protected override void SwitchTheme()
        {
            base.SwitchTheme();
            if (!IsThemeSwitched)
                return;

            var finishedPolls = View.HistoryFinishedCollection.GetItems();
            foreach (var pollHistoryItemView in finishedPolls)
                _themeService.SwitchElements(pollHistoryItemView.ThemeElements);

            var progressPolls = View.HistoryProgressCollection.GetItems();
            foreach (var pollHistoryItemView in progressPolls)
                _themeService.SwitchElements(pollHistoryItemView.ThemeElements);
        }
    }
}