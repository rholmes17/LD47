using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    public Controller controllerPrefab;
    private Controller controllerInstance;

    public GameObject startButton;
    public GameObject restartButton;
    public GameObject resumeButton;
    public GameObject quitButton;

    public Image fadeImage;
    public Color fadeColour = Color.black;

    private bool liveGame = false;
    public bool paused = true;
    private bool inCoroutine = false;

    public float fadeSpeed = 1;

    // Start is called before the first frame update
    void Start()
    {
        fadeImage.color = CreateFadeColour(1);
        fadeImage.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (!inCoroutine && Input.GetKeyDown(KeyCode.Escape))
        {
            if (!paused)
            {
                Pause();
            }
            else if(controllerInstance != null)
            {
                Resume();
            }
        }
            

        startButton.SetActive(!liveGame && paused);
        restartButton.SetActive(liveGame && paused);
        resumeButton.SetActive(controllerInstance != null && paused);

#if UNITY_WEBGL
        quitButton.SetActive(false);
#else
        quitButton.SetActive(paused);
#endif
    }

    public Color CreateFadeColour(float alpha)
    {
        Color c = fadeColour;
        c.a = alpha;
        return c;
    }

    public void StartNewGame()
    {
        StartCoroutine(StartGame());
    }

    public void EndCurrentGame()
    {
        StartCoroutine(EndGame());
    }

    private IEnumerator StartGame()
    {
        inCoroutine = true;
        if (controllerInstance != null)
        {
            Destroy(controllerInstance.gameObject);
        }

        controllerInstance = Instantiate<Controller>(controllerPrefab);
        controllerInstance.game = this;
        fadeImage.color = CreateFadeColour(1);
        fadeImage.gameObject.SetActive(true);

        liveGame = true;
        paused = false;

        float alpha = 1;

        while (alpha > 0)
        {
            fadeImage.color = CreateFadeColour(alpha);
            alpha -= fadeSpeed * Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        fadeImage.gameObject.SetActive(false);
        inCoroutine = false;
    }

    private IEnumerator EndGame()
    {
        if (controllerInstance != null)
            controllerInstance.enabled = false;
        inCoroutine = true;
        fadeImage.color = CreateFadeColour(0);
        fadeImage.gameObject.SetActive(true);

        float alpha = 0;

        while (alpha < 1)
        {
            fadeImage.color = CreateFadeColour(alpha);
            alpha += fadeSpeed*.5f * Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        if (controllerInstance != null)
            Destroy(controllerInstance.gameObject);
        controllerInstance = null;
        paused = true;
        inCoroutine = false;
        StartNewGame();
    }

    private IEnumerator PauseFade()
    {
        inCoroutine = true;
        fadeImage.color = CreateFadeColour(0);
        fadeImage.gameObject.SetActive(true);

        float alpha = 0;

        while (alpha < 1)
        {
            fadeImage.color = CreateFadeColour(alpha);
            alpha += fadeSpeed * Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        paused = true;
        inCoroutine = false;
    }

    private IEnumerator ResumeFade()
    {
        inCoroutine = true;
        fadeImage.color = CreateFadeColour(1);
        fadeImage.gameObject.SetActive(true);

        paused = false;

        float alpha = 1;

        while (alpha > 0)
        {
            fadeImage.color = CreateFadeColour(alpha);
            alpha -= fadeSpeed * Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        fadeImage.gameObject.SetActive(false);
        inCoroutine = false;
    }

    public void Resume()
    {
        StartCoroutine(ResumeFade());
    }

    public void Pause()
    {
        StartCoroutine(PauseFade());
    }

    public void Quit()
    {
        Application.Quit();
    }
}
