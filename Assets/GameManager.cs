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

    // Arcade colors, the same ones used by the aliens and power-ups. (TextMeshPro color codes.)
    const string Green = "#00FA9A";
    const string Pink = "#FF59CC";
    const string Blue = "#4DCCFF";
    const string Yellow = "#FFD54A";
    const string Red = "#FF4A4A";

    [Header("Inscribed")]
    public float endScreenDelay = 1.5f;   // seconds to watch the last explosion before the end screen
    public float blinkSpeed = 2f;         // how many times per second "PRESS SPACE" blinks

    [Header("Dynamic")]
    public GameState state = GameState.Title;

    GameObject screen;               // the framed box for the title / end screens
    Image frame;                     // the colored border around the box
    TextMeshProUGUI screenText;      // the big text in the box
    TextMeshProUGUI promptText;      // the blinking "PRESS SPACE" line
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

        string text =
            "<size=260%>" + Colored("SPACE INVADERS", Green) + "</size>\n" +
            "<size=200%>" + Colored("3D", Pink) + "</size>\n\n" +
            Colored("MOVE", Blue) + "   LEFT / RIGHT  OR  A / D\n" +
            Colored("FIRE", Blue) + "   SPACE\n" +
            Colored("QUIT", Blue) + "   ESC\n\n" +
            "POWER-UPS\n" +
            PowerUp.GetLabel(PowerUpType.MultiShot) + "    " +
            PowerUp.GetLabel(PowerUpType.SpeedBoost) + "    " +
            PowerUp.GetLabel(PowerUpType.RapidFire);
        ShowScreen(text, "PRESS SPACE TO START", Green);
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // Blink "PRESS SPACE": visible half the time, hidden the other half.
        // (unscaledTime keeps counting even while the game is frozen.)
        if (screen.activeSelf)
        {
            int blinks = Mathf.FloorToInt(Time.unscaledTime * blinkSpeed);
            promptText.enabled = blinks % 2 == 0;
        }

        bool startPressed = keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame;

        if (state == GameState.Title && startPressed)
        {
            StartGame();
        }

        // On the win / game-over screen, start a new game.
        bool gameOver = state == GameState.Won || state == GameState.Lost;
        if (gameOver && screen.activeSelf && (startPressed || keyboard.rKey.wasPressedThisFrame))
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
        screen.SetActive(false);
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
        string text =
            "<size=260%>" + Colored("YOU WIN!", Green) + "</size>\n\n" +
            "ALL ALIENS DESTROYED\n\n" +
            Colored("TIME", Blue) + "   " + time;
        StartCoroutine(ShowEndScreen(text, Green));
    }

    public void Lose(string reason)
    {
        if (state != GameState.Playing)
        {
            return;
        }
        state = GameState.Lost;

        string text =
            "<size=260%>" + Colored("GAME OVER", Red) + "</size>\n\n" +
            reason;
        AlienFleet fleet = FindAnyObjectByType<AlienFleet>();
        if (fleet != null)
        {
            text += "\n\n" + Colored("ALIENS LEFT", Blue) + "   " + fleet.aliens.Count;
        }
        StartCoroutine(ShowEndScreen(text, Red));
    }

    public void SetPowerUpText(string text)
    {
        powerUpText.text = text;
    }

    // Waits a moment so the last explosion can play, then freezes the game and shows the end screen.
    IEnumerator ShowEndScreen(string text, string frameColor)
    {
        // "Realtime", because this should still count seconds even if the game is frozen.
        yield return new WaitForSecondsRealtime(endScreenDelay);
        Time.timeScale = 0;
        ShowScreen(text + "\n\n<size=70%>ESC TO QUIT</size>", "PRESS SPACE TO PLAY AGAIN", frameColor);
    }

    void ShowScreen(string text, string prompt, string frameColor)
    {
        screenText.text = text;
        promptText.text = Colored(prompt, Yellow);
        Color color;
        ColorUtility.TryParseHtmlString(frameColor, out color);
        frame.color = color;
        screen.SetActive(true);
    }

    // Wraps text in a TextMeshPro color tag, e.g. Colored("FIRE", Blue).
    string Colored(string text, string color)
    {
        return "<color=" + color + ">" + text + "</color>";
    }

    // Makes the title / end screen box and the power-up text, inside the Canvas that's already in the scene.
    void BuildUI()
    {
        Transform canvas = FindAnyObjectByType<Canvas>().transform;

        // The box: a colored square (the frame) with a slightly smaller black square on top,
        // so only a thick border of color shows around the edge, like an old arcade screen.
        // UI objects use a RectTransform instead of a normal Transform.
        screen = new GameObject("Screen", typeof(RectTransform));
        screen.transform.SetParent(canvas, false);
        frame = screen.AddComponent<Image>();
        FillArea(frame.rectTransform, .12f, .1f, .88f, .9f);

        GameObject inside = new GameObject("Inside", typeof(RectTransform));
        inside.transform.SetParent(screen.transform, false);
        Image insideImage = inside.AddComponent<Image>();
        insideImage.color = new Color(0, 0, 0, .88f);
        FillArea(insideImage.rectTransform, 0, 0, 1, 1);
        Shrink(insideImage.rectTransform, 10);   // 10-pixel thick frame

        // The main text fills the box, except the bottom part where "PRESS SPACE" goes.
        screenText = MakeText("ScreenText", inside.transform, 40);
        FillArea(screenText.rectTransform, 0, .18f, 1, 1);
        Shrink(screenText.rectTransform, 30);

        promptText = MakeText("PromptText", inside.transform, 44);
        FillArea(promptText.rectTransform, 0, 0, 1, .18f);
        Shrink(promptText.rectTransform, 20);

        // Power-up timers in the top-right corner of the game.
        powerUpText = MakeText("PowerUpText", canvas, 32);
        powerUpText.alignment = TextAlignmentOptions.TopRight;
        powerUpText.enableAutoSizing = false;
        FillArea(powerUpText.rectTransform, .5f, .8f, 1, 1);
        powerUpText.rectTransform.offsetMax = new Vector2(-20, -20);   // 20 pixels in from the corner
    }

    TextMeshProUGUI MakeText(string name, Transform parent, float size)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;

        // Arcade look: all capital letters, spread out.
        text.fontStyle = FontStyles.UpperCase;
        text.characterSpacing = 8;

        // Shrink the text if it doesn't fit (for small game windows), but never grow past "size".
        text.enableAutoSizing = true;
        text.fontSizeMax = size;
        text.fontSizeMin = 8;
        text.fontSize = size;
        return text;
    }

    // Stretches a UI element over part of its parent. (0, 0) is the bottom-left, (1, 1) is the top-right.
    void FillArea(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Moves all four edges of a UI element inward by this many pixels.
    void Shrink(RectTransform rect, float pixels)
    {
        rect.offsetMin = new Vector2(pixels, pixels);
        rect.offsetMax = new Vector2(-pixels, -pixels);
    }
}
