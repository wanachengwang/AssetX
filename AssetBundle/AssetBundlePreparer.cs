using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine.Networking;

namespace AssetX
{
    public class AssetBundlePreparer : MonoBehaviour
    {
        static AssetBundlePreparer _instance;
        static public AssetBundlePreparer Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AssetBundlePreparer", typeof(AssetBundlePreparer));
                    DontDestroyOnLoad(go);
                    _instance = go.GetComponent<AssetBundlePreparer>();
                }
                return _instance;
            }
        }

        ProgressData _progress;

        public IProgress Prepare(System.Action finishCallback, bool onlineUpdate = true)
        {
            _progress = new ProgressData();
            BeginPrepareTask(finishCallback, onlineUpdate);
            return _progress;
        }

        void BeginPrepareTask(System.Action finishCallback, bool onlineUpdate = true)
        {
            StartCoroutine(PrepareTask(finishCallback, () => {
                OnCheckResourcesFail(finishCallback, onlineUpdate);
            }, onlineUpdate));
        }

        void OnCheckResourcesFail(System.Action finishCallback, bool onlineUpdate = true)
        {
        }

        void OnDownloadResouceFail(Helpers.OnlineOperation operation)
        {
        }

        IEnumerator PrepareTask(System.Action finishCallback, System.Action loadFailCallback, bool onlineUpdate = false)
        {
            bool err = false;
            bool hasTempCopy = false;
            NetworkReachability netState = Application.internetReachability;
            bool isGameReadyToComplete = PlayerPrefs.GetInt("GameReadyToComplete", 0) == 1;
            _progress.CurrentState = ProgressState.Init;
            CustomAssetBundleManifest tempManifest = null;
            CustomAssetBundleManifest localManifest = null;
            CustomAssetBundleManifest serverManifest = null;
            #region prepare manifest file
            string scenePath = System.IO.Path.Combine(AssetBundleConfig.PersistentDataPath, "Scene.unity3d").Replace("\\", "/");
            string tempManifestPath = System.IO.Path.Combine(AssetBundleConfig.PersistentDataPathTemp, AssetBundleConfig.ManifestRelativePath).Replace("\\", "/");
            string localManifestPath = System.IO.Path.Combine(AssetBundleConfig.PersistentDataPath, AssetBundleConfig.ManifestRelativePath).Replace("\\", "/");
            string serverManifestPath = System.IO.Path.Combine(AssetBundleConfig.ServerAssetsPath, AssetBundleConfig.ManifestRelativePath).Replace("\\", "/");

            #region  获取默认的manifest,PersistentDataPath/Temp
            //temp manifest--- a copy of manifest in streamassets
            if (!File.Exists(tempManifestPath) || !isGameReadyToComplete)
            {
                Debug.Log("temp copy of manifest");
                yield return StartCoroutine(Helpers.PathUtil.CopyFileFromAppPathAsync(AssetBundleConfig.ManifestRelativePath, AssetBundleConfig.StreamingAssetsPath, AssetBundleConfig.PersistentDataPathTemp));
            }
            if (!File.Exists(tempManifestPath))
            {
                yield return null;
            }
            TextAsset tempTextAsset = LoadLocalAsset<TextAsset>(tempManifestPath, AssetBundleConfig.ManifestName);
            if (tempTextAsset != null)
            {
                Debug.Log("has temp copy");
                hasTempCopy = true;
                tempManifest = CustomAssetBundleManifest.CreateInstance(tempTextAsset.bytes);
            }
            #endregion

            #region 获取本地manifest,PersistentDataPath
            //local manifest
            if (System.IO.File.Exists(localManifestPath) && isGameReadyToComplete)
            {
                Debug.Log("have localmanifest");
                tempTextAsset = LoadLocalAsset<TextAsset>(localManifestPath, AssetBundleConfig.ManifestName);
                localManifest = CustomAssetBundleManifest.CreateInstance(tempTextAsset.bytes);
            }
            #endregion

            #region 获取资源服务器manifest,ServerPath
            //server manifest
            if (netState != NetworkReachability.NotReachable && onlineUpdate)
            {
                PlayerPrefs.SetInt("GameReadyToComplete", 0);
                yield return null;
                string sourcePath = Path.Combine(AssetBundleConfig.ServerAssetsPath, AssetBundleConfig.ManifestRelativePath).Replace("\\", "/");
                using (UnityWebRequest www = UnityWebRequest.Get(sourcePath))
                {
                    //www.timeout = 30;//设置超时，www.SendWebRequest()连接超时会返回，且isNetworkError为true
                    yield return www.SendWebRequest();
                    if (www.isNetworkError)
                    {
                        Debug.Log("Download Error:" + www.error);
                        err = true;
                    }
                    else //if (www.responseCode == 200)//200表示接受成功
                    {
                        Debug.Log("Download no error");
                        byte[] bytes = www.downloadHandler.data;
                        Helpers.PathUtil.CopyBufferToFile(bytes, localManifestPath);
                        var ab = AssetBundle.LoadFromMemory(bytes);
                        var tx = ab.LoadAsset<TextAsset>(AssetBundleConfig.ManifestName);
                        serverManifest = CustomAssetBundleManifest.CreateInstance(tx.bytes);
                        ab.Unload(true);
                    }
                }
            }
            #endregion

            #region 非网络状态检查本地manifest
            if (err || netState == NetworkReachability.NotReachable || !onlineUpdate)
            {
                err = false;
                if (localManifest != null && isGameReadyToComplete) //包含完整本地资源
                {
                }
                else if (localManifest == null && hasTempCopy)      //包含默认资源
                {
                    Debug.Log("包含默认资源");
                    Helpers.PathUtil.CopyFile(tempManifestPath, localManifestPath);
                }
                else                                                //没有任何资源
                {                    
                    //throw new Exception("not contain default resources");
                    err = true;
                }
            }
            #endregion

            #endregion
            if (err)
            {
                loadFailCallback();
            }
            else
            {
                bool IsCompressed = true;
                List<string> needLoadFromServer = new List<string>();
                List<string> needLoadFromDefault = new List<string>();
                if (serverManifest != null)
                {
                    IsCompressed = serverManifest.IsCompressed;
                    if (!isGameReadyToComplete && localManifest != null)//上次更新资源没有完全准备好
                        localManifest.AssetMap.Clear();
                    List<string> loadFromServer = CustomAssetBundleManifest.GetDifference(localManifest, serverManifest);
                    List<string> canLoadFromDefault = CustomAssetBundleManifest.GetSame(tempManifest, serverManifest);
                    foreach (var item in loadFromServer)
                    {
                        if (canLoadFromDefault.Contains(item))
                        {
                            needLoadFromDefault.Add(item);
                        }
                        else
                        {
                            Debug.Log("needloadfromserver:" + item);
                            needLoadFromServer.Add(item);
                        }
                    }
                }
                else if (localManifest == null && hasTempCopy)
                {
                    IsCompressed = tempManifest.IsCompressed;
                    List<string> loadFromDefault = new List<string>();
                    using (var itr = tempManifest.AssetMap.GetEnumerator())
                    {
                        while (itr.MoveNext())
                        {
                            loadFromDefault.Add(itr.Current.Key);
                        }
                    }
                    needLoadFromDefault = loadFromDefault;
                }
                else if (localManifest != null && hasTempCopy)
                {
                    IsCompressed = tempManifest.IsCompressed;
                    needLoadFromDefault = GetLocalWithDefaultDifference(localManifest, tempManifest);
                    Debug.Log("PathHelper.CopyFile");
                    Helpers.PathUtil.CopyFile(tempManifestPath, localManifestPath);
                }
                _progress.CurrentStep = 0;
                _progress.AllStepCount = needLoadFromDefault.Count + needLoadFromServer.Count;
                Debug.Log("allstepcount:" + _progress.AllStepCount + needLoadFromDefault.Count + needLoadFromServer.Count);
                if (serverManifest != null)
                {
                    yield return StartCoroutine(DownloadAssetBundle(needLoadFromServer, AssetBundleConfig.ServerAssetsPath, AssetBundleConfig.PersistentDataPath, IsCompressed));
                }
                yield return StartCoroutine(CopyAssetBundle(needLoadFromDefault, AssetBundleConfig.PersistentDataPath, IsCompressed));

                _progress.CurrentState = ProgressState.Done;
                Helpers.PathUtil.DeleteDir(AssetBundleConfig.PersistentDataPathTemp);
                PlayerPrefs.SetInt("GameReadyToComplete", 1);
                yield return null;

                if (finishCallback != null)
                { finishCallback(); }
                Destroy(gameObject);
            }
        }

        List<string> GetLocalWithDefaultDifference(CustomAssetBundleManifest localManifest, CustomAssetBundleManifest defaultManifest)
        {
            List<string> difference = new List<string>();
            List<string> loadFromLocal = CustomAssetBundleManifest.GetDifference(localManifest, defaultManifest);
            foreach (var item in loadFromLocal)
            {
                difference.Add(item);
            }
            return difference;
        }

        IEnumerator CopyAssetBundle(List<string> paths, string destDir, bool isCompressed)
        {
            List<Helpers.LocalOperation> operations = new List<Helpers.LocalOperation>();
            using (var itr = paths.GetEnumerator())
            {
                while (itr.MoveNext())
                {
                    operations.Add(new Helpers.LocalOperation(itr.Current, destDir, isCompressed, this._progress));
                }
            }
            using (var itr = operations.GetEnumerator())
            {
                while (itr.MoveNext())
                {
                    yield return StartCoroutine(itr.Current);
                    _progress.CurrentState = ProgressState.Load;
                    _progress.CurrentStep++;
                    yield return StartCoroutine(itr.Current.Excute());
                }
            }
            operations.Clear();
            System.GC.Collect();
        }

        IEnumerator DownloadAssetBundle(List<string> paths, string serverDir, string destDir, bool isCompressed)
        {
            List<Helpers.OnlineOperation> operations = new List<Helpers.OnlineOperation>();
            using (var itr = paths.GetEnumerator())
            {
                while (itr.MoveNext())
                {
                    string sourcePath = Path.Combine(serverDir, itr.Current).Replace("\\", "/");
                    string destPath = Path.Combine(destDir, itr.Current).Replace("\\", "/");
                    operations.Add(new Helpers.OnlineOperation(sourcePath, destPath, isCompressed, this._progress, OnDownloadResouceFail));
                }
            }
            using (var itr = operations.GetEnumerator())
            {
                while (itr.MoveNext())
                {
                    yield return StartCoroutine(itr.Current);
                    _progress.CurrentState = ProgressState.Download;
                    _progress.CurrentStep++;
                    yield return StartCoroutine(itr.Current.Excute());
                }
            }
            operations.Clear();
            System.GC.Collect();
        }

        IEnumerator DownloadSceneFile(List<string> paths, string serverDir, string destDir, bool isCompressed)
        {
            List<Helpers.OnlineOperation> operations = new List<Helpers.OnlineOperation>();
            using (var itr = paths.GetEnumerator())
            {
                while (itr.MoveNext())
                {
                    string sourcePath = Path.Combine(serverDir, itr.Current).Replace("\\", "/");
                    string destPath = Path.Combine(destDir, itr.Current).Replace("\\", "/");
                    operations.Add(new Helpers.OnlineOperation(sourcePath, destPath, isCompressed, this._progress, OnDownloadResouceFail));
                }
            }
            using (var itr = operations.GetEnumerator())
            {
                while (itr.MoveNext())
                {
                    yield return StartCoroutine(itr.Current);
                    _progress.CurrentState = ProgressState.Download;
                    _progress.CurrentStep++;
                    yield return StartCoroutine(itr.Current.Excute());
                }
            }
            operations.Clear();
            System.GC.Collect();
        }

        IEnumerator DownloadSceneFile(string paths, string serverDir, string destDir, bool isCompressed)
        {
            List<Helpers.OnlineOperation> operations = new List<Helpers.OnlineOperation>();

            string sourcePath = Path.Combine(serverDir, paths).Replace("\\", "/");
            string destPath = Path.Combine(destDir, paths).Replace("\\", "/");
            operations.Add(new Helpers.OnlineOperation(sourcePath, destPath, isCompressed, this._progress, OnDownloadResouceFail));

            using (var itr = operations.GetEnumerator())
            {
                while (itr.MoveNext())
                {
                    yield return StartCoroutine(itr.Current);
                    _progress.CurrentState = ProgressState.Download;
                    _progress.CurrentStep++;
                    yield return StartCoroutine(itr.Current.Excute());
                }
            }
            operations.Clear();
            System.GC.Collect();
        }

        AssetType LoadLocalAsset<AssetType>(string path, string name) where AssetType : UnityEngine.Object
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(path);
            if (assetBundle != null)
            {
                AssetType tmp = assetBundle.LoadAsset<AssetType>(name);
                assetBundle.Unload(false);
                return tmp;
            }
            return null;
        }
    }

    public class ProgressData : IProgress
    {
        public ProgressState CurrentState { get; set; }
        public float Value { get; set; }
        public int AllStepCount { get; set; }
        public int CurrentStep { get; set; }
        public long AllSize { get; set; }
        public long CurrentSize { get; set; }

        public void SetProgress(long inSize, long outSize)
        {
            AllSize = outSize;
            CurrentSize = inSize;
            Value = inSize / (float)outSize;
        }
    }
}