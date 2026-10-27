using UnityEngine;

// Space background built behind the play area. Uses the picture in Resources/SpaceBackground if there is one,
// otherwise falls back to slowly drifting stars at several depths and a cratered moon.
public class Starfield : MonoBehaviour
{
    // Crater directions on the camera-facing side of the moon (z < 0 faces the camera), with their sizes.
    static readonly Vector4[] craters =
    {
        new Vector4(-.35f,  .30f, -.88f, .28f),
        new Vector4( .30f, -.25f, -.92f, .20f),
        new Vector4( .05f,  .55f, -.83f, .14f),
        new Vector4(-.55f, -.35f, -.76f, .18f),
        new Vector4( .55f,  .30f, -.78f, .12f),
        new Vector4(-.05f, -.60f, -.80f, .10f),
    };

    public static Starfield Create()
    {
        Camera cam = Camera.main;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 center = cam.transform.position;

        Starfield starfield = new GameObject("Starfield").AddComponent<Starfield>();

        Texture2D picture = Resources.Load<Texture2D>("SpaceBackground");
        if (picture != null)
        {
            starfield.AddPicture(picture, center, halfWidth, halfHeight);
            return starfield;
        }

        Material material = Resources.Load<Material>("ParticleUnlit");

        // Three layers: many small slow stars far back, fewer large fast ones in front, for a parallax feel.
        starfield.AddStarLayer(material, center, halfWidth, halfHeight, 60, .04f, .08f, .4f, 50);
        starfield.AddStarLayer(material, center, halfWidth, halfHeight, 50, .07f, .12f, .9f, 45);
        starfield.AddStarLayer(material, center, halfWidth, halfHeight, 40, .10f, .18f, 1.8f, 40);

        Transform moon = CreateSphere("Moon", starfield.transform, new Color(.78f, .78f, .74f));
        // In front of the star layers (z 40-50) so stars pass behind it, but well behind the game (z 0).
        moon.position = new Vector3(center.x + halfWidth * .72f, center.y + halfHeight * .55f, 30);
        moon.localScale = Vector3.one * 4.5f;
        foreach (Vector4 crater in craters)
        {
            Transform c = CreateSphere("Crater", moon, new Color(.55f, .55f, .52f));
            c.localPosition = new Vector3(crater.x, crater.y, crater.z).normalized * .47f;
            c.localScale = Vector3.one * crater.w;
        }

        return starfield;
    }

    // A quad far behind the game, scaled to cover the whole view without stretching the picture.
    void AddPicture(Texture2D picture, Vector3 center, float halfWidth, float halfHeight)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        RemoveCollider(quad);
        quad.name = "BackgroundPicture";
        quad.transform.SetParent(transform, false);
        quad.transform.position = new Vector3(center.x, center.y, 100);

        float pictureAspect = (float)picture.width / picture.height;
        float width = halfWidth * 2, height = halfHeight * 2;
        if (width / height > pictureAspect)
        {
            height = width / pictureAspect;
        }
        else
        {
            width = height * pictureAspect;
        }
        quad.transform.localScale = new Vector3(width, height, 1);

        Renderer quadRenderer = quad.GetComponent<Renderer>();
        Material material = new Material(Resources.Load<Material>("BackgroundUnlit")) { name = "SpaceBackground" };
        material.SetTexture("_BaseMap", picture);
        quadRenderer.sharedMaterial = material;
        quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quadRenderer.receiveShadows = false;
    }

    static Transform CreateSphere(string name, Transform parent, Color color)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        RemoveCollider(sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        Renderer sphereRenderer = sphere.GetComponent<Renderer>();
        Material material = new Material(sphereRenderer.sharedMaterial) { color = color };
        material.SetFloat("_Smoothness", .1f);   // matte, no shiny highlight
        sphereRenderer.sharedMaterial = material;
        return sphere.transform;
    }

    // Background pieces are decoration only. Destroy() isn't allowed outside Play mode (see EditModePreview).
    static void RemoveCollider(GameObject go)
    {
        if (Application.isPlaying)
        {
            Destroy(go.GetComponent<Collider>());
        }
        else
        {
            DestroyImmediate(go.GetComponent<Collider>());
        }
    }

    void AddStarLayer(Material material, Vector3 center, float halfWidth, float halfHeight,
                      float count, float minSize, float maxSize, float speed, float depth)
    {
        GameObject go = new GameObject("Stars");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(center.x, center.y + halfHeight + 1, depth);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float travel = halfHeight * 2 + 2;
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.useUnscaledTime = true;     // keeps drifting behind the title and end screens
        main.startLifetime = travel / speed;
        main.startSpeed = 0;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(.7f, .8f, 1f), new Color(1f, .95f, .8f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1000;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = count / main.startLifetime.constant;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(halfWidth * 2 + 2, .1f, .1f);

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = 0;
        velocity.y = -speed;
        velocity.z = 0;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        ps.Play();
    }
}
