using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    public Game game;

    public ToggleSwitch pumpSwitch;
    public ToggleSwitch genSwitch;
    public ToggleSwitch fireSupSwitch;

    public FillBar controlRodBar;
    public FillBar coolantFlowBar;
    public FillBar coolantTempBar;
    public FillBar reactorTempBar;

    public FillBar genFuelBar;
    public FillBar fireSupBar;

    // Display available power, total energy, time left
    public TextMeshProUGUI availablePowerDisplay;
    public TextMeshProUGUI totalPowerDisplay;
    public TextMeshProUGUI generatedEnergyDisplay;
    public TextMeshProUGUI turbineRPMDisplay;

    public WarningButton warningButton;
    public WarningButton cautionButton;
    public WarningButton fireButton;

    // Button for generator, fire suppression

    // Knob for flow rate, control rod position
    public Slider controlRodPosSlider;
    public Slider flowRateSlider;

    public LED powerLED;

    public LED pumpLED;
    public LED genLED;
    public LED fireSupLED;

    public LED turbineLED;

    public LED stabilityLED;

    public float maxCoolantTemp = 1;
    public float maxReactorTemp = 1;
    public float maxRPM = 1;

    // Generator
    [Header("Generator")]
    public float genPower = 100;
    public float genConsumption = .1f;
    private bool isGenOn = false;
    private float availableFuel = 1;

    // Coolant
    [Header("Coolant")]
    public float coolantHeatCapacity = 1;
    public float coolantDissipationFactor = 1;
    public float coolantRadiativeDissipation = .0001f;
    public float pumpSpoolRate = 1;
    private bool isPumpOn;
    private float coolantTemp = 0;
    private float idealCoolantFlowRate = 0;
    private float actualCoolantFlowRate = 0;
    public float maxCoolantFlowRate = 1; // * 700 m^3 per min
    public float stbyPumpPower = 10;    // MW
    public float maxPumpPower = 50;

    // Main Reactor
    [Header("Main Reactor")]
    public float initialReactorTemp = .6f;
    public float baseReactionRate = .1f;
    public float reactorTempGrowthRate = .01f;
    public float convectiveHeatCoeff = 1;
    public float reactorHeatCapacity = 100;
    public float controlRodSpeed = 0.1f;
    public float controlRodEffectiveness = .1f;
    public float controlRodPowerUsage = 100;
    private float convectiveEnergyTransfer;
    private float reactorTemp = 0;
    private float idealControlRodPosition = 0.5f;
    private float controlRodPosition = 0.5f;
    private float reactionRate;

    // Turbine
    [Header("Turbine")]
    public float turbineSpeedScale = .3f;
    public float turbineSpoolRate = .2f;
    public float idealRPM = 3;
    public float idealPower = 100;
    private float optimalTurbineRPM = 0;
    private float currentTurbineRPM = 0;
    private bool isFailedTurbine;

    // Power
    //public float maxPower = 1000;
    [Header("Power")]
    private float totalPower;
    private float availablePower;
    private float generatedEnergy;

    // Fire
    [Header("Fire")]
    public float fireTempEffect = .1f;
    public float fireSupPower = 50;
    public float fireSupConsumption = .1f;
    private int isFire;
    private bool isFireSupOn;
    private float availableSup = 1;


    [System.Flags]
    public enum Alerts
    {
        None = 0,
        Test = 1,
        Temp = 2,
        RPM = 4,
        genTrip = 8,
        Fire = 16
    }

    private Alerts warnings;
    private Alerts cautions;
    private Alerts fires;

    //HashSet<int> Warnings = new HashSet<int>();

    // Start is called before the first frame update
    void Start()
    {
        coolantTempBar.CurrentValue = coolantTemp / maxCoolantTemp;
        reactorTempBar.CurrentValue = reactorTemp / maxReactorTemp;

        genFuelBar.CurrentValue = availableFuel;
        fireSupBar.CurrentValue = availableSup;

        stabilityLED.SetOff();

        genSwitch.valueChanged += StartGen;
        pumpSwitch.valueChanged += StartPump;
        fireSupSwitch.valueChanged += StartFireSup;

        reactorTemp = initialReactorTemp;
        reactionRate = baseReactionRate;

        idealControlRodPosition = 1;
        controlRodPosition = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (game.paused) return;
        // Power

        totalPower = 0;
        // Power in

        // Backup generator (EDG)
        if (isGenOn)
        {
            if (availableFuel > 0)
            {
                availableFuel -= genConsumption * Time.deltaTime;
                genFuelBar.CurrentValue = availableFuel;
                totalPower += genPower;
            }
            else
                StartGen(false);
        }


        if (!isFailedTurbine && currentTurbineRPM > 0.9 * maxRPM)
        {
            SetAlert(Alerts.RPM, true, false);
        }

        // Turbine
        if (!isFailedTurbine && currentTurbineRPM > maxRPM)
        {
            isFailedTurbine = true;
            optimalTurbineRPM = 0;
            currentTurbineRPM = 0;
            turbineLED.SetOn();
            SetAlert(Alerts.RPM, true, true);
        }
        if (!isFailedTurbine && actualCoolantFlowRate > 0)
        {
            optimalTurbineRPM = actualCoolantFlowRate * coolantTemp * turbineSpeedScale * Time.deltaTime;
            coolantTemp -= currentTurbineRPM / (actualCoolantFlowRate * coolantDissipationFactor);
            totalPower += currentTurbineRPM * idealPower / idealRPM;
        }
        currentTurbineRPM = Mathf.Lerp(currentTurbineRPM, optimalTurbineRPM, .2f * Time.deltaTime);

        availablePower = totalPower;


        // Control rods
        idealControlRodPosition = controlRodPosSlider.value;
        float speedMultiplier;

        if (controlRodPosition < 0.05)
        {
            stabilityLED.SetOn();
            reactorTempGrowthRate = -reactorTempGrowthRate;
            speedMultiplier = 1;
        }
        else if ((1 - controlRodPosition) * controlRodPowerUsage <= availablePower)
        {
            availablePower -= (1 - controlRodPosition) * controlRodPowerUsage;
            speedMultiplier = 1;
        }
        else
        {
            idealControlRodPosition = 1;
            controlRodPosSlider.value = 1;
            speedMultiplier = .25f;
        }

        float delta = Mathf.Abs(controlRodPosition - idealControlRodPosition);
        if (delta > controlRodSpeed * speedMultiplier * Time.deltaTime)
            controlRodPosition = Mathf.Lerp(controlRodPosition, idealControlRodPosition, controlRodSpeed * speedMultiplier * Time.deltaTime / delta);
        else
            controlRodPosition = idealControlRodPosition;

        reactionRate += (controlRodPosition - .5f) * reactorTemp * controlRodEffectiveness * Time.deltaTime;
        reactionRate = Mathf.Clamp01(reactionRate);
        reactorTemp += reactionRate * reactorTempGrowthRate;


        // Reactor cooling
        if (actualCoolantFlowRate > 0.1f)
        {
            convectiveEnergyTransfer = convectiveHeatCoeff * (reactorTemp - coolantTemp);
            reactorTemp -= convectiveEnergyTransfer / reactorHeatCapacity * Time.deltaTime;
            coolantTemp += convectiveEnergyTransfer / (coolantHeatCapacity * actualCoolantFlowRate) * Time.deltaTime;
        }

        if (reactorTemp > .7)
        {
            SetAlert(Alerts.Temp, true, false);
            if (reactorTemp > .9)
                SetAlert(Alerts.Temp, true, true);
            if (reactorTemp > 1)
                game.EndCurrentGame();
        }
        else
            SetAlert(Alerts.Temp, false, true);
        // Power out

        idealCoolantFlowRate = flowRateSlider.value;

        // Check coolant pump
        if (isPumpOn)
        {
            //Check if too much power is being used
            if (availablePower >= stbyPumpPower + idealCoolantFlowRate * maxPumpPower)
            {
                availablePower -= stbyPumpPower + actualCoolantFlowRate * maxPumpPower;
                float pumpDelta = Mathf.Abs(actualCoolantFlowRate - idealCoolantFlowRate);
                if (pumpDelta > pumpSpoolRate * Time.deltaTime)
                    actualCoolantFlowRate = Mathf.Lerp(actualCoolantFlowRate, idealCoolantFlowRate, pumpSpoolRate * Time.deltaTime / pumpDelta);
                else
                    actualCoolantFlowRate = idealCoolantFlowRate;
            }
            else
            {
                // Trip the pump
                StartPump(false);
                actualCoolantFlowRate = 0;
                Trip();
            }
        }
        else
        {
            pumpLED.SetOff();
            actualCoolantFlowRate = 0;
        }

        // Fire suppresion
        if (isFireSupOn)
        {
            if (availableSup > 0 && availablePower >= fireSupPower)
            {
                availablePower -= fireSupPower;
                availableSup -= fireSupConsumption * Time.deltaTime;
                fireSupBar.CurrentValue = availableSup;
                if (isFire > 0)
                {
                    isFire -= 2;
                }
                else if (isFire <= 0)
                {
                    SetAlert(Alerts.Fire, false, false, true);
                }
            }
            else
            {
                StartFireSup(false);
                Trip();
            }
        }

        if (coolantTemp > .5f)
        {
            isFire++;
        }

        if (isFire > 0)
        {

            SetAlert(Alerts.Fire, true, false, true);
            reactorTemp += fireTempEffect * Time.deltaTime;
        }

        if (availablePower >= 0)
        {
            generatedEnergy += availablePower * Time.deltaTime;
            if (availablePower > 10)
            {
                SetAlert(Alerts.genTrip, false, true);
            }
            else
                SetAlert(Alerts.genTrip, true, false);
        }
        else
            availablePower = 0;


        if (totalPower > 0)
            powerLED.SetOn();
        else
            powerLED.SetOff();


        coolantTemp -= coolantRadiativeDissipation * coolantTemp * coolantTemp * coolantTemp * coolantTemp * Time.deltaTime;
        coolantTemp = Mathf.Clamp01(coolantTemp);
        reactorTemp = Mathf.Clamp01(reactorTemp);

        controlRodBar.CurrentValue = 1 - controlRodPosition;
        reactorTempBar.CurrentValue = reactorTemp;

        coolantTempBar.CurrentValue = coolantTemp;
        coolantFlowBar.CurrentValue = actualCoolantFlowRate;

        turbineRPMDisplay.text = (currentTurbineRPM * 1000).ToString("N0") + " RPM";

        totalPowerDisplay.text = "Power generated: " + totalPower.ToString("N0") + " MW";
        availablePowerDisplay.text = "Available power: " + availablePower.ToString("N0") + " MW";
        generatedEnergyDisplay.text = "Total generated energy: " + generatedEnergy.ToString("N0") + " MJ";
    }

    private void Trip()
    {
        SetAlert(Alerts.genTrip, true, true);
        availablePower = 0;
        StartGen(false);
        genSwitch.Toggle(false);
    }

    private void StartGen(bool value)
    {
        if (availableFuel > 0 && value)
        {
            isGenOn = true;
            genLED.SetOn();
        }
        else
        {
            isGenOn = false;
            genLED.SetOff();
        }
    }

    private void StartPump(bool value)
    {

        if (availablePower >= stbyPumpPower + idealCoolantFlowRate * maxPumpPower && value)
        {
            isPumpOn = true;
            pumpLED.SetOn();
        }
        else if (value)
        {
            isPumpOn = false;
            SetAlert(Alerts.genTrip, true, false);
            pumpLED.SetOff();
        }
        else
        {
            isPumpOn = false;
            pumpLED.SetOff();
        }
    }

    private void StartFireSup(bool value)
    {
        if (availableSup > 0 && value)
        {
            isFireSupOn = true;
            fireSupLED.SetOn();
        }
        else
        {
            isFireSupOn = false;
            fireSupLED.SetOff();
        }
    }

    private void SetAlert(Alerts alert, bool value, bool isWarning, bool isFire = false)
    {
        if (value)
        {
            cautions = SetFlag(cautions, alert);

            if (isWarning)
            {
                warnings = SetFlag(warnings, alert);

            }

            if (isFire)
            {
                fires = SetFlag(fires, alert);
            }
        }
        else
        {

            cautions = UnsetFlag(cautions, alert);
            if (isFire)
                fires = UnsetFlag(fires, alert);
            else
                warnings = UnsetFlag(warnings, alert);
        }

        cautionButton.SetAlarm(cautions);
        warningButton.SetAlarm(warnings);
        fireButton.SetAlarm(fires);

    }

    public static bool HasFlag(Alerts alert, Alerts flag)
    {
        return (alert & flag) == flag;
    }

    public static Alerts SetFlag(Alerts alert, Alerts flag)
    {
        return alert | flag;
    }

    public static Alerts UnsetFlag(Alerts alert, Alerts flag)
    {
        return alert & ~flag;
    }
}
