using UnityEngine;

// A short burst of tumbling debris plus a flash of light, spawned whenever something is destroyed.
public class Explosion : MonoBehaviour
{
    static Material particleMaterial;
    static Mesh debrisMesh;

    Light flash;
    float flashIntensity;
    float flashDuration = .25f;
    float age = 0;

    public static void Spawn(Vector3 position, Color color, float scale = 1f)
    {
        if (particleMaterial == null)
        {
            particleMaterial = Resources.Load<Material>("ParticleUnlit");
            debrisMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        }

        GameObject go = new GameObject("Explosion");
        go.transform.position = position;

        // AddComponent starts the system playing with default settings, so stop it before configuring.
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = .5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.4f, .9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f * scale, 9f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(.12f * scale, .3f * scale);
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, .6f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)Mathf.Clamp(25 * scale, 8, 80)) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .2f * scale;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, 0));

        ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.separateAxes = true;
        rotationOverLifetime.x = new ParticleSystem.MinMaxCurve(-8, 8);
        rotationOverLifetime.y = new ParticleSystem.MinMaxCurve(-8, 8);
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-8, 8);

        // Only used for its drag, so the debris slows down instead of flying off-screen.
        ParticleSystem.LimitVelocityOverLifetimeModule drag = ps.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.limit = 1000;
        drag.drag = 3;

        ParticleSystemRenderer psRenderer = go.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        psRenderer.mesh = debrisMesh;
        psRenderer.material = particleMaterial;

        Explosion explosion = go.AddComponent<Explosion>();
        explosion.flash = go.AddComponent<Light>();
        explosion.flash.type = LightType.Point;
        explosion.flash.color = color;
        explosion.flash.range = 4 * scale;
        explosion.flashIntensity = 6 * scale;
        explosion.flash.intensity = explosion.flashIntensity;

        ps.Play();
    }

    void Update()
    {
        age += Time.deltaTime;
        flash.intensity = Mathf.Lerp(flashIntensity, 0, age / flashDuration);
    }
}
