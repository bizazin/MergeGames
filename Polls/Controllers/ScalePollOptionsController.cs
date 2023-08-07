using System;
using System.Linq;
using Game.Services.Theme;
using Game.Signals.Poll;
using Game.Utils;
using SimpleUi.Signals;
using Ui.Views.Poll;
using Ui.Windows.Poll;
using UniRx;
using Websockets.Settings;
using Websockets.Values.Poll;
using Zenject;

namespace Ui.Controllers.Poll
{
    public class ScalePollOptionsController : AThemedController<ScalePollOptionsView>, IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;

        private readonly IConnectionSettingsDatabase _connectionSettingsDatabase;
        private readonly CompositeDisposable _disposable = new();

        public override int Order => 3;

        public ScalePollOptionsController
        (
            IThemeService themeService,
            SignalBus signalBus,
            IConnectionSettingsDatabase connectionSettingsDatabase
        ) : base(themeService)
        {
            _signalBus = signalBus;
            _connectionSettingsDatabase = connectionSettingsDatabase;
        }

        public void Initialize()
        {
            View.ScrollSnap.OnSnapPanel += OnSnapPanel;
            View.ScrollSnap.NextButton.OnClickAsObservable().Subscribe(_ =>
            {
                var isLastPanel = View.ScrollSnap.SelectedPanel == View.ScalePollOptionCollection.Count() - 1;
                if (isLastPanel)
                    _signalBus.Fire(new SignalWindowOpen(typeof(PollSelectWindow)));
            }).AddTo(View);
            _signalBus.GetStream<SignalOpenLastPollPanel>().Subscribe(_ =>
            {
                base.Show();
                View.ScrollSnap.GoToPanel(View.ScalePollOptionCollection.Count() - 1);
            }).AddTo(_disposable);
        }

        public void Dispose() => _disposable?.Dispose();    

        public void SetPollTime(string pollEndTime) =>
            View.TimeText.text = pollEndTime;

        public void SetOptions(PollValue poll)
        {
            var images = poll.answers;
            ClearExistingOptions();
            
            foreach (var pollImage in images) 
                AddOption(pollImage.answer);
        }

        public void SetView(string pollQuestion, string timeLeft)
        {
            View.PollText.text = pollQuestion;
            SetPollTime(timeLeft);
        }

        protected override void Show()
        {
            base.Show();
            View.ScrollSnap.GoToPanel(0);
        }

        private void OnSnapPanel()
        {
            var selectedPanel = View.ScrollSnap.SelectedPanel + 1;
            View.ScrollSnap.PreviousButton.gameObject.SetActive(selectedPanel != 1);
            View.SetText(selectedPanel, View.ScalePollOptionCollection.Count());
        }

        private void ClearExistingOptions()
        {
            var toggles = View.ScrollSnapCollection.GetItems();
            var scrollOptions = View.ScalePollOptionCollection.GetItems();
            
            while (toggles.Count > 0 || scrollOptions.Count > 0)
            {
                View.ScrollSnap.RemoveFromBack();
                View.ScrollSnapCollection.RemoveItem(toggles.Last());
                View.ScalePollOptionCollection.RemoveItem(scrollOptions.Last());
            }
        }

        private void AddOption(string answer)
        {
            var toggleView = View.ScrollSnapCollection.AddItem();
            toggleView.ScrollToggle.group = View.Pagination;
            var scalePollOptionView = View.ScalePollOptionCollection.AddItem();
            View.ScrollSnap.AddToBack(scalePollOptionView.gameObject);

            SetImage(scalePollOptionView, answer);
        }

        private void SetImage(ScalePollOptionView item, string image) =>
            UrlConvertUtils.SetTextureFromUrl(item.SetImage, _connectionSettingsDatabase.GetStorageDomain() + image);
    }
}