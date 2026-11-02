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

    // Speeds are in units per second.
    public float baseSpeed = 4f;            // speed with the whole fleet alive
    public float maxSpeedMultiplier = 5f;   // the last alien moves this many times faster
    public float dropDistance = .6f;        // how far the fleet moves down at each edge
    public float edgeMargin = 1f;           // how close to the screen edge the fleet gets
    public float poseTime = .5f;            // seconds between walking poses (at base speed)

    [Header("Firing")]
    public float minShotsPerSecond = 1.2f;  // with the whole fleet alive
    public float maxShotsPerSecond = 2.5f;  // with almost every alien destroyed
    [Range(0, 1)]
    public float aimedShotChance = .35f;    // chance a shot comes from right above the player

    [Range(0, 1)]
    public float powerUpDropChance = .08f;

    [Header("Dynamic")]
    public List<Alien> aliens = new List<Alien>();

    int startingCount;
    float direction = 1;           // 1 = moving right, -1 = moving left
    float dropLeft = 0;            // how much further down the fleet still has to move
    float landingHeight = -1000;   // if the bottom of the fleet reaches this, the player loses
    float alienHalfWidth = 0;
    float alienHalfHeight = 0;
    Transform ship;
    int pose = 0;
    float poseTimer = 0;

    void Start()
    {
        // Build the two pose meshes for each kind of alien once, and share them between all the aliens.
        Alien prefabAlien = alienPrefab.GetComponent<Alien>();
        Mesh[] squidMeshes = prefabAlien.MakePoseMeshes(AlienType.Squid);
        Mesh[] crabMeshes = prefabAlien.MakePoseMeshes(AlienType.Crab);
        Mesh[] octopusMeshes = prefabAlien.MakePoseMeshes(AlienType.Octopus);

        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                AlienType type = GetTypeForRow(row);
                Mesh[] meshes = squidMeshes;
                if (type == AlienType.Crab)
                {
                    meshes = crabMeshes;
                }
                else if (type == AlienType.Octopus)
                {
                    meshes = octopusMeshes;
                }

                // The aliens are children of the fleet, so moving the fleet moves all of them at once.
                GameObject alienObject = Instantiate(alienPrefab, transform);
                alienObject.transform.localPosition = GetStartPosition(col, row);
                Alien alien = alienObject.GetComponent<Alien>();
                alien.Setup(this, col, type, meshes);
                aliens.Add(alien);

                // Remember the biggest alien size, for finding the edges of the fleet later.
                Vector3 halfSize = alien.GetHalfSize();
                if (halfSize.x > alienHalfWidth)
                {
                    alienHalfWidth = halfSize.x;
                }
                if (halfSize.y > alienHalfHeight)
                {
                    alienHalfHeight = halfSize.y;
                }
            }
        }
        startingCount = aliens.Count;
        alienCounter.SetCount(aliens.Count);

        // Once the bottom of the fleet gets down to the top of the ship, the player can't win anymore.
        Spaceship spaceship = FindAnyObjectByType<Spaceship>();
        if (spaceship != null)
        {
            ship = spaceship.transform;
            foreach (Renderer part in spaceship.GetComponentsInChildren<Renderer>())
            {
                if (part.bounds.max.y > landingHeight)
                {
                    landingHeight = part.bounds.max.y;
                }
            }
        }
    }

    // Where the alien in this column and row starts, relative to the fleet.
    public Vector3 GetStartPosition(int col, int row)
    {
        float x = (col - (columns - 1) / 2f) * spacing;
        float y = firstRowHeight - row * spacing;
        return new Vector3(x, y, 0);
    }

    // Like the arcade game: squids on the top row, crabs in the middle, octopuses at the bottom.
    public AlienType GetTypeForRow(int row)
    {
        if (row == 0)
        {
            return AlienType.Squid;
        }
        else if (row <= rows / 2)
        {
            return AlienType.Crab;
        }
        else
        {
            return AlienType.Octopus;
        }
    }

    // 0 with the whole fleet alive, close to 1 when almost every alien is destroyed.
    float GetFractionDestroyed()
    {
        return 1f - (float)aliens.Count / startingCount;
    }

    // The fleet speeds up as aliens are destroyed.
    // (Mathf.Lerp goes from the first number to the second as the last number goes from 0 to 1.)
    public float GetSpeed()
    {
        return baseSpeed * Mathf.Lerp(1, maxSpeedMultiplier, GetFractionDestroyed());
    }

    void FixedUpdate()
    {
        if (aliens.Count == 0)
        {
            return;
        }

        MoveFleet();
        TurnAroundAtEdges();
        FireAtPlayer();
        AnimateAliens();
        CheckIfLanded();
    }

    void MoveFleet()
    {
        float step = GetSpeed() * Time.fixedDeltaTime;

        if (dropLeft > 0)
        {
            // Still moving down after hitting an edge.
            float drop = Mathf.Min(step, dropLeft);
            transform.position += Vector3.down * drop;
            dropLeft -= drop;
        }
        else
        {
            transform.position += Vector3.right * direction * step;
        }
    }

    void TurnAroundAtEdges()
    {
        if (dropLeft > 0)
        {
            return;
        }

        // Find the left-most and right-most points of the fleet.
        float left = 1000;
        float right = -1000;
        foreach (Alien alien in aliens)
        {
            float x = alien.transform.position.x;
            if (x - alienHalfWidth < left)
            {
                left = x - alienHalfWidth;
            }
            if (x + alienHalfWidth > right)
            {
                right = x + alienHalfWidth;
            }
        }

        // The edges of what the camera can see.
        Camera cam = Camera.main;
        float halfScreenWidth = cam.orthographicSize * cam.aspect;
        float leftEdge = cam.transform.position.x - halfScreenWidth + edgeMargin;
        float rightEdge = cam.transform.position.x + halfScreenWidth - edgeMargin;

        bool hitRightEdge = direction > 0 && right >= rightEdge;
        bool hitLeftEdge = direction < 0 && left <= leftEdge;
        if (hitRightEdge || hitLeftEdge)
        {
            direction = -direction;
            dropLeft = dropDistance;
        }
    }

    // Only the lowest alien in a column can shoot (like the arcade game), so shots always
    // come from the front of the fleet. Some shots come from the column right above the player.
    void FireAtPlayer()
    {
        float shotsPerSecond = Mathf.Lerp(minShotsPerSecond, maxShotsPerSecond, GetFractionDestroyed());
        if (Random.value >= shotsPerSecond * Time.fixedDeltaTime)
        {
            return;   // no shot this physics step
        }

        // Pick a column: usually a random one, sometimes the one closest to the player.
        int column = aliens[Random.Range(0, aliens.Count)].column;
        if (ship != null && Random.value < aimedShotChance)
        {
            float closestDistance = 1000;
            foreach (Alien alien in aliens)
            {
                float distance = Mathf.Abs(alien.transform.position.x - ship.position.x);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    column = alien.column;
                }
            }
        }

        // Find the lowest alien in that column and let it shoot.
        Alien shooter = null;
        foreach (Alien alien in aliens)
        {
            if (alien.column != column)
            {
                continue;
            }
            if (shooter == null || alien.transform.position.y < shooter.transform.position.y)
            {
                shooter = alien;
            }
        }
        shooter.Shoot();
    }

    // Switch every alien between its two walking poses. They walk faster as the fleet speeds up.
    void AnimateAliens()
    {
        poseTimer += Time.fixedDeltaTime;
        float timeBetweenPoses = poseTime * baseSpeed / GetSpeed();
        if (poseTimer < timeBetweenPoses)
        {
            return;
        }

        poseTimer = 0;
        if (pose == 0)
        {
            pose = 1;
        }
        else
        {
            pose = 0;
        }
        foreach (Alien alien in aliens)
        {
            alien.SetPose(pose);
        }
    }

    void CheckIfLanded()
    {
        // Find the lowest point of the fleet.
        float bottom = 1000;
        foreach (Alien alien in aliens)
        {
            float alienBottom = alien.transform.position.y - alienHalfHeight;
            if (alienBottom < bottom)
            {
                bottom = alienBottom;
            }
        }

        if (bottom <= landingHeight)
        {
            enabled = false;   // stop the fleet
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Lose("The aliens have landed!");
            }
        }
    }

    // Called by an alien when it's destroyed.
    public void AlienDestroyed(Alien alien)
    {
        aliens.Remove(alien);
        alienCounter.SetCount(aliens.Count);

        // Sometimes drop a power-up of a random kind.
        if (Random.value < powerUpDropChance)
        {
            PowerUpType type = (PowerUpType)Random.Range(0, 3);
            PowerUp.Spawn(alien.transform.position, type);
        }

        if (aliens.Count == 0 && GameManager.Instance != null)
        {
            GameManager.Instance.Win();
        }
    }
}
