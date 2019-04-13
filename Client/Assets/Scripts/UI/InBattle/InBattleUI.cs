using SCM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InBattleUI : UIBase {

    private bool InReplay;
    private string Winner;
    private bool IsPVP;

    protected override void StartOnlyOneTime()
    {
        Room4Client.OnBattleBegin += OnBattleBegin;
        Room4Client.OnBattleEnd += OnBattleEnd;
    }

    private void OnBattleBegin(Room4Client r, bool inReplay)
    {
        (ShowChildUI("ReplayUI", inReplay) as ReplayUI).ResetAll(inReplay);
        (ShowChildUI("StatisticsUI", true) as StatisticsUI).ResetAll();
        ShowChildUI("UnitsUI", true);
        ShowChildUI("BattleResultUI", false);

//        StaticSoundMgr.Instance.PlayBackgroundSound("BGM");
    }

    private void OnBattleEnd(Room r, string winner, bool inReplay)
    {
        var isPVP = ((Room4Client)r).IsPVP && !inReplay;
        GameCore.Instance.MeInfo.PVPCount += isPVP ? 1 : 0;

        var pvpCnt = GameCore.Instance.MeInfo.PVPCount;
        var isShowAutoAds = (pvpCnt == 0 ? false : pvpCnt % 10 == 0) && !inReplay;

        var ui = ShowChildUI("BattleResultUI", true) as BattleResultUI;

        ui.AdsBtn.SetActive(false);

        if (inReplay)
            ui.Replay();
        else if (winner == null)
            ui.Draw();
        else
        {
            if (winner == GameCore.Instance.MeID)
            {
                ui.Win();
                StaticSoundMgr.Instance.PlaySound("Win");

                // PVP胜场加1积分
                if (isPVP)
                {
                    GameCore.Instance.MeInfo.Integration += 1;
                    UserManager.SyncIntegration2Server();

                    AddTip("积分+1");
                    ui.MMUI.ShowUserInfo();
                }
            }
            else
            {
                ui.Lose();
                StaticSoundMgr.Instance.PlaySound("Lose");

                // PVP失败询问是否看有奖广告，完后加1积分
                if (isPVP && !isShowAutoAds)
                {
                    // 显示有奖广告链接
                    ui.AdsBtn.SetActive(true);
                }
            }
        }

        InReplay = inReplay;
        Winner = winner;
        IsPVP = isPVP;

        if (isShowAutoAds)
            ShowAutoAd();
 
//        StaticSoundMgr.Instance.StopBackgroundSound();
    }

    public void ShowAutoAd()
    {
        if (!UnityAdsHelper.isSupported)
            return;
        if (!UnityAdsHelper.isInitialized)
            return;
        if (UnityAdsHelper.isShowing)
            return;
        
        UnityAdsHelper.ShowAd(null, null);
    }

    public void OnReplay()
    {
        var br = GameCore.Instance.Get<BattleReplayer>();
        br.Start();
    }

    public void HideAllChildren()
    {
        ShowChildUI("ReplayUI", false);
        ShowChildUI("StatisticsUI", false);
        ShowChildUI("UnitsUI", false);
        ShowChildUI("SelectUnitUI", false);
        ShowChildUI("BattleResultUI", false);
        ShowChildUI("BattleResultUI", false);
    }

    public void OnReturn()
    {
        UIManager.Instance.ClearScene();

        HideAllChildren();

        ShowTopUI("MainArea", true);
        ShowTopUI("MainMenu", true);

        if (!InReplay && IsPVP)
        {
            // 解锁头像
            if (Winner == GameCore.Instance.MeID)
            {
                var aType = UserManager.UnlockOneAvatarAtRandom();

                if (!string.IsNullOrEmpty(aType))
                {
                    UIManager.Instance.Tips.AddTip(string.Format("解锁新头像：{0}", aType));
                }
            }
        }
    }
}
