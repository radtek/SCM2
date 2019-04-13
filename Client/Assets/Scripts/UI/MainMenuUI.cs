using SCM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : UIBase
{
    public GameObject User;
    public GameObject Unit;
    public GameObject Battle;
    public GameObject Replay;

    public Text IntegrationVal;

    public GameObject DisabledBtn;

    public MainAreaUI MAUI;
    public AvatarUI AUI;
    public NameUI NUI;

    protected override void StartOnlyOneTime()
    {
        Room4Client.OnBattleBegin += OnBattleBegin;
        Room4Client.OnBattleEnd += OnBattleEnd;

        var pvpUI = (PVPUI)MAUI.PVPUI;
        pvpUI.onMatchingState += Change2DisabledState;
        pvpUI.onNormalState += Changed2EnableadState;

        ShowUserInfo();

        Unit.GetComponent<Toggle>().onValueChanged.AddListener((isOn) => OnValueChanged(isOn, Unit));
        Battle.GetComponent<Toggle>().onValueChanged.AddListener((isOn) => OnValueChanged(isOn, Battle));
        Replay.GetComponent<Toggle>().onValueChanged.AddListener((isOn) => OnValueChanged(isOn, Replay));
        
        Battle.GetComponent<Toggle>().isOn = true;
    }

    public override void Show()
    {
        base.Show();
    }

    public void OnClickAvatarBtn()
    {
        AUI.Show();
    }

    public void OnClickNameBtn()
    {
        NUI.Show();
    }

    private void Change2DisabledState()
    {
        DisabledBtn.SetActive(true);
    }

    private void Changed2EnableadState()
    {
        DisabledBtn.SetActive(false);
    }

    private void OnBattleBegin(Room4Client r, bool inReplay)
    {
        Hide();
    }

    private void OnBattleEnd(Room r, string winner, bool isReplay)
    {
        ShowUserInfo();
    }

    public void ShowUserInfo()
    {
        var meInfo = GameCore.Instance.MeInfo;
        User.transform.Find("Name").GetComponent<Text>().text = GameCore.Instance.MeInfo.Name;
        User.transform.Find("WinCnt").GetComponent<Text>().text = SCMText.T("胜") + string.Format("：<color=green>{0}</color>", meInfo.WinCount.ToString() );
        User.transform.Find("LoseCnt").GetComponent<Text>().text = SCMText.T("负") + string.Format("：<color=red>{0}</color>", meInfo.LoseCount.ToString());

        // Icon
        Sprite img = Resources.Load<Sprite>(@"Texture\AvatarUI\" + meInfo.CurAvator);

        if (null != img)
            User.transform.Find("Icon").GetComponent<Image>().sprite = img;
        else
            User.transform.Find("Icon").gameObject.SetActive(false);

        var total = meInfo.WinCount + meInfo.LoseCount;
        User.transform.Find("Rate").GetComponent<Text>().text = SCMText.T("胜率") + string.Format("：{0}", total <= 0 ? " - " : (int)(meInfo.WinCount * 100 / total) + "%");

        var left = meInfo.Integration - meInfo.IntegrationCost;

        // 解锁积分
        IntegrationVal.text = left.ToString();
    }

    private void OnValueChanged(bool isOn, GameObject go)
    {
        if (isOn)
        {
            go.transform.Find("Mark").gameObject.SetActive(true);
            go.transform.Find("MarkIcon").gameObject.SetActive(true);

            switch (go.name)
            {
                case "Unit":
                    MAUI.Scroll(MainAreaUIPage.DescPage);
                    break;
                case "Battle":
                    MAUI.Scroll(MainAreaUIPage.PVPPage);
                    break;
                case "Replay":
                    MAUI.Scroll(MainAreaUIPage.ReplayPage);
                    FetchRecentReplayList();
                    FetchMyReplayList();
                    break;
            }
        }
        else
        {
            go.transform.Find("Mark").gameObject.SetActive(false);
            go.transform.Find("MarkIcon").gameObject.SetActive(false);
        }
    }

    void FetchRecentReplayList()
    {
        var rl = MAUI.ReplayListUI.GetComponentInChildren<ReplayListUI>();
        var conn = GameCore.Instance.ServerConnection;
        var buff = conn.Request2Srv("GetReplayList", (data) =>
        {
            var cnt = data.ReadInt();
            var arr = new BattleReplay[cnt];
            for (var i = 0; i < cnt; i++)
                arr[i] = BattleReplay.DeserializeHeader(data);

            rl.HotReplays = arr;
            if (arr == null || arr.Length == 0)
                return;

            rl.ShowReplays();
        });
        buff.Write(10);
        conn.End(buff);
    }

    void FetchMyReplayList()
    {
        var rl = MAUI.ReplayListUI.GetComponentInChildren<ReplayListUI>();
        var conn = GameCore.Instance.ServerConnection;
        var buff = conn.Request2Srv("GetMyReplayList", (data) =>
        {
            var cnt = data.ReadInt();
            var arr = new BattleReplay[cnt];
            for (var i = 0; i < cnt; i++)
                arr[i] = BattleReplay.DeserializeHeader(data);

            rl.MyReplays = arr;
            if (arr == null || arr.Length == 0)
                return;
        });
        buff.Write(10);
        conn.End(buff);
    }
}
