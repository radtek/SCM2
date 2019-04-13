using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swift;
using SCM;
using Swift.Math;

public class UIManager : SCMBehaviour {

    public Tips Tips;
    public GuideUI Guide;
    
    Canvas IndicateCanvas;
    Camera IndicateUICamera;
    RectTransform IndicateUICavansRect;

    Canvas Canvas;
    public Camera UICamera { get; private set; }
    RectTransform UICavansRect;

    MapScene MS;

    protected override void StartOnlyOneTime()
    {
        GameCore.Instance.OnMainConnectionDisconnected += OnDisconnected;
    }

    private void OnDisconnected(Connection conn, string reason)
    {
        ClearScene();

        ShowTopUI("MainArea", false);
        ShowTopUI("MainMenu", false);
        ShowTopUI("LoginUI", true);

        Tips.AddTip("网络连接中断");
    }

    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                var gd = FindObjectOfType<GameDriver>();
                var uiRootGo = gd.transform.Find("UIRoot").gameObject;
                instance = uiRootGo.AddComponent<UIManager>();
                instance.Tips = uiRootGo.GetComponentInChildren<Tips>();
                instance.Guide = uiRootGo.GetComponentInChildren<GuideUI>();
                instance.Canvas = uiRootGo.GetComponent<Canvas>();
                instance.UICavansRect = instance.Canvas.GetComponent<RectTransform>();
                instance.UICamera = instance.Canvas.worldCamera;

                var indicateUIRootGo = gd.transform.Find("IndicatorUIRoot").gameObject;
                instance.IndicateCanvas = indicateUIRootGo.GetComponent<Canvas>();
                instance.IndicateUICavansRect = instance.IndicateCanvas.GetComponent<RectTransform>();
                instance.IndicateUICamera = instance.IndicateCanvas.worldCamera;

                instance.MS = gd.transform.Find("SceneRoot").GetComponent<MapScene>();
            }

            return instance;
        }
    } static UIManager instance = null;

    public float MainUI2IndicateUIScale { get { return IndicateUICavansRect.rect.width / (float)instance.UICavansRect.rect.width; } }

    public Vec2 World2IndicateUI(Vec2 wp) { return World2IndicateUI(new Vector3((float)wp.x, 0, (float)wp.y)); }
    public Vec2 World2IndicateUI(Vector3 wp)
    {
        var vp = IndicateUICamera.WorldToViewportPoint(wp);
        var sp = new Vec2(IndicateUICavansRect.rect.width * vp.x, IndicateUICavansRect.rect.height * vp.y);
        return sp;
    }

    public Vec2 World2UI(Vec2 wp) { return World2UI(new Vector3((float)wp.x, 0, (float)wp.y)); }
    public Vec2 World2UI(Vector3 wp)
    {
        var vp = UICamera.WorldToViewportPoint(wp);
        var sp = new Vec2(UICavansRect.rect.width * vp.x, UICavansRect.rect.height * vp.y);
        return sp;
    }

    public Vector3 World2View(Vector3 wp)
    {
        return UICamera.WorldToViewportPoint(wp);
    }

    public Vector3 View2Ground(Vector3 pt)
    {
        var ray = Camera.main.ScreenPointToRay(new Vector3(pt.x, pt.y, 0));
        var t = ray.origin.y / ray.direction.y;
        var hitPt = ray.origin - t * ray.direction;
        return hitPt;
    }

    // 显示/隐藏 UI
    public UIBase ShowUI(Transform parent, string uiName, bool visible)
    {
        var t = parent.Find(uiName);
        if (t == null)
            return null;

        var ui = t.GetComponent<UIBase>();
        if (visible)
            ui.Show();
        else
            ui.Hide();

        return ui;
    }

    // 显示/隐藏 UI
    public UIBase ShowTopUI(string uiName, bool visible)
    {
        return ShowUI(transform, "XAdapter/" + uiName, visible);
    }

    // 清空场景
    public void ClearScene()
    {
        MS.Clear();
        (ShowTopUI("GuideUI", true) as GuideUI).HideAllHints();
        (ShowTopUI("InBattleUI", true) as InBattleUI).HideAllChildren();
        Tips.Clear();
    }
}
