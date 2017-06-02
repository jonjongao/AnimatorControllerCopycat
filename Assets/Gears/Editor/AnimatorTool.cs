using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;
using System.Linq;

namespace Gears
{
    public static class AnimatorTool
    {
        public static Dictionary<string, Dictionary<string, AnimatorControllerCopycat.State>> MappingAnimator(AnimatorController animator)
        {
            Dictionary<string, Dictionary<string, AnimatorControllerCopycat.State>> motionsWithLayerKey = new Dictionary<string, Dictionary<string, AnimatorControllerCopycat.State>>();

            animator.layers.ToList().ForEach(l =>
            {
                Dictionary<string, AnimatorControllerCopycat.State> motions = new Dictionary<string, AnimatorControllerCopycat.State>();

                l.stateMachine.states.ToList().ForEach(s =>
                {
                    if (s.state.motion.GetType() == typeof(BlendTree))
                    {
                        BlendTree blendtree = (BlendTree)s.state.motion;
                        MappingBlendTree(blendtree).ToList().ForEach(a =>
                        {
                            motions.Add(s.state.name + "/" + a.Key, new AnimatorControllerCopycat.State(a.Value));
                        });
                        //BlendTreeToMotion(blendtree).ToList().ForEach(a =>
                        //{
                        //    motions.Add("[BlendTree]" + s.state.name + "/" + a.Key, a.Value);
                        //});
                    }
                    else
                    {
                        motions.Add(s.state.name, new AnimatorControllerCopycat.State(s.state));
                    }
                });
                motionsWithLayerKey.Add(l.name, motions);
            });

            return motionsWithLayerKey;
        }

        public static void OverrideBlendTree(BlendTree value, int childIndex, Motion newMotion)
        {
            List<ChildMotion> cloneMotions = new List<ChildMotion>();
            value.children.ToList().ForEach(i => cloneMotions.Add(i));

            for (int i = value.children.Length - 1; i >= 0; i--) { value.RemoveChild(i); }

            for (int i = 0; i < cloneMotions.Count; i++)
            {
                if (i == childIndex)
                {
                    value.AddChild(newMotion);
                }
                else
                {
                    value.AddChild(cloneMotions[i].motion);
                }
            }

            for (int i = 0; i < value.children.Length; i++)
            {
                value.children[i] = cloneMotions[i];
            }
        }

        public static Dictionary<string, BlendTree> MappingBlendTree(BlendTree value)
        {
            Dictionary<string, BlendTree> motions = new Dictionary<string, BlendTree>();
            int duplicateKey = 0;
            motions.Add(value.name, value);
            for (int i = 0; i < value.children.Length; i++)
            {
                if (value.children[i].motion.GetType() == typeof(BlendTree))
                {
                    MappingBlendTree((BlendTree)value.children[i].motion).ToList().ForEach(a =>
                    {
                        motions.Add(value.name + "/" + a.Key, a.Value);
                    });
                }
                else
                {
                    try
                    {
                        motions.Add(value.name, value);
                    }
                    catch (System.ArgumentException)
                    {
                        motions.Add(value.name + " " + duplicateKey++, value);
                    }
                }
            }

            return motions;
        }

        public static Dictionary<string, Motion> MappingBlendTreeInMotion(BlendTree value)
        {
            Dictionary<string, Motion> motions = new Dictionary<string, Motion>();
            int duplicateKey = 0;
            value.children.ToList().ForEach(i =>
            {
                try
                {
                    if (i.motion.GetType() == typeof(BlendTree))
                    {
                        MappingBlendTreeInMotion((BlendTree)i.motion).ToList().ForEach(a =>
                        {
                            motions.Add(value.name + "/" + a.Key, a.Value);
                        });
                    }
                    else
                    {
                        try
                        {
                            motions.Add(value.name + "/" + i.motion.name, i.motion);
                        }
                        catch (System.ArgumentException)
                        {
                            motions.Add(value.name + "/" + i.motion.name + " " + duplicateKey++, i.motion);
                        }
                    }
                }
                catch (System.NullReferenceException)
                {
                    motions.Add(value.name + "/" + i.motion.name, i.motion);
                }
            });
            return motions;
        }

        public static AnimatorState GetDefaultAnimatorState(AnimatorController animator)
        {
            return GetDefaultAnimatorState(animator, 0);
        }

        public static AnimatorState GetDefaultAnimatorState(AnimatorController animator, int layerIndex)
        {
            return animator.layers[layerIndex].stateMachine.defaultState;
        }
    }
}