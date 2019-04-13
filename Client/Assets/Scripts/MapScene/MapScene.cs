using System;
using System.Collections.Generic;
using UnityEngine;
using Swift;
using SCM;
using Swift.Math;
using UnityEngine.AI;
using System.Collections;

public class MapScene : SCMBehaviour {

    public UnitCreator UC;
    public AudioCreator AC;
    public EffectCreator EC;
    public Transform Units;
    public MapGround MG;
    public IndicatorUILayer IndLayer;

    Room4Client room = new Room4Client();
    public Room4Client ClientRoom { get { return room; } }

    protected override void StartOnlyOneTime()
    {
        room.Init();
        Room4Client.OnBeforeBattleBegin += OnBeforeBattleBegin;
        Room4Client.OnBattleBegin += OnBattleBegin;
        Room4Client.NotifyAddBattleUnit += OnAddBattleUnit;
        Room4Client.NotifyAddBuildingUnit += OnAddBuildingUnit;
        Room4Client.NotifyReconstructUnit += OnReconstructUnit;
        Room4Client.OnConstructingCompleted += OnConstructingCompleted;
        Room4Client.OnConstructingCanceled += OnConstructingCanceled;
        Room4Client.NotifyUnitRemoved += OnUnitRemoved;
        Room4Client.OnDoAttack += OnDoAttack;
        Room4Client.OnResourceProduced += OnResourceProduced;

        Room4Client.OnUsrsSwitched += OnUsrsSwitched;
    }

    private void OnConstructingCanceled(Unit u)
    {
        var mu = UC.GetModel(u.UID);
        mu.AniPlayer.CancelConstructing(); // 用建造单位动画表示资源生产
        IndLayer.DestroyProgressbar(u.UID);
    }

    private void OnResourceProduced(Unit u, string resType, Fix64 num)
    {
        if (resType != "Money")
            return;

        var mu = UC.GetModel(u.UID);
        if (num > 0)
            mu.AniPlayer.ConstructingUnit(); // 用建造单位动画表示资源生产
        else
            mu.AniPlayer.Idle();
    }

    private void OnUsrsSwitched()
    {
        RefreshVisions();
    }

    void RefreshVisions()
    {
        var me = GameCore.Instance.MePlayer;
        var mus = UC.AllModels();
        foreach (var mu in mus)
        {
            if (mu.U == null)
                continue;

            mu.IsMine = mu.U.Player == me;
            mu.WithVision = mu.U.cfg.VisionRadius > 0 && (mu.IsMine || mu.IsNeutral);
            mu.RefreshVision();
            mu.RefreshColor();
        }
    }

    private void OnDoAttack(Unit attacker, Unit target)
    {
        var attackDir = (target.Pos - attacker.Pos).Dir();
        string attack12 = null;
        Transform attackStub = null;
        var m = UC.GetModel(attacker.UID);
        var attackInterval = 0.0f;
        if (target.cfg.IsAirUnit)
        {
            attack12 = "Attack02";
            attackStub = m.Attack02Stub == null ? m.Attack01Stub : m.Attack02Stub;
            attackInterval = (float)attacker.cfg.AttackInterval[1];
            if (m.AniPlayer != null)
                m.AniPlayer.AttackAir(attackDir, target.Pos);
        }
        else
        {
            attack12 = "Attack01";
            attackStub = m.Attack01Stub;
            attackInterval = (float)attacker.cfg.AttackInterval[0];
            if (m.AniPlayer != null)
                m.AniPlayer.AttackGround(attackDir, target.Pos);
        }

        attackStub = attackStub == null ? m.Root : attackStub;
        var muTo = UC.GetModel(target.UID);
        if (muTo != null)
        {
            EC.CreateEffect(attacker.UnitType + attack12, attackStub,
                muTo.Root.position + (muTo.U.cfg.IsAirUnit ? Vector3.up * ((muTo.U.cfg.SizeRadius - 1) * 2 + 1) : Vector3.zero), 
                attackInterval);

            AC.CreateAudio(attacker.UnitType + attack12, attackStub, attackInterval);
        }
    }

    private void OnUnitRemoved(Unit u)
    {
        var uid = u.UID;
        IndLayer.DestroyBloodbar(uid);
        IndLayer.DestroyProgressbar(uid);
        IndLayer.DestroyWaitingNum(u.UID);

        // 部分单位立即移除，其它单位都是延迟移除，以便播放死亡动画
        if (u.cfg.IsBuilding && !u.BuildingCompleted)
            UC.RemoveModel(uid, 0);
        else
        {
            var mu = UC.GetModel(uid);
            if (mu != null && mu.AniPlayer != null)
                mu.AniPlayer.Die();

            UC.RemoveModel(uid, 1);
        }

        if (u.UnitType == "TreasureBox")
        {
            var type = u.Tag as string;
            UIManager.Instance.Tips.AddSmallTip(TreasureBoxRunner.GetDisplayName(type), u.Pos);
        }
    }

    private void OnStartConstructingBattleUnit(Unit u, string genType)
    {
        var m = UC.GetModel(u.UID);
        m.AniPlayer.ConstructingUnit();

        var vcfg = UnitConfiguration.GetDefaultConfig(genType);

        var constructingTime = vcfg.ConstructingTime;
        
        IndLayer.CreateProgressbar(u, constructingTime).FollowTransformRoot = m.transform;
    }

    private void OnConstructingWaitingListChanged(Unit u, string genType)
    {
        var wn = u.UnitCosntructingWaitingList.Count;
        if (wn > 1)
            IndLayer.CreateWaitingNumber(u, wn - 1);
        else
            IndLayer.DestroyWaitingNum(u.UID);
    }

    private void OnConstructingCompleted(Unit u)
    {
        var m = UC.GetModel(u.UID);
        m.U = u;
        m.AniPlayer.ConstructingComplete();
        IndLayer.DestroyProgressbar(u.UID);
    }

    public void Clear()
    {
        UC.DestroyAll();
        MG.Clear();
        EC.Clear();
        AC.Clear();
    }

    private void OnBeforeBattleBegin(string usr1, string usr2, Vec2 mapSize, bool inReplay)
    {
        Clear();
    }

    private void OnBattleBegin(Room4Client r, bool inReplay)
    {
    }

    private void OnAddBattleUnit(Unit building, Unit u)
    {
        if (building != null)
        {
            IndLayer.DestroyWaitingNum(building.UID);
            IndLayer.DestroyProgressbar(building.UID); // 可能是建造单位过程中显示的进度指示，这时可以去掉了
            UC.GetModel(building.UID).AniPlayer.ConstructingUnitComplete();
        }

        var m = CreateUnitModel(u);

        if (!u.cfg.UnAttackable)
            IndLayer.CreateBloodbar(u).FollowTransformRoot = m.Root;

        AC.CreateUnitCreateAudio(u.UnitType + "Create", m.Root);
    }

    private void OnReconstructUnit(Unit u, string fromType)
    {
        if (u.BuildingCompleted)
            return;

        var m = UC.GetModel(u.UID);
        if (m.AniPlayer != null)
            m.AniPlayer.Construcing(fromType, (float)u.cfg.ConstructingTime);

        IndLayer.CreateProgressbar(u, u.cfg.ConstructingTime).FollowTransformRoot = m.transform;
    }

    private void OnAddBuildingUnit(Unit u)
    {
        var m = CreateUnitModel(u);

        if (!u.cfg.UnAttackable)
            IndLayer.CreateBloodbar(u).FollowTransformRoot = m.Root;

        if (u.BuildingCompleted)
            return;

        if (m.AniPlayer != null)
            m.AniPlayer.Construcing(null, (float)u.cfg.ConstructingTime);

        IndLayer.CreateProgressbar(u, u.cfg.ConstructingTime).FollowTransformRoot = m.transform;
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        EC.OnTimeElapsed(dt);
        AC.OnTimeElapsed(dt);
    }

    // 创建指定类型的单位模型
    MapUnit CreateUnitModel(Unit u)
    {
        var um = UC.CreateModel(u);
        um.transform.SetParent(Units, false);
        um.transform.localPosition = new Vector3((float)u.Pos.x, 0, (float)u.Pos.y);
        um.transform.localRotation = Quaternion.Euler(0, (float)u.Dir, 0);
        um.U = u;
        um.IsMine = GameCore.Instance.MePlayer == u.Player;
        um.MG = MG;
        um.gameObject.SetActive(true);

        return um;
    }
}
