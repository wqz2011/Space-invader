using UnityEngine;

// Puts the space picture (Assets/Resources/SpaceBackground) behind the game.
public static class Background
{
    public static GameObject Create()
    {
        Texture2D picture = Resources.Load<Texture2D>("SpaceBackground");
        if (picture == null)
        {
            Debug.LogWarning("No SpaceBackground picture found in Assets/Resources.");
            return null;
        }

        // A flat square (quad), far behind everything else in the game.
        Camera cam = Camera.main;
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "BackgroundPicture";
        quad.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 100);

        // Scale it to cover the whole screen without stretching the picture.
        float screenHeight = cam.orthographicSize * 2;
        float screenWidth = screenHeight * cam.aspect;
        float pictureAspect = (float)picture.width / picture.height;
        float width = screenWidth;
        float height = screenWidth / pictureAspect;
        if (height < screenHeight)
        {
            // The screen is taller than the picture, so fit the height instead.
            height = screenHeight;
            width = screenHeight * pictureAspect;
        }
        quad.transform.localScale = new Vector3(width, height, 1);

        // Unlit, so the picture shows its real colors no matter how the scene is lit.
        Material material = new Material(Resources.Load<Material>("BackgroundUnlit"));
        material.SetTexture("_BaseMap", picture);
        Renderer quadRenderer = quad.GetComponent<Renderer>();
        quadRenderer.sharedMaterial = material;
        quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // The quad is just a picture, so it doesn't need a collider.
        // (Destroy only works while the game is running; EditModePreview also calls this in the editor.)
        Collider quadCollider = quad.GetComponent<Collider>();
        if (Application.isPlaying)
        {
            Object.Destroy(quadCollider);
        }
        else
        {
            Object.DestroyImmediate(quadCollider);
        }

        return quad;
    }
}
