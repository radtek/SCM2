using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SCM;
using Swift;
using Swift.Math;
using UnityEngine.AI;

public class MapUnit : MonoBehaviour, IEventHandler
{
    public Transform Attack01Stub;
    public Transform Attack02Stub;

    public AnimationPlayer AniPlayer
    {
        get
        {
            if (aniPlayer == null)
                aniPlayer = GetComponentInChildren<AnimationPlayer>();

            return aniPlayer;
        }
    } AnimationPlayer aniPlayer;

    // 带视野
    public bool WithVision { get; set; }

    Transform root = null;
    public Transform Root { get { if (root == null) root = transform.Find("Root"); return root; } }

    // 模型预览
    public bool IsExampleUnit { get; set; }

    float nowModelDir = 0;
    Transform visionTransform;
    Transform attackRangeTransform;
    public Unit U
    {
        get { return u; }
        set
        {
            u = value;
            if (u != null)
            {
                var cfg = u.cfg;
                var sz = cfg.SizeRadius <= 0 ? 1 : cfg.SizeRadius;
                var s = (sz - 1) * 2 + 1;
                transform.localScale = Vector3.one * (float)s;

                IsMine = u.Player == GameCore.Instance.MePlayer;
                RefreshColor();

                nowModelDir = -(float)u.Dir;
                transform.localRotation = Quaternion.Euler(0, nowModelDir, 0);

                visionTransform = transform.Find("Vision");
                attackRangeTransform = transform.Find("AttackRange");
                RefreshVision();

                U.onDamage += OnDamage;
            }
        }
    } protected Unit u;

    public void RefreshColor()
    {
        var mrs = GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in mrs)
        {
            if (u.IsNeutral)
                mr.material.color = Color.green;
            else
                mr.material.color = IsMine ? Color.blue : Color.red;
        }
    }

    public void RefreshVision()
    {
        if (u != null && WithVision && U.cfg.VisionRadius > 0)
        {
            var cfg = u.cfg;
            visionTransform.gameObject.SetActive(true);
            var facV = (cfg.VisionRadius - 1) * 2 + 1;
            var facM = (cfg.SizeRadius - 1) * 2 + 1;
            visionTransform.localScale = Vector3.one * (float)(facV / facM);
            visionTransform.localPosition = u.cfg.IsAirUnit ? Vector3.up * 0.8f : Vector3.zero;
        }
        else if (visionTransform != null)
            visionTransform.gameObject.SetActive(false);

        if (u != null && attackRangeTransform != null)
        {
            var atr = u.cfg.AttackRange == null ? 0 : (u.cfg.AttackRange[0] > 0 ? u.cfg.AttackRange[0] : u.cfg.AttackRange[1]);
            if (atr > 0)
            {
                var s = ((atr - 1) * 2f + 1) / ((U.cfg.SizeRadius - 1) * 2f + 1) / 2f;
                attackRangeTransform.gameObject.SetActive(true);
                var lr = attackRangeTransform.GetComponent<LineRenderer>();
                var pts = new Vector3[32];
                for (var i = 0; i < pts.Length; i++)
                {
                    var arc = Math.PI * 2 * i / pts.Length;
                    pts[i] = new Vector3((float)Math.Cos(arc) * s, 0.1f, (float)Math.Sin(arc) * s);
                }
                lr.positionCount = pts.Length;
                lr.SetPositions(pts);
                attackRangeTransform.localPosition = Vector3.zero;
                attackRangeTransform.localRotation = Quaternion.identity;
                attackRangeTransform.localScale = Vector3.one;
            }
            else
                attackRangeTransform.gameObject.SetActive(false);
        }
    }

    public MapGround MG = null;

    // 添加描边效果
    public void AddOutLineEffect()
    {
        var skmrs = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var mr in skmrs)
        {
            mr.material.SetFloat("_Factor", 1.2f);
            mr.material.SetColor("_OutlineColor", Color.green);
        }

        var mrs = GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in mrs)
        {
            mr.material.SetFloat("_Factor", 1.2f);
            mr.material.SetColor("_OutlineColor", Color.green);
        }
    }

    // 是否玩家控制的单位
    public bool IsMine
    {
        get { return isMine; }
        set
        {
            isMine = value;

            // 光效模型，无需设置mat
            if (u.UnitType == "BioTechAOE"
                || u.UnitType == "BioTechShot"
                || u.UnitType == "VelTechRobot"
                || u.UnitType == "VelTechSeige")
                return;

            if (u.cfg.InVisible)
            {
                // 地雷
                if (u.cfg.AITypes != null && u.cfg.AITypes[0] == "Landmine")
                {
                    if (isMine)
                    {
                        SetNormalMat();
                        ChangeMatState(2);
                    }
                    else
                    {
                        SetInvisibleMat(0.0f);
                    }
                }
                else
                {
                    if (isMine)
                    {
                        SetNormalMat();
                        ChangeMatState(2);
                    }
                    else
                    {
                        SetInvisibleMat(2.0f);
                    }
                }
            }
            else
                SetNormalMat();
        }
    } bool isMine = false;

    private void OnDamage()
    {
        isInjured = true;
        elapseTime = 0f;
    }

    // 受创效果相关
    private float elapseTime = 0f;
    private bool isInjured = false;
    private float showTime = 0.1f;

    // 渐显受创泛白光效果
    private void ShowInjuredEffect()
    {
        var skMats1 = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var skMat in skMats1)
        {
            if (elapseTime > showTime)
            {
                isInjured = false;
                skMat.material.SetFloat("_SpecPower", 0f);
                continue;
            }

            skMat.material.SetFloat("_SpecPower", Mathf.Lerp(0f, 1f, elapseTime / showTime));
        }
    }

    private void SetExampleUnitMat()
    {
        Material mat = Resources.Load<Material>(@"Render\NormalUnit");
        mat.SetInt("_State", 2);
        var skmrs = GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var mr in skmrs)
        {
            var tex = mr.material.mainTexture;

            mr.material = mat;
            mr.material.mainTexture = tex;
            mr.material.SetFloat("_CellDensity", 1f);
        }

        var mrs = GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in mrs)
        {
            mr.material = mat;
            mr.material.SetFloat("_CellDensity", 1f);
            mr.material.SetColor("_MainTint", IsMine ? Color.blue : Color.red);
        }
    }

    // 是否是中立单位
    public bool IsNeutral { get { return u.Player == 0; } }
    public static float SpeedUp = 1;
    private void Update()
    {
        if (isInjured)
        {
            elapseTime += Time.deltaTime;
            ShowInjuredEffect();
        }

        if (u == null)
            return;

        if (IsExampleUnit)
            return;

        var dt = Time.deltaTime * SpeedUp;
        var toPos = u.Pos;
        var toDir = -(float)u.Dir;
        var fromPos = transform.localPosition;

        // 同步位置
        var v = u.cfg.MaxVelocity;
        if (v != 0)
        {
            var maxDist = v * dt;
            var nowModelPos = transform.localPosition;
            var toModelPos = new Vector3((float)toPos.x, transform.localPosition.y, (float)toPos.y);
            var modelPosDist = (toModelPos - nowModelPos).magnitude;
            if (maxDist < modelPosDist && modelPosDist < v * 0.5 /* 超过 0.5s 距离也直接追上 */)
                toModelPos = (float)(maxDist / modelPosDist) * (toModelPos - nowModelPos) + nowModelPos;

            if (transform.localPosition != toModelPos)
                AniPlayer.Run();

            transform.localPosition = toModelPos;
        }
        else if (u.cfg.MaxVelocity > 0)
            AniPlayer.Idle();

        // 地雷埋地下
        if (u.cfg.AITypes != null && u.cfg.AITypes[0] == "Landmine")
        {
            if (u.PreferredVelocity == Vec2.Zero)
            {
                var pos = gameObject.transform.localPosition;
                gameObject.transform.localPosition = new Vector3(pos.x, -1.0f, pos.z);
            }
            else
            {
                var pos = gameObject.transform.localPosition;
                gameObject.transform.localPosition = new Vector3(pos.x, 0.0f, pos.z);
            }
        }

        // 同步角度
        var dd = ((Fix64)(toDir - nowModelDir)).RangeIn180().Clamp(-720 * dt, 720 * dt);
        if (dd > float.Epsilon || dd < -float.Epsilon)
        {
            var me = GameCore.Instance.MePlayer;
            nowModelDir += (float)dd;
            transform.localRotation = Quaternion.Euler(0, nowModelDir, 0);
        }

        if (u.TrappedTimeLeft > 0)
            ChangeMatState(3);
        else if (!u.IsInvisible)
            ChangeMatState(1);

        if (u.cfg.InVisible && !u.IsInvisible)
            SetNormalMat();
    }

    private void ChangeMatState(int t)
    {
        var skMats = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var skMat in skMats)
            skMat.material.SetInt("_State", t);
    }

    public void SetNormalMat()
    {
        Shader shader = Resources.Load<Shader>(@"Render\NormalUnit");

        if (IsExampleUnit)
            SetExampleUnitMat();

        var skMats = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var skMat in skMats)
        {
            var modelName = skMat.material.mainTexture.name;
            var n = modelName.LastIndexOf("_");
            if (n < 0) // 纯中立单位没有 _ 命名
                continue;

            modelName = modelName.Substring(0, n);
            var ext = (isMine ? "_01" : (IsNeutral ? "_00" : "_02"));
            var texName = modelName + ext;

            // 因为之前就贴图路径命名和美术没有定好，所以这里有两种情况
            var tex = Resources.Load<Texture>(@"FBX\" + modelName + @"\Textures\" + texName);
            if (tex == null)
                tex = Resources.Load<Texture>(@"FBX\" + modelName + @"_01\Textures\" + texName);

            skMat.material.shader = shader;

            if (tex != null)
                skMat.material.mainTexture = tex;
        }
    }

    public void SetInvisibleMat(float effectVal)
    {
        var shader = Resources.Load<Shader>(@"Render\InvisibleUnit");
        var norMap = Resources.Load<Texture>(@"Render\NormalMap");

        var skMats = GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var skMat in skMats)
        {
            var tex = skMat.material.mainTexture;
            skMat.material.shader = shader;
            skMat.material.mainTexture = tex;
            skMat.material.SetTexture("_AnimatedNormalmapCloud", norMap);
            skMat.material.SetFloat("_EmissiveIntensity", effectVal);
        }
    }

    public virtual void PlayAnimation(string ani)
    {
        var a = GetComponent<Animator>();
        if (a != null)
            a.Play(ani);
    }

    public static bool inReplay = false;

    // 点击生产单位
    public virtual void OnClick(Vec2 pt, Vector3 wp)
    {
        if (inReplay)
            return;

        MG.OnClick(pt, wp);
    }

    // 长按选择升级
    public virtual void OnPress(Vec2 pt, Vector3 wp)
    {
    }

    public virtual void OnDoubleClick(Vec2 pt, Vector3 wp)
    {
    }

    public virtual void OnDragStarted(Vec2 pt, Vector3 wp)
    {
    }

    public virtual void OnDragging(Vec2 from, Vector3 fromWp, Vec2 nowPt, Vector3 nowWp)
    {
    }

    public virtual void DoDragEnded(Vec2 from, Vector3 fromWp, Vec2 to, Vector3 toWp)
    {
    }

    #region 编辑器内测试使用

    public virtual void OnRightClick(Vec2 pt, Vector3 wp)
    {
    }

    public virtual void OnRightDoubleClick(Vec2 pt, Vector3 wp)
    {
    }

    #endregion

    public void AddTip(string tip)
    {
        UIManager.Instance.Tips.AddTip(tip);
    }

    public void AddSmallTip(string tip)
    {
        UIManager.Instance.Tips.AddSmallTip(tip);
    }
}
