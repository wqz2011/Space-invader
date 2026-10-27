using UnityEngine;

public enum AlienType { Squid, Crab, Octopus }

public class Alien : MonoBehaviour
{
    // Pixel-art shapes of the three classic invaders, two animation poses each ([type][pose]).
    static readonly string[][][] shapes =
    {
        new[] // Squid
        {
            new[] { "...XX...", "..XXXX..", ".XXXXXX.", "XX.XX.XX", "XXXXXXXX", "..X..X..", ".X.XX.X.", "X.X..X.X" },
            new[] { "...XX...", "..XXXX..", ".XXXXXX.", "XX.XX.XX", "XXXXXXXX", ".X.XX.X.", "X......X", ".X....X." },
        },
        new[] // Crab
        {
            new[] { "..X.....X..", "...X...X...", "..XXXXXXX..", ".XX.XXX.XX.", "XXXXXXXXXXX", "X.XXXXXXX.X", "X.X.....X.X", "...XX.XX..." },
            new[] { "..X.....X..", "X..X...X..X", "X.XXXXXXX.X", "XXX.XXX.XXX", "XXXXXXXXXXX", ".XXXXXXXXX.", "..X.....X..", ".X.......X." },
        },
        new[] // Octopus
        {
            new[] { "....XXXX....", ".XXXXXXXXXX.", "XXXXXXXXXXXX", "XXX..XX..XXX", "XXXXXXXXXXXX", "...XX..XX...", "..XX.XX.XX..", "XX........XX" },
            new[] { "....XXXX....", ".XXXXXXXXXX.", "XXXXXXXXXXXX", "XXX..XX..XXX", "XXXXXXXXXXXX", "..XXX..XXX..", ".XX..XX..XX.", "..XX....XX.." },
        },
    };
    static readonly Color[] colors =
    {
        new Color(1f, .35f, .8f),    // Squid
        new Color(0f, .98f, .6f),    // Crab (the original Alien_Skin green)
        new Color(.3f, .8f, 1f),     // Octopus
    };
    static Material[] materials = new Material[3];

    [Header("Inscribed")]
    public GameObject bulletPrefab;
    public float pixelSize = .14f;
    public float depth = .3f;

    [Header("Dynamic")]
    public AlienFleet fleet;
    public int column;
    public AlienType type;

    MeshFilter meshFilter;
    bool dead = false;

    // Swaps the prefab's sphere for a voxel invader of the given type.
    public void Init(AlienFleet fleet, int column, AlienType type)
    {
        this.fleet = fleet;
        this.column = column;
        this.type = type;
        meshFilter = GetComponent<MeshFilter>();

        Renderer alienRenderer = GetComponent<Renderer>();
        int t = (int)type;
        // Shared per type (not one copy per alien); rebuilt if a scene reload unloaded it.
        if (materials[t] == null)
        {
            materials[t] = new Material(alienRenderer.sharedMaterial) { name = "Alien_" + type, color = colors[t] };
        }
        alienRenderer.sharedMaterial = materials[t];
        SetPose(0);

        SphereCollider sphere = GetComponent<SphereCollider>();
        sphere.enabled = false;
        Destroy(sphere);
        gameObject.AddComponent<BoxCollider>().isTrigger = true;   // sizes itself to the new mesh
    }

    public void SetPose(int pose)
    {
        meshFilter.sharedMesh = MeshFor(type, pose);
    }

    // Also used by EditModePreview to show the fleet before the game runs.
    public Mesh MeshFor(AlienType type, int pose)
    {
        return VoxelModel.Build(shapes[(int)type][pose], 'X', pixelSize, depth);
    }

    public static Color ColorFor(AlienType type)
    {
        return colors[(int)type];
    }

    // Half the alien's width and height in world units.
    public Vector2 HalfSize => Vector3.Scale(meshFilter.sharedMesh.bounds.extents, transform.lossyScale);

    // The fleet decides who fires and when (see AlienFleet.TryFire).
    public void Shoot()
    {
        Instantiate(bulletPrefab, transform.position, Quaternion.identity);
    }

    void OnTriggerEnter(Collider other)
    {
        Dart dart = other.GetComponentInParent<Dart>();
        if (dart != null && !dart.spent)
        {
            dart.spent = true;
            Destroy(dart.gameObject);
            Die();
            return;
        }

        Spaceship ship = other.GetComponentInParent<Spaceship>();
        if (ship != null)
        {
            ship.Hit();
        }
    }

    void Die()
    {
        if (dead)
        {
            return;
        }
        dead = true;
        Explosion.Spawn(transform.position, GetComponent<Renderer>().sharedMaterial.color);
        fleet.AlienDestroyed(this);
        Destroy(gameObject);
    }
}
