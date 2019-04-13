using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Swift;
using SCM;
using Swift.Math;

public class Tips : UIBase {

    public GameObject Tip = null;
    public GameObject SmallTip = null;

    GameObject fixedTip = null;
    Dictionary<GameObject, float> tips = new Dictionary<GameObject, float>();

    ServerPort sp;

    protected override void StartOnlyOneTime()
    {
        sp = GameCore.Instance.Get<ServerPort>();
        sp.OnMessage("Message", OnMessage);

        Room4Client.OnBattleBegin += OnBattleBegin;
    }

    private void OnBattleBegin(Room4Client r, bool inReplay)
    {
        WorldCenter = r.MapSize / 2;
    }

    void OnMessage(IReadableBuffer data)
    {
        var msg = data.ReadString();
        AddTip(msg);
    }

    public Vec2 WorldCenter = Vec2.Zero;
    public void AddTipImpl(string msg, int fontSize, bool holding = false)
    {
        AddTip(msg, WorldCenter, fontSize, holding);
    }

    public void AddTip(string msg, Vec2 wp, int fontSize, bool fix = false)
    {
        var tip = Instantiate(Tip) as GameObject;
        tip.gameObject.SetActive(true);
        tip.GetComponentInChildren<Text>().fontSize = fontSize;
        tip.GetComponentInChildren<Text>().text = msg;
        tip.transform.SetParent(transform, false);

        var sp = UIManager.Instance.World2UI(wp);
        tip.GetComponent<RectTransform>().anchoredPosition = new Vector2((float)sp.x, (float)sp.y);
        tips[tip] = 0;

        if (fix)
        {
            foreach (var ani in tip.GetComponentsInChildren<Animator>())
                Destroy(ani);

            tips.Remove(tip);
            fixedTip = tip;
        }
    }

    public void AddSmallTipImpl(string msg)
    {
        AddSmallTip(msg, WorldCenter);
    }

    public void AddSmallTip(string msg, Vec2 wp)
    {
        var tip = Instantiate(SmallTip) as GameObject;
        tip.gameObject.SetActive(true);
        tip.GetComponentInChildren<Text>().text = msg;
        tip.transform.SetParent(transform, false);

        var sp = UIManager.Instance.World2UI(wp);
        tip.GetComponent<RectTransform>().anchoredPosition = new Vector2((float)sp.x, (float)sp.y);

        tips[tip] = 0;
    }

    public void Clear()
    {
        foreach (var tip in tips.Keys.ToArray())
            Destroy(tip.gameObject);

        tips.Clear();

        if (fixedTip != null)
        {
            Destroy(fixedTip);
            fixedTip = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var tip in tips.Keys.ToArray())
        {
            var time = tips[tip];
            time += Time.deltaTime;
            if (time > 2)
            {
                tips.Remove(tip);
                Destroy(tip.gameObject);
            }
            else
                tips[tip] = time;
        }
    }
}
