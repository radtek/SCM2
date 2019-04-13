using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Swift;
using SCM;
using UnityEngine.UI;
using Swift.Math;

public interface IEventHandler
{
    void OnClick(Vec2 pt, Vector3 wp);
    void OnDoubleClick(Vec2 pt, Vector3 wp);
    void OnPress(Vec2 pt, Vector3 wp);
    void OnDragStarted(Vec2 pt, Vector3 wp);
    void OnDragging(Vec2 from, Vector3 fromWp, Vec2 now, Vector3 nowWp);
    void DoDragEnded(Vec2 from, Vector3 fromWp, Vec2 to, Vector3 toWp);
}

/// <summary>
/// 在全局进行处理逻辑处理
/// </summary>
public class EventLayerHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    Vector3 ptDown;
    Vector3 ptDownHit;
    Vector3 lastPt;
    IEventHandler anchorHandler;

    string state = "up"; // down, up, pressing, dragging

    int dragThreshold = 10;
    float pressThresholdLeft = 0.25f;
    float doubleClickThreshold = 0.25f;
    DateTime lastClickTime;

    public void OnPointerDown(PointerEventData e)
    {
        ptDown = PtNow;
        state = "down";
        pressThresholdLeft = 0.25f;

        anchorHandler = RayQueryHandler(ptDown, out ptDownHit);
    }

    Vector3 PtNow
    {
        get
        {
            var pt = Input.touchCount == 0 ? Input.mousePosition :
                new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, 0);

            return pt;
        }
    }

#if UNITY_EDITOR
    public static PointerEventData.InputButton CurrentClickBtn = PointerEventData.InputButton.Left;
#endif
    public void OnPointerUp(PointerEventData e)
    {
#if UNITY_EDITOR
        CurrentClickBtn = e.button;
#endif

        if (state == "down")
            DoClick(ptDown);
        else if (state == "dragging")
            DoDragEnded(ptDown, PtNow);

        state = "up";
#if UNITY_EDITOR
        CurrentClickBtn = PointerEventData.InputButton.Left;
#endif
    }

    void Update()
    {
        if (state == "up")
            return;

        var pt = PtNow;
        if (state == "down" || state == "pressing")
        {
            var dt = Time.deltaTime;
            pressThresholdLeft -= dt;
            if (state != "pressing" && pressThresholdLeft <= 0)
            {
                state = "pressing";
                DoPress(pt);
            }
            else
            {
                var d = (pt - ptDown).magnitude;
                if (d >= dragThreshold)
                {
                    state = "dragging";
                    DragStarted(ptDown);
                }
            }
        }

        if (state == "dragging" && lastPt != pt)
        {
            DoDragging(ptDown, pt);
            lastPt = pt;
        }
    }

    // 查询当前点到的单位
    public static IEventHandler RayQueryHandler(Vector2 pt, out Vector3 hitPt)
    {
        var ray = Camera.main.ScreenPointToRay(new Vector3(pt.x, pt.y, 0));
        var hits = Physics.RaycastAll(ray, 10000);
        var minDist = float.MaxValue;

        // 可能超出屏幕范围外了，就用地表 0 点计算 hitPt，先算后，后面击中了再覆盖
        var t = ray.origin.y / ray.direction.y;
        hitPt = ray.origin - t * ray.direction;

        IEventHandler handler = null;
        foreach (var h in hits)
        {
            if (h.distance >= minDist)
                continue;

            minDist = h.distance;
            var go = h.collider.gameObject;
            hitPt = h.point;
            var hd = go.GetComponent<IEventHandler>();
            if (hd != null) // could be null for NavObstacle
                handler = hd;
        }

        return handler;
    }

    void DoClick(Vector3 pt)
    {
        Vector3 hitPt;
        var handler = RayQueryHandler(pt, out hitPt);
        if (handler != null)
        {
            var now = DateTime.Now;
            var clickpt = UIManager.Instance.World2UI(hitPt);
            var dt = now - lastClickTime;

            if (dt.TotalSeconds <= doubleClickThreshold)
                handler.OnDoubleClick(clickpt, hitPt);
            else
                handler.OnClick(clickpt, hitPt);

            lastClickTime = now;
        }
    }

    // 开始拖拽
    void DragStarted(Vector3 startPt)
    {
        var fromHandler = anchorHandler;
        if (fromHandler == null)
            return;

        var sp = UIManager.Instance.World2UI(ptDownHit);
        fromHandler.OnDragStarted(sp, ptDownHit);
    }

    // 拖动中
    void DoDragging(Vector3 dragStartPt, Vector3 nowPt)
    {
        var fromHandler = anchorHandler;
        if (fromHandler == null)
            return;

        Vector3 nowHitPt;
        var toHandler = RayQueryHandler(nowPt, out nowHitPt);
        if (toHandler == null)
            return;

        var fromSp = UIManager.Instance.World2UI(ptDownHit);
        var toSp = UIManager.Instance.World2UI(nowHitPt);

        fromHandler.OnDragging(fromSp, ptDownHit, toSp, nowHitPt);
    }

    // 拖动结束
    void DoDragEnded(Vector3 fromPt, Vector3 toPt)
    {
        Vector3 toHitPt;

        var fromHandler = anchorHandler;
        if (fromHandler == null)
            return;

        RayQueryHandler(toPt, out toHitPt);

        var fromSp = UIManager.Instance.World2UI(ptDownHit);
        var toSp = UIManager.Instance.World2UI(toHitPt);

        fromHandler.DoDragEnded(fromSp, ptDownHit, toSp, toHitPt);
    }

    // 长按
    void DoPress(Vector3 pt)
    {
        Vector3 hitPt;
        var handler = RayQueryHandler(pt, out hitPt);
        if (handler != null)
            handler.OnPress(UIManager.Instance.World2UI(hitPt), hitPt);
    }
}
