using Swift.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swift;
using SCM;

/// <summary>
/// 播放模型动画
/// </summary>
public class AnimationPlayer : MonoBehaviour
{
    public UnitCreator UC;

    Animator animator;
    MapUnit mu;

    const int idle = 0;
    const int attack01 = 1;
    const int attack01_coldown = 1001;
    const int attack02 = 2;
    const int attack02_coldown = 1002;
    const int run = 3;
    const int die = 4;
    const int constructingUnit = 5;
    const int constructingPhrase1 = 6;
    const int constructingPhrase2 = 7;
    const int inBuilding = 8;

    Transform headTrans = null;
    AnimationPlayer headAni = null;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        mu = GetComponent<MapUnit>();

        headTrans = mu == null || mu.Root == null ? null : mu.Root.Find("Head");
        headAni = headTrans == null ? null : headTrans.GetComponent<AnimationPlayer>();
    }

    public void Idle()
    {
        if (animator != null)
            animator.SetInteger("state", idle);
    }

    public void Die()
    {
        if (animator != null)
            animator.SetInteger("state", die);

        if (mu != null && mu.U != null && mu.U.cfg.ReconstructFrom != null)
        {
            var ani = mu.Root.Find(mu.U.cfg.ReconstructFrom).GetComponent<Animator>();
            if (ani != null)
                ani.SetInteger("state", die);
        }

        if (headAni != null)
            headAni.Die();

        if (scaffold != null)
        {
            Destroy(scaffold.gameObject);
            scaffold = null;
            scaffoldUID = null;
        }
    }

    string scaffoldUID = null;
    MapUnit scaffold = null;
    void CreateScaffold()
    {
        if (scaffold != null)
            return;

        var mu = GetComponent<MapUnit>();
        scaffoldUID = mu.U.UID + "_Scaffold";
        scaffold = UC.CreateModel("Scaffold", scaffoldUID, false, false);
        UC.UnhandleModel(scaffoldUID); // 脚手架模型是动画的一部分，交给动画自己管理
        scaffold.transform.SetParent(transform, false);
        scaffold.transform.localPosition = Vector3.zero;
        scaffold.transform.localScale = Vector3.one;
        scaffold.transform.localRotation = Quaternion.identity;
        scaffold.gameObject.SetActive(true);
    }

    public void Construcing(string fromType, float totalTime)
    {
        CreateScaffold();
        var root = transform.Find("Root");
        if (fromType != null)
            animator = root.Find(mu.U.UnitType).GetComponent<Animator>();

        if (mu.U.cfg.ReconstructFrom == null)
            root.gameObject.SetActive(false);

        if (fromType != mu.U.cfg.ReconstructFrom && root.Find(fromType) != null)
            root.Find(fromType).gameObject.SetActive(false);

        var sfAni = scaffold.GetComponent<AnimationPlayer>();
        sfAni.ConstructingPhrase1();
        StartCoroutine(DelayRun(1 /* totalTime / 2 */, () =>
        {
            if (mu.U.cfg.ReconstructFrom != null)
            {
                var go = root.Find(mu.U.UnitType);

                if (go != null)
                    go.gameObject.SetActive(true);

                var me = go.Find("me");
                if (me != null)
                    me.gameObject.SetActive(mu.IsMine);

                var notme = go.Find("notme");
                if (notme != null)
                    notme.gameObject.SetActive(!mu.IsMine);

                mu.RefreshColor();
            }
            else
                root.gameObject.SetActive(true);

            sfAni.ConstructingPhrase2();
        }));
    }

    public void ConstructingComplete()
    {
        var room = transform.Find("Root");
        room.gameObject.SetActive(true);
        Destroy(scaffold.gameObject);
        scaffold = null;
        scaffoldUID = null;

        mu.RefreshVision();
    }

    IEnumerator DelayRun(float waitingTime, Action fun)
    {
        yield return new WaitForSeconds(waitingTime);
        fun();
    }

    void ConstructingPhrase1()
    {
        if (animator != null)
            animator.SetInteger("state", constructingPhrase1);
    }

    void ConstructingPhrase2()
    {
        if (animator != null)
            animator.SetInteger("state", constructingPhrase2);
    }

    public void CancelConstructing()
    {
        if (animator != null)
            animator.SetInteger("state", idle);

        var root = transform.Find("Root");
        if (mu.U.cfg.ReconstructTo != null)
        {
            FC.ForEach(mu.U.cfg.ReconstructTo, (i, t) =>
            {
                var c = root.Find(t);
                if (c != null)
                    c.gameObject.SetActive(false);
            });
        }

        Destroy(scaffold.gameObject);
        scaffold = null;
        scaffoldUID = null;
    }

    public void ConstructingUnit()
    {
        if (animator != null)
            animator.SetInteger("state", constructingUnit);
    }

    public void ConstructingUnitComplete()
    {
        if (animator != null)
            animator.SetInteger("state", idle);
    }

    public void Run()
    {
        if (animator != null)
            animator.SetInteger("state", run);

        if (mu != null)
        {
            transform.rotation = Quaternion.Euler(0, -mu.U.Dir, 0);
            if (headTrans != null)
                headTrans.rotation = Quaternion.Euler(0, -(mu.U.Dir - 90), 0);
        }

        if (headAni != null)
            headAni.Run();
    }

    IEnumerator RunInNextFrame(Action f)
    {
        yield return new WaitForEndOfFrame();
        f();
    }

    public void AttackGround(Fix64 attackDir, Vec2 targetPos)
    {
        if (animator != null)
        {
            animator.SetInteger("state", attack01);
            StartCoroutine(RunInNextFrame(() => animator.SetInteger("state", attack01_coldown)));
        }

        if (headTrans == null)
        {
            transform.rotation = Quaternion.Euler(0, -(float)attackDir, 0);
            if (mu != null)
                mu.U.Dir = (int)attackDir; // it's tricky here but no better substitute now
        }

        if (headAni != null)
            headAni.AttackGround(attackDir - 90, targetPos);
    }

    public void AttackAir(Fix64 attackDir, Vec2 targetPos)
    {
        if (animator != null)
        {
            animator.SetInteger("state", attack02);
            StartCoroutine(RunInNextFrame(() => animator.SetInteger("state", attack02_coldown)));
        }

        if (headTrans == null)
        {
            transform.rotation = Quaternion.Euler(0, -(float)attackDir, 0);
            if (mu != null)
                mu.U.Dir = (int)attackDir; // it's tricky here but no better substitute yet
        }

        if (headAni != null)
            headAni.AttackAir(attackDir - 90, targetPos);
    }
}
