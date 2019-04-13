using SCM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunFlag : MonoBehaviour {

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
        }
    } Unit u;

    static float tanOblique;
    float delta;
    void AdjustPos()
    {
        if (U == null || FollowTransformRoot == null)
            return;

        var sp = UIManager.Instance.World2IndicateUI(FollowTransformRoot.position + new Vector3(0, 0, (float)delta));
        RT.anchoredPosition = new Vector2((float)sp.x, (float)sp.y);
        var sz = 1 + (U.cfg.SizeRadius - 1) / 2.0f;
        transform.localScale = new Vector3(sz / transform.parent.lossyScale.x,
            sz / transform.parent.lossyScale.y,
            sz / transform.parent.lossyScale.z);
    }

    RectTransform RT;
    GameObject Star;

    private void Start()
    {
        Star = transform.Find("Star").gameObject;
        RT = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update ()
    {
        if (U == null || FollowTransformRoot == null)
        {
            Star.SetActive(false);
            return;
        }

        var p = (float)U.Hp / U.cfg.MaxHp;
        if (p == 0)
        {
            Star.SetActive(false);
        }
        else
        {
            Star.SetActive(true);
            AdjustPos();
        }
    }
}
