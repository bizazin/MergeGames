using SimpleUi.Abstracts;
using UnityEngine;
using UnityEngine.UI;

public class BossDefeatView : UiView
{
    [SerializeField] private Button repeatButton;

    public Button RepeatButton => repeatButton;
}
