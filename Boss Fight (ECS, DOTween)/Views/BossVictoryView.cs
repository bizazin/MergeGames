using SimpleUi.Abstracts;
using UnityEngine;
using UnityEngine.UI;

namespace Ui.Views.BossFight
{
	public class BossVictoryView : UiView
	{
		[SerializeField] private Button claimButton;
		[SerializeField] private Text softMultiplier;
		[SerializeField] private Text softReward;

		public Text SoftMultiplier => softMultiplier;
		public Text SoftReward => softReward;
		public Button ClaimButton => claimButton;
	}
}
