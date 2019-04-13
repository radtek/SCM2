using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Swift;
using SCM;

public class BattleResultUI : UIBase
{
    public Text ResultText;

    public GameObject ReplayImg;
    public GameObject DrawImg;
    public GameObject WinImg;
    public GameObject LoseImg;
    public GameObject AdsBtn;

    public MainMenuUI MMUI;

    public void Win()
    {
        ResultText.gameObject.SetActive(false);
        WinImg.SetActive(true);
        DrawImg.SetActive(false);
        LoseImg.SetActive(false);
        ReplayImg.SetActive(false);
    }

    public void Lose()
    {
        ResultText.gameObject.SetActive(false);
        LoseImg.SetActive(true);
        DrawImg.SetActive(false);
        WinImg.SetActive(false);
        ReplayImg.SetActive(false);
    }

    public void Draw()
    {
        ResultText.gameObject.SetActive(false);
        DrawImg.SetActive(true);   // 平局
        WinImg.SetActive(false);
        LoseImg.SetActive(false);
        ReplayImg.SetActive(false);
    }

    public void Replay()
    {
        ResultText.gameObject.SetActive(true);
        ResultText.text = "录像结束";    // 录像结束
        ReplayImg.SetActive(true);
        DrawImg.SetActive(false);
        WinImg.SetActive(false);
        LoseImg.SetActive(false);
    }

    public void OnClickAdsBtn()
    {
        if (!UnityAdsHelper.isSupported)
            return;
        if (!UnityAdsHelper.isInitialized)
            return;
        if (UnityAdsHelper.isShowing)
            return;

        UnityAdsHelper.ShowAd(null, () =>
        {
            GameCore.Instance.MeInfo.Integration += 1;
            UserManager.SyncIntegration2Server();

            AddTip("积分+1");
            MMUI.ShowUserInfo();
        });

        AdsBtn.gameObject.SetActive(false);
    }
}
