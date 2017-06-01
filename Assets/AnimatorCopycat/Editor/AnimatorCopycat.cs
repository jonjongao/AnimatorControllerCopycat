using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gears;

public class AnimatorCopycat : EditorWindow
{
    public static AnimatorCopycat window;

    public struct Task
    {
        public UnityEngine.Object value;
        public int stateNameLength;
        public int motionNameLength;

        public Task(UnityEditor.Animations.AnimatorState value)
        {
            this.value = value;
            this.stateNameLength = value.name.Length;
            this.motionNameLength = value.motion.name.Length;
        }

        public Task(UnityEditor.Animations.BlendTree value)
        {
            this.value = value;
            this.stateNameLength = value.name.Length;
            this.motionNameLength = value.children.ToList().OrderBy(i => i.motion.name).First().motion.name.Length;
        }
    }

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
    }

    void OnFocus()
    {

    }

    private static string GetSavePath()
    {
        return EditorUtility.SaveFilePanelInProject("Create Animator Controller Clone", "Untitled", "controller", "Create Animator Controller Clone");
    }

    Dictionary<string, Dictionary<string, AnimatorCopycat.Task>> motions = new Dictionary<string, Dictionary<string, AnimatorCopycat.Task>>();

    UnityEditor.Animations.AnimatorController animator;
    Vector2 scrollPosition;
    bool isEditing;

    UnityEditor.Animations.AnimatorController copy;

    float maxStateWidth;
    float maxMotionWidth;

    private void GetMotions()
    {
        if (copy == null) return;
        motions = Gears.AnimatorUtility.GetAllAnimatorMotions(copy);
        Debug.Log("get motions from " + copy.name);
        motions.ToList().ForEach(i =>
        {
            maxStateWidth = i.Value.ToList().OrderBy(a => a.Value.stateNameLength).First().Value.stateNameLength;
            maxMotionWidth = i.Value.ToList().OrderBy(b => b.Value.motionNameLength).First().Value.motionNameLength;
        });
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
            copy = new UnityEditor.Animations.AnimatorController();
            EditorUtility.CopySerialized(value, copy);
            if (CheckConfigFolder() &&
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(value), "Assets/Gears/Config/Editing.controller"))
            {
                copy = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Gears/Config/Editing.controller");
            }
            AssetDatabase.Refresh();
            GetMotions();
            isEditing = true;
        }

        if (isEditing)
        {
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(this.position.height - 100f));

            motions.ToList().ForEach(layer =>
            {
                layer.Value.ToList().ForEach(state =>
                {
                    GUILayout.BeginHorizontal();

                    if (motions[layer.Key][state.Key].value.GetType() == typeof(AnimatorState))
                    {
                        EditorGUILayout.ObjectField(motions[layer.Key][state.Key].value,
                        typeof(AnimatorState),
                        false,
                        GUILayout.Width(maxStateWidth * 13f));
                    }
                    else
                    {
                        EditorGUILayout.ObjectField(motions[layer.Key][state.Key].value,
                        typeof(UnityEditor.Animations.BlendTree),
                        false,
                        GUILayout.Width(maxStateWidth * 13f));
                    }

                    GUILayout.Label("/", GUILayout.Width(10f));

                    if (motions[layer.Key][state.Key].value.GetType() == typeof(AnimatorState))
                    {
                        var i = (AnimatorState)motions[layer.Key][state.Key].value;
                        i.motion = EditorGUILayout.ObjectField(i.motion,
                            typeof(Motion),
                            false,
                            GUILayout.Width(maxMotionWidth * 13f)) as Motion;
                    }
                    else
                    {
                        try
                        {
                            var i = (UnityEditor.Animations.BlendTree)motions[layer.Key][state.Key].value;
                            var index = int.Parse(state.Key.Substring(state.Key.Length - 2, 2));
                            var newMotion = EditorGUILayout.ObjectField(i.children[index].motion,
                                typeof(Motion),
                                false,
                                GUILayout.Width(maxMotionWidth * 13f)) as Motion;

                            if (newMotion != i.children[index].motion)
                            {
                                Gears.AnimatorUtility.CloneBlendTree(i, index, newMotion);
                            }
                        }
                        catch (System.FormatException)
                        {
                            GUILayout.Label("BlendTree", GUILayout.Width(maxMotionWidth * 13f));
                        }
                    }

                    if (GUILayout.Button(layer.Key + "/" + state.Key))
                    {
                        Gears.AnimatorUtility.GetAnimatorStateByKey(copy, layer.Key + "/" + state.Key);
                    }

                    GUILayout.EndHorizontal();
                });
            });

            GUILayout.EndScrollView();


            EditorGUILayout.ObjectField("Copy", copy, typeof(UnityEditor.Animations.AnimatorController), false);
            if (GUILayout.Button("Save"))
            {
                AssetDatabase.MoveAsset("Assets/Gears/Config/Editing.controller", GetSavePath());
                animator = null;
                copy = null;
                isEditing = false;
            }

            if (copy != null) EditorUtility.SetDirty(copy);
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

    private void OnDestroy()
    {
        if (copy != null)
        {
            AssetDatabase.DeleteAsset("Assets/Gears/Config/Editing.controller");
        }
    }
}