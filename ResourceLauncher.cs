using AssetX;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class ResourceLauncher : MonoBehaviour
{
    public Text progressLabel;
    public Text statelabel;
    public Image progressTexture;
    AssetX.IProgress progress;
    void Start()
    {
        Application.targetFrameRate = -1;
        progress = AssetX.AssetManager.Instance.PrepareSystem(OnLauncherFinished);
    }

    void Update()
    {
        if (progress != null)
        {
            progressTexture.fillAmount = progress.Value;
            progressLabel.text = (progress.Value * 100).ToString("F2") + "%";
            string state = "";
            if (progress.CurrentState == ProgressState.Init)
            {
                //state = "检查更新...";
                state = "";
            }
            else if (progress.CurrentState == ProgressState.Load)
            {
                state = progress.CurrentStep + "/" + progress.AllStepCount + " Load local resource...";
                //state = "首次加载,不消耗流量.";
                //state = Localization.Get("1000002");
            }
            else if (progress.CurrentState == ProgressState.Download)
            {
                state = progress.CurrentStep + "/" + progress.AllStepCount + " Download resource...";
                //state = progress.CurrentStep + "/" + progress.AllStepCount + " " + Localization.Get("1000003");
            }
            else if (progress.CurrentState == ProgressState.Decode)
            {
                state = progress.CurrentStep + "/" + progress.AllStepCount + " unziping resource...(" + GetSize(progress.CurrentSize) + "/" + GetSize(progress.AllSize) + ")";
                //state = "首次加载,不消耗流量.";
                //state = Localization.Get("1000002");
            }
            else if (progress.CurrentState == ProgressState.Copy)
            {
                state = progress.CurrentStep + "/" + progress.AllStepCount + "Copy Resource...";
                //state = "资源准备中,请等待...";
                //state = Localization.Get("1000004");
            }
            else if (progress.CurrentState == ProgressState.Done)
            {
                state = "Loading Complete,Please wait...";
                //state = Localization.Get("1000005");
                progressTexture.fillAmount = 1f;
                progressLabel.text = "";
            }
            if (statelabel != null) statelabel.text = state;
        }
    }

    void OnLauncherFinished()
    {


        SceneManager.LoadSceneAsync("Main");

    }
    string GetSize(long size)
    {
        if (size > 1024)
        {
            float size_kb = size / 1024f;
            if (size_kb < 1024)
            {
                return size_kb.ToString("F2") + "KB";
            }
            else
            {
                float size_mb = size_kb / 1024f;
                return size_mb.ToString("F2") + "MB";
            }
        }
        else
        {
            return size + "B";
        }
    }

}
