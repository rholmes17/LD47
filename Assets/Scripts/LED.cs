using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LED : MonoBehaviour
{
    public Image led;

    //private bool isOn;
    public Color onColour;
    public Color offColour;

    public void SetOn()
    {
        led.color = onColour;
    }

    public void SetOff()
    {
        led.color = offColour;
    }
}
