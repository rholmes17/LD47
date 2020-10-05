using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FillBar : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI displayText;
    public Image Fill;
    //public WarningButton WarningButton;

    public Gradient gradient;

    private float currentValue = 0f;
    public float CurrentValue
    {
        get
        {
            return currentValue;
        }
        set
        {
            currentValue = value;
            slider.value = currentValue;
            displayText.text = (slider.value * 100).ToString("0.00") + "%";

            Fill.color = gradient.Evaluate(currentValue);
        }
    }
    void Start()
    {
        //CurrentValue = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        //CurrentValue += 0.0005f;
    }
}
