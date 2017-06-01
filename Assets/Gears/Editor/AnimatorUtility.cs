using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

namespace Gears
{
    public enum BlendTreeOption { None, OnlyParentState, OnlyChildState, BothParentAndChildState }

    public static class AnimatorUtility
    {
        public static UnityEngine.Object GetSignatureResult(UnityEditor.Animations.AnimatorController animator, string signature)
        {
            if (signature.Contains("None (Motion)"))
                return null;
            string[] path = signature.Split('/');
            int count = path.Length;
            int error = 0;

            foreach (var l in animator.layers)
            {
                if (l.name == path[0])
                {
                    count--;
                    foreach (var s in l.stateMachine.states)
                    {
                        if (s.state.name == path[1])
                        {
                            count--;
                            if (count == 0)
                            {
                                return s.state;
                            }
                            else
                            {
                                if (s.state.motion != null)
                                {
                                    if (s.state.motion.GetType() == typeof(BlendTree))
                                    {
                                        BlendTree blendtree = (BlendTree)s.state.motion;
                                        int startIndex = 2;
                                        while (true)
                                        {
                                            foreach (ChildMotion c in blendtree.children)
                                            {
                                                if (c.motion != null)
                                                {
                                                    if (c.motion.GetType() == typeof(BlendTree))
                                                    {
                                                        if (((BlendTree)c.motion).name == path[startIndex])
                                                        {
                                                            count--;
                                                            blendtree = (BlendTree)c.motion;
                                                            startIndex++;
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (c.motion.name == path[startIndex].Split('@')[0])
                                                        {
                                                            count--;
                                                            if (count == 0)
                                                            {
                                                                return c.motion;
                                                            }
                                                            else
                                                            {
                                                                blendtree = (BlendTree)c.motion;
                                                                startIndex++;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            error++;
                                            if (error > 99)
                                            {
                                                Debug.LogError("GetSignatureResult overhead");
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static bool SetMotionBySignature(UnityEditor.Animations.AnimatorController animator, string signature, Motion value)
        {
            if (signature.Contains("None (Motion)"))
                return false;
            string[] path = signature.Split('/');
            int count = path.Length;
            int error = 0;

            foreach (var l in animator.layers)
            {
                if (l.name == path[0])
                {
                    count--;
                    foreach (var s in l.stateMachine.states)
                    {
                        if (s.state.name == path[1])
                        {
                            count--;
                            if (count == 0)
                            {
                                s.state.motion = value;
                                return true;
                            }
                            else
                            {
                                if (s.state.motion != null)
                                {
                                    if (s.state.motion.GetType() == typeof(BlendTree))
                                    {
                                        BlendTree blendtree = (BlendTree)s.state.motion;
                                        int startIndex = 2;
                                        while (true)
                                        {
                                            foreach (ChildMotion c in blendtree.children)
                                            {
                                                if (c.motion != null)
                                                {
                                                    if (c.motion.GetType() == typeof(BlendTree))
                                                    {
                                                        if (((BlendTree)c.motion).name == path[startIndex])
                                                        {
                                                            count--;
                                                            blendtree = (BlendTree)c.motion;
                                                            startIndex++;
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (c.motion.name == path[startIndex].Split('@')[0])
                                                        {
                                                            count--;
                                                            if (count == 0)
                                                            {
                                                                ChildMotion[] newMotion = blendtree.children;
                                                                newMotion[int.Parse(path[startIndex].Split('@')[1])].motion = value;
                                                                blendtree.children = newMotion;
                                                                return true;
                                                            }
                                                            else
                                                            {
                                                                blendtree = (BlendTree)c.motion;
                                                                startIndex++;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            error++;
                                            if (error > 99)
                                            {
                                                Debug.LogError("SetMotionBySignature overhead");
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static bool PingSignature(UnityEditor.Animations.AnimatorController animator, string signature)
        {
            if ((Motion)GetSignatureResult(animator, signature))
                return true;
            else
                return false;
        }
        public static bool PingSignature(UnityEditor.Animations.AnimatorController animator, string signature, Motion motion)
        {
            if ((Motion)GetSignatureResult(animator, signature) == motion)
                return true;
            else
                return false;
        }

        public static string[] GetAnimatorSignature(UnityEditor.Animations.AnimatorController animator)
        {
            return GetAnimatorSignature(animator, BlendTreeOption.OnlyParentState, "layer", "/", "name");
        }
        public static string[] GetAnimatorSignature(UnityEditor.Animations.AnimatorController animator, BlendTreeOption option, params string[] layout)
        {
            if (layout == null || layout.Length == 0)
            {
                Debug.LogError("Signature doesn't setup layout, use default layout");
                layout = new string[] { "layer", "/", "name" };
            }
            string[] _name = GetAnimatorStatesName(animator, option);
            int[] _index = GetAnimatorStatesLayerIndex(animator, option);
            string[] _layer = GetAnimatorStatesLayerName(animator, option);


            if (_index.Length != _name.Length)
            {
                Debug.LogError("Result List of Name(" + _name.Length + ") and Index(" + _index.Length + ") not match");
                foreach (var n in _name)
                    Debug.LogError("Name: " + n);
                foreach (var i in _index)
                    Debug.LogError("Index: " + i);
                return null;
            }
            if (_layer.Length != _name.Length) { Debug.LogError("Result List of Name(" + _name.Length + ") and Layer(" + _layer.Length + ") not match"); return null; }
            string[] _out = new string[_name.Length];
            for (int i = 0; i < _out.Length; i++)
            {
                foreach (string s in layout)
                {
                    if (s == "name")
                        _out[i] += _name[i];
                    else if (s == "index")
                        _out[i] += _index[i].ToString();
                    else if (s == "layer")
                        _out[i] += _layer[i].ToString();
                    else
                        _out[i] += s;
                }
            }

            if (_out.Length == null || _out.Length == 0)
                _out = new string[1] { "error" };

            return _out;
        }

        public static string[] GetAnimatorStatesLayerName(UnityEditor.Animations.AnimatorController animator)
        {
            return GetAnimatorStatesLayerName(animator, BlendTreeOption.OnlyParentState);
        }
        public static string[] GetAnimatorStatesLayerName(UnityEditor.Animations.AnimatorController animator, BlendTreeOption option)
        {
            List<string> _layer = new List<string>();
            for (int l = 0; l < animator.layers.Length; l++)
            {
                for (int s = 0; s < animator.layers[l].stateMachine.states.Length; s++)
                {
                    if (animator.layers[l].stateMachine.states[s].state.motion != null)
                    {
                        if (animator.layers[l].stateMachine.states[s].state.motion.GetType() == typeof(BlendTree))
                        {
                            BlendTree blendtree = (BlendTree)animator.layers[l].stateMachine.states[s].state.motion;
                            string veryRoot = animator.layers[l].stateMachine.states[s].state.name;
                            int _error = 0;
                            string newRoot = string.Empty;
                            int totalCount = 0;

                            switch (option)
                            {
                                case BlendTreeOption.OnlyChildState:
                                    totalCount += blendtree.children.Length;

                                    while (PingBlendTree(blendtree, out newRoot, out blendtree))
                                    {
                                        totalCount += blendtree.children.Length - 1;
                                        _error++;
                                        if (_error > 99)
                                            break;
                                    }
                                    for (int i = 0; i < totalCount; i++)
                                        _layer.Add(animator.layers[l].name);
                                    break;
                                case BlendTreeOption.OnlyParentState:
                                    _layer.Add(animator.layers[l].name);
                                    break;
                                case BlendTreeOption.BothParentAndChildState:
                                    _layer.Add(animator.layers[l].name);
                                    totalCount += blendtree.children.Length;

                                    while (PingBlendTree(blendtree, out newRoot, out blendtree))
                                    {
                                        totalCount += blendtree.children.Length - 1;
                                        _error++;
                                        if (_error > 99)
                                            break;
                                    }
                                    for (int i = 0; i < totalCount; i++)
                                        _layer.Add(animator.layers[l].name);
                                    break;
                            }
                        }
                        else
                            _layer.Add(animator.layers[l].name);
                    }
                    else
                        _layer.Add(animator.layers[l].name);
                }
            }
            return _layer.ToArray();
        }

        public static int[] GetAnimatorStatesLayerIndex(UnityEditor.Animations.AnimatorController animator)
        {
            return GetAnimatorStatesLayerIndex(animator, BlendTreeOption.OnlyParentState);
        }
        public static int[] GetAnimatorStatesLayerIndex(UnityEditor.Animations.AnimatorController animator, BlendTreeOption option)
        {
            List<int> _index = new List<int>();
            for (int l = 0; l < animator.layers.Length; l++)
            {
                for (int s = 0; s < animator.layers[l].stateMachine.states.Length; s++)
                {
                    if (animator.layers[l].stateMachine.states[s].state.motion != null)
                    {
                        if (animator.layers[l].stateMachine.states[s].state.motion.GetType() == typeof(BlendTree))
                        {
                            BlendTree blendtree = (BlendTree)animator.layers[l].stateMachine.states[s].state.motion;
                            string veryRoot = animator.layers[l].stateMachine.states[s].state.name;
                            int _error = 0;
                            string newRoot = string.Empty;
                            int totalCount = 0;

                            switch (option)
                            {
                                case BlendTreeOption.OnlyChildState:
                                    totalCount += blendtree.children.Length;

                                    while (PingBlendTree(blendtree, out newRoot, out blendtree))
                                    {
                                        totalCount += blendtree.children.Length - 1;
                                        _error++;
                                        if (_error > 99)
                                            break;
                                    }
                                    for (int i = 0; i < totalCount; i++)
                                        _index.Add(l);
                                    break;
                                case BlendTreeOption.OnlyParentState:
                                    _index.Add(l);
                                    break;
                                case BlendTreeOption.BothParentAndChildState:
                                    _index.Add(l);
                                    totalCount += blendtree.children.Length;

                                    while (PingBlendTree(blendtree, out newRoot, out blendtree))
                                    {
                                        totalCount += blendtree.children.Length - 1;
                                        _error++;
                                        if (_error > 99)
                                            break;
                                    }
                                    for (int i = 0; i < totalCount; i++)
                                        _index.Add(l);
                                    break;
                            }
                        }
                        else
                            _index.Add(l);
                    }
                    else
                        _index.Add(l);
                }
            }
            return _index.ToArray();

        }

        public static void GetAnimatorStateByKey(AnimatorController animator, string key)
        {
            var value = key.Split('/');
            Debug.Log("received key: " + key + " -> ");
            foreach (var i in value)
            {
                Debug.Log(i);
            }

            //animator.layers.Where(a=>a.name==value[0]).
        }

        public static Dictionary<string, Dictionary<string, AnimatorCopycat.Task>> GetAllAnimatorMotions(AnimatorController animator)
        {
            Dictionary<string, Dictionary<string, AnimatorCopycat.Task>> motionsWithLayerKey = new Dictionary<string, Dictionary<string, AnimatorCopycat.Task>>();

            animator.layers.ToList().ForEach(l =>
            {
                Dictionary<string, AnimatorCopycat.Task> motions = new Dictionary<string, AnimatorCopycat.Task>();

                l.stateMachine.states.ToList().ForEach(s =>
                {
                    if (s.state.motion.GetType() == typeof(BlendTree))
                    {
                        BlendTree blendtree = (BlendTree)s.state.motion;
                        UnpackBlendTree(blendtree).ToList().ForEach(a =>
                        {
                            motions.Add(s.state.name + "/" + a.Key, new AnimatorCopycat.Task(a.Value));
                        });
                        //BlendTreeToMotion(blendtree).ToList().ForEach(a =>
                        //{
                        //    motions.Add("[BlendTree]" + s.state.name + "/" + a.Key, a.Value);
                        //});
                    }
                    else
                    {
                        motions.Add(s.state.name, new AnimatorCopycat.Task(s.state));
                    }
                });
                motionsWithLayerKey.Add(l.name, motions);
            });

            return motionsWithLayerKey;
        }

        public static void CloneBlendTree(BlendTree value, int index, Motion newMotion)
        {
            List<ChildMotion> cloneMotions = new List<ChildMotion>();
            value.children.ToList().ForEach(i => cloneMotions.Add(i));

            for (int i = value.children.Length - 1; i >= 0; i--) { value.RemoveChild(i); }

            for (int i = 0; i < cloneMotions.Count; i++)
            {
                if (i == index)
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

        public static Dictionary<string, BlendTree> UnpackBlendTree(BlendTree value)
        {
            Dictionary<string, BlendTree> motions = new Dictionary<string, BlendTree>();
            int duplicateKey = 0;
            motions.Add(value.name, value);
            for (int i = 0; i < value.children.Length; i++)
            {
                if (value.children[i].motion.GetType() == typeof(BlendTree))
                {
                    UnpackBlendTree((BlendTree)value.children[i].motion).ToList().ForEach(a =>
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
            //value.children.ToList().ForEach(i =>
            //{
            //    if(i.motion.GetType()==typeof(BlendTree))
            //    {
            //        UnpackBlendTree((BlendTree)i.motion).ToList().ForEach(a =>
            //        {
            //            motions.Add(value.name + "/" + a.Key, a.Value);
            //        });
            //    }
            //});

            return motions;
        }

        public static Dictionary<string, Motion> BlendTreeToMotion(BlendTree value)
        {
            Dictionary<string, Motion> motions = new Dictionary<string, Motion>();
            int duplicateKey = 0;
            value.children.ToList().ForEach(i =>
            {
                try
                {
                    if (i.motion.GetType() == typeof(BlendTree))
                    {
                        BlendTreeToMotion((BlendTree)i.motion).ToList().ForEach(a =>
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

        public static string[] GetAnimatorStatesName(UnityEditor.Animations.AnimatorController animator)
        {
            return GetAnimatorStatesName(animator, BlendTreeOption.OnlyParentState);
        }
        public static string[] GetAnimatorStatesName(UnityEditor.Animations.AnimatorController animator, BlendTreeOption option)
        {
            List<string> _name = new List<string>();
            for (int l = 0; l < animator.layers.Length; l++)
            {
                for (int s = 0; s < animator.layers[l].stateMachine.states.Length; s++)
                {
                    if (animator.layers[l].stateMachine.states[s].state.motion != null)
                    {
                        Motion m = animator.layers[l].stateMachine.states[s].state.motion;
                        if (m.GetType() == typeof(BlendTree))
                        {
                            BlendTree blendtree = (BlendTree)m;
                            string veryRoot = animator.layers[l].stateMachine.states[s].state.name;
                            int _error = 0;
                            string newRoot = string.Empty;

                            switch (option)
                            {
                                case BlendTreeOption.OnlyChildState:
                                    _name.AddRange(DigBlendTree(blendtree, veryRoot));

                                    while (PingBlendTree(blendtree, out newRoot, out blendtree))
                                    {
                                        _name.AddRange(DigBlendTree(blendtree, veryRoot += newRoot));
                                        _error++;
                                        if (_error > 99)
                                            break;
                                    }
                                    break;
                                case BlendTreeOption.OnlyParentState:
                                    _name.Add(veryRoot);
                                    break;
                                case BlendTreeOption.BothParentAndChildState:
                                    _name.Add(veryRoot);
                                    _name.AddRange(DigBlendTree(blendtree, veryRoot));
                                    while (PingBlendTree(blendtree, out newRoot, out blendtree))
                                    {
                                        _name.AddRange(DigBlendTree(blendtree, veryRoot += newRoot));
                                        _error++;
                                        if (_error > 99)
                                            break;
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            _name.Add(animator.layers[l].stateMachine.states[s].state.name);
                        }
                    }
                    else
                    {
                        _name.Add(animator.layers[l].stateMachine.states[s].state.name);
                    }
                }
            }
            return _name.ToArray();
        }

        static bool PingBlendTree(BlendTree blendtree, out string newRoot, out BlendTree newBlendTree)
        {
            bool _stillHaveBlendTree = false;
            string _newRoot = string.Empty;
            BlendTree _newBlendTree = null;
            if (blendtree.children.Length > 0)
            {
                foreach (ChildMotion c in blendtree.children)
                {
                    if (c.motion != null)
                    {
                        if (c.motion.GetType() == typeof(BlendTree))
                        {
                            _stillHaveBlendTree = true;
                            _newBlendTree = (BlendTree)c.motion;
                            _newRoot = _newBlendTree.name;
                            break;
                        }
                    }
                }
            }

            if (_stillHaveBlendTree)
            {
                newRoot = "/" + _newRoot;
                newBlendTree = _newBlendTree;
            }
            else
            {
                newRoot = _newRoot;
                newBlendTree = blendtree;
            }
            return _stillHaveBlendTree;
        }
        static string[] DigBlendTree(BlendTree blendtree, string root)
        {
            List<string> _list = new List<string>();
            if (blendtree.children.Length > 0)
            {
                for (int b = 0; b < blendtree.children.Length; b++)
                {
                    if (blendtree.children[b].motion != null)
                    {
                        Motion cm = blendtree.children[b].motion;
                        if (cm.GetType() == typeof(BlendTree))
                        {
                            ////This make BlendTree itself won't count as State
                            //_list.Add(root + "/" + ((BlendTree)cm).name);
                        }
                        else
                        {
                            _list.Add(root + "/" + blendtree.children[b].motion.name + "@" + b);
                        }
                    }
                    else
                    {
                        _list.Add(root + "/" + "None (Motion)@" + b);
                    }
                }
            }
            return _list.ToArray();
        }

        public static string GetDefaultState(UnityEditor.Animations.AnimatorController mecanim)
        {
            return mecanim.layers[0].stateMachine.defaultState.name;
        }

        public static string GetAnimatorLayerDefaultStateByIndex(UnityEditor.Animations.AnimatorController animator, int index)
        {
            return animator.layers[index].stateMachine.defaultState.name;
        }

        public static string GetAnimatorLayerDefaultStateByName(UnityEditor.Animations.AnimatorController animator, string name)
        {
            for (int i = 0; i < animator.layers.Length; i++)
            {
                if (animator.layers[i].name == name)
                    return animator.layers[i].stateMachine.defaultState.name;
            }
            return null;
        }

        public static void InitCombo(SerializedProperty prop, UnityEditor.Animations.AnimatorController animator, string signature)
        {
            prop.FindPropertyRelative("signature").stringValue = signature;
            prop.FindPropertyRelative("stateName").stringValue = AnimatorUtility.GetSignatureResult(animator, signature).name;
            prop.FindPropertyRelative("layerName").stringValue = signature.Split('/')[0];
            prop.FindPropertyRelative("defaultState").stringValue = AnimatorUtility.GetAnimatorLayerDefaultStateByName(animator, prop.FindPropertyRelative("layerName").stringValue);
        }

        public static void InitAnimatorStateSeeker(SerializedProperty prop, UnityEditor.Animations.AnimatorController animator, string signature)
        {
            prop.FindPropertyRelative("signature").stringValue = signature;
            prop.FindPropertyRelative("stateName").stringValue = AnimatorUtility.GetSignatureResult(animator, signature).name;
            prop.FindPropertyRelative("layerName").stringValue = signature.Split('/')[0];
        }

        public static int GetAnimatorLayerIndexBySignature(UnityEditor.Animations.AnimatorController animator, string signature)
        {
            string[] split = signature.Split('/');
            string layer = split[0];
            for (int l = 0; l < animator.layers.Length; l++)
            {
                if (animator.layers[l].name == layer)
                    return l;
            }
            Debug.LogWarning("Fail to get state index by signature");
            return 0;
        }

        public static AnimatorState GetAnimatorStateByIndex(UnityEditor.Animations.AnimatorController animator, int index)
        {
            AnimatorState[] _states = GetAnimatorStates(animator);
            if (_states[index] != null)
                return _states[index];
            else
                return null;
        }

        public static AnimatorState GetAnimatorState(UnityEditor.Animations.AnimatorController animator, string name)
        {
            for (int l = 0; l < animator.layers.Length; l++)
            {
                for (int s = 0; s < animator.layers[l].stateMachine.states.Length; s++)
                {
                    if (animator.layers[l].stateMachine.states[s].state.name == name)
                        return animator.layers[l].stateMachine.states[s].state;
                }
            }
            return null;
        }

        public static int GetLayerIndexByName(UnityEditor.Animations.AnimatorController animator, string name)
        {
            List<string> _layer = new List<string>();
            for (int l = 0; l < animator.layers.Length; l++)
            {
                if (animator.layers[l].name == name)
                    return l;
            }
            Debug.LogError("Can't find '" + name + "' layer");
            return 0;
        }

        public static AnimatorState[] GetAnimatorStates(UnityEditor.Animations.AnimatorController animator)
        {
            List<AnimatorState> _animatorState = new List<AnimatorState>();
            for (int l = 0; l < animator.layers.Length; l++)
            {
                for (int s = 0; s < animator.layers[l].stateMachine.states.Length; s++)
                {
                    if (animator.layers[l].stateMachine.states[s].state)
                        _animatorState.Add(animator.layers[l].stateMachine.states[s].state);
                }
            }
            return _animatorState.ToArray();
        }

        public static AnimatorState[] GetAnimatorStates(UnityEditor.Animations.AnimatorController animator, int index)
        {
            AnimatorStateMachine _stateMachine = animator.layers[index].stateMachine;
            ChildAnimatorState[] _childAnimatorState = _stateMachine.states;
            AnimatorState[] _animatorState = new AnimatorState[_childAnimatorState.Length];
            for (int i = 0; i < _childAnimatorState.Length; i++)
            {
                _animatorState[i] = _childAnimatorState[i].state;
            }
            return _animatorState;
        }

        public static AnimatorState[] GetAnimatorStates(Animator animator, int index)
        {
            UnityEditor.Animations.AnimatorController _controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            return GetAnimatorStates(_controller, index);
        }
    }
}