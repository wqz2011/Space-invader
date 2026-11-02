using UnityEngine;
using TMPro;

public enum PowerUpType { MultiShot, SpeedBoost, RapidFire }

// A spinning crate that a destroyed alien sometimes drops. Catch it with the ship for a few seconds of a power-up.
public class PowerUp : MonoBehaviour
{
    [Header("Inscribed")]
    public float fallSpeed = 3f;
    public float spinSpeed = 180f;
    public float duration = 8f;
    public float bottomY = -12f;

    [Header("Dynamic")]
    public PowerUpType type;

    Rigidbody rb;
    Transform crate;
    bool collected = false;

    public static Color GetColor(PowerUpType type)
    {
        if (type == PowerUpType.MultiShot)
        {
            return new Color(1f, .84f, .29f);    // yellow
        }
        else if (type == PowerUpType.SpeedBoost)
        {
            return new Color(.29f, .89f, 1f);    // light blue
        }
        else
        {
            return new Color(1f, .42f, .84f);    // pink
        }
    }

    // The letter printed on the crate.
    public static string GetLetter(PowerUpType type)
    {
        if (type == PowerUpType.MultiShot)
        {
            return "M";
        }
        else if (type == PowerUpType.SpeedBoost)
        {
            return "S";
        }
        else
        {
            return "R";
        }
    }

    public static string GetName(PowerUpType type)
    {
        if (type == PowerUpType.MultiShot)
        {
            return "Multi-shot";
        }
        else if (type == PowerUpType.SpeedBoost)
        {
            return "Speed boost";
        }
        else
        {
            return "Rapid fire";
        }
    }

    // A colored label like "[M] Multi-shot" for the title screen and the power-up timers.
    // (<color=#...> is TextMeshPro's way of coloring part of a text.)
    public static string GetLabel(PowerUpType type)
    {
        string colorCode = ColorUtility.ToHtmlStringRGB(GetColor(type));
        return "<color=#" + colorCode + ">[" + GetLetter(type) + "] " + GetName(type) + "</color>";
    }

    // Creates a power-up crate at the given position.
    public static void Spawn(Vector3 position, PowerUpType type)
    {
        // The main object: an invisible box that falls and checks if it touches the ship.
        GameObject powerUpObject = new GameObject("PowerUp_" + type);
        powerUpObject.transform.position = position;
        BoxCollider box = powerUpObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        Rigidbody body = powerUpObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        // The colored crate you see. It spins on its own, so the letter in front of it stays readable.
        GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(crate.GetComponent<Collider>());
        crate.name = "Crate";
        crate.transform.SetParent(powerUpObject.transform, false);
        crate.transform.localScale = new Vector3(.7f, .7f, .7f);
        crate.GetComponent<Renderer>().material.color = GetColor(type);

        // The letter in front of the crate.
        GameObject letter = new GameObject("Letter", typeof(RectTransform));   // text needs a RectTransform
        letter.transform.SetParent(powerUpObject.transform, false);
        letter.transform.localPosition = new Vector3(0, 0, -.6f);
        TextMeshPro text = letter.AddComponent<TextMeshPro>();
        text.text = GetLetter(type);
        text.fontSize = 5;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.rectTransform.sizeDelta = new Vector2(1, 1);

        PowerUp powerUp = powerUpObject.AddComponent<PowerUp>();
        powerUp.type = type;
        powerUp.crate = crate.transform;
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
            Destroy(gameObject);   // fell off the bottom of the screen
        }
    }

    void Update()
    {
        crate.Rotate(new Vector3(1, 1, 0) * spinSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        Spaceship ship = other.GetComponentInParent<Spaceship>();
        if (ship == null || collected)
        {
            return;
        }
        collected = true;   // the ship has several colliders, so only collect once

        ship.ApplyPowerUp(type, duration);
        Explosion.Spawn(transform.position, GetColor(type), .6f);
        Destroy(gameObject);
    }
}
