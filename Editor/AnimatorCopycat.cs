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
    public List<Assignment> assignment = new List<Assignment>();

    public UnityEditor.Animations.AnimatorController controller;
    public Object fbx;
    private string fbxPath;
    private string folderPath;
    private string modelRootName;
    private UnityEditor.Animations.AnimatorController editingClone;
    private string[] signature;
    private AnimationClip[] clips;
    private ReorderableList finder;
    private Vector2 scroll { get; set; }
    private bool editing { get; set; }
    private bool signatureSet { get; set; }
    private string configPath = "Assets/Gears/Config/AnimatorCopycatConfig.asset";
    private AnimatorCopycatDatabase db { get; set; }

    [MenuItem("Window/Animator Copycat")]
    static void Init()
    {
        window = EditorWindow.GetWindow<AnimatorCopycat>();
        window.titleContent.text = "Copycat";
        window.Show();
    }

    void OnEnable()
    {
        if (CheckConfigFolder())
        {
            if (db = AssetDatabase.LoadAssetAtPath<AnimatorCopycatDatabase>(configPath))
            {
                SetAssignment(db.NameFeed(), db.SignatureFeed());
            }
        }
        minSize = new Vector2(400, 300);
    }

    void OnFocus()
    {

    }

    private static string GetSavePath()
    {
        return EditorUtility.SaveFilePanelInProject("Create Animator Controller Clone", "Untitled", "controller", "Create Animator Controller Clone");
    }

    void initialize()
    {
        if (db && !db.keepEditData)
        {
            CleanData();
        }
        Repaint();
    }

    void CleanData()
    {
        if (editingClone)
        {
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(editingClone));
            AssetDatabase.Refresh();
        }
        controller = null;
        editingClone = null;
        editing = false;
        fbx = null;
        signatureSet = false;
        signature = new string[0];
        finder = null;
        //if (assignment != null && assignment.Count > 0)
        //{
        //    foreach (Assignment a in assignment)
        //    {
        //        a.found = false;
        //        a.set = false;
        //    }
        //}
        assignment = new List<Assignment>();
        OnEnable();
        Repaint();
    }

    string[] GetAssignmentName()
    {
        if (assignment == null || assignment.Count == 0)
            return new string[0];
        string[] names = new string[assignment.Count];
        for (int i = 0; i < assignment.Count; i++)
        {
            names[i] = assignment[i].name;
        }
        return names;
    }
    string[] GetAssignmentSignature()
    {
        if (assignment == null || assignment.Count == 0)
            return new string[0];
        string[] path = new string[assignment.Count];
        for (int i = 0; i < assignment.Count; i++)
        {
            path[i] = assignment[i].signature;
        }
        return path;
    }

    void SetAssignment(string[] preset, string[] signature)
    {
        assignment = new List<Assignment>();
        for (int i = 0; i < preset.Length; i++)
        {
            Assignment a = new Assignment();
            a.name = preset[i];
            a.signature = signature[i];
            assignment.Add(a);
        }
    }

    void OnGUI()
    {
        scroll = GUILayout.BeginScrollView(scroll);
        #region Spacing
        GUILayout.BeginHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginVertical();
        GUILayout.Space(5);
        #endregion
        UnityEditor.Animations.AnimatorController c = (UnityEditor.Animations.AnimatorController)EditorGUILayout.ObjectField("Template", controller, typeof(UnityEditor.Animations.AnimatorController), false);

        if (c != controller)
        {
            if (editing)
            {
                editingClone = null;
                editing = false;
                signatureSet = false;
            }
            controller = c;
        }

        #region CheckController
        if (controller != null)
        {
            if (!editing)
            {
                string path = AssetDatabase.GetAssetPath(controller);
                string newPath = configPath.Substring(0, configPath.LastIndexOf('/')) + "/" + controller.name + ".edit" + ".controller";

                if (editingClone == null)
                {
                    if (editingClone = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(newPath))
                    {
                        AssetDatabase.Refresh();
                        editing = true;
                    }
                }

                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Start Edit", GUILayout.Height(30)))
                {
                    if (editingClone == null)
                    {
                        if (AssetDatabase.CopyAsset(path, newPath))
                        {
                            AssetDatabase.Refresh();
                            editingClone = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(newPath);
                            AssetDatabase.Refresh();
                            editing = true;
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
            }
        }
        #endregion

        if (editing && !signatureSet)
        {
            signature = Gears.AnimationUtility.GetAnimatorSignature(editingClone, BlendTreeOption.OnlyChildState, "layer", "/", "name");
            signatureSet = true;
        }

        fbx = EditorGUILayout.ObjectField("Target Model Root", fbx, typeof(Object), false);

        if (db)
            db.keepEditData = EditorGUILayout.Toggle(new GUIContent("Keep Edit Data"), db.keepEditData);

        if (fbx)
        {
            modelRootName = fbx.name;
            fbxPath = AssetDatabase.GetAssetPath(fbx);
            folderPath = fbxPath.Substring(0, fbxPath.LastIndexOf('/'));
        }

        if (finder == null)
        {
            if (assignment == null)
                assignment = new List<Assignment>();
            finder = new ReorderableList(assignment, typeof(Assignment), false, true, true, true);
            finder.drawHeaderCallback = (r) =>
            {
                EditorGUI.LabelField(r, new GUIContent("Contract"));
            };
            finder.drawElementCallback = (r, i, a, f) =>
            {
                if (assignment[i] != null)
                {
                    Assignment e = assignment[i];
                    float w = r.width * .5f - 5f;
                    float h = EditorGUIUtility.singleLineHeight;
                    int select = 0;
                    r.y += 2;
                    if (e.found)
                        GUI.Box(new Rect(r.x, r.y, 20, h), EditorGUIUtility.FindTexture("winbtn_mac_max"), EditorStyles.label);
                    else
                        GUI.Box(new Rect(r.x, r.y, 20, h), EditorGUIUtility.FindTexture("winbtn_mac_close"), EditorStyles.label);
                    r.x += 15;
                    if (e.set)
                        GUI.Box(new Rect(r.x, r.y, 20, h), EditorGUIUtility.FindTexture("winbtn_mac_max"), EditorStyles.label);
                    else
                        GUI.Box(new Rect(r.x, r.y, 20, h), EditorGUIUtility.FindTexture("winbtn_mac_close"), EditorStyles.label);
                    r.x += 20;
                    e.name = EditorGUI.TextField(new Rect(r.x, r.y, 100, h), e.name);
                    r.x += 105;
                    if (editing && signatureSet)
                    {
                        EditorGUI.LabelField(new Rect(r.x, r.y, 55, h), new GUIContent("assign to"));
                        r.x += 60;
                        select = EditorGUI.Popup(new Rect(r.x, r.y, r.width - r.x - 40, h),
                             signature.ToList().IndexOf(e.signature), signature);
                        r.x += r.width - r.x - 40;
                        if (select < 0)
                            select = 0;
                        else if (select > signature.Length)
                            select = signature.Length - 1;

                        e.signature = signature[select];

                        if (e.found && Gears.AnimationUtility.PingSignature(editingClone, e.signature))
                        {
                            if (Gears.AnimationUtility.PingSignature(editingClone, e.signature, e.motion))
                                e.set = true;
                            else
                                e.set = false;
                        }
                    }

                    if (e.found)
                        if (GUI.Button(new Rect(r.x + 5, r.y, 45, h), new GUIContent("Apply"), EditorStyles.miniButton))
                        {
                            if (Gears.AnimationUtility.SetMotionBySignature(editingClone, e.signature, e.motion))
                            {
                                e.set = true;
                                signature = Gears.AnimationUtility.GetAnimatorSignature(editingClone, BlendTreeOption.OnlyChildState, "layer", "/", "name");
                                e.signature = signature[select];
                            }
                        }
                }
            };
        }
        DrawWrapperLine();
        #region Preset
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save As Preset"))
        {
            if (CheckConfigFolder())
            {
                if (db != null)
                {
                    db.Init(GetAssignmentName(), GetAssignmentSignature());
                }
                else
                {
                    db = ScriptableObject.CreateInstance<AnimatorCopycatDatabase>();
                    db.hideFlags = HideFlags.NotEditable;
                    AssetDatabase.CreateAsset(db, @configPath);
                    AssetDatabase.Refresh();
                    db.Init(GetAssignmentName(), GetAssignmentSignature());
                }
            }
        }
        GUILayout.Space(5);
        if (!db) GUI.backgroundColor = Color.gray;
        else GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Load Preset"))
        {
            if (db != null)
                SetAssignment(db.NameFeed(), db.SignatureFeed());
        }
        GUI.backgroundColor = Color.white;
        GUILayout.Space(5);
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Clean Data", GUILayout.Width(80)))
        {
            CleanData();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
        #endregion
        DrawWrapperLine();
        if (editing)
            finder.DoLayoutList();

        if (editing && fbx && signatureSet)
        {
            if (GUILayout.Button("Apply All"))
            {
                int select = 0;
                for (int i = 0; i < assignment.Count; i++)
                {
                    if (!assignment[i].found)
                        return;
                    select = signature.ToList().IndexOf(assignment[i].signature);
                    if (Gears.AnimationUtility.SetMotionBySignature(editingClone, assignment[i].signature, assignment[i].motion))
                    {
                        assignment[i].set = true;
                        signature = Gears.AnimationUtility.GetAnimatorSignature(editingClone, BlendTreeOption.OnlyChildState, "layer", "/", "name");
                        assignment[i].signature = signature[select];
                    }
                }
            }
            DrawWrapperLine();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save", GUILayout.Width(position.width * .5f), GUILayout.Height(30)))
            {
                string assetPath = GetSavePath();
                if (string.IsNullOrEmpty(assetPath)) return;
                if (AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(editingClone), assetPath))
                {
                    AssetDatabase.Refresh();
                    initialize();
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        if (fbx)
        {
            clips = new AnimationClip[assignment.Count];
            for (int i = 0; i < assignment.Count; i++)
            {
                Assignment e = assignment[i];
                string path = folderPath + "/" + modelRootName + "@" + e.name + ".FBX";

                if (clips[i] = AssetDatabase.LoadAssetAtPath<AnimationClip>(path))
                {
                    e.found = true;
                    e.path = path;
                    e.motion = clips[i];
                }
                else
                {
                    e.found = false;
                    e.path = string.Empty;
                    e.motion = null;
                }
            }
        }
        #region Spacing
        GUILayout.EndVertical();
        GUILayout.Space(5);
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        #endregion
        Repaint();
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