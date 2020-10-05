using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Threading;

public class ToggleSwitch : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private bool _isOn = false;
    public bool isOn
    {
        get
        {
            return _isOn;
        }
    }

    public RectTransform toggleIndicator;
    public Image backgroundImage;


    public Color onColour;
    public Color offColour;

    private float offY;
    private float onY;

    [SerializeField]
    private float tweenTime = 0.25f;

    private AudioSource audioSource;

    public delegate void ValueChanged(bool value);
    public event ValueChanged valueChanged;


    // Start is called before the first frame update
    void Start()
    {
        offY = toggleIndicator.anchoredPosition.y;
        onY = (backgroundImage.rectTransform.rect.height - toggleIndicator.rect.height)/2;
        audioSource = this.GetComponent<AudioSource>();
        backgroundImage.color = offColour;
    }

    private void OnEnable()
    {
        Toggle(isOn);
    }

    public void Toggle(bool value, bool playSFX = true)
    {
        if (value!=_isOn)
        {
            _isOn = value;

            ToggleColour(_isOn);
            MoveIndicator(_isOn);
            if (playSFX)
                audioSource.Play();

            if (valueChanged != null)
                valueChanged(isOn);
        }
    }

    private void ToggleColour(bool value)
    {
        if (value)
            backgroundImage.DOColor(onColour, tweenTime);
        else
            backgroundImage.DOColor(offColour, tweenTime);
    }

    private void MoveIndicator(bool value)
    {
        if (value)
            toggleIndicator.DOAnchorPosY(onY, tweenTime);
        else
            toggleIndicator.DOAnchorPosY(offY, tweenTime);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Toggle(!isOn);
    }

}
