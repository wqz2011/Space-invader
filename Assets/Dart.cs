using UnityEngine;

public class Dart : MonoBehaviour
{
    [Header("Inscribed")]
    public float speed = 15f;
    public float maxDistance = 25f;

    [Header("Dynamic")]
    // Set once the dart has hit something, so one dart can't destroy two targets in the same physics step.
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
        // Darts fly along their own "up" so multi-shot darts can be fired at an angle.
        rb.MovePosition(rb.position + transform.up * speed * Time.fixedDeltaTime);
        if ((rb.position - startPosition).sqrMagnitude > maxDistance * maxDistance)
        {
            Destroy(gameObject);
        }
    }
}
