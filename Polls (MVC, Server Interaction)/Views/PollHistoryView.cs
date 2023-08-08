using Ui.Views.Theme.Impls;
using UnityEngine;
using UnityEngine.UI;

namespace Ui.Views.Poll
{
	public class PollHistoryView : AThemedUiView
	{
		[SerializeField] private Button closeButton;
		[SerializeField] private HistoryFinishedCollection historyFinishedCollection;
		[SerializeField] private HistoryProgressCollection historyProgressCollection;

		public HistoryFinishedCollection HistoryFinishedCollection => historyFinishedCollection;
		public HistoryProgressCollection HistoryProgressCollection => historyProgressCollection;

		public Button CloseButton => closeButton;
	}
}