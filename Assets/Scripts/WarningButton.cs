using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class WarningButton : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI text;

    public Color onColour;
    public Color offColour;

    private AudioSource alarmAudio;
    private AudioSource click;

    private bool isAlarm = false;

    private Controller.Alerts ignoredAlerts;
    private Controller.Alerts currentAlerts;
    private Controller.Alerts activeAlerts;

    private void Start()
    {
        button.image.color = offColour;
    }

    public void SetAlarm(Controller.Alerts alert)
    {
        activeAlerts = alert;
        ignoredAlerts &= alert;
        currentAlerts = Controller.UnsetFlag(alert, ignoredAlerts);
        if (currentAlerts!=0)
        {
            
            isAlarm = true;
            button.image.color = onColour;
        }
        else
        {
            if (isAlarm)
            {
                isAlarm = false;
            }
            button.image.color = offColour;
        }
    }

    public void OnClick()
    {
        ignoredAlerts |= currentAlerts;
        SetAlarm(activeAlerts);
    }

}
