using UnityEngine;
using TMPro;

public enum PowerUpType { MultiShot, SpeedBoost, RapidFire }

// A spinning crate dropped by destroyed aliens. Catch it with the ship for a temporary buff.
public class PowerUp : MonoBehaviour
{
    static readonly Color[] colors =
    {
        new Color(1f, .84f, .29f),   // MultiShot
        new Color(.29f, .89f, 1f),   // SpeedBoost
        new Color(1f, .42f, .84f),   // RapidFire
    };
    static readonly string[] labels = { "M", "S", "R" };
    static readonly string[] names = { "Multi-shot", "Speed boost", "Rapid fire" };

    [Header("Inscribed")]
    public float fallSpeed = 3f;
    public float spinSpeed = 180f;
    public float duration = 8f;
    public float bottomY = -12f;

    [Header("Dynamic")]
    public PowerUpType type;

    Rigidbody rb;
    Transform body;
    bool collected = false;

    public static string Describe(PowerUpType type)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(colors[(int)type]) + ">[" + labels[(int)type] + "] " + names[(int)type] + "</color>";
    }

    public static PowerUp Spawn(Vector3 position, PowerUpType type)
    {
        GameObject root = new GameObject("PowerUp_" + type);
        root.transform.position = position;
        root.AddComponent<BoxCollider>().isTrigger = true;
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(body.GetComponent<Collider>());
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = Vector3.one * .7f;
        body.GetComponent<Renderer>().material.color = colors[(int)type];

        GameObject label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = new Vector3(0, 0, -.6f);
        TextMeshPro text = label.AddComponent<TextMeshPro>();
        text.text = labels[(int)type];
        text.fontSize = 5;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.rectTransform.sizeDelta = new Vector2(1, 1);

        PowerUp powerUp = root.AddComponent<PowerUp>();
        powerUp.type = type;
        powerUp.body = body.transform;
        return powerUp;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + Vector3.down * fallSpeed * Time.fixedDeltaTime);
        if (rb.position.y < bottomY)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        body.Rotate(new Vector3(1, 1, 0) * spinSpeed * Time.deltaTime, Space.Self);
    }

    void OnTriggerEnter(Collider other)
    {
        Spaceship ship = other.GetComponentInParent<Spaceship>();
        if (ship == null || collected)
        {
            return;
        }
        collected = true;
        ship.ApplyPowerUp(type, duration);
        Explosion.Spawn(transform.position, colors[(int)type], .6f);
        Destroy(gameObject);
    }
}
