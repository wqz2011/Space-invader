using UnityEngine;
using System.Collections.Generic;

public class AlienFleet : MonoBehaviour
{
    [Header("Inscribed")]
    public GameObject alienPrefab;
    public AlienCounter alienCounter;

    public int columns = 11;
    public int rows = 4;
    public float spacing = 2f;
    public float firstRowHeight = 8f;

    // Speeds are in units per second (the old code moved .1 units per physics step = 5 units/sec).
    public float baseSpeed = 4f;
    // The last surviving alien moves this many times faster than the full fleet.
    public float maxSpeedMultiplier = 5f;
    public float dropDistance = .6f;
    public float edgeMargin = 1f;
    // Seconds between animation poses at base speed; poses switch faster as the fleet speeds up.
    public float poseInterval = .5f;

    [Header("Firing")]
    // Shots per second for the whole fleet, rising as the fleet shrinks.
    public float minShotsPerSecond = 1.2f;
    public float maxShotsPerSecond = 2.5f;
    // Fraction of shots fired from the column closest to the player instead of a random column.
    [Range(0, 1)]
    public float aimedShotChance = .35f;

    [Range(0, 1)]
    public float powerUpDropChance = .08f;

    [Header("Dynamic")]
    public List<Alien> aliens = new List<Alien>();

    int startingCount;
    float direction = 1;
    float dropRemaining = 0;
    float invasionLineY = float.NegativeInfinity;
    Vector2 alienHalfSize;
    Transform ship;
    int pose = 0;
    float poseTimer = 0;

    void Start()
    {
        // Aliens are children of the fleet so the whole fleet can be moved with a single transform.
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                Alien alien = Instantiate(alienPrefab, transform).GetComponent<Alien>();
                alien.transform.localPosition = SlotPosition(i, j);
                alien.Init(this, i, TypeForRow(j));
                aliens.Add(alien);

                Vector2 halfSize = alien.HalfSize;
                alienHalfSize = Vector2.Max(alienHalfSize, halfSize);
            }
        }
        startingCount = aliens.Count;
        alienCounter.SetCount(aliens.Count);

        // Once the bottom of the fleet reaches the top of the ship, the ship can no longer survive.
        Spaceship spaceship = FindAnyObjectByType<Spaceship>();
        if (spaceship != null)
        {
            ship = spaceship.transform;
            float shipTop = float.NegativeInfinity;
            foreach (Renderer r in spaceship.GetComponentsInChildren<Renderer>())
            {
                shipTop = Mathf.Max(shipTop, r.bounds.max.y);
            }
            invasionLineY = shipTop;
        }
    }

    // Starting position of the alien in column i, row j, relative to the fleet.
    public Vector3 SlotPosition(int i, int j)
    {
        return new Vector3((i - (columns - 1) / 2f) * spacing, firstRowHeight - j * spacing, 0);
    }

    // Arcade layout: squids on top, crabs in the middle, octopuses at the bottom.
    public AlienType TypeForRow(int j)
    {
        return j == 0 ? AlienType.Squid : j <= rows / 2 ? AlienType.Crab : AlienType.Octopus;
    }

    float DestroyedFraction => 1 - (float)aliens.Count / startingCount;

    public float CurrentSpeed => baseSpeed * Mathf.Lerp(1, maxSpeedMultiplier, DestroyedFraction);

    // Only the lowest alien in a column fires, like the original arcade game, so shots never come
    // from behind the front line. Some shots are aimed at the column above the player.
    void TryFire()
    {
        float shotsPerSecond = Mathf.Lerp(minShotsPerSecond, maxShotsPerSecond, DestroyedFraction);
        if (Random.value >= shotsPerSecond * Time.fixedDeltaTime)
        {
            return;
        }

        int column = aliens[Random.Range(0, aliens.Count)].column;
        if (ship != null && Random.value < aimedShotChance)
        {
            float closest = float.MaxValue;
            foreach (Alien alien in aliens)
            {
                float distance = Mathf.Abs(alien.transform.position.x - ship.position.x);
                if (distance < closest)
                {
                    closest = distance;
                    column = alien.column;
                }
            }
        }

        Alien shooter = null;
        foreach (Alien alien in aliens)
        {
            if (alien.column == column && (shooter == null || alien.transform.position.y < shooter.transform.position.y))
            {
                shooter = alien;
            }
        }
        shooter.Shoot();
    }

    void FixedUpdate()
    {
        if (aliens.Count == 0)
        {
            return;
        }

        float step = CurrentSpeed * Time.fixedDeltaTime;
        if (dropRemaining > 0)
        {
            float drop = Mathf.Min(step, dropRemaining);
            transform.position += Vector3.down * drop;
            dropRemaining -= drop;
        }
        else
        {
            transform.position += Vector3.right * direction * step;
        }

        // Find the fleet's current extents in a single pass.
        float left = float.MaxValue, right = float.MinValue, bottom = float.MaxValue;
        foreach (Alien alien in aliens)
        {
            Vector3 position = alien.transform.position;
            left = Mathf.Min(left, position.x - alienHalfSize.x);
            right = Mathf.Max(right, position.x + alienHalfSize.x);
            bottom = Mathf.Min(bottom, position.y - alienHalfSize.y);
        }

        Camera cam = Camera.main;
        float halfWidth = cam.orthographicSize * cam.aspect;
        float leftBound = cam.transform.position.x - halfWidth + edgeMargin;
        float rightBound = cam.transform.position.x + halfWidth - edgeMargin;
        if (dropRemaining <= 0 && (direction > 0 && right >= rightBound || direction < 0 && left <= leftBound))
        {
            direction = -direction;
            dropRemaining = dropDistance;
        }

        TryFire();
        Animate();

        if (bottom <= invasionLineY)
        {
            enabled = false;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Lose("The aliens have landed!");
            }
        }
    }

    // Flips every alien between its two poses, faster as the fleet speeds up.
    void Animate()
    {
        poseTimer += Time.fixedDeltaTime;
        if (poseTimer < poseInterval * baseSpeed / CurrentSpeed)
        {
            return;
        }
        poseTimer = 0;
        pose = 1 - pose;
        foreach (Alien alien in aliens)
        {
            alien.SetPose(pose);
        }
    }

    public void AlienDestroyed(Alien alien)
    {
        aliens.Remove(alien);
        alienCounter.SetCount(aliens.Count);

        if (Random.value < powerUpDropChance)
        {
            PowerUp.Spawn(alien.transform.position, (PowerUpType)Random.Range(0, 3));
        }

        if (aliens.Count == 0 && GameManager.Instance != null)
        {
            GameManager.Instance.Win();
        }
    }
}
