using UnityEngine;

public enum AlienType { Squid, Crab, Octopus }

public class Alien : MonoBehaviour
{
    // Pixel art for the three kinds of aliens ("X" = a cube, "." = empty).
    // Each kind has two poses, A and B. The fleet switches between them so the aliens look like they're walking.
    static string[] squidA = { "...XX...", "..XXXX..", ".XXXXXX.", "XX.XX.XX", "XXXXXXXX", "..X..X..", ".X.XX.X.", "X.X..X.X" };
    static string[] squidB = { "...XX...", "..XXXX..", ".XXXXXX.", "XX.XX.XX", "XXXXXXXX", ".X.XX.X.", "X......X", ".X....X." };

    static string[] crabA = { "..X.....X..", "...X...X...", "..XXXXXXX..", ".XX.XXX.XX.", "XXXXXXXXXXX", "X.XXXXXXX.X", "X.X.....X.X", "...XX.XX..." };
    static string[] crabB = { "..X.....X..", "X..X...X..X", "X.XXXXXXX.X", "XXX.XXX.XXX", "XXXXXXXXXXX", ".XXXXXXXXX.", "..X.....X..", ".X.......X." };

    static string[] octopusA = { "....XXXX....", ".XXXXXXXXXX.", "XXXXXXXXXXXX", "XXX..XX..XXX", "XXXXXXXXXXXX", "...XX..XX...", "..XX.XX.XX..", "XX........XX" };
    static string[] octopusB = { "....XXXX....", ".XXXXXXXXXX.", "XXXXXXXXXXXX", "XXX..XX..XXX", "XXXXXXXXXXXX", "..XXX..XXX..", ".XX..XX..XX.", "..XX....XX.." };

    [Header("Inscribed")]
    public GameObject bulletPrefab;
    public float pixelSize = .14f;
    public float depth = .3f;

    [Header("Dynamic")]
    public AlienFleet fleet;
    public int column;
    public AlienType type;

    Mesh poseA;
    Mesh poseB;
    bool dead = false;

    public static string[] GetShape(AlienType type, int pose)
    {
        if (type == AlienType.Squid)
        {
            if (pose == 0) { return squidA; } else { return squidB; }
        }
        else if (type == AlienType.Crab)
        {
            if (pose == 0) { return crabA; } else { return crabB; }
        }
        else
        {
            if (pose == 0) { return octopusA; } else { return octopusB; }
        }
    }

    public static Color GetColor(AlienType type)
    {
        if (type == AlienType.Squid)
        {
            return new Color(1f, .35f, .8f);    // pink
        }
        else if (type == AlienType.Crab)
        {
            return new Color(0f, .98f, .6f);    // green (the original Alien_Skin color)
        }
        else
        {
            return new Color(.3f, .8f, 1f);     // light blue
        }
    }

    // Builds the two pose meshes for one kind of alien. The fleet calls this once for each kind
    // and shares the meshes between all aliens of that kind.
    public Mesh[] MakePoseMeshes(AlienType type)
    {
        Mesh[] meshes = new Mesh[2];
        meshes[0] = VoxelModel.Build(GetShape(type, 0), 'X', pixelSize, depth);
        meshes[1] = VoxelModel.Build(GetShape(type, 1), 'X', pixelSize, depth);
        return meshes;
    }

    // Called by the fleet right after it creates this alien. Turns the prefab's sphere into a pixel-art alien.
    public void Setup(AlienFleet fleet, int column, AlienType type, Mesh[] poses)
    {
        this.fleet = fleet;
        this.column = column;
        this.type = type;
        poseA = poses[0];
        poseB = poses[1];

        GetComponent<Renderer>().material.color = GetColor(type);
        SetPose(0);

        // The prefab's sphere collider doesn't fit the new shape, so swap it for a box that does.
        // (A new BoxCollider automatically sizes itself to the mesh.)
        Destroy(GetComponent<SphereCollider>());
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
    }

    public void SetPose(int pose)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (pose == 0)
        {
            meshFilter.sharedMesh = poseA;
        }
        else
        {
            meshFilter.sharedMesh = poseB;
        }
    }

    // Half of the alien's width and height in world units, used by the fleet to find its edges.
    public Vector3 GetHalfSize()
    {
        Vector3 meshHalfSize = GetComponent<MeshFilter>().sharedMesh.bounds.extents;
        return Vector3.Scale(meshHalfSize, transform.lossyScale);
    }

    // The fleet decides which alien shoots and when (see AlienFleet.FireAtPlayer).
    public void Shoot()
    {
        Instantiate(bulletPrefab, transform.position, Quaternion.identity);
    }

    void OnTriggerEnter(Collider other)
    {
        // Hit by one of the player's darts?
        Dart dart = other.GetComponentInParent<Dart>();
        if (dart != null && !dart.spent)
        {
            dart.spent = true;
            Destroy(dart.gameObject);
            Die();
            return;
        }

        // Crashed into the player's ship?
        Spaceship ship = other.GetComponentInParent<Spaceship>();
        if (ship != null)
        {
            ship.Hit();
        }
    }

    void Die()
    {
        // Two parts of a dart can touch the alien in the same physics step, so only die once.
        if (dead)
        {
            return;
        }
        dead = true;

        Explosion.Spawn(transform.position, GetColor(type), 1f);
        fleet.AlienDestroyed(this);
        Destroy(gameObject);
    }
}
