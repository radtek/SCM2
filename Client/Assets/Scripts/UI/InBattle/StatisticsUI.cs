using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swift;
using UnityEngine.UI;
using Swift.Math;
using SCM;

public class StatisticsUI : UIBase
{
    public Text Money;
    public Text Gas;
    public SysSettingUI SysSetting;
    public Text FPS;
    public Text Timer;

    protected override void StartOnlyOneTime()
    {
        Room4Client.OnBattleBegin += Room4Client_OnBattleBegin1;
        Room4Client.OnResourceChanged += ResourceChanged;
        Room4Client.OnBattleBegin += Room4Client_OnBattleBegin;
        Room4Client.OnFrameElapsed += Room4Client_OnFrameElapsed;
        gameObject.SetActive(false);
    }

    int t = 0;

    private void Room4Client_OnBattleBegin1(Room4Client arg1, bool arg2)
    {
        t = 0;
        Timer.text = "00:00";
    }
    
    private void Room4Client_OnFrameElapsed(Room4Client r)
    {
        t += Room.FrameInterval;
        var secs = t / 1000;
        var min = secs / 60;
        secs = secs - min * 60;
        Timer.text = (min.ToString().PadLeft(2, '0')) + ":" + (secs.ToString().PadLeft(2, '0'));
    }

    private void Room4Client_OnBattleBegin(Room4Client arg1, bool arg2)
    {
        gameObject.SetActive(true);
    }

    bool resourceNumNeedRefresh = false;
    private void ResourceChanged(int player, string type, Fix64 num)
    {
        if (player != GameCore.Instance.MePlayer)
            return;

        resourceNumNeedRefresh = true;
    }

    public void ResetAll()
    {
        var gc = GameCore.Instance;
        Money.text = gc.GetMyResource("Money").ToString();
        Gas.text = gc.GetMyResource("Gas").ToString();
        Timer.text = "00:00";
    }

    public void OnOpenSysSetting()
    {
        var rp = GameCore.Instance.Get<BattleReplayer>();
        SysSetting.SelShowBtn(rp.InReplaying);
        SysSetting.Show();
    }

    List<float> last10FrameTimeElapsed = new List<float>();
    private void Update()
    {
        float dt = Time.deltaTime;
        last10FrameTimeElapsed.Add(dt);
        if (last10FrameTimeElapsed.Count > 10)
            last10FrameTimeElapsed.RemoveAt(0);

        var tt = 0.0f;
        foreach (var t in last10FrameTimeElapsed)
            tt += t;

        FPS.text = ((int)(10 / tt)).ToString();

        if (resourceNumNeedRefresh)
        {
            resourceNumNeedRefresh = false;

            var money = ((int)GameCore.Instance.GetMyResource("Money")).ToString();
            var gas = ((int)GameCore.Instance.GetMyResource("Gas")).ToString();
            Money.text = money;
            Gas.text = gas;
        }
    }
}
