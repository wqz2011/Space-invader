using UnityEngine;
using UnityEngine.InputSystem;

public class Spaceship : MonoBehaviour
{
    // Pixel art for the ship: X = hull, C = cockpit, R = red wing lights, E = engine glow.
    static string[] shape =
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
    public float pixelSize = .14f;   // same as the aliens, so everything looks like the same pixel art
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

    // The time (Time.time) when each power-up runs out.
    float multiShotEndTime = 0;
    float speedBoostEndTime = 0;
    float rapidFireEndTime = 0;

    // The seconds last shown for each power-up, so the text only changes when a number changes.
    int lastMultiShotSeconds = -1;
    int lastSpeedBoostSeconds = -1;
    int lastRapidFireSeconds = -1;

    // How far the ship's model reaches to the left, right and up from its center.
    float leftSize;
    float rightSize;
    float noseHeight;

    Material engineMaterial;
    bool destroyed = false;

    // The new model is built in Awake (not Start), so it already exists when the fleet measures the ship in its Start.
    void Awake()
    {
        Material originalMaterial = GetOriginalMaterial();

        // Remove the prefab's old capsule parts. Hide them first, because Destroy only happens at the end of the frame.
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        engineMaterial = BuildModel(transform, originalMaterial);

        // The ship's hitbox. It works with the Rigidbody on the ship.
        transform.Find("Hull").gameObject.AddComponent<BoxCollider>();
    }

    // The material on the prefab's capsule parts. The new model's colors start from it.
    public Material GetOriginalMaterial()
    {
        return GetComponentInChildren<Renderer>(true).sharedMaterial;
    }

    // Builds the pixel-art ship's parts under "parent" and returns the engine material (so it can flicker).
    // EditModePreview also uses this to show the ship before the game runs.
    public Material BuildModel(Transform parent, Material originalMaterial)
    {
        AddModelPart(parent, "Hull", 'X', MakeMaterial(originalMaterial, hullTint));
        AddModelPart(parent, "Cockpit", 'C', MakeMaterial(originalMaterial, new Color(.25f, .85f, 1f)));
        AddModelPart(parent, "WingLights", 'R', MakeMaterial(originalMaterial, new Color(1f, .25f, .25f)));

        // Unlit, so the engines look like they're glowing no matter how they're lit.
        Material engines = MakeMaterial(Resources.Load<Material>("BackgroundUnlit"), new Color(1f, .6f, .15f));
        AddModelPart(parent, "Engines", 'E', engines);
        return engines;
    }

    Material MakeMaterial(Material original, Color color)
    {
        Material material = new Material(original);
        material.color = color;
        return material;
    }

    void AddModelPart(Transform parent, string name, char cell, Material material)
    {
        GameObject part = new GameObject(name);
        part.layer = gameObject.layer;   // keep the Spaceship layer, so the collision rules stay the same
        part.transform.SetParent(parent, false);
        part.AddComponent<MeshFilter>().sharedMesh = VoxelModel.Build(shape, cell, pixelSize, depth);
        part.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    void Start()
    {
        // Measure the new model, so we know how close to the screen edge the ship can go
        // and where the nose is (that's where the darts come out).
        Renderer[] parts = GetComponentsInChildren<Renderer>();
        Bounds bounds = parts[0].bounds;
        foreach (Renderer part in parts)
        {
            bounds.Encapsulate(part.bounds);
        }
        leftSize = transform.position.x - bounds.min.x;
        rightSize = bounds.max.x - transform.position.x;
        noseHeight = bounds.max.y - transform.position.y;
    }

    void Update()
    {
        // Make the engines flicker between orange and yellow.
        float flicker = Mathf.PerlinNoise(Time.unscaledTime * 12f, 0);
        engineMaterial.color = Color.Lerp(new Color(1f, .35f, .05f), new Color(1f, .85f, .3f), flicker);

        Keyboard keyboard = Keyboard.current;
        if (!GameManager.IsPlaying() || keyboard == null)
        {
            return;
        }

        Move(keyboard);
        Shoot(keyboard);
        UpdatePowerUpText();
    }

    void Move(Keyboard keyboard)
    {
        float input = 0;
        if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
        {
            input += 1;
        }
        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
        {
            input -= 1;
        }

        float currentSpeed = speed;
        if (Time.time < speedBoostEndTime)
        {
            currentSpeed = speed * speedBoostMultiplier;
        }

        Vector3 position = transform.position;
        position.x += input * currentSpeed * Time.deltaTime;

        // Keep the whole ship on the screen.
        Camera cam = Camera.main;
        float halfScreenWidth = cam.orthographicSize * cam.aspect;
        float leftLimit = cam.transform.position.x - halfScreenWidth + leftSize;
        float rightLimit = cam.transform.position.x + halfScreenWidth - rightSize;
        position.x = Mathf.Clamp(position.x, leftLimit, rightLimit);

        transform.position = position;
    }

    void Shoot(Keyboard keyboard)
    {
        // Normally you press Space for each shot. With Rapid Fire you can just hold it down.
        bool wantsToShoot;
        float cooldown;
        if (Time.time < rapidFireEndTime)
        {
            wantsToShoot = keyboard.spaceKey.isPressed;
            cooldown = rapidFireCooldown;
        }
        else
        {
            wantsToShoot = keyboard.spaceKey.wasPressedThisFrame;
            cooldown = shotCooldown;
        }

        if (!wantsToShoot || Time.time - lastShot < cooldown)
        {
            return;
        }
        lastShot = Time.time;

        Vector3 nose = transform.position + Vector3.up * noseHeight;
        Instantiate(dartPrefab, nose, Quaternion.identity);

        // Multi-shot adds two more darts, angled to the left and right.
        if (Time.time < multiShotEndTime)
        {
            Instantiate(dartPrefab, nose, Quaternion.Euler(0, 0, multiShotAngle));
            Instantiate(dartPrefab, nose, Quaternion.Euler(0, 0, -multiShotAngle));
        }
    }

    // Called by a power-up when the ship catches it.
    public void ApplyPowerUp(PowerUpType type, float duration)
    {
        if (type == PowerUpType.MultiShot)
        {
            multiShotEndTime = Time.time + duration;
        }
        else if (type == PowerUpType.SpeedBoost)
        {
            speedBoostEndTime = Time.time + duration;
        }
        else if (type == PowerUpType.RapidFire)
        {
            rapidFireEndTime = Time.time + duration;
        }
    }

    // Shows the active power-ups and their seconds left in the top-right corner.
    void UpdatePowerUpText()
    {
        int multiShotSeconds = SecondsLeft(multiShotEndTime);
        int speedBoostSeconds = SecondsLeft(speedBoostEndTime);
        int rapidFireSeconds = SecondsLeft(rapidFireEndTime);

        // Only rebuild the text when one of the numbers has changed, not every frame.
        if (multiShotSeconds == lastMultiShotSeconds && speedBoostSeconds == lastSpeedBoostSeconds && rapidFireSeconds == lastRapidFireSeconds)
        {
            return;
        }
        lastMultiShotSeconds = multiShotSeconds;
        lastSpeedBoostSeconds = speedBoostSeconds;
        lastRapidFireSeconds = rapidFireSeconds;

        string text = "";
        if (multiShotSeconds > 0)
        {
            text += PowerUp.GetLabel(PowerUpType.MultiShot) + "  " + multiShotSeconds + "s\n";
        }
        if (speedBoostSeconds > 0)
        {
            text += PowerUp.GetLabel(PowerUpType.SpeedBoost) + "  " + speedBoostSeconds + "s\n";
        }
        if (rapidFireSeconds > 0)
        {
            text += PowerUp.GetLabel(PowerUpType.RapidFire) + "  " + rapidFireSeconds + "s\n";
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPowerUpText(text);
        }
    }

    int SecondsLeft(float endTime)
    {
        int seconds = Mathf.CeilToInt(endTime - Time.time);
        if (seconds < 0)
        {
            seconds = 0;
        }
        return seconds;
    }

    // Called when an alien bullet (or an alien) hits the ship.
    public void Hit()
    {
        // The ship has several colliders, so it can be hit more than once in the same physics step.
        if (destroyed)
        {
            return;
        }
        destroyed = true;

        Explosion.Spawn(transform.position, hullTint, 2.5f);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPowerUpText("");
            GameManager.Instance.Lose("Your ship was destroyed!");
        }
        Destroy(gameObject);
    }
}
