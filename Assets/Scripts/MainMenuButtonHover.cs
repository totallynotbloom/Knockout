using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float scaleLerpSpeed = 12f;

    [Header("Audio")]
    [SerializeField] private bool playHoverSound = true;

    private Vector3 defaultScale;
    private Vector3 targetScale;

    private void Awake()
    {
        defaultScale = transform.localScale;
        targetScale = defaultScale;
    }

    private void OnEnable()
    {
        targetScale = defaultScale;
        transform.localScale = defaultScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleLerpSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Keep keyboard and mouse highlight behavior in sync.
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);

        SetHoverState(true, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            EventSystem.current.SetSelectedGameObject(null);

        SetHoverState(false, false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        // eventData is null when selected by script; avoid duplicate hover SFX.
        bool canPlaySound = eventData != null;
        SetHoverState(true, canPlaySound);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetHoverState(false, false);
    }

    private void SetHoverState(bool isHovered, bool canPlaySound)
    {
        targetScale = isHovered ? defaultScale * hoverScale : defaultScale;

        if (isHovered && canPlaySound && playHoverSound && MusicManager.Instance != null)
            MusicManager.Instance.PlayNavigationSound();
    }
}
