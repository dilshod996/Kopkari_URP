using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSound : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private UISoundType soundType = UISoundType.Click;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (SoundManager.Instance == null) return;
        SoundManager.Instance.PlayUI(soundType);
    }
}
