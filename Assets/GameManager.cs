using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public enum GameState { Title, Playing, Won, Lost }

// Owns the title / win / lose screens and the overall flow of a round.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // True when there is no GameManager in the scene, so the gameplay scripts still work on their own.
    public static bool IsPlaying => Instance == null || Instance.state == GameState.Playing;

    // Set before reloading the scene so "play again" skips the title screen.
    static bool skipTitle = false;

    [Header("Inscribed")]
    public float endScreenDelay = 1.5f;

    [Header("Dynamic")]
    public GameState state = GameState.Title;

    GameObject overlay;
    TextMeshProUGUI overlayText;
    TextMeshProUGUI buffText;
    float roundStartTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Starfield.Create();
        BuildUI();

        if (skipTitle)
        {
            skipTitle = false;
            StartGame();
        }
        else
        {
            state = GameState.Title;
            Time.timeScale = 0;
            ShowOverlay(
                "<size=110><b>SPACE INVADERS <color=#00FA9A>3D</color></b></size>\n\n" +
                "<size=40>LEFT / RIGHT (or A / D) to move     SPACE to fire</size>\n\n" +
                "<size=34>Destroyed aliens sometimes drop power-ups:\n" +
                PowerUp.Describe(PowerUpType.MultiShot) + "     " +
                PowerUp.Describe(PowerUpType.SpeedBoost) + "     " +
                PowerUp.Describe(PowerUpType.RapidFire) + "</size>\n\n" +
                "<size=50>Press SPACE or ENTER to start</size>");
        }
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool confirm = keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame;
        if (state == GameState.Title && confirm)
        {
            StartGame();
        }
        else if ((state == GameState.Won || state == GameState.Lost) && overlay.activeSelf
                 && (confirm || keyboard.rKey.wasPressedThisFrame))
        {
            skipTitle = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Application.Quit();
        }
    }

    void StartGame()
    {
        state = GameState.Playing;
        Time.timeScale = 1;
        roundStartTime = Time.time;
        overlay.SetActive(false);
    }

    public void Win()
    {
        if (state != GameState.Playing)
        {
            return;
        }
        state = GameState.Won;
        StartCoroutine(ShowEndScreen(
            "<size=110><b><color=#00FA9A>YOU WIN!</color></b></size>\n\n" +
            "<size=44>Every alien destroyed in " + FormatTime(Time.time - roundStartTime) + "</size>"));
    }

    public void Lose(string reason)
    {
        if (state != GameState.Playing)
        {
            return;
        }
        state = GameState.Lost;
        AlienFleet fleet = FindAnyObjectByType<AlienFleet>();
        string remaining = fleet != null ? "\n<size=36>Aliens remaining: " + fleet.aliens.Count + "</size>" : "";
        StartCoroutine(ShowEndScreen(
            "<size=110><b><color=#FF4A4A>GAME OVER</color></b></size>\n\n" +
            "<size=44>" + reason + "</size>" + remaining));
    }

    public void SetBuffText(string text)
    {
        buffText.text = text;
    }

    // Lets the final explosion play before freezing the game behind the end screen.
    IEnumerator ShowEndScreen(string message)
    {
        yield return new WaitForSecondsRealtime(endScreenDelay);
        Time.timeScale = 0;
        ShowOverlay(message + "\n\n<size=50>Press SPACE or ENTER to play again</size>\n<size=30>ESC to quit</size>");
    }

    void ShowOverlay(string message)
    {
        overlayText.text = message;
        overlay.SetActive(true);
    }

    static string FormatTime(float seconds)
    {
        int whole = Mathf.FloorToInt(seconds);
        return (whole / 60) + ":" + (whole % 60).ToString("00");
    }

    void BuildUI()
    {
        Transform canvas = FindAnyObjectByType<Canvas>().transform;

        overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas, false);
        Stretch(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        overlay.GetComponent<Image>().color = new Color(0, 0, 0, .75f);

        overlayText = CreateText("OverlayText", overlay.transform, TextAlignmentOptions.Center);
        Stretch(overlayText.rectTransform, Vector2.zero, Vector2.one);

        buffText = CreateText("PowerUpText", canvas, TextAlignmentOptions.TopRight);
        buffText.fontSize = 32;
        Stretch(buffText.rectTransform, new Vector2(.5f, .8f), Vector2.one);
        buffText.rectTransform.offsetMax = new Vector2(-20, -20);
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
