using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Yaprak acilma animasyon klibini (orn. misir_Yapragi.fbx icindeki "Leaf" klibi) elle,
// Inspector'da sub-asset aramaya gerek kalmadan secip hedef LeafController'lara uygulamak icin.
public class LeafAnimationAssignerWindow : EditorWindow
{
    private const string DefaultSourcePath = "Assets/_GAME/Models/CornLeafAnimated/misir_Yapragi.fbx";

    private Object animationSource;
    private AnimationClip[] foundClips = new AnimationClip[0];
    private int selectedClipIndex = -1;
    private LeafController[] targets = new LeafController[0];

    [MenuItem("Tools/Misir Kocani/Yaprak Animasyonu Ata")]
    public static void ShowWindow()
    {
        GetWindow<LeafAnimationAssignerWindow>("Yaprak Animasyonu Ata");
    }

    private void OnEnable()
    {
        if (animationSource == null)
        {
            animationSource = AssetDatabase.LoadAssetAtPath<Object>(DefaultSourcePath);
        }

        RefreshClips();

        if (targets == null || targets.Length == 0)
        {
            targets = Object.FindObjectsOfType<LeafController>(true);
        }
    }

    private void RefreshClips()
    {
        if (animationSource == null)
        {
            foundClips = new AnimationClip[0];
            selectedClipIndex = -1;
            return;
        }

        string path = AssetDatabase.GetAssetPath(animationSource);
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

        List<AnimationClip> clips = new List<AnimationClip>();

        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                clips.Add(clip);
            }
        }

        foundClips = clips.ToArray();
        selectedClipIndex = foundClips.Length > 0 ? 0 : -1;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("1) Animasyon Kaynagi", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Animasyonun icinde bulundugu FBX/model dosyasini surukle (varsayilan: misir_Yapragi.fbx).", MessageType.None);

        EditorGUI.BeginChangeCheck();
        animationSource = EditorGUILayout.ObjectField("Kaynak Dosya", animationSource, typeof(Object), false);
        if (EditorGUI.EndChangeCheck())
        {
            RefreshClips();
        }

        if (GUILayout.Button("Klipleri Yenile"))
        {
            RefreshClips();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("2) Animasyon Klibi", EditorStyles.boldLabel);

        if (foundClips.Length == 0)
        {
            EditorGUILayout.HelpBox("Bu dosyada animasyon klibi bulunamadi.", MessageType.Warning);
        }
        else
        {
            string[] names = new string[foundClips.Length];

            for (int i = 0; i < foundClips.Length; i++)
            {
                names[i] = foundClips[i].name;
            }

            selectedClipIndex = EditorGUILayout.Popup("Klip", selectedClipIndex, names);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("3) Hedef Yapraklar (LeafController)", EditorStyles.boldLabel);

        for (int i = 0; i < targets.Length; i++)
        {
            targets[i] = (LeafController)EditorGUILayout.ObjectField(
                targets[i] != null ? targets[i].gameObject.name : "(bos)",
                targets[i],
                typeof(LeafController),
                true
            );
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Sahnedeki Tum Yapraklari Bul"))
        {
            targets = Object.FindObjectsOfType<LeafController>(true);
        }

        if (GUILayout.Button("+ Bos Alan Ekle"))
        {
            System.Array.Resize(ref targets, targets.Length + 1);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(15);

        bool canApply = selectedClipIndex >= 0 && selectedClipIndex < foundClips.Length && HasAnyTarget();

        GUI.enabled = canApply;

        if (GUILayout.Button("Secili Klibi Tum Hedeflere Uygula", GUILayout.Height(32)))
        {
            ApplyClip(foundClips[selectedClipIndex]);
        }

        GUI.enabled = true;
    }

    private bool HasAnyTarget()
    {
        foreach (LeafController t in targets)
        {
            if (t != null)
                return true;
        }

        return false;
    }

    private void ApplyClip(AnimationClip clip)
    {
        int appliedCount = 0;

        foreach (LeafController controller in targets)
        {
            if (controller == null)
                continue;

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty prop = so.FindProperty("replacementPeelClip");

            if (prop == null)
            {
                Debug.LogWarning($"Yaprak Animasyonu Ata: '{controller.gameObject.name}' uzerinde 'replacementPeelClip' alani bulunamadi.");
                continue;
            }

            prop.objectReferenceValue = clip;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            appliedCount++;
        }

        if (appliedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Debug.Log($"Yaprak Animasyonu Ata: '{clip.name}' klibi {appliedCount} hedefe uygulandi.");
    }
}
