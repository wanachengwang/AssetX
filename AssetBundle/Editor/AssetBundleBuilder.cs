#define LEAVE_CFG_FILE

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System;

namespace AssetX
{
    public class AssetBundleBuilder
    {
        public static void ResetAssetBundleNames()
        {
            string[] assetNames = AssetDatabase.GetAllAssetBundleNames();
            foreach (var item in assetNames)
            {
                AssetDatabase.RemoveAssetBundleName(item, true);
            }
        }

        public static void AutoSetAssetBundleNames()
        {
            ResetAssetBundleNames();
            foreach(var filter in AssetBundleConfig.Filters)
            {
                if(filter._bValid)
                {
                    SetAssetBundleNames(filter.GetPrjRelativeSrcDir().ToLower(), String.Empty, filter._dstDir);
                }
            }
            DependencyManifest.LoadData();
            Debug.Log("Auto set AssetBundleName done!");
        }

        /*
        * All paramter string should be lower case
        */
        static void SetAssetBundleNames(string prjRelSrcPath, string relativePath, string dstDir, bool bSubfile = false)
        {
            string path = Path.Combine(prjRelSrcPath, relativePath).Replace("\\", "/").ToLower();
            if (File.Exists(path))
            {
                //Note: files in root would fail in replacing and assetbundleName would be file name with ext.
                string assetbundleName = relativePath.Replace("/" + Path.GetFileName(relativePath), bSubfile ? "_subfile" : "");
                if(!string.IsNullOrEmpty(dstDir))
                {
                    assetbundleName = dstDir + "/" + assetbundleName;
                }
                AssetImporter.GetAtPath(path).assetBundleName = assetbundleName;
            }
            else if (Directory.Exists(path))
            {
                string[] dirs = Directory.GetDirectories(path);
                string[] files = Directory.GetFiles(path);
                for (int i = 0; i < dirs.Length; i++)
                {
                    string dirPath = dirs[i].Replace("\\", "/").ToLower();
                    string newRelativePath = dirPath.Replace(prjRelSrcPath + "/", "");
                    SetAssetBundleNames(prjRelSrcPath, newRelativePath, dstDir);
                }
                for (int i = 0; i < files.Length; i++)
                {
                    string filePath = files[i].Replace("\\", "/").ToLower();
                    if (!filePath.EndsWith(".meta") && !filePath.EndsWith(AssetBundleConfig.AbDepsManifest))
                    {
                        string newRelativePath = filePath.Replace(prjRelSrcPath + "/", "");
                        SetAssetBundleNames(prjRelSrcPath, newRelativePath, dstDir, dirs.Length > 0);
                    }
                }
            }
        }

        public static void BuildAssetBundle(BuildTarget target, bool isCompress = false)
        {
            foreach (var filter in AssetBundleConfig.Filters)
            {
                if (filter._bValid)
                {
                    string dstDir = filter.GetPrjRelativeDstDir();
                    if (Directory.Exists(dstDir))
                        Directory.Delete(dstDir, true);
                }
            }

            BuildPipeline.BuildAssetBundles(AssetBundleConfig.AssetBundlesRoot, BuildAssetBundleOptions.UncompressedAssetBundle, target);
            GenerateAssetBundle(target, isCompress);
#if !LEAVE_CFG_FILE
            DeleteConfigFile();
#endif
            AssetDatabase.ImportAsset(AssetBundleConfig.DstAssetsDir);
            AssetDatabase.Refresh();
        }

        public static void GenerateAssetBundle(BuildTarget target, bool isCompress)
        {
            // Load manifest txt or assetbundle
            string manifestPathName = Path.Combine(AssetBundleConfig.AssetBundlesRoot, new DirectoryInfo(AssetBundleConfig.AssetBundlesRoot).Name).Replace("\\", "/").ToLower();
            AssetBundle bundle = AssetBundle.LoadFromFile(manifestPathName);
            AssetBundleManifest u3dManifest = bundle.LoadAsset("AssetBundleManifest") as AssetBundleManifest;
            List<AssetBundleInfo> assetBundles = new List<AssetBundleInfo>();
            if (u3dManifest != null)
            {
                foreach (var item in u3dManifest.GetAllAssetBundles())
                {
                    string itemPath = Path.Combine(AssetBundleConfig.AssetBundlesRoot, item).Replace("\\", "/").ToLower();
                    AssetBundleInfo info = new AssetBundleInfo();
                    info.relativePath = item;
                    info.dependencies = u3dManifest.GetAllDependencies(item);
                    info.MD5 = GetMD5HashFromFile(itemPath);
                    AssetBundle subAssetBundle = AssetBundle.LoadFromFile(itemPath);
                    if (subAssetBundle == null)
                        throw new System.Exception(item + "is not existed! CreateManifest()");
                    info.allAssetsName = subAssetBundle.GetAllAssetNames();
                    SetAssetNames(info.allAssetsName); // This would be used as key to query assetbundle file.
                    assetBundles.Add(info);
                    subAssetBundle.Unload(true);                   
                }
            }
            bundle.Unload(true);

            CheckRepeatedAssetName(assetBundles);

            //create my manifest
            string tempPath = "Assets/assetsmanifest.json";
            CustomAssetBundleManifest.JsonSerialize(assetBundles, tempPath, isCompress);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            //AssetImporter.GetAtPath(tempPath).assetBundleName = AssetBundleConfig.ManifestRelativePath;
            AssetBundleBuild[] buildManifests = new AssetBundleBuild[1];
            buildManifests[0].assetNames = new string[] { tempPath };
            buildManifests[0].assetBundleName = AssetBundleConfig.ManifestRelativePath;
            BuildPipeline.BuildAssetBundles(AssetBundleConfig.AssetBundlesRoot, buildManifests, BuildAssetBundleOptions.UncompressedAssetBundle, target);
#if !LEAVE_CFG_FILE
            FileUtil.DeleteFileOrDirectory(AssetBundleConfig.DstDir + "AssetBundle");
            FileUtil.DeleteFileOrDirectory(tempPath);
#endif
        }

        static void SetAssetNames(string[] assetNames)
        {
            if (assetNames != null)
            {
                for (int i = 0; i < assetNames.Length; i++)
                {
                    assetNames[i] = Path.GetFileNameWithoutExtension(assetNames[i]);
                }
            }
        }

        static void CheckRepeatedAssetName(List<AssetBundleInfo> assetBundles)
        {
            Dictionary<string, string> assetMap = new Dictionary<string, string>();
            foreach(var abinfo in assetBundles)
            {
                foreach(var assetName in abinfo.allAssetsName)
                {
                    if(assetMap.ContainsKey(assetName))
                    {
                        Debug.LogError("Error: Repeated asset name detected:" + assetName + "--" + assetMap[assetName] + "--" + abinfo.relativePath);
                        throw new Exception("Repeated asset name detected!");
                    }
                    assetMap[assetName] = abinfo.relativePath;
                }
            }
        }

        static void DeleteConfigFile()
        {
            Helpers.PathUtil.DeleteAllFiles(AssetBundleConfig.DstAssetsDir, "*.manifest");
            Helpers.PathUtil.DeleteAllFiles(AssetBundleConfig.DstAssetsDir, "*.meta");
        }

        static string GetMD5HashFromFile(string file)
        {
            try
            {
                FileStream sf = new FileStream(file, FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                Byte[] retVal = md5.ComputeHash(sf);
                sf.Close();
                StringBuilder sb = new StringBuilder();
                for (int index = 0; index < retVal.Length; ++index)
                {
                    sb.Append(retVal[index].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail, error:" + ex.Message);
            }
        }

        static List<string> GetAllAssetPaths(string path, System.Type type)
        {
            List<string> assetFiles = new List<string>();
            if (System.IO.File.Exists(path))
            {
                assetFiles.Add(path.ToLower());
            }
            else if (System.IO.Directory.Exists(path))
            {
                string[] guids = AssetDatabase.FindAssets("t:" + type.Name, new string[] { path });
                List<string> unique_guids = new List<string>();
                for (int i = 0; i < guids.Length; i++)
                {
                    if (!unique_guids.Contains(guids[i]))
                    {
                        unique_guids.Add(guids[i]);
                        assetFiles.Add(AssetDatabase.GUIDToAssetPath(guids[i]).ToLower());
                    }
                }
            }
            else
            {
                Debug.LogError(path);
            }
            return assetFiles;
        }
#region Generate asset   
        static void GenerateMaterial(string path)
        {
            UnityEngine.Object[] mats = GetTargetAssets(path, typeof(Material));
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i] as Material;
                if (mat.shader != null)
                {
                    string shaderName = mat.shader.name;
                    int index = shaderName.LastIndexOf("/");
                    if (index >= 0 && index < shaderName.Length)
                    {
                        string shaderGroup = shaderName.Substring(0, index);
                        shaderGroup = shaderGroup.Replace("/", "_").Replace(" ", "_");
                        AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mat)).assetBundleName = "builtin_shader/" + shaderGroup;
                    }
                    else
                    {
                        AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mat)).assetBundleName = "builtin_shader/" + shaderName;
                    }
                }
                else
                {
                    AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mat)).assetBundleName = "builtin_shader/shader_default";
                }
            }
        }
        static void GenerateAsset(string path, string assetName, System.Type type)
        {
            UnityEngine.Object[] ts = GetTargetAssets(path, type);
            for (int index = 0; index < ts.Length; index++)
            {
                var t = ts[index];
                try
                {
                    AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)).assetBundleName = assetName;
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e + path + t);
                }
            }
        }
        static UnityEngine.Object[] GetTargetAssets(string path, System.Type type)
        {
            List<UnityEngine.Object> targets = new List<UnityEngine.Object>();
            if (File.Exists(path))
            {
                var asset = AssetDatabase.LoadAssetAtPath(path, type);
                if (asset != null)
                    targets.Add(asset);
            }
            else if (Directory.Exists(path))
            {
                string[] guids = AssetDatabase.FindAssets("t:" + type.Name, new string[] { path });
                List<string> unique_guids = new List<string>();
                foreach (var guid in guids)
                {
                    if (!unique_guids.Contains(guid))
                    {
                        unique_guids.Add(guid);
                    }
                }

                List<string> assets = new List<string>();
                foreach (var guid in unique_guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!System.IO.File.Exists(assetPath))
                        continue;
                    assets.Add(assetPath);
                }

                foreach (var file in assets)
                {
                    var t = AssetDatabase.LoadAssetAtPath(file, type);
                    if (t != null)
                        targets.Add(t);
                }
            }
            else
            {
                Debug.LogWarning("\"" + path + "\"" + " is not a file path or directory!");
            }
            return targets.ToArray();
        }
#endregion
    }
}
