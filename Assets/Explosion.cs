using UnityEngine;

// The explosion effect: a quick flash of light plus a bunch of little cubes (Debris) that fly out and shrink away.
public class Explosion : MonoBehaviour
{
    [Header("Inscribed")]
    public float flashTime = .25f;   // how long the flash of light takes to fade out
    public float lifetime = 1f;      // remove the explosion after this long (the debris is gone by then)

    Light flash;
    float startBrightness;
    Material debrisMaterial;
    float age = 0;

    // size: 1 for an alien, bigger for the ship, smaller for a bullet.
    public static void Spawn(Vector3 position, Color color, float size)
    {
        GameObject explosionObject = new GameObject("Explosion");
        explosionObject.transform.position = position;
        Explosion explosion = explosionObject.AddComponent<Explosion>();

        // The flash: a point light in the same color as whatever blew up.
        explosion.flash = explosionObject.AddComponent<Light>();
        explosion.flash.type = LightType.Point;
        explosion.flash.color = color;
        explosion.flash.range = 4 * size;
        explosion.startBrightness = 6 * size;
        explosion.flash.intensity = explosion.startBrightness;

        // All the debris shares one material. It's unlit, so the pieces are always bright.
        explosion.debrisMaterial = new Material(Resources.Load<Material>("BackgroundUnlit"));
        explosion.debrisMaterial.color = color;

        // Bigger explosions get more pieces.
        int pieces = Mathf.RoundToInt(20 * size);
        pieces = Mathf.Clamp(pieces, 8, 60);
        for (int i = 0; i < pieces; i++)
        {
            Debris.Spawn(position, explosion.debrisMaterial, size);
        }
    }

    void Update()
    {
        age += Time.deltaTime;

        // Fade the flash out.
        flash.intensity = Mathf.Lerp(startBrightness, 0, age / flashTime);

        if (age >= lifetime)
        {
            Destroy(debrisMaterial);   // materials made in code have to be cleaned up by hand
            Destroy(gameObject);
        }
    }
}
