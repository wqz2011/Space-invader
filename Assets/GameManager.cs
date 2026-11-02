using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public enum GameState { Title, Playing, Won, Lost }

// Runs the title screen, the win and game-over screens, and keeps track of what state the game is in.
public class GameManager : MonoBehaviour
{
    // Lets other scripts reach the GameManager with GameManager.Instance.
    public static GameManager Instance;

    // Set before reloading the scene for "play again", so we skip the title screen.
    static bool skipTitle = false;

    [Header("Inscribed")]
    public float endScreenDelay = 1.5f;   // seconds to watch the last explosion before the end screen

    [Header("Dynamic")]
    public GameState state = GameState.Title;

    GameObject overlay;              // the dark screen behind the title / end text
    TextMeshProUGUI overlayText;
    TextMeshProUGUI powerUpText;     // the active power-ups in the top-right corner
    float startTime;

    // Other scripts ask this before doing anything (like moving or shooting).
    public static bool IsPlaying()
    {
        // No GameManager in the scene? Then just let the game run.
        if (Instance == null)
        {
            return true;
        }
        return Instance.state == GameState.Playing;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Background.Create();
        BuildUI();

        if (skipTitle)
        {
            skipTitle = false;
            StartGame();
            return;
        }

        // Show the title screen. Time.timeScale = 0 freezes everything until the player starts.
        state = GameState.Title;
        Time.timeScale = 0;
        ShowOverlay(
            "<size=110><b>SPACE INVADERS <color=#00FA9A>3D</color></b></size>\n\n" +
            "<size=40>LEFT / RIGHT (or A / D) to move     SPACE to fire</size>\n\n" +
            "<size=34>Destroyed aliens sometimes drop power-ups:\n" +
            PowerUp.GetLabel(PowerUpType.MultiShot) + "     " +
            PowerUp.GetLabel(PowerUpType.SpeedBoost) + "     " +
            PowerUp.GetLabel(PowerUpType.RapidFire) + "</size>\n\n" +
            "<size=50>Press SPACE or ENTER to start</size>");
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool startPressed = keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame;

        if (state == GameState.Title && startPressed)
        {
            StartGame();
        }

        // On the win / game-over screen, start a new game.
        bool gameOver = state == GameState.Won || state == GameState.Lost;
        if (gameOver && overlay.activeSelf && (startPressed || keyboard.rKey.wasPressedThisFrame))
        {
            skipTitle = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Quit();
        }
    }

    void Quit()
    {
        // Application.Quit doesn't do anything inside the Unity editor, so there we stop Play mode instead.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void StartGame()
    {
        state = GameState.Playing;
        Time.timeScale = 1;
        startTime = Time.time;
        overlay.SetActive(false);
    }

    public void Win()
    {
        if (state != GameState.Playing)
        {
            return;
        }
        state = GameState.Won;

        int seconds = Mathf.FloorToInt(Time.time - startTime);
        string time = (seconds / 60) + ":" + (seconds % 60).ToString("00");
        StartCoroutine(ShowEndScreen(
            "<size=110><b><color=#00FA9A>YOU WIN!</color></b></size>\n\n" +
            "<size=44>Every alien destroyed in " + time + "</size>"));
    }

    public void Lose(string reason)
    {
        if (state != GameState.Playing)
        {
            return;
        }
        state = GameState.Lost;

        string message = "<size=110><b><color=#FF4A4A>GAME OVER</color></b></size>\n\n<size=44>" + reason + "</size>";
        AlienFleet fleet = FindAnyObjectByType<AlienFleet>();
        if (fleet != null)
        {
            message += "\n<size=36>Aliens remaining: " + fleet.aliens.Count + "</size>";
        }
        StartCoroutine(ShowEndScreen(message));
    }

    public void SetPowerUpText(string text)
    {
        powerUpText.text = text;
    }

    // Waits a moment so the last explosion can play, then freezes the game and shows the end screen.
    IEnumerator ShowEndScreen(string message)
    {
        // "Realtime", because this should still count seconds even if the game is frozen.
        yield return new WaitForSecondsRealtime(endScreenDelay);
        Time.timeScale = 0;
        ShowOverlay(message + "\n\n<size=50>Press SPACE or ENTER to play again</size>\n<size=30>ESC to quit</size>");
    }

    void ShowOverlay(string message)
    {
        overlayText.text = message;
        overlay.SetActive(true);
    }

    // Makes the title / end screen and the power-up text, inside the Canvas that's already in the scene.
    void BuildUI()
    {
        Transform canvas = FindAnyObjectByType<Canvas>().transform;

        // A see-through black panel that covers the whole screen.
        // UI objects use a RectTransform instead of a normal Transform.
        overlay = new GameObject("Overlay", typeof(RectTransform));
        overlay.transform.SetParent(canvas, false);
        Image panel = overlay.AddComponent<Image>();
        panel.color = new Color(0, 0, 0, .75f);
        FillArea(panel.rectTransform, 0, 0, 1, 1);

        overlayText = MakeText("OverlayText", overlay.transform);
        overlayText.alignment = TextAlignmentOptions.Center;
        FillArea(overlayText.rectTransform, 0, 0, 1, 1);

        // Power-up timers in the top-right corner.
        powerUpText = MakeText("PowerUpText", canvas);
        powerUpText.alignment = TextAlignmentOptions.TopRight;
        powerUpText.fontSize = 32;
        FillArea(powerUpText.rectTransform, .5f, .8f, 1, 1);
        powerUpText.rectTransform.offsetMax = new Vector2(-20, -20);   // 20 pixels in from the corner
    }

    TextMeshProUGUI MakeText(string name, Transform parent)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        return text;
    }

    // Stretches a UI element over part of the screen. (0, 0) is the bottom-left, (1, 1) is the top-right.
    void FillArea(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
