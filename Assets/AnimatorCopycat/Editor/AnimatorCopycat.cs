using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gears;

[System.Serializable]
public class Assignment
{
    public bool found = false;
    public bool set = false;
    public string name = string.Empty;
    public string path = string.Empty;
    public string signature = string.Empty;
    public Motion motion = null;
}


public class AnimatorCopycat : EditorWindow
{
    public static AnimatorCopycat window;

    private UnityEditor.Animations.AnimatorController editingClone;
    private Vector2 scroll;
    private string configPath = "Assets/Gears/Config/AnimatorCopycatConfig.asset";

    [MenuItem("Window/Animator Copycat")]
    static void Init()
    {
        window = EditorWindow.GetWindow<AnimatorCopycat>();
        window.titleContent.text = "Copycat";
        window.Show();
    }

    void OnEnable()
    {
        minSize = new Vector2(400, 300);

        GetMotions();
    }

    void OnFocus()
    {

    }

    private static string GetSavePath()
    {
        return EditorUtility.SaveFilePanelInProject("Create Animator Controller Clone", "Untitled", "controller", "Create Animator Controller Clone");
    }

    Dictionary<string, Dictionary<string, Motion>> motions = new Dictionary<string, Dictionary<string, Motion>>();

    Dictionary<string, Dictionary<string, Motion>> newMotions;

    UnityEditor.Animations.AnimatorController animator;
    Vector2 scrollPosition;
    bool isEditing;

    private void GetMotions()
    {
        if (animator == null) return;
        motions = Gears.AnimatorUtility.GetAllAnimatorMotions(animator);
        //newMotions = new Dictionary<string, Dictionary<string, Motion>>(motions);
    }

    private void OnGUI()
    {
        var value = (UnityEditor.Animations.AnimatorController)EditorGUILayout.ObjectField("Template", animator, typeof(UnityEditor.Animations.AnimatorController), false);

        if (value != animator)
        {
            if (isEditing)
            {
                isEditing = false;
            }
            animator = value;
            GetMotions();
            isEditing = true;
        }

        if (isEditing)
        {
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(200));

            motions.ToList().ForEach(layer =>
            {
                layer.Value.ToList().ForEach(state =>
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Label(state.Value.name, EditorStyles.whiteLabel, GUILayout.Width(80f));
                    //EditorGUILayout.ObjectField(state.Value, typeof(Motion), false, GUILayout.MaxWidth(100f));
                    GUILayout.Label("to", EditorStyles.whiteMiniLabel, GUILayout.Width(20f));
                    motions[layer.Key][state.Key] = EditorGUILayout.ObjectField(motions[layer.Key][state.Key],
                        typeof(Motion),
                        false,
                        GUILayout.Width(100f)) as Motion;

                    GUILayout.Button(layer.Key + "/" + state.Key);
                    //EditorGUILayout.Popup(0, popup.ToArray());
                    GUILayout.EndHorizontal();
                });
            });

            GUILayout.EndScrollView();

            EditorUtility.SetDirty(animator);
            
        }

    }

    bool CheckConfigFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Gears/Config"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Gears"))
            {
                AssetDatabase.CreateFolder("Assets", "Gears");
                AssetDatabase.Refresh();
            }
            if (!AssetDatabase.IsValidFolder("Assets/Gears/Config"))
            {
                AssetDatabase.CreateFolder("Assets/Gears", "Config");
                AssetDatabase.Refresh();
            }
        }
        if (AssetDatabase.IsValidFolder("Assets/Gears/Config"))
            return true;
        else
            return false;
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    void OnDisable()
    {

    }

    private static void DrawWrapperLine()
    {
        Rect r = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUIUtility.singleLineHeight);
        GUI.Box(new Rect(r.x - 15f, r.y + (r.height * .5f), r.width + 30f, r.height), GUIContent.none, (GUIStyle)"IN Title");
    }
}