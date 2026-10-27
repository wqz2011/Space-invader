# Space Invaders 3D – Assignment 1

**Course:** Video Game Design and Development, Fall 2026 – CSCI 381

**Starting point:** the in-class project [Oren-Steinberg/Space-Invaders-3D](https://github.com/Oren-Steinberg/Space-Invaders-3D)

**Engine:** Unity 6000.6.0f1

## How to run

1. Open the `Space-Invaders-3D` folder in Unity Hub with Unity **6000.6.0f1**.
2. Open `Assets/Scenes/SampleScene.unity`. The scene already shows the background, aliens and ship in the editor.
3. Press **Play**, then **Space** or **Enter** on the title screen.

| Key | Action |
|---|---|
| Left / Right arrows, or A / D | Move the ship |
| Space | Fire (hold during Rapid Fire) |
| Space / Enter | Start the game; play again after winning or losing |
| Esc | Quit (in a built game) |

---

## How the requirements are met

### Required (all of the following)

| Requirement | How it's met | Where |
|---|---|---|
| **Make the remaining aliens speed up as their compatriots are destroyed** | Fleet speed grows with the fraction of aliens destroyed: from `baseSpeed` (4 units/s) with a full fleet to `maxSpeedMultiplier` × that (20 units/s) for the last alien. The marching animation also speeds up to match. | `AlienFleet.CurrentSpeed`, `AlienFleet.Animate` |
| **Detect when the aliens have reached a point where the game can no longer be won** | Every physics step the fleet measures its lowest alien. Once the bottom of the fleet reaches the top of the spaceship, the ship can't escape, so the game ends with "GAME OVER – The aliens have landed!". | `AlienFleet.FixedUpdate`, `GameManager.Lose` |
| **Prevent the spaceship from moving offscreen** | The ship's x position is clamped to the camera's visible width, minus how far the ship's model extends left and right. It's computed from the camera every frame, so it works at any window size or aspect ratio. | `Spaceship.ClampToScreen` |
| **Find at least three ways the code is inefficient, non-idiomatic or poorly written, and fix them** | Six issues were found and fixed (see below). | see [Code issues fixed](#code-issues-found-and-fixed) |

### Chosen improvements (four required)

All four are from the assignment's list:

| Option | What was done | Where |
|---|---|---|
| **Add a starting and ending screen** | A title screen with controls and a power-up legend. A **YOU WIN** screen with your clear time and a **GAME OVER** screen with the reason and aliens remaining. Space/Enter plays again. The game is paused behind each screen. | `GameManager` |
| **Replace the background** | A pixel-art picture of Earth and the Moon, matching the pixel-art ships, shown on an unlit quad behind the play area and scaled to fill the screen without stretching. The camera clears to black. *(Image AI-generated with Google Gemini.)* | `Starfield`, `Assets/Resources/SpaceBackground.jpg` |
| **Add animations when collisions occur** | Every destruction plays an explosion: tumbling cube debris in the destroyed object's color plus a short flash of light. Used for aliens, the spaceship (larger), alien bullets shot down by darts, and collected power-ups. | `Explosion` |
| **Replace the aliens and spaceship with better graphics** | 3D "voxel" versions of the classic arcade sprites. There are three alien types with two-frame marching animations, a voxel fighter ship, and red zigzag alien bullets. Details below. | `VoxelModel`, `Alien`, `Spaceship`, `AlienBullet` |

### Extras beyond the requirements

- **Game balancing** : the game is harder but still beatable. See [Balancing](#game-balancing).
- **Random power-up drops** (a custom "other idea"): destroyed aliens sometimes drop power-ups. See [Power-ups](#power-ups).

---

## Code issues found and fixed

1. **Redundant component lookup.** `Alien` already stored an `AlienFleet` reference, but called `fleet.GetComponent<AlienFleet>()` on every hit to find the fleet it already had. It now calls `fleet.AlienDestroyed(this)` directly.

2. **Buggy and wasteful fleet edge detection.** The fleet's left/right extents started at `0` instead of `float.MaxValue` / `float.MinValue`. Whenever every alien was on one side of x = 0, the true edge was wrong. The code also looped over every alien twice per physics step, moving each one separately.
   **Fix:** aliens are now children of the fleet, so the whole fleet moves with **one** transform change. A single loop measures left, right and bottom. The hard-coded ±16 bounds now come from the camera's view.

3. **UI text rebuilt every frame.** `AlienFleet.Update` copied the alien count into `AlienCounter` every frame, and `AlienCounter.Update` built a new string every frame, creating garbage for no reason.
   **Fix:** the text is updated only when an alien dies (`AlienCounter.SetCount`).

4. **Misused physics.** Aliens, bullets and darts were non-kinematic Rigidbodies moved by setting `transform.position`, which fights the physics engine and makes objects bounce. Darts collided with each other, and alien bullets destroyed each other. `OnCollisionEnter` called `Destroy(coll.gameObject)`, destroying *whatever* it touched. The scene also added a **second** Rigidbody to the Spaceship, which already had one in its prefab.
   **Fix:** kinematic Rigidbodies with trigger colliders moved via `Rigidbody.MovePosition`, and `OnTriggerEnter` handlers that check what they hit (`Dart`, `Spaceship`). The duplicate Rigidbody is removed.

5. **Frame-rate-dependent, hidden tuning values.** Speeds were "units per physics step" (e.g. `.1f`), so changing the physics timestep changed the game's speed. They were `static`, so they couldn't be tuned in the Inspector. `Invoke("startSidewaysMotion", …)` used a string method name, which breaks silently if the method is renamed.
   **Fix:** speeds are in units per second × `Time.fixedDeltaTime`, as normal Inspector fields. The fleet's drop is handled directly in `FixedUpdate`.

6. **Smaller cleanups.** Removed empty `Start`/`Update` methods (Unity still calls them every frame), unused `using` directives and commented-out dead code. The Spaceship's `shotCooldown` did nothing, because firing already required a new key press; it is now a real cooldown.

---

## Changes from the original version

### Gameplay flow (`GameManager`, new)
- Title, win and lose screens, built in code under the existing Canvas.
- Tracks the game state (`Title`, `Playing`, `Won`, `Lost`). Gameplay scripts only respond to input while playing.
- After a win or loss, the final explosion plays for 1.5 s before the end screen appears. "Play again" reloads the scene and skips the title screen.

### Graphics
- **`VoxelModel` (new):** turns a pixel-art pattern written as strings (`X` = a cube) into a single 3D mesh. Faces hidden between neighboring cubes are skipped, and each pattern is built once and cached, so a voxel alien costs no more to draw than the old sphere.
- **Aliens:** pink **squids** (top row), green **crabs** (middle) and cyan **octopuses** (bottom), each with two poses that alternate as the fleet marches. The hitbox is a box fitted to the shape.
- **Spaceship:** a voxel fighter with a white hull, cyan cockpit, red wing lights and flickering orange engines. Same pixel size as the aliens. Darts now launch from the nose.
- **Alien bullets:** bright red, unlit zigzag bolts that wiggle as they fall, easy to see against the dark blue background.
- **Background:** the pixel-art Earth-and-Moon picture. If that file is missing, the game falls back to drifting parallax stars and a cratered moon.

### Game balancing

The original was too easy: every alien fired at random (most shots came from the back rows), bullets were slow, and the player could fire as fast as they could press Space.

| Setting | Original | Now |
|---|---|---|
| Fleet size | 10 × 3 = 30 aliens | 11 × 4 = 44 aliens |
| Fleet speed | 5 units/s, constant | 4 → 20 units/s as aliens die |
| Drop at each screen edge | ~0.5 | 0.6 |
| Who fires | any alien, at random | only the lowest alien in each column (like the arcade game) |
| Aimed shots | none | 35% of shots come from the column above the player |
| Fleet fire rate | ~3 shots/s, mostly from the back | 1.2 → 2.5 shots/s as the fleet shrinks |
| Alien bullet speed | 5 units/s | 8 units/s |
| Player fire cooldown | none in practice | 0.35 s |

It stays beatable: darts can shoot down alien bullets, and only one alien per column can fire at a time. Firing is now decided once per physics step by the fleet instead of by every alien separately, which is also cheaper.

### Power-ups 

Each destroyed alien has an 8% chance to drop a spinning, lettered crate. Catch it with the ship for 8 seconds of:

- **[M] Multi-shot:** three darts in a spread.
- **[S] Speed boost:** 1.7× movement speed.
- **[R] Rapid fire:** hold Space to auto-fire.

Active power-ups and their remaining seconds are shown in the top-right corner.


### Asset and scene changes
- **Prefabs:** `Alien`, `AlienBullet` and `Dart` now use kinematic Rigidbodies with trigger colliders.
- **Scene:**
  - Added a **GameManager** object with `GameManager` and `EditModePreview`.
  - Removed the duplicate Spaceship Rigidbody.
  - The camera clears to solid black.
  - The alien counter is anchored to the top-left corner (it was positioned off-center).
- **New assets in `Assets/Resources`:** `SpaceBackground.jpg` (background picture), `BackgroundUnlit.mat` (unlit material for the picture, engines and bullets) and `ParticleUnlit.mat` (explosion debris).

### Files
| File | Status |
|---|---|
| `Alien.cs`, `AlienBullet.cs`, `AlienCounter.cs`, `AlienFleet.cs`, `Dart.cs`, `Spaceship.cs` | Modified |
| `GameManager.cs`, `Explosion.cs`, `PowerUp.cs`, `Starfield.cs`, `VoxelModel.cs`, `EditModePreview.cs` | New |

---

