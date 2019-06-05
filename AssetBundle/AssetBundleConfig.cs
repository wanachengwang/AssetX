using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using LitJson;

public class AssetBundleConfig
{
    //Constant, not changable 
    public static string OriginAssetsRoot { get { return Application.dataPath; } } 
    public static string AssetBundlesRoot { get { return Application.streamingAssetsPath; } }
    public static string AssetBundlesManifest { get { return new DirectoryInfo(AssetBundlesRoot).Name; } }
    public static string AssetBundlesManifestPathName { get { return Path.Combine(AssetBundlesRoot, new DirectoryInfo(AssetBundlesRoot).Name).Replace("\\", "/").ToLower(); } }

    public static string AbDepsManifest { get { return "deps.manifest"; } }
    public static string AssetBundlesDepsDirName { get { return "_deps_"; } }

    public static string ManifestName { get { return "assetsmanifest"; } }
    public static string ManifestRelativePath { get { return ManifestName; } }  // or add a folder
    public static string StreamingAssetsPath { get { return Application.streamingAssetsPath; } }
#if UNITY_EDITOR
    public static string PersistentDataPath { get { return System.IO.Path.Combine(Application.dataPath, "PersistentData").Replace("\\", "/"); } }
#else
    public static string PersistentDataPath { get { return Application.persistentDataPath; } }
#endif
    public static string PersistentDataPathTemp { get { return System.IO.Path.Combine(PersistentDataPath, "temp").Replace("\\", "/"); } }
    public static string ServerAssetsPath
    {
        get
        {
#if UNITY_ANDROID
            return StreamingAssetsPath;
#elif UNITY_IOS
			return StreamingAssetsPath;
#else
            return "file:///" + StreamingAssetsPath;
#endif
        }
    }

    //Customizable config in editor
#if UNITY_EDITOR
    public class PathFilter
    {
        public bool _bValid = true;
        public string _tag;
        public string _srcDir;
        public string _dstDir;

        public string GetPrjRelativeSrcDir()
        {
            return "Assets/" + _srcDir;
        }
        public string GetPrjRelativeDstDir()
        {
            return "Assets/StreamingAssets/" + _dstDir;
        }
    }
    public class ConfigData
    {
        const string DefSrcAssetsDir = "prefabs/assetbundles";
        const string DefDstAssetsDir = "assetbundles";
        const string DefSrcScenesDir = "prefabs/scenes";
        const string DefDstScenesDir = "scenes";

        public PathFilter[] _pathFilters = new PathFilter[2]{
            new PathFilter(){ _bValid = true,  _tag = "Assets", _srcDir = DefSrcAssetsDir, _dstDir = DefDstAssetsDir},
            new PathFilter(){ _bValid = false, _tag = "Scenes", _srcDir = DefSrcScenesDir, _dstDir = DefDstScenesDir},
        };
        public UnityEditor.BuildAssetBundleOptions _optWin;
        public UnityEditor.BuildAssetBundleOptions _optAnd;
        public UnityEditor.BuildAssetBundleOptions _optIOS;
    }

    public static PathFilter[] Filters { get { return Data._pathFilters; } }
    public static string SrcAssetsDir { get { return Data._pathFilters[0].GetPrjRelativeSrcDir(); } }
    public static string DstAssetsDir { get { return Data._pathFilters[0].GetPrjRelativeDstDir(); } }
    public static string AssetBundlesDepsDir { get { return string.IsNullOrEmpty(Data._pathFilters[0]._dstDir) ? 
                AssetBundlesDepsDirName :
                (Data._pathFilters[0]._dstDir + "/" + AssetBundlesDepsDirName); } }
    public static string PrjRelativeDepsPath { get { return Path.Combine(DstAssetsDir, AbDepsManifest); } }

    static string Key2AssetBundleConfig { get { return Application.dataPath + "_AssetBundleConfig"; } }
    static ConfigData _cfgData;
    static ConfigData Data
    {
        get
        {
            if (_cfgData == null)
                LoadConfig();
            return _cfgData;
        }
    }
    static void LoadConfig()
    {
        string jsonConfig = PlayerPrefs.GetString(Key2AssetBundleConfig, string.Empty).Trim();
        if (!string.IsNullOrEmpty(jsonConfig))
        {
            _cfgData = JsonMapper.ToObject<ConfigData>(jsonConfig);
            return;
        }
        _cfgData = new ConfigData();
    }
    public static void SaveConfig()
    {
        string jsonConfig = JsonMapper.ToJson(Data);
        PlayerPrefs.SetString(Key2AssetBundleConfig, jsonConfig);
    }
#endif
}