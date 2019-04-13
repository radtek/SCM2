using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Swift;
using SCM;
using Swift.Math;

public class IndicatorUILayer : UIBase {

    public Bloodbar BB;
    public Progressbar PB;
    public WaitingNum WN;
    public GameObject NameBoard;
    public RunFlag RF;

    Dictionary<string, Bloodbar> bbs = new Dictionary<string, Bloodbar>();
    Dictionary<string, RunFlag> rfs = new Dictionary<string, RunFlag>();
    Dictionary<string, Progressbar> pbs = new Dictionary<string, Progressbar>();
    Dictionary<string, WaitingNum> wns = new Dictionary<string, WaitingNum>();
    Dictionary<string, GameObject> nbs = new Dictionary<string, GameObject>();

    protected override void StartOnlyOneTime()
    {
        Room4Client.OnFrameElapsed += OnFrameElapsed;
        Room4Client.OnBattleEnd += OnBattleEnd;
        GameCore.Instance.OnMainConnectionDisconnected += Instance_OnMainConnectionDisconnected;
    }

    private void Instance_OnMainConnectionDisconnected(Connection arg1, string arg2)
    {
        Clear();
    }

    private void OnBattleEnd(Room r, string winner, bool inReplay)
    {
        Clear();
    }

    public void Clear()
    {
        foreach (var bb in bbs.Values)
            Destroy(bb.gameObject);

        foreach (var pc in pbs.Values)
            Destroy(pc.gameObject);

        foreach (var wn in wns.Values)
            Destroy(wn.gameObject);

        foreach (var nb in nbs.Values)
            Destroy(nb);

        bbs.Clear();
        pbs.Clear();
        wns.Clear();
        nbs.Clear();
    }

    private void OnFrameElapsed(Room4Client room)
    {
        PushProgressTime(Room.FrameInterval / 1000.0f);
    }

    public void PushProgressTime(float te)
    {
        foreach (var pb in pbs.Values)
            pb.MoveForward(te);
    }

    // 创建单位加强标志
    public RunFlag CreateRunFlag(Unit u)
    {
        var rf = Instantiate(RF) as RunFlag;
        rfs[u.UID] = rf;
        rf.U = u;
        rf.transform.SetParent(transform, false);
        rf.transform.localScale = new Vector3((float)u.cfg.SizeRadius, 1, 1);
        rf.transform.localScale *= UIManager.Instance.MainUI2IndicateUIScale;
        rf.transform.localRotation = Quaternion.identity;
        rf.transform.localPosition = Vector3.zero;
        rf.gameObject.SetActive(true);

        return rf;
    }

    // 创建对应血条
    public Bloodbar CreateBloodbar(Unit u)
    {
        var bb = Instantiate(BB) as Bloodbar;
        bbs[u.UID] = bb;
        bb.U = u;
        bb.transform.SetParent(transform, false);
        bb.transform.localScale = new Vector3((float)u.cfg.SizeRadius * 10, 1, 1);
        bb.transform.localScale *= UIManager.Instance.MainUI2IndicateUIScale;
        bb.transform.localRotation = Quaternion.identity;
        bb.transform.localPosition = Vector3.zero;
        bb.gameObject.SetActive(true);

        //if (u.UnitType != "CrystalMachine" && u.UnitType != "Accessory" && u.cfg.IsBuilding)
        //{
        //    var nb = Instantiate(NameBoard) as GameObject;
        //    nb.transform.SetParent(transform, false);
        //    nb.transform.localScale = Vector3.one * UIManager.Instance.MainUI2IndicateUIScale;
        //    nb.transform.localRotation = Quaternion.identity;
        //    var sp = UIManager.Instance.World2IndicateUI(u.Pos);
        //    nb.GetComponent<RectTransform>().anchoredPosition = new Vector2((float)sp.x, (float)sp.y);
        //    nb.GetComponentInChildren<Text>().text = u.cfg.DisplayName;
        //    nb.SetActive(true);
        //    nbs[u.UID] = nb;
        //}

        return bb;
    }

    // 创建建造进度指示
    public Progressbar CreateProgressbar(Unit u, Fix64 totalTime)
    {
        var pb = Instantiate(PB) as Progressbar;
        pbs[u.UID] = pb;
        pb.U = u;
        pb.ResetProgress(totalTime);
        pb.transform.SetParent(transform, false);
        pb.transform.localScale = new Vector3((float)u.cfg.SizeRadius * 10, 1, 1);
        pb.transform.localScale *= UIManager.Instance.MainUI2IndicateUIScale;
        pb.transform.localRotation = Quaternion.identity;
        pb.transform.localPosition = Vector3.zero;
        pb.gameObject.SetActive(true);

        return pb;
    }

    public WaitingNum CreateWaitingNumber(Unit u, int num)
    {
        var uid = u.UID;

        if (wns.ContainsKey(uid))
            DestroyWaitingNum(uid);

        if (!pbs.ContainsKey(uid)) // ignore the operation
            return null;

        var pb = pbs[uid];

        var wn = Instantiate(WN) as WaitingNum;
        wns[uid] = wn;
        wn.U = u;
        wn.GetComponent<Text>().text = num < 10 ? num.ToString() : "*";
        wn.transform.SetParent(transform, false);
        wn.transform.localScale *= UIManager.Instance.MainUI2IndicateUIScale;
        wn.transform.localRotation = Quaternion.identity;
        wn.transform.localPosition = Vector3.zero;
        wn.PrograssbarRect = pb.GetComponent<RectTransform>();
        wn.gameObject.SetActive(true);

        return wn;
    }

    // 销毁血条
    public void DestroyBloodbar(string uid)
    {
        if (!bbs.ContainsKey(uid))
            return;

        var bb = bbs[uid];
        bbs.Remove(uid);
        bb.U = null;
        bb.gameObject.SetActive(false);
        Destroy(bb.gameObject);

        //if (nbs.ContainsKey(uid))
        //{
        //    var nb = nbs[uid];
        //    nbs.Remove(uid);
        //    Destroy(nb);
        //}
    }

    // 销毁进度指示
    public void DestroyProgressbar(string uid)
    {
        if (!pbs.ContainsKey(uid))
            return;

        var pb = pbs[uid];
        pbs.Remove(uid);
        pb.gameObject.SetActive(false);
        Destroy(pb.gameObject);
    }

    // 销毁建造等待数量提示
    public void DestroyWaitingNum(string uid)
    {
        if (!wns.ContainsKey(uid))
            return;

        var wn = wns[uid];
        wns.Remove(uid);
        wn.gameObject.SetActive(false);
        Destroy(wn.gameObject);
    }
}
