using UnityEngine;

// Shows the game's generated visuals (background picture, voxel aliens and spaceship) in the editor before
// pressing Play. Those visuals are normally built by the scripts at runtime, so without this the scene would show
// the original capsule ship, no aliens and the default sky.
//
// Everything built here is hidden and marked "don't save", so it never ends up in the scene file, and it is
// removed before Play mode starts, when the real game objects get built instead.
[ExecuteAlways]
public class EditModePreview : MonoBehaviour
{
#if UNITY_EDITOR
    Transform root, fleetPreview, shipPreview;
    Spaceship ship;
    AlienFleet fleet;
    Renderer[] hiddenShipParts = new Renderer[0];
    int builtHash;

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            Build();
        }
    }

    void OnDisable()
    {
        Clear();
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            return;
        }
        // Rebuild when a setting that changes the preview is edited in the Inspector.
        if (SettingsHash() != builtHash)
        {
            Clear();
            Build();
        }
        // Follow the real objects if they are moved in the Scene view.
        if (fleetPreview != null && fleet != null)
        {
            fleetPreview.position = fleet.transform.position;
        }
        if (shipPreview != null && ship != null)
        {
            shipPreview.position = ship.transform.position;
        }
    }

    void Build()
    {
        ship = FindAnyObjectByType<Spaceship>();
        fleet = FindAnyObjectByType<AlienFleet>();
        builtHash = SettingsHash();
        root = new GameObject("EditModePreview").transform;

        Starfield.Create().transform.SetParent(root, false);

        if (fleet != null && fleet.alienPrefab != null)
        {
            BuildFleet();
        }
        if (ship != null)
        {
            BuildShip();
        }

        // Hide from the Hierarchy and never save: this is only a preview.
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m != null && !UnityEditor.EditorUtility.IsPersistent(m))
                {
                    m.hideFlags = HideFlags.HideAndDontSave;
                }
            }
        }
    }

    void BuildFleet()
    {
        Alien alienPrefab = fleet.alienPrefab.GetComponent<Alien>();
        Material baseMaterial = fleet.alienPrefab.GetComponent<Renderer>().sharedMaterial;
        Material[] materials = new Material[3];

        fleetPreview = new GameObject("Fleet").transform;
        fleetPreview.SetParent(root, false);
        fleetPreview.position = fleet.transform.position;

        for (int i = 0; i < fleet.columns; i++)
        {
            for (int j = 0; j < fleet.rows; j++)
            {
                AlienType type = fleet.TypeForRow(j);
                int t = (int)type;
                if (materials[t] == null)
                {
                    materials[t] = new Material(baseMaterial) { color = Alien.ColorFor(type) };
                }

                GameObject alien = new GameObject("Alien", typeof(MeshFilter), typeof(MeshRenderer));
                alien.transform.SetParent(fleetPreview, false);
                alien.transform.localPosition = fleet.SlotPosition(i, j);
                alien.transform.localScale = fleet.alienPrefab.transform.localScale;
                alien.GetComponent<MeshFilter>().sharedMesh = alienPrefab.MeshFor(type, 0);
                alien.GetComponent<MeshRenderer>().sharedMaterial = materials[t];
            }
        }
    }

    void BuildShip()
    {
        shipPreview = new GameObject("Spaceship").transform;
        shipPreview.SetParent(root, false);
        shipPreview.position = ship.transform.position;
        ship.BuildModel(shipPreview, ship.PrefabHullMaterial);

        // Hide the original capsule parts while previewing. forceRenderingOff isn't saved with the scene.
        hiddenShipParts = ship.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in hiddenShipParts)
        {
            r.forceRenderingOff = true;
        }
    }

    void Clear()
    {
        foreach (Renderer r in hiddenShipParts)
        {
            if (r != null)
            {
                r.forceRenderingOff = false;
            }
        }
        hiddenShipParts = new Renderer[0];

        if (root == null)
        {
            return;
        }
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m != null && !UnityEditor.EditorUtility.IsPersistent(m))
                {
                    DestroyImmediate(m);
                }
            }
        }
        DestroyImmediate(root.gameObject);
        root = null;
    }

    // Combines the settings that affect the preview, so Update can tell when it needs rebuilding.
    int SettingsHash()
    {
        int hash = Camera.main != null ? Camera.main.aspect.GetHashCode() : 0;
        if (fleet != null)
        {
            hash = hash * 31 + fleet.columns;
            hash = hash * 31 + fleet.rows;
            hash = hash * 31 + fleet.spacing.GetHashCode();
            hash = hash * 31 + fleet.firstRowHeight.GetHashCode();
        }
        if (ship != null)
        {
            hash = hash * 31 + ship.pixelSize.GetHashCode();
            hash = hash * 31 + ship.depth.GetHashCode();
            hash = hash * 31 + ship.hullTint.GetHashCode();
        }
        return hash;
    }
#endif
}
