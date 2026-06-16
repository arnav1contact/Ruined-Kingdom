using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class CharacterCreationPrototypeBuilder
{
    [MenuItem("Tools/Ruined Kingdom/Create Character Creation Prototype")]
    public static void CreateCharacterCreationPrototype()
    {
        RemoveOldInputEventSystem();

        GameObject creatorObject = GetOrCreateGameObject("Character Creation Controller", Vector3.zero);
        CharacterCreationController creator = GetOrAddComponent<CharacterCreationController>(creatorObject);

        GameObject player = GameObject.Find("Player");
        GameObject gameplayRoot = GameObject.Find("Movement Test Room");

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

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = creatorObject;
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

    static void RemoveOldInputEventSystem()
    {
        StandaloneInputModule oldInputModule = Object.FindFirstObjectByType<StandaloneInputModule>();
        if (oldInputModule != null)
        {
            Object.DestroyImmediate(oldInputModule.gameObject);
        }
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

    static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
