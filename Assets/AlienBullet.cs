using UnityEngine;

public class AlienBullet : MonoBehaviour
{
    // The arcade-style zigzag "lightning bolt", in two poses that alternate as it falls.
    static readonly string[][] shapes =
    {
        new[] { ".X.", "X..", ".X.", "..X", ".X.", "X..", ".X." },
        new[] { ".X.", "..X", ".X.", "X..", ".X.", "..X", ".X." },
    };
    static Material material;

    [Header("Inscribed")]
    public float speed = 8f;
    public float bottomY = -12f;
    public float pixelSize = .12f;
    // Bright and unlit so the bullets stand out against the dark blue background.
    public Color color = new Color(1f, .12f, .08f);
    public float poseInterval = .08f;

    Rigidbody rb;
    MeshFilter meshFilter;
    int pose = 0;
    float poseTimer = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meshFilter = GetComponent<MeshFilter>();

        // Replace the prefab's grey pellet with the red voxel bolt.
        transform.localScale = Vector3.one;
        if (material == null)
        {
            material = new Material(Resources.Load<Material>("BackgroundUnlit")) { name = "AlienBullet_Red" };
        }
        material.color = color;
        GetComponent<Renderer>().sharedMaterial = material;
        meshFilter.sharedMesh = VoxelModel.Build(shapes[0], 'X', pixelSize, pixelSize);

        SphereCollider sphere = GetComponent<SphereCollider>();
        sphere.enabled = false;
        Destroy(sphere);
        gameObject.AddComponent<BoxCollider>().isTrigger = true;   // sizes itself to the new mesh
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + Vector3.down * speed * Time.fixedDeltaTime);
        if (rb.position.y < bottomY)
        {
            Destroy(gameObject);
        }

        poseTimer += Time.fixedDeltaTime;
        if (poseTimer >= poseInterval)
        {
            poseTimer = 0;
            pose = 1 - pose;
            meshFilter.sharedMesh = VoxelModel.Build(shapes[pose], 'X', pixelSize, pixelSize);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Spaceship ship = other.GetComponentInParent<Spaceship>();
        if (ship != null)
        {
            ship.Hit();
            Destroy(gameObject);
            return;
        }

        // The player's darts can shoot down alien bullets.
        Dart dart = other.GetComponentInParent<Dart>();
        if (dart != null && !dart.spent)
        {
            dart.spent = true;
            Explosion.Spawn(transform.position, GetComponent<Renderer>().sharedMaterial.color, .4f);
            Destroy(dart.gameObject);
            Destroy(gameObject);
        }
    }
}
