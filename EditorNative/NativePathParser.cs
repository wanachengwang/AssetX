using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
namespace AssetX
{
    public class NativePathParser
    {
        static string _rootPath { get { return AssetBundleConfig.SrcAssetsDir; } }

        List<string> _assetTags = new List<string>();
        Dictionary<string, string> _assetMap = new Dictionary<string, string>();

        public void Init()
        {
            _assetTags.Clear();
            _assetMap.Clear();
            if (System.IO.Directory.Exists(System.IO.Directory.GetCurrentDirectory() + _rootPath))
                Debug.LogError(System.IO.Directory.GetCurrentDirectory() + _rootPath);

            string[] guids = UnityEditor.AssetDatabase.FindAssets("*", new string[] { _rootPath });
            List<string> unique_guids = new List<string>();
            foreach (var guid in guids)
            {
                if (!unique_guids.Contains(guid))
                {
                    unique_guids.Add(guid);
                }
            }

            foreach (var guid in unique_guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid).ToLower();
                if (!System.IO.File.Exists(assetPath))
                {
                    continue;
                }
                try
                {
                    string tag = assetPath.Replace(_rootPath + "/", "").Replace(System.IO.Path.GetExtension(assetPath), "");
                    tag= tag.Substring(tag.LastIndexOf("/") + 1);
                    if (!_assetMap.ContainsKey(tag))
                    {
                        _assetTags.Add(tag);
                        _assetMap[tag] = assetPath;
                    }
                    else
                    {
                        Debug.LogError("repeated:" + tag);
                    }
                }
                catch (System.Exception)
                {
                    Debug.Log(assetPath);
                }
            }
        }

        public bool GetAssetPath(string assetName, out string assetPath)
        {
            string assetTag = System.IO.Path.GetFileName(assetName);
            return _assetMap.TryGetValue(assetTag, out assetPath);
        }

        public string[] GetFiles(string path)
        {
            List<string> subFiles = new List<string>();
            for (int i = 0; i < _assetTags.Count; i++)
            {
                if (_assetTags[i].Contains(path))
                {
                    subFiles.Add(_assetTags[i]);
                }
            }
            return subFiles.ToArray();
        }
    }
}
#endif