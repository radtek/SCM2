using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AttackEffect : MonoBehaviour {

	public enum MovePathType
    {
        OnAttacker, // 攻击者位置播放
        OnTarget, // 目标位置播放
        FromGun2Target, // 从攻击位置飞向目标点
        Expand, // 区域向外扩展
    }

    public MovePathType MovePath;
    public bool DoNotScale;
    public Vector3 To;
    public float Expand2;
    public float ExpandSpeed;

    private void Start()
    {
        if (Expand2 < 0)
            transform.localScale = Vector3.one * (-Expand2);
    }

    private void Update()
    {
        if (Expand2 == 0)
            return;

        var dt = Time.deltaTime;
        var s = transform.localScale;
        var gs = transform.lossyScale;
        var pgs = transform.parent.lossyScale;
        s += Vector3.one * ExpandSpeed * dt / pgs.x;
        s.y = 1;
        transform.localScale = s;
        gs = transform.lossyScale;
        gameObject.SetActive((Expand2 > 0 && gs.x < Expand2) || (Expand2 < 0 && gs.x > 0));
    }
}
