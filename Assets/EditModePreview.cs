using UnityEngine;

// Shows the background, the aliens and the pixel-art ship in the editor BEFORE you press Play.
// Normally the scripts only make those once the game starts, so the scene would show the old capsule ship,
// no aliens and the default sky.
//
// [ExecuteAlways] makes Unity run this script in the editor too, not only in Play mode.
// Everything it makes is hidden and never saved into the scene, and it's all removed when you press Play
// (then the real game objects get made instead).
[ExecuteAlways]
public class EditModePreview : MonoBehaviour
{
#if UNITY_EDITOR
    GameObject preview;
    Transform fleetPreview;
    Transform shipPreview;
    AlienFleet fleet;
    Spaceship ship;

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            MakePreview();
        }
    }

    void OnDisable()
    {
        RemovePreview();
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            return;
        }

        // If the real fleet or ship gets moved in the Scene view, move the preview with it.
        if (fleet != null && fleetPreview != null)
        {
            fleetPreview.position = fleet.transform.position;
        }
        if (ship != null && shipPreview != null)
        {
            shipPreview.position = ship.transform.position;
        }
    }

    void MakePreview()
    {
        preview = new GameObject("Preview");

        GameObject background = Background.Create();
        if (background != null)
        {
            background.transform.SetParent(preview.transform, true);
        }

        fleet = FindAnyObjectByType<AlienFleet>();
        if (fleet != null)
        {
            MakeFleetPreview();
        }

        ship = FindAnyObjectByType<Spaceship>();
        if (ship != null)
        {
            MakeShipPreview();
        }

        // Hide everything we made: it doesn't show in the Hierarchy and never gets saved into the scene.
        foreach (Transform child in preview.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    // Puts a still alien in every spot the real fleet will fill when the game starts.
    void MakeFleetPreview()
    {
        fleetPreview = new GameObject("Fleet").transform;
        fleetPreview.SetParent(preview.transform, false);
        fleetPreview.position = fleet.transform.position;

        Alien prefabAlien = fleet.alienPrefab.GetComponent<Alien>();
        Material prefabMaterial = fleet.alienPrefab.GetComponent<Renderer>().sharedMaterial;
        // Only the first pose is needed, since the preview doesn't animate.
        Mesh squidMesh = VoxelModel.Build(Alien.GetShape(AlienType.Squid, 0), 'X', prefabAlien.pixelSize, prefabAlien.depth);
        Mesh crabMesh = VoxelModel.Build(Alien.GetShape(AlienType.Crab, 0), 'X', prefabAlien.pixelSize, prefabAlien.depth);
        Mesh octopusMesh = VoxelModel.Build(Alien.GetShape(AlienType.Octopus, 0), 'X', prefabAlien.pixelSize, prefabAlien.depth);

        for (int col = 0; col < fleet.columns; col++)
        {
            for (int row = 0; row < fleet.rows; row++)
            {
                AlienType type = fleet.GetTypeForRow(row);
                Mesh mesh = squidMesh;
                if (type == AlienType.Crab)
                {
                    mesh = crabMesh;
                }
                else if (type == AlienType.Octopus)
                {
                    mesh = octopusMesh;
                }

                GameObject alien = new GameObject("Alien");
                alien.transform.SetParent(fleetPreview, false);
                alien.transform.localPosition = fleet.GetStartPosition(col, row);
                alien.transform.localScale = fleet.alienPrefab.transform.localScale;

                Material material = new Material(prefabMaterial);
                material.color = Alien.GetColor(type);
                alien.AddComponent<MeshFilter>().sharedMesh = mesh;
                alien.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
        }
    }

    void MakeShipPreview()
    {
        shipPreview = new GameObject("Ship").transform;
        shipPreview.SetParent(preview.transform, false);
        shipPreview.position = ship.transform.position;
        ship.BuildModel(shipPreview, ship.GetOriginalMaterial());

        // Hide the old capsule parts while previewing. (forceRenderingOff isn't saved with the scene.)
        foreach (Renderer part in ship.GetComponentsInChildren<Renderer>(true))
        {
            part.forceRenderingOff = true;
        }
    }

    void RemovePreview()
    {
        // Show the capsule parts again.
        if (ship != null)
        {
            foreach (Renderer part in ship.GetComponentsInChildren<Renderer>(true))
            {
                part.forceRenderingOff = false;
            }
        }

        if (preview == null)
        {
            return;
        }

        // Meshes and materials made in code aren't removed along with the objects, so delete them by hand.
        // (IsPersistent means it's a real file in the project, like the quad mesh. Those must not be deleted.)
        foreach (MeshFilter filter in preview.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh != null && !UnityEditor.EditorUtility.IsPersistent(filter.sharedMesh))
            {
                DestroyImmediate(filter.sharedMesh);
            }
        }
        foreach (Renderer part in preview.GetComponentsInChildren<Renderer>(true))
        {
            if (part.sharedMaterial != null && !UnityEditor.EditorUtility.IsPersistent(part.sharedMaterial))
            {
                DestroyImmediate(part.sharedMaterial);
            }
        }

        DestroyImmediate(preview);
        preview = null;
    }
#endif
}
