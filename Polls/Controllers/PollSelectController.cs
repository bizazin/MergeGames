using System;
using Game.Services.Theme;
using Game.Signals.Poll;
using Game.Utils;
using SimpleUi.Signals;
using Ui.Views.Poll;
using Ui.Windows;
using Ui.Windows.Poll;
using UniRx;
using Websockets;
using Websockets.Models.Poll;
using Websockets.Settings;
using Websockets.Values.Poll;
using Zenject;

namespace Ui.Controllers.Poll
{
    public class PollSelectController : AThemedController<PollSelectView>, IInitializable, IDisposable
    {
        private readonly IWebSockets _webSockets;
        private readonly SignalBus _signalBus;
        private readonly IConnectionSettingsDatabase _connectionSettingsDatabase;
        private readonly CompositeDisposable _disposable = new();

        private int _answerId;
        private PollValue _currentPoll;
        private PollGridItemView[] _twoGridsViews;
        private PollGridItemView[] _threeGridsViews;
        private PollGridItemView[] _currentGridViews;

        public override int Order => 3;

        public PollSelectController
        (
            IThemeService themeService,
            IConnectionSettingsDatabase connectionSettingsDatabase,
            IWebSockets webSockets,
            SignalBus signalBus
        ) : base(themeService)
        {
            _webSockets = webSockets;
            _signalBus = signalBus;
            _connectionSettingsDatabase = connectionSettingsDatabase;
        }

        public void Initialize()
        {
            _threeGridsViews = View.PollItemsView;
            _twoGridsViews = new[] {View.PollItemsView[0], View.PollItemsView[2]};
            View.CloseButton.OnClickAsObservable()
                .Subscribe(_ => _signalBus.Fire(new SignalWindowOpen(typeof(CareerWindow)))).AddTo(View);
            View.BackButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _signalBus.Fire(new SignalWindowOpen(typeof(ScalePollOptionsWindow)));
                    _signalBus.Fire(new SignalOpenLastPollPanel());
                }).AddTo(View);
            View.VoteButton.OnClickAsObservable().Subscribe(_ => OnVoteButtonClick()).AddTo(View);
            View.HistoryButton.OnClickAsObservable()
                .Subscribe(_ => _signalBus.Fire(new SignalWindowOpen(typeof(PollHistoryWindow)))).AddTo(View);
            _signalBus.GetStream<SignalPollAdd>().Subscribe(OnPollAdd).AddTo(_disposable);
        }

        public void Dispose() => _disposable?.Dispose();
        
        public void SetOptionsWithToggles(PollValue poll)
        {
            SetCurrentPoll(poll);
            for (var i = 0; i < _currentGridViews.Length; i++)
            {
                var pollGrid = _currentGridViews[i];
                SetImage(pollGrid, poll.answers[i].answer);
                pollGrid.DehighlightOption();
                pollGrid.ToggleEnable(true);
                pollGrid.ScaleButton.interactable = true;
                pollGrid.ChoseOptionToggle.OnValueChangedAsObservable().Subscribe(isOn => pollGrid.SetFrame(isOn))
                    .AddTo(View);
                pollGrid.ChoseOptionToggle.isOn = false;
                pollGrid.ScaleButton.OnClickAsObservable()
                    .Subscribe(_ => pollGrid.ChoseOptionToggle.isOn = true).AddTo(View);
            }

            View.OnVoteButton(true);
        }

        public void SetOptionsWithPercents(PollValue poll, PollOptionPercent[] percents)
        {
            SetCurrentPoll(poll);
            for (var i = 0; i < _currentGridViews.Length; i++)
            {
                var pollGrid = _currentGridViews[i];
                SetImage(pollGrid, poll.answers[i].answer);
                pollGrid.DehighlightOption();
                pollGrid.ToggleEnable(false);
                pollGrid.SetPercentText(percents[i].percent);
                pollGrid.ScaleButton.interactable = false;
            }

            View.OnVoteButton(false);

            if (poll.isVoted)
                HighlightChosen(poll.selectedOption, percents[poll.selectedOption].percent);
        }

        public void SetBackButton(bool isActive) 
            => View.OnBackButton(isActive);

        public void SetView(string pollQuestion, string timeLeft)
        {
            View.PollText.text = pollQuestion;
            SetPollTime(timeLeft);
        }

        public void SetPollTime(string pollEndTime) =>
            View.TimeText.text = pollEndTime;

        public void SwapOnPercents(PollOptionPercent[] percents)
        {
            for (var i = 0; i < _currentGridViews.Length; i++)
            {
                var pollGrid = _currentGridViews[i];
                pollGrid.ToggleEnable(false);
                pollGrid.SetPercentText(percents[i].percent);
            }

            HighlightChosen(_answerId, percents[_answerId].percent);
        }

        private void OnPollAdd(SignalPollAdd signal)
        {
            _webSockets.PollVotes(_currentPoll.id);

            _currentPoll.isVoted = true;
            _currentPoll.selectedOption = _answerId;
        }

        private void SetCurrentPoll(PollValue poll)
        {
            _currentPoll = poll;
            var isTwoOptions = poll.answers.Length == 2;
            _currentGridViews = isTwoOptions ? _twoGridsViews : _threeGridsViews;
            View.SetGridsCount(isTwoOptions);
            View.ProgressEnable(DateTime.Parse(poll.endDate) >= DateTime.UtcNow);
        }

        private void SetImage(PollGridItemView item, string image) =>
            UrlConvertUtils.SetTextureFromUrl(texture => item.OptionImage.texture = texture,
                _connectionSettingsDatabase.GetStorageDomain() + image);

        private void HighlightChosen(int optionId, int percent) =>
            _currentGridViews[optionId].HighlightChoseOption(percent);

        private void OnVoteButtonClick()
        {
            _answerId = GetAnswerIndex();

            if (_answerId == -1) 
                return; 
            View.OnVoteButton(false);

            _signalBus.Fire(new SignalWindowOpen(typeof(WaitingWindow)));
            _webSockets.PollAdd(_answerId, _currentPoll.id);
        }

        private int GetAnswerIndex()
        {
            for (var i = 0; i < _currentGridViews.Length; i++)
                if (_currentGridViews[i].IsSelected)
                    return i;
            return -1;
        }
    }
}