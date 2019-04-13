using SCM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitingNum : MonoBehaviour {

    public RectTransform PrograssbarRect;
    public Unit U;

    RectTransform RT;
    void AdjustPos()
    {
        RT.anchoredPosition = new Vector2(PrograssbarRect.anchoredPosition.x - PrograssbarRect.rect.width * PrograssbarRect.transform.localScale.x / 2 - RT.rect.width / 2,
            PrograssbarRect.anchoredPosition.y);
    }

    private void Start()
    {
        RT = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (U == null || PrograssbarRect == null)
            return;

        AdjustPos();
    }
}
