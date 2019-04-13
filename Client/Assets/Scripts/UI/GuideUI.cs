using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Swift;
using Swift.Math;
using SCM;

/// <summary>
/// 指引
/// </summary>
public class GuideUI : UIBase {

    public RectTransform ClickHint;
    public RectTransform PressHint;

    // 提示点击
    public void ToClick(Vec2 sp, string msg)
    {
        HideAllHints();
        Set(ClickHint, sp, msg);
    }

    // 提示长按
    public void ToPress(Vec2 sp, string msg)
    {
        HideAllHints();
        Set(PressHint, sp, msg);
    }

    // 拖动提示
    public void ToDrag(string msg, params Vec2[] pts)
    {
        HideAllHints();
        Set(ClickHint, pts[0], msg);
        dragPath = pts;
        dragPathDiv = 0;
    }

    Vec2[] dragPath = null;
    int dragPathDiv = 0;
    private void Update()
    {
        if (dragPath == null)
            return;

        var maxD = 200 * Time.deltaTime;
        if (dragPathDiv >= dragPath.Length - 1)
        {
            dragPathDiv = 0;
            ClickHint.anchoredPosition = new Vector2(
                (float)dragPath[0].x, (float)dragPath[0].y);
            return;
        }

        var n = dragPath[dragPathDiv + 1];
        var o = new Vec2(ClickHint.anchoredPosition.x, ClickHint.anchoredPosition.y);
        var d = n - o;
        var l = d.Length;
        if (l < 1)
        {
            dragPathDiv++;
            ClickHint.anchoredPosition = new Vector2(
                (float)n.x, (float)n.y);
        }
        else
        {
            var np = maxD < l ? o + d * maxD / l : n;
            ClickHint.anchoredPosition = new Vector2(
                (float)np.x, (float)np.y);
        }
    }

    void Set(RectTransform rect, Vec2 sp, string msg = null)
    {
        rect.gameObject.SetActive(true);
        rect.anchoredPosition = new Vector2((float)sp.x, (float)sp.y);
        if (rect.anchoredPosition.y < rect.parent.GetComponent<RectTransform>().rect.height / 2)
        {
            rect.Find("Up").gameObject.SetActive(false);
            rect.Find("Down").gameObject.SetActive(true);
        }
        else
        {
            rect.Find("Up").gameObject.SetActive(true);
            rect.Find("Down").gameObject.SetActive(false);
        }

        msg = msg == null ? "" : msg;
        msg = msg.Replace("<u>", "<color=#00EEFFFF><b>").Replace("</u>", "</b></color>");
        msg = msg.Replace("<op>", "<color=#FFFF00FF><b>").Replace("</op>", "</b></color>");
        foreach (var t in rect.GetComponentsInChildren<Text>())
            t.text = msg;
    }

    // 隐藏提示信息
    public void HideAllHints()
    {
        dragPath = null;
        ClickHint.gameObject.SetActive(false);
        PressHint.gameObject.SetActive(false);
    }
}
