using DanielLochner.Assets.SimpleScrollSnap;
using Ui.Views.Theme.Impls;
using UnityEngine;
using UnityEngine.UI;

namespace Ui.Views.News
{
    public class NewsView : AThemedUiView
    {
        public NewsCollection NewsCollection;
        public ScrollSnapCollection ScrollSnapCollection;
        public Button CloseButton;
        public SimpleScrollSnap SimpleScrollSnap;
        public ToggleGroup Pagination;
        
        [SerializeField] private Text _snapIndexText;

        public void SetText(int currentNews, int newsCount) => 
            _snapIndexText.text = $" {currentNews}/{newsCount}";
    }
}