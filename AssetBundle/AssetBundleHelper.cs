using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace AssetX.Helpers
{
    public class PathUtil
    {
        public static void PreparePath(string path)
        {
            if (File.Exists(path))
            {
                Debug.Log("File.Delete" + path);
                File.Delete(path);
            }
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Debug.Log(" Directory.CreateDirectory" + directory);
                Directory.CreateDirectory(directory);
            }
        }

        public static void DeleteDir(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static void DeleteDiff(List<string> lh, List<string> rh)
        {
            List<string> diff = new List<string>();
            foreach (var path in rh)
            {
                if (!lh.Contains(path))
                    diff.Add(path);
            }
            foreach (var path in diff)
            {
                DeleteDir(path);
            }
        }

        public static void DeleteAllFiles(string dir, string searchPattern)
        {
            var directories = Directory.GetDirectories(dir);
            if (directories != null)
            {
                for (int i = 0; i < directories.Length; i++)
                {
                    DeleteAllFiles(directories[i], searchPattern);
                }
            }
            var files = Directory.GetFiles(dir, searchPattern);
            if (files != null)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    File.Delete(files[i]);
                }
            }
        }

        public static void CopyBufferToFile(byte[] bytes, string outFile)
        {
            PreparePath(outFile);
            System.IO.File.WriteAllBytes(outFile, bytes);
        }
        public static void CopyStringToFile(string context, string outFile)
        {
            PreparePath(outFile);
            System.IO.File.WriteAllText(outFile, context);
        }
        public static IEnumerator CopyFileFromAppPathAsync(string relativePath, string inDir, string outDir)
        {
            bool isDone = false;
            string inFile = System.IO.Path.Combine(inDir, relativePath).Replace("\\", "/");
            string outFile = System.IO.Path.Combine(outDir, relativePath).Replace("\\", "/");
            PreparePath(outFile);
            //TODO: use webrequest or just File IO
            UnityWebRequest www = UnityWebRequest.Get(inFile);
            yield return www.SendWebRequest();
            if (www.isNetworkError)
            {
                Debug.Log("Error : ======> " + www.error);
            }
            else
            {
                using (FileStream outStream = new FileStream(outFile, FileMode.Create))
                {
                    byte[] bytes = www.downloadHandler.data;
                    outStream.Write(bytes, 0, bytes.Length);

                    outStream.Flush();
                    outStream.Close();
                    outStream.Dispose();
                    Debug.Log("CopyTxt Success!" + "\n" + "Path: ======> " + outFile);
                    isDone = true;
                }
            }
            while (!isDone)
            {
                yield return null;
            }
        }

        public static void CopyFile(string inFile, string outFile, int blockLength = 2048)
        {
            using (FileStream inStream = File.OpenRead(inFile))
            {
                PreparePath(outFile);
                using (FileStream outStream = new FileStream(outFile, FileMode.Create))
                {
                    byte[] blockBuf = new byte[2048];
                    int readLength = 0;
                    while ((readLength = inStream.Read(blockBuf, 0, blockLength)) > 0)
                    {
                        outStream.Write(blockBuf, 0, readLength);
                    }
                    outStream.Flush();
                    outStream.Close();
                }
                inStream.Close();
            }
        }
    }

    public class LocalOperation : IEnumerator
    {
        ProgressData _progress;
        string _relativePath;
        string _destDir;
        bool _isCompressed;
        bool _isDone = false;
        bool _isRun = true;
        public LocalOperation(string relativePath, string destDir, bool isCompressed, ProgressData progress)
        {
            _relativePath = relativePath;
            _destDir = destDir;
            _isCompressed = isCompressed;
            _progress = progress;
        }

        public bool MoveNext()
        {
            if (!_isRun)
            {
                _progress.SetProgress(0L, 1L);
                _isRun = true;
                return true;
            }
            return false;
        }

        public IEnumerator Excute()
        {
            string tempOutDir = AssetBundleConfig.PersistentDataPathTemp;
            string inDir = AssetBundleConfig.StreamingAssetsPath;

            //Loom.RunAsync(
            //() =>
            //{
            if (_isCompressed)
            {
                lock (_progress)
                {
                    _progress.CurrentState = ProgressState.Copy;
                    _progress.SetProgress(1L, 1L);
                }
                //PathHelper.CopyFileFromAppDataPath(relativePath, inDir, tempOutDir);

                //string inPath = System.IO.Path.Combine(tempOutDir, relativePath);
                //lock (progress) { progress.CurrentState = ProgressState.Decode; }
                //string outPath = System.IO.Path.Combine(destDir, relativePath);
                ////PathHelper.PreparePath(System.IO.Path.GetDirectoryName(outPath));
                //PathHelper.PreparePath(outPath);
                //LZMAHelper.DecompressFile(inPath, outPath, progress);
                //PathHelper.DeleteFile(tempOutDir);
            }
            else
            {
                lock (_progress)
                {
                    _progress.CurrentState = ProgressState.Copy;
                    _progress.SetProgress(1L, 1L);
                }
                yield return PathUtil.CopyFileFromAppPathAsync(_relativePath, inDir, _destDir);
            }
            _isDone = true;
            _isRun = false;
            //});
            while (!_isDone)
            {
                yield return null;
            }
        }

        public object Current { get { return null; } }
        public void Reset() { }
    }

    public class OnlineOperation : IEnumerator
    {
        Action<OnlineOperation> _onDownloadFail;
        ProgressData _progress;
        string _srcPath;
        string _dstPath;
        bool _isCompressed;
        bool _isDone = false;
        bool _err = false;
        bool _invokeEvent = false;
        bool _reset = false;
        WWW _www;
        public OnlineOperation(string sourcePath, string destPath, bool isCompressed, ProgressData progress, System.Action<OnlineOperation> downloadFail)
        {
            this._srcPath = sourcePath;
            this._dstPath = destPath;
            this._isCompressed = isCompressed;
            this._progress = progress;
            this._onDownloadFail = downloadFail;
        }
        public bool MoveNext()
        {
            if (_www == null)
            {
                Debug.Log(_srcPath);
                _www = new WWW(_srcPath);
                return true;
            }
            return false;
        }

        public IEnumerator Excute()
        {
            while (!_isDone)
            {
                if (_www != null)
                {
                    if (_www.error != null) //throw new System.Exception("www err: " + www.error);
                        _err = true;
                    if (_err)
                    {
                        if (!_invokeEvent)
                        {
                            _invokeEvent = true;
                            _onDownloadFail(this);
                        }
                        while (true)
                        {
                            if (_reset)
                            {
                                _www.Dispose();
                                _www = new WWW(_srcPath);
                                _err = false;
                                _invokeEvent = false;
                                _reset = false;
                                yield return null;
                                break;
                            }
                            yield return null;
                        }
                    }
                    else
                    {
                        if (_www.isDone)
                        {
                            byte[] data = _www.bytes;
                            Loom.RunAsync(() => {
                                //if (isCompressed)
                                //{
                                //    lock (progress) { progress.CurrentState = ProgressState.Decode; }
                                //    PathHelper.PreparePath(destPath);
                                //    LZMAHelper.DecompressBufferToFile(data, destPath, progress);
                                //}
                                //else
                                //{
                                lock (_progress) { _progress.CurrentState = ProgressState.Copy; }
                                PathUtil.CopyBufferToFile(data, _dstPath);
                                //}
                                _isDone = true;
                            });
                            while (!_isDone)
                            {
                                yield return null;
                            }
                        }
                        else
                        {
                            long i = (long)(100 * _www.progress);
                            _progress.SetProgress(i, 100L);
                            yield return null;
                        }
                    }
                }
            }
        }

        public object Current { get { return null; } }
        public void Reset() { }

        public void ResetDownLoad()
        {
            _reset = true;
        }
    }
}
