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
    const string KingdomScenePath = "Assets/Scenes/Kingdom.unity";
    const string ForestScenePath = "Assets/Scenes/Forest.unity";

    [MenuItem("Tools/Ruined Kingdom/Create Prototype Scene Set")]
    public static void CreatePrototypeSceneSet()
    {
        CreateKingdomScene();
        CreateForestScene();
        ConfigureBuildSettings();
        EditorSceneManager.OpenScene(KingdomScenePath);
    }

    [MenuItem("Tools/Ruined Kingdom/Create Kingdom Scene")]
    public static void CreateKingdomScene()
    {
        InputActionReference moveReference = GetOrCreateMoveActionReference();
        InputActionReference attackReference = GetOrCreateInputActionReference("Player/Attack", AttackReferencePath);
        Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        Sprite tileSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = GetOrCreateGameObject("Kingdom Scene", Vector3.zero);
        GameObject player = ConfigurePlayer(playerSprite, moveReference, attackReference);
        player.name = "Player";
        player.transform.position = new Vector3(0f, -2.2f, 0f);

        ConfigureCamera(player.transform);
        ConfigureDebugOverlay(player.GetComponent<PlayerMovementController>());
        ConfigureKingdomScene(root.transform, tileSprite, player);
        ConfigureCharacterCreationForScene(player, root);

        EnsureScenesFolder();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), KingdomScenePath);
        ConfigureBuildSettings();
        Selection.activeGameObject = player;
    }

    [MenuItem("Tools/Ruined Kingdom/Create Forest Scene")]
    public static void CreateForestScene()
    {
        InputActionReference moveReference = GetOrCreateMoveActionReference();
        InputActionReference attackReference = GetOrCreateInputActionReference("Player/Attack", AttackReferencePath);
        Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        Sprite tileSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = GetOrCreateGameObject("Forest Scene", Vector3.zero);
        GameObject player = ConfigurePlayer(playerSprite, moveReference, attackReference);
        player.name = "Player";
        player.transform.position = new Vector3(0f, -5f, 0f);

        ConfigureCamera(player.transform);
        ConfigureDebugOverlay(player.GetComponent<PlayerMovementController>());
        ConfigureForestScene(root.transform, tileSprite, player);

        EnsureScenesFolder();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ForestScenePath);
        ConfigureBuildSettings();
        Selection.activeGameObject = player;
    }

    [MenuItem("Tools/Ruined Kingdom/Create Movement Test Room")]
    public static void CreateMovementTestRoom()
    {
        CleanupObsoletePrototypeObjects();

        InputActionReference moveReference = GetOrCreateMoveActionReference();
        InputActionReference attackReference = GetOrCreateInputActionReference("Player/Attack", AttackReferencePath);
        Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        Sprite tileSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);

        GameObject roomRoot = GetOrCreateGameObject("Movement Test Room", Vector3.zero);
        GameObject player = ConfigurePlayer(playerSprite, moveReference, attackReference);
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
        GetOrAddComponent<HitFlashOnDamage>(player).RefreshRenderers();
        GetOrAddComponent<DamageNumberEmitter>(player);

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
        GetOrAddComponent<HitFlashOnDamage>(player).RefreshRenderers();

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
        GetOrAddComponent<ToastHudController>(systems);
        GetOrAddComponent<FloatingCombatTextHud>(systems);
        GetOrAddComponent<ControlHelpHudController>(systems);
        GetOrAddComponent<SceneTransitionPromptController>(systems);

        SceneSpawnResolver spawnResolver = GetOrAddComponent<SceneSpawnResolver>(systems);
        SerializedObject spawnResolverObject = new SerializedObject(spawnResolver);
        spawnResolverObject.FindProperty("player").objectReferenceValue = player.transform;
        spawnResolverObject.FindProperty("fallbackSpawnPointName").stringValue = "Kingdom Player Spawn";
        spawnResolverObject.ApplyModifiedProperties();

        PlayerInteractionController interaction = GetOrAddComponent<PlayerInteractionController>(player);
        SerializedObject interactionObject = new SerializedObject(interaction);
        interactionObject.FindProperty("interactionRange").floatValue = 1.85f;
        interactionObject.FindProperty("interactionLayers").intValue = ~0;
        interactionObject.FindProperty("dialogueHud").objectReferenceValue = dialogueHud;
        interactionObject.ApplyModifiedProperties();
    }

    static void ConfigureKingdomScene(Transform root, Sprite sprite, GameObject player)
    {
        ConfigureSpawn(root, "Kingdom Player Spawn", new Vector3(0f, -2.2f, 0f));
        CameraAreaBounds2D hubBounds = ConfigureAreaBounds(root, "Kingdom Camera Bounds", new Vector2(-18f, -11f), new Vector2(18f, 11f));
        hubBounds.ApplyTo(Camera.main == null ? null : Camera.main.GetComponent<CameraFollow2D>());

        ConfigureGroundPatch(root, "Kingdom Grass", sprite, Vector3.zero, new Vector3(40f, 24f, 1f), new Color(0.25f, 0.52f, 0.29f));
        ConfigureGroundPatch(root, "Central Plaza", sprite, Vector3.zero, new Vector3(9.5f, 5.2f, 1f), new Color(0.36f, 0.35f, 0.32f));
        ConfigureGroundPatch(root, "North Road", sprite, new Vector3(0f, 7.2f, 0f), new Vector3(1.8f, 9f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(root, "South Road", sprite, new Vector3(0f, -7.2f, 0f), new Vector3(1.8f, 9f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(root, "East Road", sprite, new Vector3(9f, -0.2f, 0f), new Vector3(17f, 1.35f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(root, "West Road", sprite, new Vector3(-9f, -0.2f, 0f), new Vector3(17f, 1.35f, 1f), new Color(0.48f, 0.34f, 0.18f));

        ConfigureWall(root, "Kingdom North Wall West", sprite, new Vector3(-11.8f, 11.8f, 0f), new Vector3(17f, 0.7f, 1f));
        ConfigureWall(root, "Kingdom North Wall East", sprite, new Vector3(11.8f, 11.8f, 0f), new Vector3(17f, 0.7f, 1f));
        ConfigureWall(root, "Kingdom South Wall", sprite, new Vector3(0f, -11.8f, 0f), new Vector3(40f, 0.7f, 1f));
        ConfigureWall(root, "Kingdom West Wall", sprite, new Vector3(-20f, 0f, 0f), new Vector3(0.7f, 23f, 1f));
        ConfigureWall(root, "Kingdom East Wall", sprite, new Vector3(20f, 0f, 0f), new Vector3(0.7f, 23f, 1f));

        ConfigureBuilding(root, sprite, "Blacksmith", new Vector3(-9.4f, 5.8f, 0f), new Vector3(3.2f, 2.1f, 1f), new Color(0.25f, 0.22f, 0.2f), new Vector3(42f, 0f, 0f), root.Find("Kingdom Player Spawn"), hubBounds, "Enter Blacksmith", typeof(WeaponSmithInteractable), "Royal Blacksmith");
        ConfigureBuilding(root, sprite, "Adventurer Guild", new Vector3(-4.5f, 5.9f, 0f), new Vector3(3.4f, 2.15f, 1f), new Color(0.32f, 0.28f, 0.42f), new Vector3(52f, 0f, 0f), root.Find("Kingdom Player Spawn"), hubBounds, "Enter Guild", typeof(QuestBoardInteractable), "Guild Clerk");
        ConfigureBuilding(root, sprite, "Healer Hall", new Vector3(8.6f, 5.8f, 0f), new Vector3(3.1f, 2f, 1f), new Color(0.55f, 0.75f, 0.82f), new Vector3(62f, 0f, 0f), root.Find("Kingdom Player Spawn"), hubBounds, "Enter Healer", typeof(HealerServiceInteractable), "Healer");
        ConfigureBuilding(root, sprite, "Inn", new Vector3(-8.7f, -5.6f, 0f), new Vector3(3.4f, 2.1f, 1f), new Color(0.42f, 0.25f, 0.16f), new Vector3(42f, -10f, 0f), root.Find("Kingdom Player Spawn"), hubBounds, "Enter Inn", typeof(HubServiceInteractable), "Innkeeper");
        ConfigureBuilding(root, sprite, "Alchemist", new Vector3(8.7f, -5.6f, 0f), new Vector3(3.1f, 2f, 1f), new Color(0.35f, 0.24f, 0.52f), new Vector3(52f, -10f, 0f), root.Find("Kingdom Player Spawn"), hubBounds, "Enter Alchemist", typeof(HubServiceInteractable), "Alchemist");
        ConfigureBuilding(root, sprite, "Town Hall", new Vector3(4.35f, -6.6f, 0f), new Vector3(4.2f, 2.45f, 1f), new Color(0.5f, 0.45f, 0.32f), new Vector3(62f, -10f, 0f), root.Find("Kingdom Player Spawn"), hubBounds, "Enter Town Hall", typeof(HubServiceInteractable), "Steward");
        ConfigureBuilding(root, sprite, "Armory", new Vector3(-13f, 2.2f, 0f), new Vector3(2.8f, 1.8f, 1f), new Color(0.22f, 0.24f, 0.28f), new Vector3(42f, -20f, 0f), root.Find("Kingdom Player Spawn"), hubBounds, "Enter Armory", typeof(HubServiceInteractable), "Armorer");
        ConfigureBuilding(root, sprite, "Storehouse", new Vector3(13f, 2.2f, 0f), new Vector3(2.8f, 1.8f, 1f), new Color(0.46f, 0.32f, 0.18f), new Vector3(52f, -20f, 0f), root.Find("Kingdom Player Spawn"), hubBounds, "Enter Storehouse", typeof(HubServiceInteractable), "Quartermaster");

        ConfigureHubService(root, "Healer Shrine", sprite, new Vector3(4.75f, -1.65f, 0f), new Vector3(0.9f, 0.9f, 1f), new Color(0.7f, 0.95f, 1f), "Healer Shrine", "Recover", typeof(HealerServiceInteractable));
        ConfigureTrainingDummy(root, sprite, new Vector3(6.15f, -1.95f, 0f));

        ConfigureScenePortal(root, "Forest Scene Portal", sprite, new Vector3(0f, 10.1f, 0f), new Vector3(3f, 1.4f, 1f), "Forest Gate", "Enter Forest", "Forest", "Forest Entry Spawn", "Enter the forest?", "Leave the kingdom and travel to the forest.");
        ConfigureSimpleInteractable(root, "Kingdom Guard", sprite, new Vector3(-2.4f, 8.7f, 0f), new Vector3(0.65f, 1f, 1f), new Color(0.35f, 0.45f, 0.85f), "Gate Guard", "Talk", new[]
        {
            "The forest is a separate scene now. Use the gold gate marker north of town.",
            "No enemies should appear inside the kingdom."
        }, true);
    }

    static void ConfigureForestScene(Transform root, Sprite sprite, GameObject player)
    {
        ConfigureSpawn(root, "Forest Entry Spawn", new Vector3(0f, -5.2f, 0f));
        CameraAreaBounds2D forestBounds = ConfigureAreaBounds(root, "Forest Camera Bounds", new Vector2(-10f, -7f), new Vector2(10f, 9f));
        forestBounds.ApplyTo(Camera.main == null ? null : Camera.main.GetComponent<CameraFollow2D>());

        ConfigureGroundPatch(root, "Forest Floor", sprite, Vector3.zero, new Vector3(20f, 16f, 1f), new Color(0.1f, 0.36f, 0.16f));
        ConfigureGroundPatch(root, "Forest Main Path", sprite, new Vector3(0f, -0.6f, 0f), new Vector3(3.3f, 12.5f, 1f), new Color(0.38f, 0.27f, 0.16f));
        ConfigureGroundPatch(root, "Forest Clearing", sprite, new Vector3(0f, 4.7f, 0f), new Vector3(9f, 4f, 1f), new Color(0.14f, 0.42f, 0.2f));

        ConfigureWall(root, "Forest North Boundary", sprite, new Vector3(0f, 8.6f, 0f), new Vector3(20f, 0.55f, 1f));
        ConfigureWall(root, "Forest South Boundary West", sprite, new Vector3(-6.5f, -8.2f, 0f), new Vector3(7f, 0.55f, 1f));
        ConfigureWall(root, "Forest South Boundary East", sprite, new Vector3(6.5f, -8.2f, 0f), new Vector3(7f, 0.55f, 1f));
        ConfigureWall(root, "Forest West Boundary", sprite, new Vector3(-10.2f, 0f, 0f), new Vector3(0.55f, 16f, 1f));
        ConfigureWall(root, "Forest East Boundary", sprite, new Vector3(10.2f, 0f, 0f), new Vector3(0.55f, 16f, 1f));

        for (int i = 0; i < 34; i++)
        {
            float x = i % 2 == 0 ? Random.Range(-8.6f, -3.1f) : Random.Range(3.1f, 8.6f);
            float y = Random.Range(-6.3f, 7.2f);
            ConfigureProp(root, $"Forest Tree {i + 1}", sprite, new Vector3(x, y, 0f), new Vector3(1.05f, 1.8f, 1f), new Color(0.03f, 0.24f, 0.08f));
        }

        ConfigureScenePortal(root, "Kingdom Return Portal", sprite, new Vector3(0f, -7.1f, 0f), new Vector3(3f, 1.25f, 1f), "Kingdom Road", "Return to Kingdom", "Kingdom", "Kingdom Player Spawn", "Return to kingdom?", "Leave the forest and return to the kingdom.");

        ConfigureSimpleInteractable(root, "Forest Ranger", sprite, new Vector3(-3.4f, -4.2f, 0f), new Vector3(0.65f, 1f, 1f), new Color(0.36f, 0.72f, 0.28f), "Forest Ranger", "Talk", new[]
        {
            "This is the normal forest entrance area.",
            "Walk north to the dark marker in the clearing to start the Lost Woods minigame."
        }, true);

        GameObject chest = ConfigureSimpleInteractable(root, "Forest Entry Chest", sprite, new Vector3(3.6f, -3.9f, 0f), new Vector3(0.75f, 0.55f, 1f), new Color(0.45f, 0.25f, 0.1f), "Forest Chest", "Open", null, true);
        ForestLootChestInteractable forestChest = GetOrAddComponent<ForestLootChestInteractable>(chest);
        SerializedObject chestObject = new SerializedObject(forestChest);
        SetSerializedStringIfPresent(chestObject, "displayName", "Forest Chest");
        SetSerializedStringIfPresent(chestObject, "promptText", "Open");
        chestObject.ApplyModifiedProperties();

        ConfigureLostWoodsDungeon(root, sprite, player.transform, new Vector3(0f, 24f, 0f), new Vector3(0f, 6.4f, 0f));
    }

    static void ConfigureCharacterCreationForScene(GameObject player, GameObject gameplayRoot)
    {
        GameObject creatorObject = GetOrCreateGameObject("Character Creation Controller", Vector3.zero);
        CharacterCreationController creator = GetOrAddComponent<CharacterCreationController>(creatorObject);

        SerializedObject creatorObjectSerialized = new SerializedObject(creator);
        creatorObjectSerialized.FindProperty("showOnStart").boolValue = true;
        creatorObjectSerialized.FindProperty("gameplayRoot").objectReferenceValue = gameplayRoot;
        creatorObjectSerialized.FindProperty("playerMovement").objectReferenceValue = player == null ? null : player.GetComponent<PlayerMovementController>();
        creatorObjectSerialized.FindProperty("playerCombat").objectReferenceValue = player == null ? null : player.GetComponent<PlayerCombatController>();
        creatorObjectSerialized.FindProperty("playerInventory").objectReferenceValue = player == null ? null : player.GetComponent<PlayerInventoryHudController>();
        creatorObjectSerialized.FindProperty("visualApplier").objectReferenceValue = player == null ? null : player.GetComponentInChildren<CharacterVisualApplier>();
        creatorObjectSerialized.FindProperty("playerBodyRenderer").objectReferenceValue = player == null ? null : player.GetComponent<SpriteRenderer>();
        creatorObjectSerialized.FindProperty("playerWeaponRenderer").objectReferenceValue = FindPlayerWeaponRenderer(player);
        creatorObjectSerialized.ApplyModifiedProperties();
    }

    static SpriteRenderer FindPlayerWeaponRenderer(GameObject player)
    {
        if (player == null)
        {
            return null;
        }

        Transform weapon = player.transform.Find("Sword Weapon/Blade");
        return weapon == null ? null : weapon.GetComponent<SpriteRenderer>();
    }

    static void EnsureScenesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }

    static void ConfigureBuildSettings()
    {
        EnsureScenesFolder();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(KingdomScenePath, true),
            new EditorBuildSettingsScene(ForestScenePath, true)
        };
    }

    static void CleanupObsoletePrototypeObjects()
    {
        foreach (EnemyCombatController enemy in Object.FindObjectsByType<EnemyCombatController>(FindObjectsSortMode.None))
        {
            if (enemy != null)
            {
                Object.DestroyImmediate(enemy.gameObject);
            }
        }

        string[] obsoleteNames =
        {
            "Training Yard Enemy",
            "Enemy",
            "Center Block",
            "Tree Placeholder",
            "Rock Placeholder",
            "North Wall",
            "South Wall",
            "East Wall",
            "West Wall",
            "North Kingdom Boundary",
            "Forest North Boundary",
            "Forest West Boundary",
            "Forest East Boundary"
        };

        for (int i = 0; i < obsoleteNames.Length; i++)
        {
            GameObject existing = GameObject.Find(obsoleteNames[i]);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
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

    static GameObject ConfigureEnemy(Transform parent, string enemyName, Sprite enemySprite, Transform player, Vector3 position, Color bodyColor, float maxHealth, float moveSpeed, string routeName, string archetypeName, string materialReward, int experienceReward, int pixicoinReward)
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
        GetOrAddComponent<HitFlashOnDamage>(enemy).RefreshRenderers();
        GetOrAddComponent<DamageNumberEmitter>(enemy);

        EnemyCombatController enemyCombat = GetOrAddComponent<EnemyCombatController>(enemy);
        ConfigureEnemyVisuals(enemy.transform, enemySprite, bodyColor);
        ConfigureWeapon(enemy.transform, "Sword Weapon", new Color(0.95f, 0.7f, 0.6f), 25, out Transform enemyWeaponPivot, out Transform enemyWeaponBlade, out SpriteRenderer enemyWeaponRenderer);
        GetOrAddComponent<HitFlashOnDamage>(enemy).RefreshRenderers();

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

        EnemyRewardComponent reward = GetOrAddComponent<EnemyRewardComponent>(enemy);
        SerializedObject rewardObject = new SerializedObject(reward);
        rewardObject.FindProperty("health").objectReferenceValue = health;
        rewardObject.FindProperty("routeName").stringValue = routeName;
        rewardObject.FindProperty("archetypeName").stringValue = archetypeName;
        rewardObject.FindProperty("experienceReward").intValue = experienceReward;
        rewardObject.FindProperty("pixicoinReward").intValue = pixicoinReward;
        rewardObject.FindProperty("materialReward").stringValue = materialReward;
        rewardObject.FindProperty("materialCount").intValue = 1;
        rewardObject.ApplyModifiedProperties();

        ConfigureResourceBar(enemy.transform, "Health Bar", new Vector3(0f, 0.75f, 0f), new Color(0.9f, 0.1f, 0.12f), health, null, WorldResourceBar2D.ResourceType.Health);
        ConfigureYSort(enemy);
        return enemy;
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
        floor.transform.localScale = new Vector3(42f, 30f, 1f);

        SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(floor);
        spriteRenderer.sprite = tileSprite;
        spriteRenderer.color = new Color(0.25f, 0.52f, 0.29f);
        spriteRenderer.sortingOrder = -10;

        ConfigureGroundPatch(parent, "Center Dirt Path", tileSprite, Vector3.zero, new Vector3(9f, 1.1f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(parent, "Vertical Dirt Path", tileSprite, Vector3.zero, new Vector3(1.1f, 6.5f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(parent, "Garden Patch NW", tileSprite, new Vector3(-12.5f, 7.4f, 0f), new Vector3(3.8f, 2.1f, 1f), new Color(0.2f, 0.42f, 0.22f));
        ConfigureGroundPatch(parent, "Garden Patch SE", tileSprite, new Vector3(12.4f, -7.2f, 0f), new Vector3(3.8f, 2.1f, 1f), new Color(0.2f, 0.42f, 0.22f));
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
        ConfigureWall(parent, "North Kingdom Boundary West", wallSprite, new Vector3(-12f, 12.8f, 0f), new Vector3(18f, 0.75f, 1f));
        ConfigureWall(parent, "North Kingdom Boundary East", wallSprite, new Vector3(12f, 12.8f, 0f), new Vector3(18f, 0.75f, 1f));
        ConfigureWall(parent, "South Kingdom Boundary", wallSprite, new Vector3(0f, -12.8f, 0f), new Vector3(42f, 0.75f, 1f));
        ConfigureWall(parent, "West Kingdom Boundary", wallSprite, new Vector3(-20.8f, 0f, 0f), new Vector3(0.75f, 25f, 1f));
        ConfigureWall(parent, "East Kingdom Boundary", wallSprite, new Vector3(20.8f, 0f, 0f), new Vector3(0.75f, 25f, 1f));
        ConfigureProp(parent, "Town Tree Placeholder", wallSprite, new Vector3(-15.3f, 7.7f, 0f), new Vector3(1.1f, 1.8f, 1f), new Color(0.08f, 0.35f, 0.12f));
        ConfigureProp(parent, "Town Rock Placeholder", wallSprite, new Vector3(15.1f, -7.8f, 0f), new Vector3(1.1f, 0.85f, 1f), new Color(0.38f, 0.38f, 0.42f));
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
        Transform hubSpawn = ConfigureSpawn(parent, "Hub Spawn", new Vector3(0f, -2.2f, 0f));
        CameraAreaBounds2D hubBounds = ConfigureAreaBounds(parent, "Hub Camera Bounds", new Vector2(-18f, -12f), new Vector2(18f, 12f));

        if (Camera.main != null)
        {
            hubBounds.ApplyTo(Camera.main.GetComponent<CameraFollow2D>());
        }

        ConfigureHubPlaza(parent, sprite);
        ConfigureKingdomBuildings(parent, sprite, hubSpawn, hubBounds);

        Vector3 forestSceneCenter = new Vector3(0f, 82f, 0f);
        Transform forestSpawn = ConfigureSpawn(parent, "Forest Approach Spawn", forestSceneCenter + new Vector3(0f, -5.3f, 0f));
        Transform seaSpawn = ConfigureSpawn(parent, "Sea Road Spawn", new Vector3(17.4f, 0f, 0f));
        Transform desertSpawn = ConfigureSpawn(parent, "Desert Gate Spawn", new Vector3(0f, -15.2f, 0f));
        Transform volcanoSpawn = ConfigureSpawn(parent, "Volcano Road Spawn", new Vector3(-17.4f, 0f, 0f));
        Transform ruinsSpawn = ConfigureSpawn(parent, "Old Ruins Spawn", new Vector3(17.4f, 15.2f, 0f));

        CameraAreaBounds2D forestBounds = ConfigureAreaBounds(parent, "Forest Camera Bounds", new Vector2(-10f, 74f), new Vector2(10f, 91f));
        CameraAreaBounds2D seaBounds = ConfigureAreaBounds(parent, "Sea Camera Bounds", new Vector2(11f, -5f), new Vector2(24f, 5f));
        CameraAreaBounds2D desertBounds = ConfigureAreaBounds(parent, "Desert Camera Bounds", new Vector2(-7f, -21f), new Vector2(7f, -10f));
        CameraAreaBounds2D volcanoBounds = ConfigureAreaBounds(parent, "Volcano Camera Bounds", new Vector2(-24f, -5f), new Vector2(-11f, 5f));
        CameraAreaBounds2D ruinsBounds = ConfigureAreaBounds(parent, "Ruins Camera Bounds", new Vector2(11f, 10f), new Vector2(24f, 21f));

        ConfigureGate(parent, "North Forest Gate", sprite, new Vector3(0f, 9.4f, 0f), forestSpawn, forestBounds, "Forest Road", "Travel to Forest", "The northern road leaves the safe kingdom behind.");
        ConfigureGate(parent, "East Sea Gate", sprite, new Vector3(15.25f, -1.2f, 0f), seaSpawn, seaBounds, "Sea Road", "Preview Sea", "The sea road is marked for a future route.");
        ConfigureGate(parent, "South Desert Gate", sprite, new Vector3(0f, -10.2f, 0f), desertSpawn, desertBounds, "Desert Gate", "Preview Desert", "The desert pass is marked for a future route.");
        ConfigureGate(parent, "West Volcano Gate", sprite, new Vector3(-15.25f, -1.2f, 0f), volcanoSpawn, volcanoBounds, "Volcano Road", "Preview Volcano", "The western cliffs are marked for a future route.");
        ConfigureGate(parent, "Old Ruins Gate", sprite, new Vector3(13.4f, 7.75f, 0f), ruinsSpawn, ruinsBounds, "Old Ruins", "Preview Ruins", "The ruin road is marked for a future route.");

        ConfigureGate(parent, "Forest Return Gate", sprite, forestSceneCenter + new Vector3(0f, -7.2f, 0f), hubSpawn, hubBounds, "Kingdom Road", "Return to Kingdom", "Back to the kingdom.");
        ConfigureGate(parent, "Sea Return Gate", sprite, new Vector3(12.8f, 0f, 0f), hubSpawn, hubBounds, "Return Road", "Return to Hub", "Back to the central hub.");
        ConfigureGate(parent, "Desert Return Gate", sprite, new Vector3(0f, -11.5f, 0f), hubSpawn, hubBounds, "Return Road", "Return to Hub", "Back to the central hub.");
        ConfigureGate(parent, "Volcano Return Gate", sprite, new Vector3(-12.8f, 0f, 0f), hubSpawn, hubBounds, "Return Road", "Return to Hub", "Back to the central hub.");
        ConfigureGate(parent, "Ruins Return Gate", sprite, new Vector3(12.8f, 15.2f, 0f), hubSpawn, hubBounds, "Return Road", "Return to Hub", "Back to the central hub.");

        ConfigureForestFirstArea(parent, sprite, player.transform, forestSceneCenter);
        ConfigureFutureRoutePreview(parent, sprite, "Sea", new Vector3(18f, 0f, 0f), new Color(0.1f, 0.48f, 0.62f), "A future coastal combat route.");
        ConfigureFutureRoutePreview(parent, sprite, "Desert", new Vector3(0f, -16f, 0f), new Color(0.68f, 0.52f, 0.24f), "A future heat and endurance route.");
        ConfigureFutureRoutePreview(parent, sprite, "Volcano", new Vector3(-18f, 0f, 0f), new Color(0.32f, 0.18f, 0.18f), "A future heavy enemy route.");
        ConfigureFutureRoutePreview(parent, sprite, "Ruins", new Vector3(18f, 16f, 0f), new Color(0.28f, 0.28f, 0.34f), "A future ancient magic route.");

        ConfigureSimpleInteractable(parent, "Hub Guide", sprite, new Vector3(-2.8f, -2.1f, 0f), new Vector3(0.65f, 1f, 1f), new Color(0.95f, 0.78f, 0.28f), "Hub Guide", "Talk", new[]
        {
            "The kingdom is bigger now. Buildings can be entered, and the forest is the first real danger.",
            "The town should feel safe. If something wants to stab you, it belongs beyond the north road."
        }, true);

        ConfigureSimpleInteractable(parent, "Prototype Chest", sprite, new Vector3(2.15f, -1.15f, 0f), new Vector3(0.8f, 0.55f, 1f), new Color(0.48f, 0.27f, 0.1f), "Old Chest", "Open", new[]
        {
            "Prototype reward: imagine ores, weapon skills, elemental shards, or Pixicoins here.",
            "No farming required. We are building toward combat routes, loot, and return-to-hub upgrades."
        }, true);

        ConfigureRouteChest(parent, "Forest Reward Chest", sprite, new Vector3(-4.5f, 2.6f, 0f), "Forest");
        ConfigureRouteChest(parent, "Sea Reward Chest", sprite, new Vector3(4.5f, 2.05f, 0f), "Sea");
        ConfigureRouteChest(parent, "Desert Reward Chest", sprite, new Vector3(4.55f, -2.25f, 0f), "Desert");
        ConfigureRouteChest(parent, "Volcano Reward Chest", sprite, new Vector3(-4.5f, -2.25f, 0f), "Volcano");
        ConfigureRouteChest(parent, "Ruins Reward Chest", sprite, new Vector3(-3.65f, 3.25f, 0f), "Ruins");
    }

    static void ConfigureHubPlaza(Transform parent, Sprite sprite)
    {
        ConfigureGroundPatch(parent, "Kingdom Plaza Stone", sprite, Vector3.zero, new Vector3(9.6f, 5.4f, 1f), new Color(0.36f, 0.35f, 0.32f));
        ConfigureGroundPatch(parent, "Kingdom North Road", sprite, new Vector3(0f, 8f, 0f), new Vector3(1.6f, 14f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(parent, "Kingdom East Road", sprite, new Vector3(9.8f, -0.4f, 0f), new Vector3(18f, 1.35f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(parent, "Kingdom South Road", sprite, new Vector3(0f, -8f, 0f), new Vector3(1.6f, 14f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(parent, "Kingdom West Road", sprite, new Vector3(-9.8f, -0.4f, 0f), new Vector3(18f, 1.35f, 1f), new Color(0.48f, 0.34f, 0.18f));
        ConfigureGroundPatch(parent, "Market Yard", sprite, new Vector3(6.8f, 4.5f, 0f), new Vector3(7f, 4f, 1f), new Color(0.28f, 0.42f, 0.28f));
        ConfigureGroundPatch(parent, "Crafting Yard", sprite, new Vector3(-6.8f, 4.5f, 0f), new Vector3(7f, 4f, 1f), new Color(0.32f, 0.31f, 0.26f));

        GameObject noticeBoard = ConfigureSimpleInteractable(parent, "Kingdom Route Board", sprite, new Vector3(-3.9f, -1.15f, 0f), new Vector3(1.15f, 0.75f, 1f), new Color(0.28f, 0.18f, 0.08f), "Route Board", "Read", new[]
        {
            "Ruined Kingdom loop target: gear up in the hub, choose a route, fight through danger, bring resources home, unlock the kingdom.",
            "Current focus: kingdom services and the northern forest."
        }, true);
        QuestBoardInteractable questBoard = GetOrAddComponent<QuestBoardInteractable>(noticeBoard);
        SerializedObject questBoardObject = new SerializedObject(questBoard);
        SetSerializedStringIfPresent(questBoardObject, "displayName", "Route Board");
        SetSerializedStringIfPresent(questBoardObject, "promptText", "Read");
        questBoardObject.ApplyModifiedProperties();
    }

    static void ConfigureKingdomBuildings(Transform parent, Sprite sprite, Transform hubSpawn, CameraAreaBounds2D hubBounds)
    {
        ConfigureBuilding(parent, sprite, "Blacksmith", new Vector3(-9.4f, 5.8f, 0f), new Vector3(3.2f, 2.1f, 1f), new Color(0.25f, 0.22f, 0.2f), new Vector3(42f, 0f, 0f), hubSpawn, hubBounds, "Enter Blacksmith", typeof(WeaponSmithInteractable), "Royal Blacksmith");
        ConfigureBuilding(parent, sprite, "Adventurer Guild", new Vector3(-4.5f, 5.9f, 0f), new Vector3(3.4f, 2.15f, 1f), new Color(0.32f, 0.28f, 0.42f), new Vector3(52f, 0f, 0f), hubSpawn, hubBounds, "Enter Guild", typeof(QuestBoardInteractable), "Guild Clerk");
        ConfigureBuilding(parent, sprite, "Healer Hall", new Vector3(8.6f, 5.8f, 0f), new Vector3(3.1f, 2f, 1f), new Color(0.55f, 0.75f, 0.82f), new Vector3(62f, 0f, 0f), hubSpawn, hubBounds, "Enter Healer", typeof(HealerServiceInteractable), "Healer");
        ConfigureBuilding(parent, sprite, "Inn", new Vector3(-8.7f, -5.6f, 0f), new Vector3(3.4f, 2.1f, 1f), new Color(0.42f, 0.25f, 0.16f), new Vector3(42f, -10f, 0f), hubSpawn, hubBounds, "Enter Inn", typeof(HubServiceInteractable), "Innkeeper");
        ConfigureBuilding(parent, sprite, "Alchemist", new Vector3(8.7f, -5.6f, 0f), new Vector3(3.1f, 2f, 1f), new Color(0.35f, 0.24f, 0.52f), new Vector3(52f, -10f, 0f), hubSpawn, hubBounds, "Enter Alchemist", typeof(HubServiceInteractable), "Alchemist");
        ConfigureBuilding(parent, sprite, "Town Hall", new Vector3(4.35f, -6.6f, 0f), new Vector3(4.2f, 2.45f, 1f), new Color(0.5f, 0.45f, 0.32f), new Vector3(62f, -10f, 0f), hubSpawn, hubBounds, "Enter Town Hall", typeof(HubServiceInteractable), "Steward");
        ConfigureBuilding(parent, sprite, "Armory", new Vector3(-13f, 2.2f, 0f), new Vector3(2.8f, 1.8f, 1f), new Color(0.22f, 0.24f, 0.28f), new Vector3(42f, -20f, 0f), hubSpawn, hubBounds, "Enter Armory", typeof(HubServiceInteractable), "Armorer");
        ConfigureBuilding(parent, sprite, "Storehouse", new Vector3(13f, 2.2f, 0f), new Vector3(2.8f, 1.8f, 1f), new Color(0.46f, 0.32f, 0.18f), new Vector3(52f, -20f, 0f), hubSpawn, hubBounds, "Enter Storehouse", typeof(HubServiceInteractable), "Quartermaster");

        ConfigureHubService(parent, "Healer Shrine", sprite, new Vector3(4.75f, -1.65f, 0f), new Vector3(0.9f, 0.9f, 1f), new Color(0.7f, 0.95f, 1f), "Healer Shrine", "Recover", typeof(HealerServiceInteractable));
        ConfigureTrainingDummy(parent, sprite, new Vector3(6.15f, -1.95f, 0f));
        ConfigureSimpleInteractable(parent, "Kingdom Guard", sprite, new Vector3(-2.25f, 8.25f, 0f), new Vector3(0.65f, 1f, 1f), new Color(0.35f, 0.45f, 0.85f), "Gate Guard", "Talk", new[]
        {
            "No monsters inside the kingdom. That is the whole point of walls.",
            "The forest road is open, but the woods do not stay the same twice."
        }, true);
    }

    static void ConfigureBuilding(Transform parent, Sprite sprite, string name, Vector3 exteriorPosition, Vector3 exteriorScale, Color color, Vector3 interiorCenter, Transform hubSpawn, CameraAreaBounds2D hubBounds, string prompt, System.Type interiorNpcType, string npcName)
    {
        ConfigureSceneryBlock(parent, $"{name} Exterior", sprite, exteriorPosition, exteriorScale, color);
        Transform interiorSpawn = ConfigureSpawn(parent, $"{name} Interior Spawn", interiorCenter + new Vector3(0f, -1.25f, 0f));
        CameraAreaBounds2D interiorBounds = ConfigureAreaBounds(parent, $"{name} Camera Bounds", new Vector2(interiorCenter.x - 4f, interiorCenter.y - 3f), new Vector2(interiorCenter.x + 4f, interiorCenter.y + 3f));

        ConfigureGate(parent, $"{name} Door", sprite, exteriorPosition + new Vector3(0f, -exteriorScale.y * 0.55f, 0f), interiorSpawn, interiorBounds, name, prompt, $"Entered {name}.");

        ConfigureGroundPatch(parent, $"{name} Interior Floor", sprite, interiorCenter, new Vector3(7f, 4.4f, 1f), new Color(0.27f, 0.24f, 0.2f));
        ConfigureWall(parent, $"{name} Interior North Wall", sprite, interiorCenter + new Vector3(0f, 2.45f, 0f), new Vector3(7.4f, 0.4f, 1f));
        ConfigureWall(parent, $"{name} Interior South Wall", sprite, interiorCenter + new Vector3(0f, -2.45f, 0f), new Vector3(7.4f, 0.4f, 1f));
        ConfigureWall(parent, $"{name} Interior West Wall", sprite, interiorCenter + new Vector3(-3.8f, 0f, 0f), new Vector3(0.4f, 4.6f, 1f));
        ConfigureWall(parent, $"{name} Interior East Wall", sprite, interiorCenter + new Vector3(3.8f, 0f, 0f), new Vector3(0.4f, 4.6f, 1f));
        ConfigureGate(parent, $"{name} Exit", sprite, interiorCenter + new Vector3(0f, -2.05f, 0f), hubSpawn, hubBounds, "Door", "Exit", "Back outside.");

        GameObject npc = ConfigureSimpleInteractable(parent, $"{name} NPC", sprite, interiorCenter + new Vector3(0f, 0.65f, 0f), new Vector3(0.65f, 1f, 1f), Color.Lerp(color, Color.white, 0.35f), npcName, "Talk", null, true);
        Component component = npc.GetComponent(interiorNpcType);
        if (component == null)
        {
            component = npc.AddComponent(interiorNpcType);
        }

        SerializedObject npcObject = new SerializedObject(component);
        SetSerializedStringIfPresent(npcObject, "displayName", npcName);
        SetSerializedStringIfPresent(npcObject, "promptText", "Talk");
        SetSerializedStringIfPresent(npcObject, "serviceName", npcName);
        npcObject.ApplyModifiedProperties();
    }

    static GameObject ConfigureSceneryBlock(Transform parent, string name, Sprite sprite, Vector3 position, Vector3 scale, Color color)
    {
        GameObject scenery = GetOrCreateChild(parent, name, position);
        scenery.transform.localScale = scale;

        foreach (SimpleInteractable interactable in scenery.GetComponents<SimpleInteractable>())
        {
            Object.DestroyImmediate(interactable);
        }

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(scenery);
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 0;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(scenery);
        collider.size = Vector2.one;
        collider.offset = Vector2.zero;
        collider.isTrigger = false;

        ConfigureYSort(scenery);
        return scenery;
    }

    static void ConfigureHubService(Transform parent, string name, Sprite sprite, Vector3 position, Vector3 scale, Color color, string displayName, string prompt, System.Type componentType)
    {
        GameObject service = ConfigureSimpleInteractable(parent, name, sprite, position, scale, color, displayName, prompt, null, true);
        Component component = service.GetComponent(componentType);
        if (component == null)
        {
            component = service.AddComponent(componentType);
        }

        SerializedObject serviceObject = new SerializedObject(component);
        SetSerializedStringIfPresent(serviceObject, "displayName", displayName);
        SetSerializedStringIfPresent(serviceObject, "promptText", prompt);
        SetSerializedStringIfPresent(serviceObject, "serviceName", displayName);
        serviceObject.ApplyModifiedProperties();
    }

    static void ConfigureRouteChest(Transform parent, string name, Sprite sprite, Vector3 position, string routeName)
    {
        GameObject chest = ConfigureSimpleInteractable(parent, name, sprite, position, new Vector3(0.75f, 0.55f, 1f), new Color(0.5f, 0.28f, 0.12f), $"{routeName} Chest", "Claim", null, true);
        RouteRewardChestInteractable routeChest = GetOrAddComponent<RouteRewardChestInteractable>(chest);
        SerializedObject chestObject = new SerializedObject(routeChest);
        SetSerializedStringIfPresent(chestObject, "displayName", $"{routeName} Chest");
        SetSerializedStringIfPresent(chestObject, "promptText", "Claim");
        chestObject.FindProperty("routeName").stringValue = routeName;
        chestObject.ApplyModifiedProperties();
    }

    static void ConfigureForestFirstArea(Transform parent, Sprite sprite, Transform player, Vector3 center)
    {
        ConfigureGroundPatch(parent, "Forest Outskirts Ground", sprite, center, new Vector3(17f, 14f, 1f), new Color(0.12f, 0.42f, 0.18f));
        ConfigureGroundPatch(parent, "Forest Walkthrough Path", sprite, center + new Vector3(0f, 0.1f, 0f), new Vector3(3.8f, 12f, 1f), new Color(0.42f, 0.31f, 0.18f));
        ConfigureGroundPatch(parent, "Forest Clearing", sprite, center + new Vector3(0f, 4.8f, 0f), new Vector3(8.8f, 3.8f, 1f), new Color(0.15f, 0.38f, 0.18f));

        ConfigureSimpleInteractable(parent, "Forest Ranger", sprite, center + new Vector3(-3.2f, -4.1f, 0f), new Vector3(0.65f, 1f, 1f), new Color(0.36f, 0.72f, 0.28f), "Forest Ranger", "Talk", new[]
        {
            "This first stretch is only the forest entrance.",
            "Walk north to the old stump marker, then enter the Lost Woods minigame from there.",
            "Choose left or right. Good choices move you deeper. Bad choices send you back."
        }, true);

        for (int i = 0; i < 28; i++)
        {
            float x = i % 2 == 0 ? Random.Range(-7.3f, -3.2f) : Random.Range(3.2f, 7.3f);
            float y = Random.Range(20.1f, 32.7f);
            ConfigureProp(parent, $"Forest Outskirts Tree {i + 1}", sprite, new Vector3(x, y, 0f), new Vector3(1.05f, 1.75f, 1f), new Color(0.03f, 0.24f, 0.08f));
        }

        ConfigureSimpleInteractable(parent, "Forest Outskirts Chest", sprite, center + new Vector3(3.7f, -3.5f, 0f), new Vector3(0.75f, 0.55f, 1f), new Color(0.45f, 0.25f, 0.1f), "Forest Chest", "Open", null, true);
        ForestLootChestInteractable outskirtsChest = GetOrAddComponent<ForestLootChestInteractable>(GameObject.Find("Forest Outskirts Chest"));
        SerializedObject outskirtsChestObject = new SerializedObject(outskirtsChest);
        SetSerializedStringIfPresent(outskirtsChestObject, "displayName", "Forest Chest");
        SetSerializedStringIfPresent(outskirtsChestObject, "promptText", "Open");
        outskirtsChestObject.ApplyModifiedProperties();

        ConfigureWall(parent, "Forest North Boundary", sprite, center + new Vector3(0f, 7.4f, 0f), new Vector3(17f, 0.55f, 1f));
        ConfigureWall(parent, "Forest West Boundary", sprite, center + new Vector3(-8.8f, 0f, 0f), new Vector3(0.55f, 14f, 1f));
        ConfigureWall(parent, "Forest East Boundary", sprite, center + new Vector3(8.8f, 0f, 0f), new Vector3(0.55f, 14f, 1f));

        ConfigureLostWoodsDungeon(parent, sprite, player, center + new Vector3(0f, 18f, 0f), center + new Vector3(0f, 6.2f, 0f));
    }

    static void ConfigureLostWoodsDungeon(Transform parent, Sprite sprite, Transform player, Vector3 dungeonCenter, Vector3 entrancePosition)
    {
        GameObject dungeon = GetOrCreateChild(parent, "Lost Woods Dungeon", dungeonCenter);
        ConfigureGroundPatch(dungeon.transform, "Lost Woods Room Ground", sprite, Vector3.zero, new Vector3(13f, 8.5f, 1f), new Color(0.07f, 0.28f, 0.11f));
        ConfigureGroundPatch(dungeon.transform, "Lost Woods Fork Path", sprite, new Vector3(0f, -1.4f, 0f), new Vector3(8f, 1.1f, 1f), new Color(0.34f, 0.24f, 0.15f));
        ConfigureGroundPatch(dungeon.transform, "Lost Woods North Path", sprite, new Vector3(0f, 1.3f, 0f), new Vector3(1.25f, 4.3f, 1f), new Color(0.34f, 0.24f, 0.15f));

        Transform spawn = ConfigureSpawn(dungeon.transform, "Lost Woods Player Spawn", new Vector3(0f, -2.8f, 0f));
        CameraAreaBounds2D bounds = ConfigureAreaBounds(parent, "Lost Woods Camera Bounds", new Vector2(dungeonCenter.x - 6f, dungeonCenter.y - 4f), new Vector2(dungeonCenter.x + 6f, dungeonCenter.y + 4f));

        GameObject entranceMarker = ConfigureSimpleInteractable(parent, "Lost Woods Entrance", sprite, entrancePosition, new Vector3(2.1f, 1.35f, 1f), new Color(0.08f, 0.18f, 0.08f), "Lost Woods", "Enter Dungeon", new[] { "Enter the shifting woods." }, false);
        LostWoodsEntranceInteractable entrance = GetOrAddComponent<LostWoodsEntranceInteractable>(entranceMarker);

        GameObject leftGate = ConfigureSimpleInteractable(dungeon.transform, "Left Woods Path", sprite, new Vector3(-3.3f, 2.8f, 0f), new Vector3(1.05f, 1f, 1f), new Color(0.04f, 0.2f, 0.08f), "Left Path", "Choose Left", null, true);
        GameObject rightGate = ConfigureSimpleInteractable(dungeon.transform, "Right Woods Path", sprite, new Vector3(3.3f, 2.8f, 0f), new Vector3(1.05f, 1f, 1f), new Color(0.04f, 0.2f, 0.08f), "Right Path", "Choose Right", null, true);

        LostWoodsDungeonController controller = GetOrAddComponent<LostWoodsDungeonController>(dungeon);

        GameObject[] treeProps = new GameObject[24];
        for (int i = 0; i < treeProps.Length; i++)
        {
            treeProps[i] = GetOrCreateChild(dungeon.transform, $"Generated Tree {i + 1}", Vector3.zero);
            treeProps[i].transform.localScale = new Vector3(1.05f, 1.8f, 1f);
            SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(treeProps[i]);
            renderer.sprite = sprite;
            renderer.color = new Color(0.02f, 0.22f, 0.07f);
            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(treeProps[i]);
            collider.size = new Vector2(0.75f, 0.32f);
            collider.offset = new Vector2(0f, -0.34f);
            ConfigureYSort(treeProps[i]);
        }

        GameObject[] chests = new GameObject[3];
        for (int i = 0; i < chests.Length; i++)
        {
            chests[i] = ConfigureSimpleInteractable(dungeon.transform, $"Generated Forest Chest {i + 1}", sprite, Vector3.zero, new Vector3(0.75f, 0.55f, 1f), new Color(0.45f, 0.25f, 0.1f), "Forest Chest", "Open", null, true);
            GetOrAddComponent<ForestLootChestInteractable>(chests[i]);
        }

        GameObject[] enemies = new GameObject[5];
        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i] = ConfigureEnemy(dungeon.transform, $"Generated Forest Enemy {i + 1}", sprite, player, Vector3.zero, new Color(0.1f, 0.55f, 0.16f), 45f + i * 8f, 2.05f + i * 0.08f, "Forest", i % 2 == 0 ? "Forest Skirmisher" : "Forest Guard", "Life Moss", 12 + i * 2, 7 + i * 2);
            enemies[i].SetActive(false);
        }

        SerializedObject controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("playerSpawn").objectReferenceValue = spawn;
        controllerObject.FindProperty("cameraBounds").objectReferenceValue = bounds;
        SetObjectArray(controllerObject.FindProperty("treeProps"), treeProps);
        SetObjectArray(controllerObject.FindProperty("enemies"), enemies);
        SetObjectArray(controllerObject.FindProperty("chests"), chests);
        controllerObject.FindProperty("targetDepth").intValue = 5;
        controllerObject.ApplyModifiedProperties();

        SerializedObject entranceObject = new SerializedObject(entrance);
        SetSerializedStringIfPresent(entranceObject, "displayName", "Lost Woods");
        SetSerializedStringIfPresent(entranceObject, "promptText", "Enter");
        entranceObject.FindProperty("dungeon").objectReferenceValue = controller;
        entranceObject.ApplyModifiedProperties();

        ConfigureLostWoodsGate(leftGate, controller, true);
        ConfigureLostWoodsGate(rightGate, controller, false);
    }

    static void ConfigureLostWoodsGate(GameObject gate, LostWoodsDungeonController controller, bool chooseLeft)
    {
        LostWoodsGateInteractable gateInteractable = GetOrAddComponent<LostWoodsGateInteractable>(gate);
        SerializedObject gateObject = new SerializedObject(gateInteractable);
        SetSerializedStringIfPresent(gateObject, "displayName", chooseLeft ? "Left Path" : "Right Path");
        SetSerializedStringIfPresent(gateObject, "promptText", chooseLeft ? "Choose Left" : "Choose Right");
        gateObject.FindProperty("dungeon").objectReferenceValue = controller;
        gateObject.FindProperty("chooseLeft").boolValue = chooseLeft;
        gateObject.ApplyModifiedProperties();
    }

    static void ConfigureFutureRoutePreview(Transform parent, Sprite sprite, string routeName, Vector3 center, Color color, string description)
    {
        ConfigureGroundPatch(parent, $"{routeName} Preview Ground", sprite, center, new Vector3(9f, 6f, 1f), color);
        ConfigureSimpleInteractable(parent, $"{routeName} Preview Sign", sprite, center + new Vector3(-2.6f, 1.6f, 0f), new Vector3(1f, 0.7f, 1f), new Color(0.28f, 0.2f, 0.12f), $"{routeName} Route", "Read", new[]
        {
            description,
            "This area is a landmark placeholder until its combat route is built."
        }, true);
    }

    static void SetObjectArray(SerializedProperty property, GameObject[] objects)
    {
        property.arraySize = objects.Length;
        for (int i = 0; i < objects.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
        }
    }

    static void ConfigureTrainingDummy(Transform parent, Sprite sprite, Vector3 position)
    {
        GameObject dummy = ConfigureSimpleInteractable(parent, "Training Dummy", sprite, position, new Vector3(0.72f, 1.05f, 1f), new Color(0.72f, 0.42f, 0.16f), "Training Dummy", "Inspect", new[]
        {
            "Hit me to test weapon timing, range, damage numbers, and hit flash.",
            "I do not fight back. Not everything in this kingdom is immediately rude."
        }, true);

        HealthComponent health = GetOrAddComponent<HealthComponent>(dummy);
        SerializedObject healthObject = new SerializedObject(health);
        healthObject.FindProperty("maxHealth").floatValue = 999f;
        healthObject.FindProperty("currentHealth").floatValue = 999f;
        healthObject.FindProperty("regenerationPerSecond").floatValue = 80f;
        healthObject.FindProperty("destroyWhenEmpty").boolValue = false;
        healthObject.ApplyModifiedProperties();

        GetOrAddComponent<HitFlashOnDamage>(dummy).RefreshRenderers();
        GetOrAddComponent<DamageNumberEmitter>(dummy);
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

        ConfigureEnemy(parent, $"{biomeName} Enemy A", sprite, player, center + new Vector3(2.2f, 1.35f, 0f), enemyColor, GetRouteEnemyHealth(biomeName, false), GetRouteEnemySpeed(biomeName, false), biomeName, GetRouteArchetype(biomeName, false), GetRouteMaterial(biomeName), GetRouteExperience(biomeName, false), GetRoutePixicoins(biomeName, false));
        ConfigureEnemy(parent, $"{biomeName} Enemy B", sprite, player, center + new Vector3(-1.6f, -1.75f, 0f), enemyColor * 0.85f + Color.white * 0.15f, GetRouteEnemyHealth(biomeName, true), GetRouteEnemySpeed(biomeName, true), biomeName, GetRouteArchetype(biomeName, true), GetRouteMaterial(biomeName), GetRouteExperience(biomeName, true), GetRoutePixicoins(biomeName, true));
    }

    static float GetRouteEnemyHealth(string biomeName, bool secondEnemy)
    {
        return biomeName switch
        {
            "Sea" => secondEnemy ? 62f : 48f,
            "Desert" => secondEnemy ? 92f : 72f,
            "Volcano" => secondEnemy ? 135f : 105f,
            "Ruins" => secondEnemy ? 115f : 85f,
            _ => secondEnemy ? 90f : 65f
        };
    }

    static float GetRouteEnemySpeed(string biomeName, bool secondEnemy)
    {
        return biomeName switch
        {
            "Sea" => secondEnemy ? 2.8f : 3.15f,
            "Volcano" => secondEnemy ? 1.45f : 1.7f,
            "Desert" => secondEnemy ? 1.75f : 2.05f,
            "Ruins" => secondEnemy ? 2.1f : 2.35f,
            _ => secondEnemy ? 1.85f : 2.15f
        };
    }

    static string GetRouteArchetype(string biomeName, bool secondEnemy)
    {
        return biomeName switch
        {
            "Sea" => secondEnemy ? "Sea Skirmisher" : "Sea Cutter",
            "Desert" => secondEnemy ? "Desert Guard" : "Glass Striker",
            "Volcano" => secondEnemy ? "Basalt Guard" : "Ember Brute",
            "Ruins" => secondEnemy ? "Arcane Guard" : "Ruin Striker",
            _ => secondEnemy ? "Forest Guard" : "Forest Skirmisher"
        };
    }

    static string GetRouteMaterial(string biomeName)
    {
        return biomeName switch
        {
            "Sea" => "Salt Pearl",
            "Desert" => "Sun Glass",
            "Volcano" => "Ember Ore",
            "Ruins" => "Arcane Fragment",
            _ => "Life Moss"
        };
    }

    static int GetRouteExperience(string biomeName, bool secondEnemy)
    {
        int baseValue = biomeName switch
        {
            "Sea" => 14,
            "Desert" => 17,
            "Volcano" => 22,
            "Ruins" => 20,
            _ => 15
        };

        return secondEnemy ? baseValue + 5 : baseValue;
    }

    static int GetRoutePixicoins(string biomeName, bool secondEnemy)
    {
        int baseValue = biomeName switch
        {
            "Sea" => 10,
            "Desert" => 13,
            "Volcano" => 18,
            "Ruins" => 16,
            _ => 11
        };

        return secondEnemy ? baseValue + 5 : baseValue;
    }

    static Transform ConfigureSpawn(Transform parent, string name, Vector3 position)
    {
        GameObject spawn = GetOrCreateChild(parent, name, position);
        SceneSpawnPoint spawnPoint = GetOrAddComponent<SceneSpawnPoint>(spawn);
        SerializedObject spawnObject = new SerializedObject(spawnPoint);
        spawnObject.FindProperty("spawnPointName").stringValue = name;
        spawnObject.ApplyModifiedProperties();
        return spawn.transform;
    }

    static GameObject ConfigureScenePortal(Transform parent, string name, Sprite sprite, Vector3 position, Vector3 scale, string displayName, string prompt, string targetSceneName, string targetSpawnPointName, string promptTitle, string promptBody)
    {
        GameObject portal = ConfigureSimpleInteractable(parent, name, sprite, position, scale, new Color(0.74f, 0.58f, 0.18f), displayName, prompt, null, false);
        ScenePortalInteractable scenePortal = GetOrAddComponent<ScenePortalInteractable>(portal);
        SerializedObject portalObject = new SerializedObject(scenePortal);
        SetSerializedStringIfPresent(portalObject, "displayName", displayName);
        SetSerializedStringIfPresent(portalObject, "promptText", prompt);
        portalObject.FindProperty("targetSceneName").stringValue = targetSceneName;
        portalObject.FindProperty("targetSpawnPointName").stringValue = targetSpawnPointName;
        portalObject.FindProperty("promptTitle").stringValue = promptTitle;
        portalObject.FindProperty("promptBody").stringValue = promptBody;
        portalObject.ApplyModifiedProperties();
        return portal;
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
        GameObject gate = ConfigureSimpleInteractable(parent, name, sprite, position, new Vector3(1.85f, 1.25f, 1f), new Color(0.74f, 0.58f, 0.18f), displayName, prompt, new[]
        {
            $"Travel through {displayName}?"
        }, false);

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
