using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Swift;
using SCM;
using Swift.Math;

public class Bloodbar : MonoBehaviour {

    public Transform FollowTransformRoot;
    public Unit U { get { return u; }
        set
        {
            u = value;
            if (u == null)
                return;

            tanOblique = Mathf.Tan(Camera.main.GetComponent<MainCamera>().Oblique);
            delta = u.cfg.SizeRadius * ((u.cfg.IsBuilding ? 0 : 1) + tanOblique);
            delta = GameCore.Instance.MePlayer == 1 ? delta : -delta;
            myUnit = U.Player == GameCore.Instance.MePlayer;
            netrualUnit = u.Player == 0;
        }
    } Unit u;

    static float tanOblique;
    float delta;
    void AdjustPos()
    {
        if (U == null || FollowTransformRoot == null)
            return;

        var sp = UIManager.Instance.World2IndicateUI(FollowTransformRoot.position + new Vector3(0, 0, (float)delta));
        RT.anchoredPosition = new Vector2((float)sp.x, (float)sp.y + (u.cfg.IsAirUnit ? 20 : 0));
    }

    RectTransform RT;
    GameObject Bg;
    Image RedValue;
    Image BlueValue;
    Image GreenValue;
    bool myUnit = false;
    bool netrualUnit = false;

    private void Start()
    {
        Bg = transform.Find("Bg").gameObject;
        RT = GetComponent<RectTransform>();
        RedValue = transform.Find("Red").GetComponent<Image>();
        BlueValue = transform.Find("Blue").GetComponent<Image>();
        GreenValue = transform.Find("Green").GetComponent<Image>();
    }

    // Update is called once per frame
    void Update ()
    {
        if (U == null || FollowTransformRoot == null)
        {
            Bg.SetActive(false);
            RedValue.gameObject.SetActive(false);
            BlueValue.gameObject.SetActive(false);
            GreenValue.gameObject.SetActive(false);
            return;
        }

        var p = (float)U.Hp / U.cfg.MaxHp;
        if (p >= 1 || p == 0)
        {
            Bg.SetActive(false);
            RedValue.gameObject.SetActive(false);
            BlueValue.gameObject.SetActive(false);
            GreenValue.gameObject.SetActive(false);
        }
        else
        {
            Bg.SetActive(true);
            BlueValue.gameObject.SetActive(myUnit);
            GreenValue.gameObject.SetActive(netrualUnit);
            RedValue.gameObject.SetActive(!myUnit && !netrualUnit);
            RedValue.fillAmount = p;
            BlueValue.fillAmount = p;
            GreenValue.fillAmount = p;
            AdjustPos();
        }
    }
}
