using UnityEngine;
using System.Collections;
using Gears;

[System.Serializable]
public class AnimatorCopycatDatabase : ScriptableObject
{
    [SerializeField]
    public bool keepEditData;
    [SerializeField]
    public string[] preset;
    [SerializeField]
    public string[] signature;

    public void Init(string[] setting)
    {
        preset = new string[setting.Length];
        for (int i = 0; i < setting.Length; i++)
        {
            preset[i] = setting[i];
        }
    }

    public void Init(string[] setting,string[] value)
    {
        Init(setting);
        signature = new string[value.Length];
        for (int i = 0; i < value.Length; i++)
        {
            signature[i] = value[i];
        }
    }

    public string[] NameFeed()
    {
        return preset;
    }

    public string[] SignatureFeed()
    {
        return signature;
    }
}
