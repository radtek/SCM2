using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Swift;
using System;
using SCM;

public class ReplayListUI : UIBase
{
    public GameObject MyList;
    public GameObject HotList;
    public GameObject ReplayItem;

    protected override void StartOnlyOneTime()
    {
    }

    public void ShowReplays()
    {
        ClearContent(MyList);
        ClearContent(HotList);

        BuildMyReplays();
        BuildHotReplays();
    }

    public BattleReplay[] HotReplays
    {
        set
        {
            hotReplays = value;
        }
    }
    BattleReplay[] hotReplays = null;

    public BattleReplay[] MyReplays
    {
        set
        {
            myReplays = value;
        }
    }
    BattleReplay[] myReplays = null;

    public void OnGetReplay(string replayID)
    {
        var conn = GameCore.Instance.ServerConnection;
        var buff = conn.Request2Srv("GetReplay", (data) =>
        {
            var exists = data.ReadBool();
            if (!exists)
            {
                AddTip("没有找到录像");
                return;
            }

            UIManager.Instance.ShowTopUI("ReplayUI", true);
            var replayer = GameCore.Instance.Get<BattleReplayer>();
            replayer.Clear();
            replayer.ReadFromBuffer(data);
            replayer.Start();
        });
        buff.Write(replayID);
        conn.End(buff);
    }

    private GameObject CreateReplayItem(Transform parent)
    {
        GameObject item = GameObject.Instantiate(ReplayItem) as GameObject;
        item.SetActive(true);
        item.transform.SetParent(parent);
        item.transform.localPosition = Vector3.zero;
        item.transform.localScale = Vector3.one;
        item.transform.localEulerAngles = Vector3.zero;

        return item;
    }

    private void BuildHotReplays()
    {
        if (hotReplays == null)
            return;

        for (int i = 0; i < hotReplays.Length; i++)
        {
            GameObject item = CreateReplayItem(HotList.transform);
            ShowHotReplayInfo(item, hotReplays[i]);
            var replayID = hotReplays[i].ID;
            item.GetComponent<Button>().onClick.AddListener(() => OnGetReplay(replayID));
        }
    }

    private void BuildMyReplays()
    {
        if (myReplays == null)
            return;

        for (int i = 0; i < myReplays.Length; i++)
        {
            GameObject item = CreateReplayItem(MyList.transform);
            ShowMyReplayInfo(item, myReplays[i]);
            var replayID = myReplays[i].ID;
            item.GetComponent<Button>().onClick.AddListener(() => OnGetReplay(replayID));
        }
    }

    string FormatReplayDateTime(BattleReplay replay)
    {
        var d = replay.Date.ToString("MM-dd HH:mm");
        var secs = replay.Length / 10;
        var mins = secs / 60;
        secs -= mins * 60;
        var t = mins.ToString().PadLeft(2, '0') + ":" + secs.ToString().PadLeft(2, '0');
        return d + " ( " + t + " )";
    }

    private void ShowHotReplayInfo(GameObject item, BattleReplay replay)
    {
        item.transform.Find("Title").GetComponentInChildren<Text>().text = replay.UsrName1 + " vs " + replay.UsrName2;
        item.transform.Find("Data").GetComponentInChildren<Text>().text = FormatReplayDateTime(replay);
    }

    private void ShowMyReplayInfo(GameObject item, BattleReplay replay)
    {
        var opponent = replay.Usr1 == GameCore.Instance.MeID ? replay.UsrName2 : replay.UsrName1;
        item.transform.Find("Title").GetComponentInChildren<Text>().text = "vs " + opponent;
        item.transform.Find("Data").GetComponentInChildren<Text>().text = FormatReplayDateTime(replay);
    }

    private void ClearContent(GameObject go)
    {
        foreach (Transform trans in go.transform)
            Destroy(trans.gameObject);
    }
}
