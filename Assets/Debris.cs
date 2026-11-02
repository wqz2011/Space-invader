using UnityEngine;

// One little cube from an explosion. It flies outward, slows down, tumbles, and shrinks until it's gone.
public class Debris : MonoBehaviour
{
    Vector3 velocity;
    Vector3 spin;
    Vector3 startScale;
    float lifetime;
    float age = 0;

    public static void Spawn(Vector3 position, Material material, float size)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = "Debris";
        Destroy(piece.GetComponent<Collider>());   // just for looks, it shouldn't hit anything
        piece.GetComponent<Renderer>().sharedMaterial = material;

        // Start near the center, at a random angle and size.
        piece.transform.position = position + Random.insideUnitSphere * .2f * size;
        piece.transform.rotation = Random.rotation;
        piece.transform.localScale = Vector3.one * Random.Range(.12f, .3f) * size;

        Debris debris = piece.AddComponent<Debris>();
        debris.velocity = Random.onUnitSphere * Random.Range(3f, 9f) * size;   // random direction and speed
        debris.spin = Random.insideUnitSphere * 500;                         // degrees per second
        debris.lifetime = Random.Range(.4f, .9f);
        debris.startScale = piece.transform.localScale;
    }

    void Update()
    {
        age += Time.deltaTime;

        transform.position += velocity * Time.deltaTime;
        velocity *= 1 - 3 * Time.deltaTime;   // slow down a bit every frame, like air drag
        transform.Rotate(spin * Time.deltaTime);

        // Shrink from full size to nothing over its lifetime.
        float lifeLeft = 1 - age / lifetime;
        transform.localScale = startScale * Mathf.Max(lifeLeft, 0);

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
