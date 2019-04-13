using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Swift;

public class UnitCardUI : MonoBehaviour, IPointerDownHandler
{
    public RectTransform DragCancelArea;
    public Action OnPtDown;
    public Action OnBeginDrag;
    public Action OnDrag;
    public Action<bool> OnEndDrag;

    const int dragThreshold = 20;
    bool down = false;
    bool dragging = false;
    Vector3 dragStartPos;
    Vector3 dragLastPos;

    Vector3 PtNow
    {
        get
        {
            var pt = Input.touchCount == 0 ? Input.mousePosition :
                new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, 0);

            return pt;
        }
    }

    bool PointerOnMe
    {
        get
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                GetComponent<RectTransform>(), PtNow, UIManager.Instance.UICamera);
        }
    }

    bool PointerOnDragCancelArea
    {
        get
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                DragCancelArea, PtNow, UIManager.Instance.UICamera);
        }
    }

    bool PointerDown
    {
        get
        {
            return Input.touchCount == 0 ? Input.GetMouseButton(0) :
                Input.GetTouch(0).pressure > 0;
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (OnPtDown != null)
            OnPtDown();

        down = true;
        dragStartPos = PtNow;
        dragLastPos = Vector3.zero;
    }

    private void Update()
    {
        if (!down)
            return;

        if (PointerDown)
        {
            var pt = PtNow;

            if (dragging)
            {
                var d = pt - dragLastPos;
                if (d.magnitude >= 1)
                {
                    OnDrag.SC();
                    dragLastPos = pt;
                }
            }
            else
            {
                var d = pt - dragStartPos;
                if (d.magnitude >= dragThreshold)
                {
                    dragging = true;
                    dragLastPos = pt;
                    OnBeginDrag.SC();
                }
            }
        }
        else
        {
            if (dragging)
                OnEndDrag.SC(PointerOnDragCancelArea);

            down = false;
            dragging = false;
        }
    }
}
