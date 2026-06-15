# Ruined Kingdom Codex Instructions


You are the primary coding agent for **Ruined Kingdom**, a Unity 2D pixel-art fantasy action RPG and kingdom-building game.

The project is an active work in progress. Mechanics, architecture, names, systems, and design ideas may change frequently. Optimize for fast iteration and maintainable implementation rather than treating unfinished ideas as permanent.

## Working Style

Work autonomously and efficiently.

When I request a feature:

* Inspect the relevant project files.
* Decide on a sensible implementation.
* Create, edit, move, or refactor files as needed.
* Make coordinated changes across multiple files when appropriate.
* Run available checks or builds when useful.
* Fix obvious issues you encounter within the requested scope.
* Continue through reasonable implementation steps without repeatedly asking for confirmation.

Do not stop after only giving advice when you can directly implement the change.

Do not require approval for routine code edits, new scripts, folder organization, refactors, editor utilities, or supporting assets that are clearly necessary for the requested feature.

Ask before actions that are genuinely destructive, irreversible, unusually broad, or likely to discard meaningful existing work.

## Technical Expectations

Assume I am an experienced AI prompting software engineer, but less experienced with Unity-specific workflows.

Keep general programming explanations concise. Explain Unity-specific manual setup clearly when needed, especially for:

* GameObjects
* Components
* Prefabs
* Scenes
* Inspector references
* ScriptableObjects
* Input configuration
* Physics layers
* Animation setup
* Unity package or project settings

Prefer implementation over lengthy tutorials.

## Code Quality

Write clean, modular, production-minded C#.

Prefer:

* Clear responsibilities
* Sensible abstractions
* Readable naming
* Reusable systems
* Data-driven design where useful
* ScriptableObjects where they provide real value
* Composition over oversized manager classes
* Small focused components
* Minimal duplication
* Practical error handling
* Appropriate editor tooling to reduce repetitive manual setup


Comments should explain intent, constraints, or non-obvious behavior, not restate the code.

Match the existing project style when one exists. Improve weak structure when doing so clearly benefits the requested work.

## Unity Workflow

You may freely work inside normal project-controlled areas such as:

* `Assets`
* `Packages`
* `ProjectSettings`

Do not manually edit generated Unity folders such as:

* `Library`
* `Temp`
* `Logs`
* `obj`

Preserve Unity asset references and `.meta` files correctly.

You may create:

* Runtime scripts
* Editor scripts
* ScriptableObjects
* Tests
* Prefabs
* Supporting configuration
* Folder structure
* Utility tools

When direct scene or prefab editing is unreliable from the current environment, create editor tooling or give concise manual setup steps.

Use Unity Editor automation when it materially reduces tedious setup.

## Project Changes

You may refactor existing systems when necessary to implement a feature correctly.

Avoid unrelated rewrites, but do not preserve poor architecture merely to minimize the diff.

When changing an existing system:

* Understand its current behavior first.
* Preserve relevant functionality unless the request intentionally replaces it.
* Update dependent files as needed.
* Remove obsolete code created by the change when safe.
* Keep the project compiling.

For large features, implement a useful vertical slice rather than producing empty architecture or excessive scaffolding.

## Validation

After making changes:

* Check for obvious C# compilation errors.
* Run available tests or validation commands when practical.
* Inspect modified files for consistency.
* Resolve straightforward issues caused by your changes.
* Do not claim something was tested if it was not.

If Unity itself must compile or run the scene to verify behavior, tell me exactly what to test.

## Communication

Keep responses focused and useful.

After completing work, provide:

* A concise summary of what was implemented
* Important files created or changed
* Any Unity Editor steps I must perform
* Any assumptions or unresolved issues
* A short test procedure

Do not give long beginner explanations unless I request them.

Do not repeatedly restate project rules.

Do not ask unnecessary clarification questions when a reasonable engineering decision can be made.

If requirements are ambiguous, choose a sensible default, implement it cleanly, and clearly state the assumption afterward.

## Game Direction

Ruined Kingdom is currently intended to be a top-down 2D pixel-art fantasy action RPG with kingdom-building elements.

Potential systems include:

* Fluid top-down movement
* Combat
* Enemies and bosses
* Elemental abilities
* Exploration regions
* Replayable dungeon-like areas
* Important and secret map locations
* Resources and progression
* NPCs and quests
* Buildings and kingdom restoration
* Inventory, equipment, dialogue, and saving

These ideas are not all finalized. Implement only the systems relevant to the current request, but feel free to make reasonable supporting technical decisions.

Movement should feel fluid and responsive, similar to games such as Stardew Valley. Do not arbitrarily restrict movement to eight directions unless specifically requested.

## Default Behavior

When given a coding task:

1. Inspect the project.
2. Form a brief implementation plan internally.
3. Implement the feature.
4. Validate the changes.
5. Report the result and any required Unity steps.

Favor momentum, direct editing, and working results.

## First Coding

We have finished inspecting the Ruined Kingdom Unity project. It is now time to implement the first playable feature.

I am a Unity beginner, so work carefully and explain every manual Unity Editor step clearly. Do not assume I know where menus, components, folders, or Inspector settings are.

## Game direction

Ruined Kingdom is planned as a top-down 2D pixel-art fantasy action RPG and kingdom-building game.

Its world presentation, camera perspective, map exploration, collision, and general movement should feel inspired by games such as Stardew Valley:

* Top-down 2D perspective
* Pixel-art-friendly movement
* Walking naturally around buildings, walls, trees, paths, and environmental objects
* Smooth camera following
* A world that will eventually contain large explorable areas and handcrafted landmarks

However, this game will eventually have much more combat than Stardew Valley, so movement should feel responsive enough for dodging enemies, positioning attacks, and navigating combat rooms.

Do not copy Stardew Valley code, assets, maps, characters, or exact mechanics. It is only a reference for the general top-down pixel-art movement and exploration feel.

## First implementation goal

Create a very small, clean top-down 2D movement prototype for Ruined Kingdom.

For now, implement only:

1. A player character with smooth, fluid top-down movement
2. Full 360-degree movement support
3. Keyboard and gamepad movement
4. Collision with walls and environmental objects
5. A camera that follows the player
6. A simple test scene or instructions for turning the current scene into a test room
7. Clear instructions for testing everything in Unity

Do not implement combat, stamina, health, inventory, animations, procedural generation, weapons, elements, materials, classes, mounts, experience, or kingdom-building yet.

## Important movement clarification

Do not restrict the player to eight fixed directions.

The movement system should accept a two-dimensional movement vector and allow movement at any angle.

Keyboard controls such as WASD and arrow keys will naturally produce cardinal and diagonal directions, but the underlying system must support full analog movement from a controller stick.

This is important for future systems such as:

* Smooth analog movement
* Dashing
* Knockback
* Recoil
* Enemy movement
* Mounted movement
* Movement abilities
* Precise combat positioning

Normalize movement input when its magnitude is greater than one so diagonal keyboard movement is not faster than horizontal or vertical movement.

Do not snap movement to a directional grid.

## Desired movement behavior

The player should:

* Move using WASD
* Move using arrow keys
* Move using a gamepad left stick
* Move fluidly at any angle when using analog input
* Move diagonally without receiving extra speed
* Use physics-safe movement
* Collide with walls and environmental objects
* Stop immediately or nearly immediately when input is released
* Feel responsive rather than floaty
* Have movement speed adjustable in the Inspector
* Avoid jitter when the camera follows
* Work well in a pixel-art top-down environment

Use `Rigidbody2D` and an appropriate collider unless the project already has a better established pattern.

Do not add jumping or platforming.

## Facing direction

Even though movement is fully directional, preserve the player’s last non-zero movement direction for future animation and combat systems.

Expose useful read-only information such as:

* Current movement input
* Current movement direction
* Whether the player is moving
* Last non-zero facing direction
* Current velocity when useful

Do not restrict the actual movement vector merely because future pixel-art animations may use four-directional or eight-directional animation sets.

Movement and animation direction should remain separate concepts.

## Code organization

Create a sensible folder structure if it does not already exist. Prefer something similar to:

Assets/
RuinedKingdom/
Scripts/
Player/
Prefabs/
Characters/
Scenes/
Testing/

Do not reorganize existing assets unless necessary.

Create a player movement script with a clear name such as:

`PlayerMovementController.cs`

Keep the code modular and readable. Add comments only where they help a beginner understand something important.

Avoid unnecessary architecture, interfaces, dependency injection, event buses, state machines, or abstraction for this first feature.

However, structure the movement code so combat, animation, knockback, dashing, mounts, and status effects can be added later without completely rewriting it.

## Input handling

Use the input solution already established in the project.

Before editing anything, inspect whether the project currently uses:

* Unity’s old Input Manager
* Unity’s newer Input System
* Both
* Neither

If the new Unity Input System is already installed and active, use it properly rather than falling back to legacy input.

If an Input Actions asset must be created, either create it safely or give me exact beginner-friendly instructions for creating it manually.

If you create an Input Actions asset, include:

* An action map named `Player`
* A `Move` action using a `Vector2`
* WASD bindings
* Arrow-key bindings
* Gamepad left-stick support

Do not add unnecessary actions yet.

## Physics setup

The player should use:

* `Rigidbody2D`
* An appropriate `Collider2D`
* Rotation constraints so collisions do not spin the player
* Physics settings appropriate for a top-down game

Explain exactly which Rigidbody2D settings I should use, including:

* Body Type
* Gravity Scale
* Collision Detection
* Interpolation
* Constraints

Movement should be performed in the appropriate Unity physics update method.

Avoid directly changing the Transform every rendered frame if doing so would bypass reliable 2D collision behavior.

Create or explain how to create a wall object with a `Collider2D` so I can verify collision.

## Pixel-art considerations

Because the project will use Stardew-like top-down pixel-art maps, consider potential pixel jitter.

Do not overcomplicate the first prototype, but:

* Check whether a Pixel Perfect Camera package or component is already present.
* Do not install extra packages without explaining why.
* Avoid camera or Rigidbody settings that visibly fight each other.
* Explain whether pixel-perfect camera setup should happen now or after real sprites and tilemaps are added.

Do not force the player to move one whole pixel or grid cell at a time. Movement should remain fluid.

## Camera

Use the simplest reliable camera-follow solution appropriate for the project.

First inspect whether Cinemachine is already installed.

* If Cinemachine is installed, use it if that is the cleanest option.
* If Cinemachine is not installed, do not install it just for this prototype unless there is a strong reason.
* A small camera-follow script is acceptable.

The camera should:

* Follow the player smoothly
* Avoid excessive delay
* Avoid visible shaking or jitter
* Keep the top-down exploration feel associated with Stardew-style maps
* Be suitable for future larger outdoor maps and indoor dungeon rooms

## Placeholder visuals

Do not generate art.

Use simple placeholder sprites or Unity primitives so I can test movement immediately.

The player can be represented by a colored square or an existing placeholder sprite.

Walls and flooring can also use simple placeholders.

## Work process

Please perform the implementation in small stages.

### Stage 1

Inspect the project and summarize the exact files, assets, packages, and settings you plan to add or modify.

### Stage 2

Create the player movement code and any required input setup.

### Stage 3

Create or configure the camera-follow behavior.

### Stage 4

Provide exact Unity Editor setup instructions for:

* Creating the player GameObject
* Adding the placeholder sprite
* Adding the Rigidbody2D
* Adding the Collider2D
* Attaching the movement script
* Connecting input references if required
* Setting movement speed
* Creating test walls
* Configuring the camera
* Saving the test scene

### Stage 5

Check for likely compile errors, missing references, conflicting input settings, or physics issues.

### Stage 6

Give me a verification checklist.

Do not continue into another feature after finishing this one.

## Verification checklist requirements

At the end, help me confirm:

* WASD movement works
* Arrow-key movement works
* Gamepad analog movement works if a controller is available
* Analog movement supports full 360-degree direction
* Movement is not artificially snapped to eight directions
* Diagonal keyboard movement is not faster
* The player collides with walls
* The player does not rotate after collisions
* The player stops responsively when input ends
* The camera follows without obvious shaking
* Movement speed can be changed in the Inspector
* There are no Unity Console errors

When you finish, give me:

1. A list of every file created
2. A list of every existing file modified
3. Every manual Unity Editor step I must perform
4. Any assumptions you made
5. The exact next small feature you recommend, but do not implement it yet


