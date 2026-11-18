using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class InteractionPrototypeSetup
{
    const string Root = "Assets/ScriptableObjects/Interactions";
    [MenuItem("Tools/Sims Mock/Create Interaction Prototype")]
    public static void CreatePrototype()
    {
        EnsureFolder("Assets/ScriptableObjects"); EnsureFolder(Root); EnsureFolder("Assets/Scenes");
        string scenePath = "Assets/Scenes/InteractionPrototype.unity";
        if (File.Exists(scenePath) && !EditorUtility.DisplayDialog("Replace prototype scene?", "InteractionPrototype シーンを新しいクリック操作のサンプルで置き換えますか？", "置き換える", "キャンセル")) return;
        var sit = CreateDefinition("Sit.asset", "座る", "椅子でひと休みする", SimsMock.Interaction.InteractionKind.Sit, 2f, 1.35f, 1);
        var work = CreateDefinition("Work.asset", "作業する", "作業台で作業を進める", SimsMock.Interaction.InteractionKind.Work, 3f, 1.5f, 1);
        var tidy = CreateDefinition("Tidy.asset", "整える", "作業台を整頓する", SimsMock.Interaction.InteractionKind.Work, 1.5f, 1.5f, 1);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(.55f, .58f, .62f);
        CreateLight();
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Prototype Terrain"; floor.transform.position = new Vector3(0, -.15f, 0); floor.transform.localScale = new Vector3(14, .3f, 14); floor.AddComponent<GroundMoveTarget>(); Paint(floor, new Color(.28f,.34f,.29f));
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule); player.name = "Player"; player.transform.position = new Vector3(0, 1, -2); Paint(player, new Color(.25f,.55f,.95f));
        var movement = player.AddComponent<ClickMovementController>();
        var scanner = player.AddComponent<MockInteractionScanner>();
        var executor = player.AddComponent<InteractionExecutor>(); executor.Configure(player.transform, movement);
        CreateFurniture("Chair", new Vector3(-2, .55f, 1), new Vector3(1,1.1f,1), new Color(.55f,.3f,.18f), new List<InteractionDefinition>{sit});
        CreateFurniture("Workbench", new Vector3(2, .55f, 1), new Vector3(1.6f,1.1f,1), new Color(.65f,.46f,.25f), new List<InteractionDefinition>{work,tidy});
        var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera"; var camera = cameraObject.AddComponent<Camera>(); camera.fieldOfView = 55; cameraObject.AddComponent<AudioListener>(); var follow = cameraObject.AddComponent<FollowCamera>(); follow.Configure(player.transform);
        var panelSettings = GetOrCreatePanelSettings();
        var controllerObject = new GameObject("Interaction Prototype Controller");
        var panelRenderer = controllerObject.AddComponent<PanelRenderer>(); panelRenderer.panelSettings = panelSettings;
        var ui = controllerObject.AddComponent<InteractionPrototypeController>(); ui.Configure(player.transform, camera, scanner, executor);
        EditorSceneManager.SaveScene(scene, scenePath);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == scenePath)) scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        EditorUtility.DisplayDialog("Sims Mock", "InteractionPrototype シーンを作成しました。Play して家具を左クリックし、地面を左クリックして操作を試してください。", "OK");
    }
    static PanelSettings GetOrCreatePanelSettings()
    {
        const string path = "Assets/ScriptableObjects/Objects/InteractionPrototypePanelSettings.asset";
        var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(settings, path);
        }
        return settings;
    }
    static InteractionDefinition CreateDefinition(string file, string title, string description, SimsMock.Interaction.InteractionKind kind, float duration, float distance, int value)
    {
        string path = Root + "/" + file;
        var asset = AssetDatabase.LoadAssetAtPath<InteractionDefinition>(path);
        if (asset == null) { asset = ScriptableObject.CreateInstance<InteractionDefinition>(); AssetDatabase.CreateAsset(asset,path); }
        asset.Configure(title,description,kind,duration,distance,value); EditorUtility.SetDirty(asset); return asset;
    }
    static void CreateFurniture(string name, Vector3 position, Vector3 scale, Color color, List<InteractionDefinition> actions)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.position = position; obj.transform.localScale = scale; Paint(obj,color);
        var point = new GameObject("Interaction Point"); point.transform.SetParent(obj.transform,false); point.transform.localPosition = new Vector3(0,0,-.7f);
        var interactable = obj.AddComponent<MockInteractable>(); interactable.Configure(name, actions);
    }
    static void Paint(GameObject obj, Color color)
    { var renderer = obj.GetComponent<Renderer>(); if(renderer == null) return; var shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) shader = Shader.Find("Standard"); if (shader == null) return; var material = new Material(shader); material.color = color; renderer.material = material; }
    static void CreateLight()
    { var lightObject = new GameObject("Directional Light"); lightObject.transform.rotation = Quaternion.Euler(50,-30,0); var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; }
    static void EnsureFolder(string path)
    { if (AssetDatabase.IsValidFolder(path)) return; var parent = Path.GetDirectoryName(path).Replace('\\','/'); var name = Path.GetFileName(path); if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent); AssetDatabase.CreateFolder(parent,name); }
}
