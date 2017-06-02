using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

namespace Gears
{
    public class AnimatorControllerCopycat : EditorWindow
    {
        public static AnimatorControllerCopycat window;

        public struct State
        {
            public UnityEngine.Object value;
            public int stateNameLength;
            public int motionNameLength;

            public State(AnimatorState value)
            {
                this.value = value;
                this.stateNameLength = value.name.Length;
                this.motionNameLength = value.motion.name.Length;
            }

            public State(BlendTree value)
            {
                this.value = value;
                this.stateNameLength = value.name.Length;
                this.motionNameLength = value.children.ToList().OrderBy(i => i.motion.name).First().motion.name.Length;
            }
        }

        Dictionary<string, Dictionary<string, AnimatorControllerCopycat.State>> states = new Dictionary<string, Dictionary<string, AnimatorControllerCopycat.State>>();

        private Vector2 scroll;
        private AnimatorController template;
        private bool isEditing;

        private AnimatorController copy;
        private int hashCatch;
        private string pathCatch;
        private float maxStateWidth;
        private float maxMotionWidth;

        [MenuItem("Window/Animator Controller Copycat")]
        static void Init()
        {
            window = EditorWindow.GetWindow<AnimatorControllerCopycat>();
            window.titleContent.text = "Copycat";
            window.Show();
        }

        private void OnEnable()
        {
            minSize = new Vector2(400, 300);
        }

        private void OnDestroy()
        {
            if (copy != null)
            {
                AssetDatabase.DeleteAsset(pathCatch + hashCatch + ".controller");
                CleanCatch();
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical(GUILayout.Height(50f));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var value = EditorGUILayout.ObjectField("Template", template,
                typeof(AnimatorController),
                false, GUILayout.Width(350f)) as AnimatorController;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            if (!isEditing)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("Drag refer AnimatorController to template field  ", MessageType.Info);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            if (value != template)
            {
                if (isEditing)
                {
                    OnDestroy();
                    isEditing = false;
                }
                template = value;
                CopyAnimator(value);
                GetStates(copy);
                isEditing = true;
            }

            if (isEditing)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Box("Parent", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(maxStateWidth * 13f), GUILayout.MinWidth(130f));
                GUILayout.Space(6f);
                GUILayout.Box("Motion", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(maxMotionWidth * 13f), GUILayout.MinWidth(130f));
                GUILayout.Space(20f);
                GUILayout.Box("Path", EditorStyles.centeredGreyMiniLabel, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                scroll = GUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(this.position.height - 100f));

                states.ToList().ForEach(layer =>
                {
                    layer.Value.ToList().ForEach(state =>
                    {
                        GUILayout.BeginHorizontal();

                        if (states[layer.Key][state.Key].value.GetType() == typeof(AnimatorState))
                        {
                            EditorGUILayout.ObjectField(states[layer.Key][state.Key].value,
                            typeof(AnimatorState),
                            false,
                            GUILayout.Width(maxStateWidth * 13f));
                        }
                        else
                        {
                            EditorGUILayout.ObjectField(states[layer.Key][state.Key].value,
                            typeof(UnityEditor.Animations.BlendTree),
                            false,
                            GUILayout.Width(maxStateWidth * 13f));
                        }

                        GUILayout.Label("/", GUILayout.Width(10f));

                        if (states[layer.Key][state.Key].value.GetType() == typeof(AnimatorState))
                        {
                            var i = (AnimatorState)states[layer.Key][state.Key].value;
                            i.motion = EditorGUILayout.ObjectField(i.motion,
                                typeof(Motion),
                                false,
                                GUILayout.Width(maxMotionWidth * 13f)) as Motion;
                        }
                        else
                        {
                            try
                            {
                                var i = (UnityEditor.Animations.BlendTree)states[layer.Key][state.Key].value;
                                var index = int.Parse(state.Key.Substring(state.Key.Length - 2, 2));
                                var newMotion = EditorGUILayout.ObjectField(i.children[index].motion,
                                    typeof(Motion),
                                    false,
                                    GUILayout.Width(maxMotionWidth * 13f)) as Motion;

                                if (newMotion != i.children[index].motion)
                                {
                                    Gears.AnimatorTool.OverrideBlendTree(i, index, newMotion);
                                }
                            }
                            catch (System.FormatException)
                            {
                                GUILayout.Label("BlendTree", GUILayout.Width(maxMotionWidth * 13f));
                            }
                        }

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20f);
                        GUILayout.Label(layer.Key + "/" + state.Key);
                        GUILayout.EndHorizontal();

                        GUILayout.EndHorizontal();
                    });
                });

                GUILayout.EndScrollView();

                GUILayout.BeginVertical(GUILayout.Height(100f));
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.ObjectField("Copy", copy, typeof(UnityEditor.Animations.AnimatorController), false, GUILayout.Width(350f));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(6);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.color = Color.green;
                if (GUILayout.Button("Save", GUILayout.Width(100f), GUILayout.Height(50f)))
                {
                    var save = GetSavePath();
                    if (!string.IsNullOrEmpty(save))
                    {
                        AssetDatabase.MoveAsset(pathCatch + hashCatch + ".controller", save);
                        CleanCatch();

                        CopyAnimator(template);
                        GetStates(copy);
                    }
                }
                GUI.color = Color.white;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();


                GUILayout.FlexibleSpace();

                GUILayout.EndVertical();




                if (copy != null) EditorUtility.SetDirty(copy);
            }
        }

        private static string GetSavePath()
        {
            return EditorUtility.SaveFilePanelInProject("Create Animator Controller Copy", "Untitled", "controller", "Create Animator Controller Copy");
        }

        private void GetStates(AnimatorController animator)
        {
            if (animator == null) return;
            states = Gears.AnimatorTool.MappingAnimator(animator);
            states.ToList().ForEach(i =>
            {
                maxStateWidth = i.Value.ToList().OrderBy(a => a.Value.stateNameLength).First().Value.stateNameLength;
                maxMotionWidth = i.Value.ToList().OrderBy(b => b.Value.motionNameLength).First().Value.motionNameLength;
            });
        }

        private void CopyAnimator(AnimatorController animator)
        {
            var path = AssetDatabase.GetAssetPath(animator);

            hashCatch = animator.GetHashCode();
            pathCatch = path.Remove(path.LastIndexOf('/') + 1);

            if (AssetDatabase.CopyAsset(path, pathCatch + hashCatch + ".controller"))
            {
                copy = AssetDatabase.LoadAssetAtPath<AnimatorController>(pathCatch + hashCatch + ".controller");
            }
            AssetDatabase.Refresh();
        }

        private void CleanCatch()
        {
            copy = null;
            hashCatch = 0;
            pathCatch = string.Empty;
        }
    }
}