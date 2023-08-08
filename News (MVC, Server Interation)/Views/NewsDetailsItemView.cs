using SimpleUi.Abstracts;
using TMPro;
using Ui.Views.Theme.Impls;
using UnityEngine;
using UnityEngine.UI;

namespace Ui.Views.News
{
    public class NewsDetailsItemView : AThemedUiView
    {
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private RawImage headerImage;
        [SerializeField] private Button detailsRaycastButton;

        public TMP_Text HeaderText => headerText;
        public TMP_Text DetailsText => detailsText;
        public RawImage HeaderImage => headerImage;
        public Button DetailsRaycastButton => detailsRaycastButton;
    }
}