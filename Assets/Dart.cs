using UnityEngine;

public class Dart : MonoBehaviour
{
    [Header("Inscribed")]
    public float speed = 15f;
    public float maxDistance = 25f;

    [Header("Dynamic")]
    // Set to true once the dart hits something, so one dart can't destroy two things at once.
    public bool spent = false;

    Rigidbody rb;
    Vector3 startPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
    }

    void FixedUpdate()
    {
        // Fly in the direction the dart is pointing ("up" for the dart).
        // Normally that's straight up, but multi-shot darts are fired at an angle.
        rb.MovePosition(rb.position + transform.up * speed * Time.fixedDeltaTime);

        // Remove the dart once it has flown off the screen.
        if (Vector3.Distance(rb.position, startPosition) > maxDistance)
        {
            Destroy(gameObject);
        }
    }
}
