using UnityEngine;

public class AlienBullet : MonoBehaviour
{
    // Pixel art for the zigzag "lightning bolt". It switches between the two poses as it falls.
    static string[] boltA = { ".X.", "X..", ".X.", "..X", ".X.", "X..", ".X." };
    static string[] boltB = { ".X.", "..X", ".X.", "X..", ".X.", "..X", ".X." };

    // Every bullet looks the same, so they all share these instead of each building their own.
    // (static = one copy for the whole class, made by the first bullet that needs it)
    static Mesh meshA;
    static Mesh meshB;
    static Material redMaterial;

    [Header("Inscribed")]
    public float speed = 8f;
    public float bottomY = -12f;
    public float pixelSize = .12f;
    public Color color = new Color(1f, .12f, .08f);
    public float poseTime = .08f;   // seconds between wiggles

    Rigidbody rb;
    MeshFilter meshFilter;
    bool showingPoseA = true;
    float poseTimer = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meshFilter = GetComponent<MeshFilter>();

        if (meshA == null)
        {
            meshA = VoxelModel.Build(boltA, 'X', pixelSize, pixelSize);
            meshB = VoxelModel.Build(boltB, 'X', pixelSize, pixelSize);
        }
        if (redMaterial == null)
        {
            // Unlit, so the bullets stay bright red against the dark background no matter how they're lit.
            redMaterial = new Material(Resources.Load<Material>("BackgroundUnlit"));
        }
        redMaterial.color = color;

        // Replace the prefab's grey ball with the red bolt.
        transform.localScale = Vector3.one;
        meshFilter.sharedMesh = meshA;
        GetComponent<Renderer>().sharedMaterial = redMaterial;

        // Swap the ball-shaped collider for a box that fits the bolt.
        Destroy(GetComponent<SphereCollider>());
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
    }

    void FixedUpdate()
    {
        // Fall straight down.
        rb.MovePosition(rb.position + Vector3.down * speed * Time.fixedDeltaTime);
        if (rb.position.y < bottomY)
        {
            Destroy(gameObject);
        }

        // Wiggle: switch to the other pose every poseTime seconds.
        poseTimer += Time.fixedDeltaTime;
        if (poseTimer >= poseTime)
        {
            poseTimer = 0;
            showingPoseA = !showingPoseA;
            if (showingPoseA)
            {
                meshFilter.sharedMesh = meshA;
            }
            else
            {
                meshFilter.sharedMesh = meshB;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Hit the player's ship?
        Spaceship ship = other.GetComponentInParent<Spaceship>();
        if (ship != null)
        {
            ship.Hit();
            Destroy(gameObject);
            return;
        }

        // Shot down by one of the player's darts?
        Dart dart = other.GetComponentInParent<Dart>();
        if (dart != null && !dart.spent)
        {
            dart.spent = true;
            Explosion.Spawn(transform.position, color, .4f);
            Destroy(dart.gameObject);
            Destroy(gameObject);
        }
    }
}
