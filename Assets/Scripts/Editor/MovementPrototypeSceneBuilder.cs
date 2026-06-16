using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class MovementPrototypeSceneBuilder
{
    const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
    const string MoveReferencePath = "Assets/RuinedKingdom/Input/PlayerMove.inputactionreference.asset";
    const string AttackReferencePath = "Assets/RuinedKingdom/Input/PlayerAttack.inputactionreference.asset";
    const string PlayerSpritePath = "Assets/Sprites/Player.png";
    const string TileSpritePath = "Assets/Tiles/GrassSquare.png";

    [MenuItem("Tools/Ruined Kingdom/Create Movement Test Room")]
    public static void CreateMovementTestRoom()
    {
        InputActionReference moveReference = GetOrCreateMoveActionReference();
        InputActionReference attackReference = GetOrCreateInputActionReference("Player/Attack", AttackReferencePath);
        Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        Sprite tileSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);

        GameObject roomRoot = GetOrCreateGameObject("Movement Test Room", Vector3.zero);
        GameObject player = ConfigurePlayer(playerSprite, moveReference, attackReference);
        ConfigureEnemy(roomRoot.transform, "Training Yard Enemy", tileSprite, player.transform, new Vector3(3.75f, 1.75f, 0f), new Color(0.85f, 0.12f, 0.1f), 75f, 2.2f);
        ConfigureCamera(player.transform);
        ConfigureDebugOverlay(player.GetComponent<PlayerMovementController>());
        ConfigureFloor(roomRoot.transform, tileSprite);
        ConfigureWalls(roomRoot.transform, tileSprite);
        ConfigureHubAdventureScaffold(roomRoot.transform, tileSprite, player);

        Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    static GameObject ConfigurePlayer(Sprite playerSprite, InputActionReference moveReference, InputActionReference attackReference)
    {
        GameObject player = GetOrCreateGameObject("Player", Vector3.zero);
        player.transform.localScale = Vector3.one;

        SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(player);
        spriteRenderer.sprite = playerSprite;
        spriteRenderer.color = new Color(0.56f, 0.2f, 0.9f);
        spriteRenderer.sortingOrder = 10;
        spriteRenderer.enabled = false;

        Rigidbody2D rb = GetOrAddComponent<Rigidbody2D>(player);
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        CapsuleCollider2D collider = GetOrAddComponent<CapsuleCollider2D>(player);
        collider.size = new Vector2(0.52f, 0.32f);
        collider.offset = new Vector2(0f, -0.28f);

        PlayerController2D oldController = player.GetComponent<PlayerController2D>();
        if (oldController != null)
        {
            oldController.enabled = false;
        }

        PlayerMovementController movement = GetOrAddComponent<PlayerMovementController>(player);
        SerializedObject movementObject = new SerializedObject(movement);
        movementObject.FindProperty("moveSpeed").floatValue = 5f;
        movementObject.FindProperty("moveAction").objectReferenceValue = moveReference;
        movementObject.ApplyModifiedProperties();

        HealthComponent health = GetOrAddComponent<HealthComponent>(player);
        SerializedObject healthObject = new SerializedObject(health);
        healthObject.FindProperty("maxHealth").floatValue = 100f;
        healthObject.FindProperty("currentHealth").floatValue = 100f;
        healthObject.FindProperty("regenerationPerSecond").floatValue = 3f;
        healthObject.FindProperty("destroyWhenEmpty").boolValue = false;
        healthObject.ApplyModifiedProperties();

        StaminaComponent stamina = GetOrAddComponent<StaminaComponent>(player);
        SerializedObject staminaObject = new SerializedObject(stamina);
        staminaObject.FindProperty("maxStamina").floatValue = 100f;
        staminaObject.FindProperty("currentStamina").floatValue = 100f;
        staminaObject.FindProperty("regenerationPerSecond").floatValue = 28f;
        staminaObject.FindProperty("regenerationDelayAfterSpend").floatValue = 0.45f;
        staminaObject.ApplyModifiedProperties();

        PlayerInventoryHudController inventory = GetOrAddComponent<PlayerInventoryHudController>(player);
        SerializedObject inventoryObject = new SerializedObject(inventory);
        inventoryObject.FindProperty("health").objectReferenceValue = health;
        inventoryObject.FindProperty("stamina").objectReferenceValue = stamina;
        inventoryObject.ApplyModifiedProperties();
        inventory.ApplyStartingClass(CharacterClass.Mercenary);

        PlayerCombatController combat = GetOrAddComponent<PlayerCombatController>(player);
        CharacterVisualApplier visualApplier = ConfigureCharacterVisuals(player.transform, playerSprite);
        ConfigureWeapon(player.transform, "Sword Weapon", new Color(0.9f, 0.9f, 1f), 25, out Transform playerWeaponPivot, out Transform playerWeaponBlade, out SpriteRenderer playerWeaponRenderer);

        SerializedObject combatObject = new SerializedObject(combat);
        combatObject.FindProperty("movementController").objectReferenceValue = movement;
        combatObject.FindProperty("health").objectReferenceValue = health;
        combatObject.FindProperty("stamina").objectReferenceValue = stamina;
        combatObject.FindProperty("inventory").objectReferenceValue = inventory;
        combatObject.FindProperty("attackAction").objectReferenceValue = attackReference;
        combatObject.FindProperty("weaponPivot").objectReferenceValue = playerWeaponPivot;
        combatObject.FindProperty("weaponBlade").objectReferenceValue = playerWeaponBlade;
        combatObject.FindProperty("weaponRenderer").objectReferenceValue = playerWeaponRenderer;
        combatObject.FindProperty("lightDamage").floatValue = 18f;
        combatObject.FindProperty("lightStaminaCost").floatValue = 12f;
        combatObject.FindProperty("lightCooldown").floatValue = 0.28f;
        combatObject.FindProperty("strongDamage").floatValue = 36f;
        combatObject.FindProperty("strongStaminaCost").floatValue = 28f;
        combatObject.FindProperty("strongCooldown").floatValue = 0.65f;
        combatObject.FindProperty("chargeDamage").floatValue = 55f;
        combatObject.FindProperty("chargeStaminaCost").floatValue = 40f;
        combatObject.FindProperty("minimumChargeTime").floatValue = 0.55f;
        combatObject.FindProperty("maximumChargeTime").floatValue = 1.5f;
        combatObject.FindProperty("chargeCooldown").floatValue = 0.9f;
        combatObject.FindProperty("dodgeStaminaCost").floatValue = 25f;
        combatObject.FindProperty("dodgeDistance").floatValue = 1.8f;
        combatObject.FindProperty("dodgeDuration").floatValue = 0.18f;
        combatObject.FindProperty("dodgeCooldown").floatValue = 0.35f;
        combatObject.FindProperty("jumpStaminaCost").floatValue = 18f;
        combatObject.FindProperty("jumpDistance").floatValue = 0.9f;
        combatObject.FindProperty("jumpDuration").floatValue = 0.32f;
        combatObject.FindProperty("jumpCooldown").floatValue = 0.55f;
        combatObject.FindProperty("hitboxForwardOffset").floatValue = 0.85f;
        combatObject.ApplyModifiedProperties();

        ConfigureFacingIndicator(player.transform, movement);
        DestroyChildIfExists(player.transform, "Health Bar");
        DestroyChildIfExists(player.transform, "Stamina Bar");
        ConfigurePlayerInteraction(player);
        ConfigureYSort(player);

        return player;
    }

    static void ConfigureCamera(Transform target)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = GetOrCreateGameObject("Main Camera", new Vector3(0f, 0f, -10f));
            camera = GetOrAddComponent<Camera>(cameraObject);
            cameraObject.tag = "MainCamera";
        }

        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = target.position + new Vector3(0f, 0f, -10f);

        CameraFollow2D follow = GetOrAddComponent<CameraFollow2D>(camera.gameObject);
        SerializedObject followObject = new SerializedObject(follow);
        followObject.FindProperty("target").objectReferenceValue = target;
        followObject.FindProperty("followSmoothTime").floatValue = 0.1f;
        followObject.FindProperty("offset").vector3Value = new Vector3(0f, 0f, -10f);
        followObject.ApplyModifiedProperties();
    }

    static void ConfigurePlayerInteraction(GameObject player)
    {
        GameObject systems = GetOrCreateGameObject("Ruined Kingdom Runtime Systems", Vector3.zero);
        DialogueHudController dialogueHud = GetOrAddComponent<DialogueHudController>(systems);
        GetOrAddComponent<HubWorldClockController>(systems);

        PlayerInteractionController interaction = GetOrAddComponent<PlayerInteractionController>(player);
        SerializedObject interactionObject = new SerializedObject(interaction);
        interactionObject.FindProperty("interactionRange").floatValue = 1.15f;
        interactionObject.FindProperty("interactionLayers").intValue = ~0;
        interactionObject.FindProperty("dialogueHud").objectReferenceValue = dialogueHud;
        interactionObject.ApplyModifiedProperties();
    }

    static void ConfigureFacingIndicator(Transform player, PlayerMovementController movement)
    {
        GameObject indicator = GetOrCreateChild(player, "Facing Indicator", Vector3.down * 0.65f);
        indicator.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

        SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(indicator);
        spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);
        spriteRenderer.color = new Color(1f, 0.9f, 0.2f);
        spriteRenderer.sortingOrder = 20;

        PlayerFacingIndicator2D facingIndicator = GetOrAddComponent<PlayerFacingIndicator2D>(indicator);
        SerializedObject indicatorObject = new SerializedObject(facingIndicator);
        indicatorObject.FindProperty("movementController").objectReferenceValue = movement;
        indicatorObject.FindProperty("indicator").objectReferenceValue = indicator.transform;
        indicatorObject.FindProperty("distanceFromPlayer").floatValue = 0.65f;
        indicatorObject.ApplyModifiedProperties();
    }

    static void ConfigureEnemy(Transform parent, string enemyName, Sprite enemySprite, Transform player, Vector3 position, Color bodyColor, float maxHealth, float moveSpeed)
    {
        GameObject enemy = GetOrCreateChild(parent, enemyName, position);
        enemy.transform.localScale = Vector3.one;

        SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(enemy);
        spriteRenderer.sprite = enemySprite;
        spriteRenderer.color = bodyColor;
        spriteRenderer.sortingOrder = 10;
        spriteRenderer.enabled = false;

        Rigidbody2D rb = GetOrAddComponent<Rigidbody2D>(enemy);
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(enemy);
        collider.size = new Vector2(0.58f, 0.36f);
        collider.offset = new Vector2(0f, -0.28f);

        HealthComponent health = GetOrAddComponent<HealthComponent>(enemy);
        SerializedObject healthObject = new SerializedObject(health);
        healthObject.FindProperty("maxHealth").floatValue = maxHealth;
        healthObject.FindProperty("currentHealth").floatValue = maxHealth;
        healthObject.FindProperty("regenerationPerSecond").floatValue = 0f;
        healthObject.FindProperty("destroyWhenEmpty").boolValue = true;
        healthObject.ApplyModifiedProperties();

        EnemyCombatController enemyCombat = GetOrAddComponent<EnemyCombatController>(enemy);
        ConfigureEnemyVisuals(enemy.transform, enemySprite, bodyColor);
        ConfigureWeapon(enemy.transform, "Sword Weapon", new Color(0.95f, 0.7f, 0.6f), 25, out Transform enemyWeaponPivot, out Transform enemyWeaponBlade, out SpriteRenderer enemyWeaponRenderer);

        SerializedObject enemyObject = new SerializedObject(enemyCombat);
        enemyObject.FindProperty("target").objectReferenceValue = player;
        enemyObject.FindProperty("targetHealth").objectReferenceValue = player.GetComponent<HealthComponent>();
        enemyObject.FindProperty("moveSpeed").floatValue = moveSpeed;
        enemyObject.FindProperty("detectionRange").floatValue = 6f;
        enemyObject.FindProperty("stopDistance").floatValue = 1.05f;
        enemyObject.FindProperty("damage").floatValue = 8f;
        enemyObject.FindProperty("attackCooldown").floatValue = 0.8f;
        enemyObject.FindProperty("weaponPivot").objectReferenceValue = enemyWeaponPivot;
        enemyObject.FindProperty("weaponBlade").objectReferenceValue = enemyWeaponBlade;
        enemyObject.FindProperty("weaponRenderer").objectReferenceValue = enemyWeaponRenderer;
        enemyObject.ApplyModifiedProperties();

        ConfigureResourceBar(enemy.transform, "Health Bar", new Vector3(0f, 0.75f, 0f), new Color(0.9f, 0.1f, 0.12f), health, null, WorldResourceBar2D.ResourceType.Health);
        ConfigureYSort(enemy);
    }

    static CharacterVisualApplier ConfigureCharacterVisuals(Transform owner, Sprite sprite)
    {
        GameObject visuals = GetOrCreateChild(owner, "Visuals", Vector3.zero);

        SpriteRenderer shadow = ConfigureSpriteLayer(visuals.transform, "Shadow", sprite, new Vector3(0f, -0.42f, 0f), new Vector3(0.78f, 0.22f, 1f), new Color(0f, 0f, 0f, 0.32f), 0);
        SpriteRenderer body = ConfigureSpriteLayer(visuals.transform, "Body Primary", sprite, new Vector3(0f, 0f, 0f), new Vector3(0.78f, 1.05f, 1f), new Color(0.56f, 0.2f, 0.9f), 1);
        SpriteRenderer armor = ConfigureSpriteLayer(visuals.transform, "Armor Secondary", sprite, new Vector3(0f, 0.08f, 0f), new Vector3(0.9f, 0.42f, 1f), new Color(0.72f, 0.68f, 0.58f), 2);
        ConfigureSpriteLayer(visuals.transform, "Head Placeholder", sprite, new Vector3(0f, 0.44f, 0f), new Vector3(0.56f, 0.42f, 1f), new Color(0.95f, 0.78f, 0.56f), 3);

        CharacterVisualApplier applier = GetOrAddComponent<CharacterVisualApplier>(visuals);
        SerializedObject applierObject = new SerializedObject(applier);
        applierObject.FindProperty("primaryRenderer").objectReferenceValue = body;
        applierObject.FindProperty("secondaryRenderer").objectReferenceValue = armor;
        applierObject.FindProperty("weaponRenderer").objectReferenceValue = null;
        applierObject.ApplyModifiedProperties();

        _ = shadow;
        return applier;
    }

    static void ConfigureEnemyVisuals(Transform owner, Sprite sprite, Color bodyColor)
    {
        GameObject visuals = GetOrCreateChild(owner, "Visuals", Vector3.zero);

        ConfigureSpriteLayer(visuals.transform, "Shadow", sprite, new Vector3(0f, -0.42f, 0f), new Vector3(0.78f, 0.22f, 1f), new Color(0f, 0f, 0f, 0.32f), 0);
        ConfigureSpriteLayer(visuals.transform, "Body", sprite, Vector3.zero, new Vector3(0.82f, 1f, 1f), bodyColor, 1);
        ConfigureSpriteLayer(visuals.transform, "Face", sprite, new Vector3(0f, 0.34f, 0f), new Vector3(0.46f, 0.24f, 1f), new Color(0.18f, 0.03f, 0.03f), 2);
    }

    static SpriteRenderer ConfigureSpriteLayer(Transform parent, string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder)
    {
        GameObject layer = GetOrCreateChild(parent, name, localPosition);
        layer.transform.localScale = localScale;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(layer);
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    static void ConfigureWeapon(
        Transform owner,
        string name,
        Color color,
        int sortingOrder,
        out Transform pivot,
        out Transform blade,
        out SpriteRenderer bladeRenderer)
    {
        GameObject pivotObject = GetOrCreateChild(owner, name, Vector3.down * 0.48f);
        pivotObject.transform.localScale = Vector3.one;
        pivot = pivotObject.transform;

        GameObject bladeObject = GetOrCreateChild(pivot, "Blade", new Vector3(0.38f, 0f, 0f));
        bladeObject.transform.localScale = new Vector3(0.65f, 0.14f, 1f);
        blade = bladeObject.transform;

        bladeRenderer = GetOrAddComponent<SpriteRenderer>(bladeObject);
        bladeRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);
        bladeRenderer.color = color;
        bladeRenderer.sortingOrder = sortingOrder;

        CharacterVisualApplier applier = owner.GetComponentInChildren<CharacterVisualApplier>();
        if (applier != null)
        {
            SerializedObject applierObject = new SerializedObject(applier);
            applierObject.FindProperty("weaponRenderer").objectReferenceValue = bladeRenderer;
            applierObject.ApplyModifiedProperties();
        }
    }

    static void ConfigureResourceBar(
        Transform parent,
        string name,
        Vector3 localPosition,
        Color fillColor,
        HealthComponent health,
        StaminaComponent stamina,
        WorldResourceBar2D.ResourceType resourceType)
    {
        GameObject bar = GetOrCreateChild(parent, name, localPosition);
        bar.transform.localScale = new Vector3(1.1f, 0.12f, 1f);

        GameObject background = GetOrCreateChild(bar.transform, "Background", Vector3.zero);
        background.transform.localScale = Vector3.one;

        SpriteRenderer backgroundRenderer = GetOrAddComponent<SpriteRenderer>(background);
        backgroundRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);
        backgroundRenderer.color = new Color(0.08f, 0.08f, 0.08f);
        backgroundRenderer.sortingOrder = 30;

        GameObject fill = GetOrCreateChild(bar.transform, "Fill", Vector3.zero);
        fill.transform.localScale = Vector3.one;
        fill.transform.localPosition = Vector3.zero;

        SpriteRenderer fillRenderer = GetOrAddComponent<SpriteRenderer>(fill);
        fillRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);
        fillRenderer.color = fillColor;
        fillRenderer.sortingOrder = 31;

        WorldResourceBar2D resourceBar = GetOrAddComponent<WorldResourceBar2D>(bar);
        SerializedObject barObject = new SerializedObject(resourceBar);
        barObject.FindProperty("healthSource").objectReferenceValue = health;
        barObject.FindProperty("staminaSource").objectReferenceValue = stamina;
        barObject.FindProperty("resourceType").enumValueIndex = (int)resourceType;
        barObject.FindProperty("fill").objectReferenceValue = fill.transform;
        barObject.ApplyModifiedProperties();
    }

    static void ConfigureDebugOverlay(PlayerMovementController movement)
    {
        GameObject overlay = GetOrCreateGameObject("Movement Debug Overlay", Vector3.zero);

        MovementDebugOverlay debugOverlay = GetOrAddComponent<MovementDebugOverlay>(overlay);
        SerializedObject overlayObject = new SerializedObject(debugOverlay);
        overlayObject.FindProperty("playerMovement").objectReferenceValue = movement;
        overlayObject.FindProperty("playerCombat").objectReferenceValue = movement.GetComponent<PlayerCombatController>();
        overlayObject.FindProperty("showOverlay").boolValue = true;
        overlayObject.ApplyModifiedProperties();
    }

    static void ConfigureFloor(Transform parent, Sprite tileSprite)
    {
        GameObject floor = GetOrCreateChild(parent, "Test Floor", Vector3.zero);
        floor.transform.localScale = new Vector3(12f, 8f, 1f);

        SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(floor);
        spriteRenderer.sprite = tileSprite;
        spriteRenderer.color = new Color(0.25f, 0.52f, 0.29f);
        spriteRenderer.sortingOrder = -10;

        ConfigureGroundPatch(parent, "Center Dirt Path", tileSprite, Vector3.zero, new Vector3(9f, 1.1f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(parent, "Vertical Dirt Path", tileSprite, Vector3.zero, new Vector3(1.1f, 6.5f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(parent, "Garden Patch NW", tileSprite, new Vector3(-3.5f, 2.2f, 0f), new Vector3(2.4f, 1.3f, 1f), new Color(0.2f, 0.42f, 0.22f));
        ConfigureGroundPatch(parent, "Garden Patch SE", tileSprite, new Vector3(3.8f, -2.2f, 0f), new Vector3(2.4f, 1.3f, 1f), new Color(0.2f, 0.42f, 0.22f));
    }

    static void ConfigureGroundPatch(Transform parent, string name, Sprite sprite, Vector3 position, Vector3 scale, Color color)
    {
        GameObject patch = GetOrCreateChild(parent, name, position);
        patch.transform.localScale = scale;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(patch);
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = -9;
    }

    static void ConfigureWalls(Transform parent, Sprite wallSprite)
    {
        ConfigureWall(parent, "North Wall", wallSprite, new Vector3(0f, 4.5f, 0f), new Vector3(12f, 1f, 1f));
        ConfigureWall(parent, "South Wall", wallSprite, new Vector3(0f, -4.5f, 0f), new Vector3(12f, 1f, 1f));
        ConfigureWall(parent, "West Wall", wallSprite, new Vector3(-6.5f, 0f, 0f), new Vector3(1f, 8f, 1f));
        ConfigureWall(parent, "East Wall", wallSprite, new Vector3(6.5f, 0f, 0f), new Vector3(1f, 8f, 1f));
        ConfigureWall(parent, "Center Block", wallSprite, new Vector3(2f, 0.5f, 0f), new Vector3(2f, 2f, 1f));
        ConfigureProp(parent, "Tree Placeholder", wallSprite, new Vector3(-3.9f, 1.7f, 0f), new Vector3(1.1f, 1.8f, 1f), new Color(0.08f, 0.35f, 0.12f));
        ConfigureProp(parent, "Rock Placeholder", wallSprite, new Vector3(3.6f, -1.8f, 0f), new Vector3(1.1f, 0.85f, 1f), new Color(0.38f, 0.38f, 0.42f));
    }

    static void ConfigureWall(Transform parent, string name, Sprite wallSprite, Vector3 position, Vector3 scale)
    {
        GameObject wall = GetOrCreateChild(parent, name, position);
        wall.transform.localScale = scale;

        SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(wall);
        spriteRenderer.sprite = wallSprite;
        spriteRenderer.color = new Color(0.37f, 0.32f, 0.26f);
        spriteRenderer.sortingOrder = 0;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(wall);
        collider.size = Vector2.one;
        collider.offset = Vector2.zero;
    }

    static void ConfigureProp(Transform parent, string name, Sprite sprite, Vector3 position, Vector3 scale, Color color)
    {
        GameObject prop = GetOrCreateChild(parent, name, position);
        prop.transform.localScale = scale;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(prop);
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 0;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(prop);
        collider.size = new Vector2(0.8f, 0.28f);
        collider.offset = new Vector2(0f, -0.32f);

        ConfigureYSort(prop);
    }

    static void ConfigureHubAdventureScaffold(Transform parent, Sprite sprite, GameObject player)
    {
        Transform hubSpawn = ConfigureSpawn(parent, "Hub Spawn", new Vector3(0f, -1.4f, 0f));
        CameraAreaBounds2D hubBounds = ConfigureAreaBounds(parent, "Hub Camera Bounds", new Vector2(-7f, -5f), new Vector2(7f, 5f));

        if (Camera.main != null)
        {
            hubBounds.ApplyTo(Camera.main.GetComponent<CameraFollow2D>());
        }

        ConfigureHubPlaza(parent, sprite);

        Transform forestSpawn = ConfigureSpawn(parent, "Forest Approach Spawn", new Vector3(0f, 15.2f, 0f));
        Transform seaSpawn = ConfigureSpawn(parent, "Sea Road Spawn", new Vector3(17.4f, 0f, 0f));
        Transform desertSpawn = ConfigureSpawn(parent, "Desert Gate Spawn", new Vector3(0f, -15.2f, 0f));
        Transform volcanoSpawn = ConfigureSpawn(parent, "Volcano Road Spawn", new Vector3(-17.4f, 0f, 0f));
        Transform ruinsSpawn = ConfigureSpawn(parent, "Old Ruins Spawn", new Vector3(17.4f, 15.2f, 0f));

        CameraAreaBounds2D forestBounds = ConfigureAreaBounds(parent, "Forest Camera Bounds", new Vector2(-7f, 10f), new Vector2(7f, 21f));
        CameraAreaBounds2D seaBounds = ConfigureAreaBounds(parent, "Sea Camera Bounds", new Vector2(11f, -5f), new Vector2(24f, 5f));
        CameraAreaBounds2D desertBounds = ConfigureAreaBounds(parent, "Desert Camera Bounds", new Vector2(-7f, -21f), new Vector2(7f, -10f));
        CameraAreaBounds2D volcanoBounds = ConfigureAreaBounds(parent, "Volcano Camera Bounds", new Vector2(-24f, -5f), new Vector2(-11f, 5f));
        CameraAreaBounds2D ruinsBounds = ConfigureAreaBounds(parent, "Ruins Camera Bounds", new Vector2(11f, 10f), new Vector2(24f, 21f));

        ConfigureGate(parent, "North Forest Gate", sprite, new Vector3(0f, 3.15f, 0f), forestSpawn, forestBounds, "Forest Road", "Travel to Forest", "The northern road smells like pine, rain, and trouble.");
        ConfigureGate(parent, "East Sea Gate", sprite, new Vector3(5.2f, 0f, 0f), seaSpawn, seaBounds, "Sea Road", "Travel to Sea", "Salt wind cuts across the road. Fast enemies will live here later.");
        ConfigureGate(parent, "South Desert Gate", sprite, new Vector3(0f, -3.55f, 0f), desertSpawn, desertBounds, "Desert Gate", "Travel to Desert", "The south pass opens toward heat, glass, and endurance fights.");
        ConfigureGate(parent, "West Volcano Gate", sprite, new Vector3(-5.2f, 0f, 0f), volcanoSpawn, volcanoBounds, "Volcano Road", "Travel to Volcano", "The western cliffs glow red. Heavy armor may matter here someday.");
        ConfigureGate(parent, "Old Ruins Gate", sprite, new Vector3(4.35f, 3.2f, 0f), ruinsSpawn, ruinsBounds, "Old Ruins", "Travel to Ruins", "A broken road leads toward older magic and meaner enemies.");

        ConfigureGate(parent, "Forest Return Gate", sprite, new Vector3(0f, 11.5f, 0f), hubSpawn, hubBounds, "Return Road", "Return to Hub", "Back to the central hub.");
        ConfigureGate(parent, "Sea Return Gate", sprite, new Vector3(12.8f, 0f, 0f), hubSpawn, hubBounds, "Return Road", "Return to Hub", "Back to the central hub.");
        ConfigureGate(parent, "Desert Return Gate", sprite, new Vector3(0f, -11.5f, 0f), hubSpawn, hubBounds, "Return Road", "Return to Hub", "Back to the central hub.");
        ConfigureGate(parent, "Volcano Return Gate", sprite, new Vector3(-12.8f, 0f, 0f), hubSpawn, hubBounds, "Return Road", "Return to Hub", "Back to the central hub.");
        ConfigureGate(parent, "Ruins Return Gate", sprite, new Vector3(12.8f, 15.2f, 0f), hubSpawn, hubBounds, "Return Road", "Return to Hub", "Back to the central hub.");

        ConfigureBiomeZone(parent, sprite, player.transform, "Forest", new Vector3(0f, 16f, 0f), new Color(0.12f, 0.42f, 0.18f), new Color(0.03f, 0.22f, 0.08f), new Color(0.1f, 0.55f, 0.16f));
        ConfigureBiomeZone(parent, sprite, player.transform, "Sea", new Vector3(18f, 0f, 0f), new Color(0.1f, 0.48f, 0.62f), new Color(0.03f, 0.2f, 0.44f), new Color(0.1f, 0.75f, 0.95f));
        ConfigureBiomeZone(parent, sprite, player.transform, "Desert", new Vector3(0f, -16f, 0f), new Color(0.68f, 0.52f, 0.24f), new Color(0.45f, 0.31f, 0.12f), new Color(0.95f, 0.68f, 0.18f));
        ConfigureBiomeZone(parent, sprite, player.transform, "Volcano", new Vector3(-18f, 0f, 0f), new Color(0.32f, 0.18f, 0.18f), new Color(0.55f, 0.08f, 0.03f), new Color(0.95f, 0.2f, 0.05f));
        ConfigureBiomeZone(parent, sprite, player.transform, "Ruins", new Vector3(18f, 16f, 0f), new Color(0.28f, 0.28f, 0.34f), new Color(0.18f, 0.16f, 0.22f), new Color(0.55f, 0.22f, 0.95f));

        ConfigureSimpleInteractable(parent, "Hub Guide", sprite, new Vector3(-2.2f, -0.85f, 0f), new Vector3(0.65f, 1f, 1f), new Color(0.95f, 0.78f, 0.28f), "Hub Guide", "Talk", new[]
        {
            "This hub is the heart of the kingdom prototype.",
            "Use E near gates to jump to biome combat spaces. The final game can turn these into real roads, dungeons, and landmarks."
        }, true);

        ConfigureSimpleInteractable(parent, "Prototype Chest", sprite, new Vector3(2.15f, -1.15f, 0f), new Vector3(0.8f, 0.55f, 1f), new Color(0.48f, 0.27f, 0.1f), "Old Chest", "Open", new[]
        {
            "Prototype reward: imagine ores, weapon skills, elemental shards, or Pixicoins here.",
            "No farming required. We are building toward combat routes, loot, and return-to-hub upgrades."
        }, true);
    }

    static void ConfigureHubPlaza(Transform parent, Sprite sprite)
    {
        ConfigureGroundPatch(parent, "Hub Plaza Stone", sprite, Vector3.zero, new Vector3(5.6f, 3.8f, 1f), new Color(0.36f, 0.35f, 0.32f));
        ConfigureGroundPatch(parent, "Hub North Road", sprite, new Vector3(0f, 5.6f, 0f), new Vector3(1.4f, 12f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(parent, "Hub East Road", sprite, new Vector3(6.8f, 0f, 0f), new Vector3(13.6f, 1.2f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(parent, "Hub South Road", sprite, new Vector3(0f, -5.6f, 0f), new Vector3(1.4f, 12f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(parent, "Hub West Road", sprite, new Vector3(-6.8f, 0f, 0f), new Vector3(13.6f, 1.2f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureSimpleInteractable(parent, "Hub Notice Board", sprite, new Vector3(-2.8f, 1.55f, 0f), new Vector3(1.15f, 0.75f, 1f), new Color(0.28f, 0.18f, 0.08f), "Notice Board", "Read", new[]
        {
            "Ruined Kingdom loop target: gear up in the hub, choose a route, fight through danger, bring resources home, unlock the kingdom.",
            "Next good systems: enemy drops, route rewards, a proper upgrade anvil, and NPC services."
        }, true);
    }

    static void ConfigureBiomeZone(Transform parent, Sprite sprite, Transform player, string biomeName, Vector3 center, Color groundColor, Color propColor, Color enemyColor)
    {
        ConfigureGroundPatch(parent, $"{biomeName} Ground", sprite, center, new Vector3(11f, 8f, 1f), groundColor);
        ConfigureGroundPatch(parent, $"{biomeName} Main Path", sprite, center + new Vector3(0f, -1.2f, 0f), new Vector3(8f, 1.1f, 1f), new Color(0.44f, 0.32f, 0.2f));

        ConfigureSimpleInteractable(parent, $"{biomeName} Sign", sprite, center + new Vector3(-3.8f, 2.45f, 0f), new Vector3(1f, 0.7f, 1f), new Color(0.3f, 0.22f, 0.1f), $"{biomeName} Route", "Read", new[]
        {
            $"{biomeName} is a prototype combat route.",
            "For now it is a separate arena around the hub. Later it can become a full explorable region with bosses, resources, secrets, and shortcuts."
        }, true);

        ConfigureProp(parent, $"{biomeName} Blocker A", sprite, center + new Vector3(-2.9f, -1.7f, 0f), new Vector3(1.15f, 1.25f, 1f), propColor);
        ConfigureProp(parent, $"{biomeName} Blocker B", sprite, center + new Vector3(3.1f, 1.2f, 0f), new Vector3(1.25f, 1.45f, 1f), propColor);

        ConfigureEnemy(parent, $"{biomeName} Enemy A", sprite, player, center + new Vector3(2.2f, 1.35f, 0f), enemyColor, 65f, 2.15f);
        ConfigureEnemy(parent, $"{biomeName} Enemy B", sprite, player, center + new Vector3(-1.6f, -1.75f, 0f), enemyColor * 0.85f + Color.white * 0.15f, 90f, 1.85f);
    }

    static Transform ConfigureSpawn(Transform parent, string name, Vector3 position)
    {
        GameObject spawn = GetOrCreateChild(parent, name, position);
        return spawn.transform;
    }

    static CameraAreaBounds2D ConfigureAreaBounds(Transform parent, string name, Vector2 min, Vector2 max)
    {
        GameObject boundsObject = GetOrCreateChild(parent, name, new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f));
        CameraAreaBounds2D bounds = GetOrAddComponent<CameraAreaBounds2D>(boundsObject);
        SerializedObject boundsSerialized = new SerializedObject(bounds);
        boundsSerialized.FindProperty("minimumPosition").vector2Value = min;
        boundsSerialized.FindProperty("maximumPosition").vector2Value = max;
        boundsSerialized.ApplyModifiedProperties();
        return bounds;
    }

    static void ConfigureGate(Transform parent, string name, Sprite sprite, Vector3 position, Transform destination, CameraAreaBounds2D destinationBounds, string displayName, string prompt, string arrivalMessage)
    {
        GameObject gate = ConfigureSimpleInteractable(parent, name, sprite, position, new Vector3(0.95f, 0.95f, 1f), new Color(0.2f, 0.12f, 0.32f), displayName, prompt, new[]
        {
            $"Travel through {displayName}?"
        }, true);

        ZoneGateInteractable gateInteractable = GetOrAddComponent<ZoneGateInteractable>(gate);
        SerializedObject gateObject = new SerializedObject(gateInteractable);
        SetSerializedStringIfPresent(gateObject, "displayName", displayName);
        SetSerializedStringIfPresent(gateObject, "promptText", prompt);
        SerializedProperty lines = gateObject.FindProperty("dialogueLines");
        if (lines != null)
        {
            lines.arraySize = 1;
            lines.GetArrayElementAtIndex(0).stringValue = $"Taking {displayName}.";
        }
        gateObject.FindProperty("destination").objectReferenceValue = destination;
        gateObject.FindProperty("destinationBounds").objectReferenceValue = destinationBounds;
        gateObject.FindProperty("arrivalMessage").stringValue = arrivalMessage;
        gateObject.ApplyModifiedProperties();
    }

    static GameObject ConfigureSimpleInteractable(Transform parent, string name, Sprite sprite, Vector3 position, Vector3 scale, Color color, string displayName, string prompt, string[] lines, bool blocksMovement)
    {
        GameObject interactableObject = GetOrCreateChild(parent, name, position);
        interactableObject.transform.localScale = scale;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(interactableObject);
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 0;

        Collider2D collider = GetOrAddComponent<BoxCollider2D>(interactableObject);
        if (collider is BoxCollider2D box)
        {
            box.size = Vector2.one;
            box.offset = Vector2.zero;
            box.isTrigger = !blocksMovement;
        }

        SimpleInteractable interactable = GetOrAddComponent<SimpleInteractable>(interactableObject);
        SerializedObject interactableSerialized = new SerializedObject(interactable);
        SetSerializedStringIfPresent(interactableSerialized, "displayName", displayName);
        SetSerializedStringIfPresent(interactableSerialized, "promptText", prompt);
        SerializedProperty dialogueLines = interactableSerialized.FindProperty("dialogueLines");
        if (dialogueLines != null)
        {
            dialogueLines.arraySize = lines == null ? 0 : lines.Length;
            for (int i = 0; lines != null && i < lines.Length; i++)
            {
                dialogueLines.GetArrayElementAtIndex(i).stringValue = lines[i];
            }
        }
        interactableSerialized.ApplyModifiedProperties();

        ConfigureYSort(interactableObject);
        return interactableObject;
    }

    static void SetSerializedStringIfPresent(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.stringValue = value;
        }
    }

    static void ConfigureYSort(GameObject target)
    {
        YSort2D ySort = GetOrAddComponent<YSort2D>(target);
        ySort.RefreshRenderers();
    }

    static InputActionReference GetOrCreateMoveActionReference()
    {
        return GetOrCreateInputActionReference("Player/Move", MoveReferencePath);
    }

    static InputActionReference GetOrCreateInputActionReference(string actionPath, string referencePath)
    {
        InputActionReference existingReference = AssetDatabase.LoadAssetAtPath<InputActionReference>(referencePath);
        if (existingReference != null)
        {
            return existingReference;
        }

        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        if (inputActions == null)
        {
            Debug.LogError($"Could not find input actions at {InputActionsPath}.");
            return null;
        }

        InputAction action = inputActions.FindAction(actionPath);
        if (action == null)
        {
            Debug.LogError($"Could not find the {actionPath} action in the input actions asset.");
            return null;
        }

        string folder = Path.GetDirectoryName(referencePath);
        if (!AssetDatabase.IsValidFolder("Assets/RuinedKingdom"))
        {
            AssetDatabase.CreateFolder("Assets", "RuinedKingdom");
        }

        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/RuinedKingdom", "Input");
        }

        InputActionReference reference = InputActionReference.Create(action);
        AssetDatabase.CreateAsset(reference, referencePath);
        AssetDatabase.SaveAssets();
        return reference;
    }

    static GameObject GetOrCreateGameObject(string name, Vector3 position)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            existing.transform.position = position;
            return existing;
        }

        return new GameObject(name)
        {
            transform =
            {
                position = position
            }
        };
    }

    static GameObject GetOrCreateChild(Transform parent, string name, Vector3 localPosition)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            existing.localPosition = localPosition;
            return existing.gameObject;
        }

        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        child.transform.localPosition = localPosition;
        return child;
    }

    static void DestroyChildIfExists(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }
    }

    static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
