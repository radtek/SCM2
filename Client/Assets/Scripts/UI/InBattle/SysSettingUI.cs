using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Swift;
using SCM;

public class SysSettingUI : UIBase
{
    public GameObject SurrenderBtn;
    public GameObject ReplayExitBtn;

    protected override void StartOnlyOneTime()
    {
        var gc = GameCore.Instance;
        var nc = gc.Get<NetCore>();

        nc.OnDisconnected += OnDisconnected;
        Room4Client.OnBattleBegin += OnBattleBegin;
        Room4Client.OnBattleEnd += OnBattleEnd;
    }

    private void OnBattleEnd(Room r, string winner, bool inReplay)
    {
        Hide();
    }

    private void OnBattleBegin(Room4Client r, bool inReplay)
    {
        Hide();
    }

    private void OnDisconnected(Connection conn, string reason)
    {
        Hide();
    }

    public void SelShowBtn(bool isInReplaying)
    {
        if (isInReplaying)
        {
            SurrenderBtn.SetActive(false);
            ReplayExitBtn.SetActive(true);
        }
        else
        {
            SurrenderBtn.SetActive(true);
            ReplayExitBtn.SetActive(false);
        }
    }

    // 点击其它区域关闭界面
    public void OnCancel()
    {
        Hide();
    }

    public override void Show()
    {
        base.Show();
    }

    // 投降
    public void OnSurrender()
    {
        // 结束录像或通知服务器认输
        var rp = GameCore.Instance.Get<BattleReplayer>();
        if (rp.InReplaying)
        {
            var br = GameCore.Instance.Get<BattleReplayer>();
            br.SpeedUpFactor = 1;
            rp.Stop();
        }
        else
        {
            var conn = GameCore.Instance.ServerConnection;
            conn.End(conn.Send2Srv("Surrender"));
        }
    }
}
