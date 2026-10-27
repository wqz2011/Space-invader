using UnityEngine;
using UnityEngine.InputSystem;

public class Spaceship : MonoBehaviour
{
    // Pixel-art fighter: X = hull, C = cockpit, R = wing lights, E = engine glow.
    static readonly string[] shape =
    {
        "......X......",
        ".....XCX.....",
        ".....XCX.....",
        "....XXCXX....",
        ".R..XXXXX..R.",
        ".X.XXXXXXX.X.",
        "XXXXXXXXXXXXX",
        "XRXXXXXXXXXRX",
        "XX..XX.XX..XX",
        "....EE.EE....",
    };

    [Header("Inscribed")]
    public GameObject dartPrefab;
    // Same pixel size as the aliens, so everything shares one pixel-art scale.
    public float pixelSize = .14f;
    public float depth = .35f;
    public Color hullTint = new Color(.85f, .88f, .95f);

    public float speed = 10f;
    public float shotCooldown = .35f;

    [Header("Power-ups")]
    public float speedBoostMultiplier = 1.7f;
    public float rapidFireCooldown = .08f;
    public float multiShotAngle = 12f;

    [Header("Dynamic")]
    public float lastShot = -1;

    float multiShotUntil, speedBoostUntil, rapidFireUntil;
    int lastBuffDisplay = -1;

    // How far the ship's model reaches left, right and up from its pivot.
    float leftExtent, rightExtent, noseOffset;
    Color hullColor = Color.white;
    bool destroyed = false;
    Material engineMaterial;

    // Built in Awake so the fleet sees the new model when it measures the ship in its Start.
    void Awake()
    {
        Material prefabHull = PrefabHullMaterial;

        // Remove the old capsule parts. Deactivate them first because Destroy only happens at the end of the frame.
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        engineMaterial = BuildModel(transform, prefabHull);
        // The ship's hitbox; uses the kinematic Rigidbody on the root.
        transform.Find("Hull").gameObject.AddComponent<BoxCollider>();
    }

    // The material on the prefab's original capsule parts, used as the base for the voxel parts' materials.
    public Material PrefabHullMaterial => GetComponentInChildren<Renderer>(true).sharedMaterial;

    // Builds the voxel fighter's parts under `parent` and returns the engine material (so it can flicker).
    // Also used by EditModePreview to show the ship before the game runs.
    public Material BuildModel(Transform parent, Material baseMaterial)
    {
        AddModelPart(parent, "Hull", 'X', new Material(baseMaterial) { color = hullTint });
        AddModelPart(parent, "Cockpit", 'C', new Material(baseMaterial) { color = new Color(.25f, .85f, 1f) });
        AddModelPart(parent, "WingLights", 'R', new Material(baseMaterial) { color = new Color(1f, .25f, .25f) });

        // Unlit so the engines look like they glow regardless of the scene lighting.
        Material engines = new Material(Resources.Load<Material>("BackgroundUnlit")) { color = new Color(1f, .6f, .15f) };
        AddModelPart(parent, "Engines", 'E', engines);
        return engines;
    }

    void AddModelPart(Transform parent, string name, char cell, Material material)
    {
        GameObject part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        part.layer = gameObject.layer;   // keeps the Spaceship layer's collision rules
        part.transform.SetParent(parent, false);
        part.GetComponent<MeshFilter>().sharedMesh = VoxelModel.Build(shape, cell, pixelSize, depth);
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    void Start()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }
        leftExtent = transform.position.x - bounds.min.x;
        rightExtent = bounds.max.x - transform.position.x;
        noseOffset = bounds.max.y - transform.position.y;
        hullColor = renderers[0].sharedMaterial.color;
    }

    void Update()
    {
        // Flickering engine glow, between orange and yellow.
        float flicker = Mathf.PerlinNoise(Time.unscaledTime * 12f, 0);
        engineMaterial.color = Color.Lerp(new Color(1f, .35f, .05f), new Color(1f, .85f, .3f), flicker);

        Keyboard keyboard = Keyboard.current;
        if (!GameManager.IsPlaying || keyboard == null)
        {
            return;
        }

        float input = 0;
        if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
        {
            input += 1;
        }
        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
        {
            input -= 1;
        }

        float currentSpeed = Time.time < speedBoostUntil ? speed * speedBoostMultiplier : speed;
        Vector3 position = transform.position;
        position.x += input * currentSpeed * Time.deltaTime;
        position.x = ClampToScreen(position.x);
        transform.position = position;

        // Rapid fire lets the player simply hold the fire button.
        bool rapidFire = Time.time < rapidFireUntil;
        bool firePressed = rapidFire ? keyboard.spaceKey.isPressed : keyboard.spaceKey.wasPressedThisFrame;
        float cooldown = rapidFire ? rapidFireCooldown : shotCooldown;
        if (firePressed && Time.time - lastShot > cooldown)
        {
            Fire();
        }

        UpdateBuffDisplay();
    }

    float ClampToScreen(float x)
    {
        Camera cam = Camera.main;
        float halfWidth = cam.orthographicSize * cam.aspect;
        float left = cam.transform.position.x - halfWidth + leftExtent;
        float right = cam.transform.position.x + halfWidth - rightExtent;
        return Mathf.Clamp(x, left, right);
    }

    void Fire()
    {
        lastShot = Time.time;
        Vector3 nose = transform.position + Vector3.up * noseOffset;
        Instantiate(dartPrefab, nose, Quaternion.identity);
        if (Time.time < multiShotUntil)
        {
            Instantiate(dartPrefab, nose, Quaternion.Euler(0, 0, multiShotAngle));
            Instantiate(dartPrefab, nose, Quaternion.Euler(0, 0, -multiShotAngle));
        }
    }

    public void ApplyPowerUp(PowerUpType type, float duration)
    {
        switch (type)
        {
            case PowerUpType.MultiShot:
                multiShotUntil = Time.time + duration;
                break;
            case PowerUpType.SpeedBoost:
                speedBoostUntil = Time.time + duration;
                break;
            case PowerUpType.RapidFire:
                rapidFireUntil = Time.time + duration;
                break;
        }
    }

    // Only rebuilds the HUD string when one of the displayed whole-second timers changes.
    void UpdateBuffDisplay()
    {
        int multi = SecondsLeft(multiShotUntil), boost = SecondsLeft(speedBoostUntil), rapid = SecondsLeft(rapidFireUntil);
        int display = multi * 10000 + boost * 100 + rapid;
        if (display == lastBuffDisplay || GameManager.Instance == null)
        {
            return;
        }
        lastBuffDisplay = display;

        string text = "";
        if (multi > 0) text += PowerUp.Describe(PowerUpType.MultiShot) + "  " + multi + "s\n";
        if (boost > 0) text += PowerUp.Describe(PowerUpType.SpeedBoost) + "  " + boost + "s\n";
        if (rapid > 0) text += PowerUp.Describe(PowerUpType.RapidFire) + "  " + rapid + "s\n";
        GameManager.Instance.SetBuffText(text);
    }

    int SecondsLeft(float until)
    {
        return Mathf.Clamp(Mathf.CeilToInt(until - Time.time), 0, 99);
    }

    public void Hit()
    {
        // The ship is made of several colliders, so it can be "hit" more than once in the same step.
        if (destroyed)
        {
            return;
        }
        destroyed = true;
        Explosion.Spawn(transform.position, hullColor, 2.5f);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetBuffText("");
            GameManager.Instance.Lose("Your ship was destroyed!");
        }
        Destroy(gameObject);
    }
}
