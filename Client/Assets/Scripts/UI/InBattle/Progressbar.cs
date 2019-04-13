using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Swift;
using SCM;
using Swift.Math;

public class Progressbar : MonoBehaviour
{
    public Transform FollowTransformRoot;
    public Unit U
    {
        get { return u; }
        set
        {
            u = value;
            if (u == null)
                return;

            tanOblique = Mathf.Tan(Camera.main.GetComponent<MainCamera>().Oblique);
            delta = u.cfg.SizeRadius * ((u.cfg.IsBuilding ? 0 : 1) + tanOblique) - 1; /* just under the bloodbar */
            delta = GameCore.Instance.MePlayer == 1 ? delta : -delta;
        }
    } Unit u;

    static float tanOblique;
    float delta;
    void AdjustPos()
    {
        var sp = UIManager.Instance.World2IndicateUI(FollowTransformRoot.position + new Vector3(0, 0, (float)delta));
        RT.anchoredPosition = new Vector2((float)sp.x, (float)sp.y);
    }

    RectTransform RT;
    GameObject Bg;
    Image Value;

    private void Start()
    {
        Bg = transform.Find("Bg").gameObject;
        RT = GetComponent<RectTransform>();
        Value = transform.Find("Value").GetComponent<Image>();
    }

    Fix64 TotalTime;
    Fix64 timeElapsed;
    public void MoveForward(float te)
    {
        timeElapsed += te;
    }

    public void ResetProgress(Fix64 totalTime)
    {
        timeElapsed = 0;
        TotalTime = totalTime;
    }

    void Update()
    {
        if (U == null || FollowTransformRoot == null || TotalTime == 0)
        {
            Bg.SetActive(false);
            Value.gameObject.SetActive(false);
            TotalTime = 0;
            return;
        }

        var p = timeElapsed / TotalTime;
        if (p >= 1 || p == 0)
        {
            Bg.SetActive(false);
            Value.gameObject.SetActive(false);
        }
        else
        {
            Bg.SetActive(true);
            Value.gameObject.SetActive(true);
            Value.fillAmount = (float)p;
            AdjustPos();
        }
    }
}
