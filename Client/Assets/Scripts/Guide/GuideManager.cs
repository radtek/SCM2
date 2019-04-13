using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swift;
using SCM;
using System.Collections;
using Swift.Math;

/// <summary>
/// 客户端指引用
/// </summary>
public class GuideManager : Component
{
    static GuideManager()
    {
        Room4Client.OnBattleBegin += (r, isReplay) =>
        {
            if (isReplay)
                return;

            CreateGuide(r, "Guide_" + r.Lv.LevelID, r.Lv.LevelID);
        };

        Room4Client.OnBattleEnd += (r, winner, isReplay) =>
        {
            Clear();
        };

        GameCore.Instance.OnMainConnectionDisconnected += (conn, reason) =>
        {
            Clear();
        };
    }

    static void Clear()
    {
        var cm = GameCore.Instance.Get<CoroutineManager>();
        foreach (var g in gs.ValueArray)
            cm.Stop(g);

        gs.Clear();
        UIManager.Instance.Guide.HideAllHints();
    }

    static StableDictionary<string, ICoroutine> gs = new StableDictionary<string, ICoroutine>();
    public static void CreateGuide(Room4Client r, string id, string type)
    {
        switch (type)
        {
            case "PVE001":
                // gs[type] = GameCore.Instance.Get<CoroutineManager>().Start(PVE001GuideImpl(r));
                break;
        }
    }

    // PVE001 基本建造教学
    static IEnumerator PVE001GuideImpl(Room4Client r)
    {
        var g = UIManager.Instance.Guide;


        var cc = r.GetMyFirstUnitByType("CommanderCenter");

        // 提示建造矿机
        yield return new TimeWaiter(1000);
        g.ToClick(UIManager.Instance.World2UI(cc.Pos), "建造<u>矿机</u>可以加速资源生产");
        yield return new ConditionWaiter(() => r.GetMyFirstUnitByType("CCAcc") != null);
        g.HideAllHints();
        yield return new ConditionWaiter(() => r.GetMyFirstUnitByType("CCAcc").BuildingCompleted);
        UIManager.Instance.Tips.AddTip("矿机建造完成");
        g.ToClick(UIManager.Instance.World2UI(cc.Pos), "试试补充更多<u>矿机</u>");
        yield return new ConditionWaiter(() => r.GetMyUnitsByType("CCAcc").Length >= 2);
        g.HideAllHints();

        // 提示建造兵营
        yield return new ConditionWaiter(() => r.GetMyResource("Money") >= UnitConfiguration.GetDefaultConfig("Barrack").Cost);
        g.HideAllHints();
        g.ToPress(UIManager.Instance.World2UI(cc.Pos + new Vec2(-80, -50)), "<op>长按</op>:建造<u>兵营</u>");
        // 等待开始建造兵营
        yield return new ConditionWaiter(() => r.GetMyFirstUnitByType("Barrack") != null);
        g.HideAllHints();
        yield return new ConditionWaiter(() => r.GetMyFirstUnitByType("Barrack").BuildingCompleted
            && r.GetMyResource("Money") >= UnitConfiguration.GetDefaultConfig("Soldier").Cost);
        g.ToPress(UIManager.Instance.World2UI(cc.Pos) - new Vec2(75, 135), "选择:<u>枪兵</u>");
        yield return new TimeWaiter(3000);
        g.ToPress(UIManager.Instance.World2UI(cc.Pos) + new Vec2(0, 200), "<op>单击</op>地面释放<u>枪兵</u>");
        yield return new TimeWaiter(1000);
        yield return new ConditionWaiter(() => r.GetMyFirstUnitByType("Soldier") != null);
        g.HideAllHints();

        // 提示使用雷达
        yield return new ConditionWaiter(() => r.GetMyResource("Money") >= UnitConfiguration.GetDefaultConfig("RadarSign").Cost);
        g.ToDrag("从<u>基地</u>拖出释放<u>雷达</u>",
            UIManager.Instance.World2UI(cc.Pos),
            UIManager.Instance.World2UI(r.MapSize - cc.Pos));
        yield return new ConditionWaiter(() => r.GetMyFirstUnitByType("RadarSign") != null);
        g.HideAllHints();

        UIManager.Instance.Tips.AddTip("目标：建造更多部队，摧毁对方基地");
    }

    // PVE001 基本建造教学
    static IEnumerator PVE999GuideImpl(Room4Client r)
    {
        var g = UIManager.Instance.Guide;

        yield return new TimeWaiter(1000);

        var cc = r.GetMyFirstUnitByType("CommanderCenter");
        g.ToClick(UIManager.Instance.World2UI(cc.Pos), "帝国部队正在攻击我们的<u>基地</u>");
        yield return new TimeWaiter(2000);

        // 提示保护基地
        var sds = r.GetUnitsInArea(cc.Pos, 200, (u) => u.Player == 1);
        if (sds.Length > 0)
        {
            var enemy = sds[0];
            g.ToClick(UIManager.Instance.World2UI(enemy.Pos), "<op>点击</op>移动不对，消灭帝国士兵");
            yield return new TimeWaiter(2000);
            // 等待移动指令
            yield return new ConditionWaiter(() => r.GetMyFirstUnitByType("Soldier").MovePath.Count > 0);
            g.HideAllHints();

            // 等到枪兵被消灭
            yield return new ConditionWaiter(() => enemy.Hp <= 0);
        }

        yield return new TimeWaiter(1000);

        // 提示建造兵营
        g.ToPress(UIManager.Instance.World2UI(cc.Pos + new Vec2(-80, -50)), "<op>长按</op>:建造<u>兵营</u>，生产更多士兵");
        // 等待开始建造兵营
        yield return new ConditionWaiter(() => r.GetMyFirstUnitByType("Barrack") != null);
        g.HideAllHints();

        // 提示建造矿机
        yield return new TimeWaiter(2000);
        g.ToClick(UIManager.Instance.World2UI(cc.Pos), "建造<u>矿机</u>可以加速资源生产");
        yield return new ConditionWaiter(() => r.GetMyFirstUnitByType("CCAcc") != null);
        g.HideAllHints();

        // 等待兵营建造完成
        yield return new ConditionWaiter(() => r.GetMyFirstUnitByType("Barrack") != null &&
            r.GetMyFirstUnitByType("Barrack").BuildingCompleted);

        // 提示建造枪兵
        var brk = r.GetMyFirstUnitByType("Barrack");
        g.ToClick(UIManager.Instance.World2UI(brk.Pos), "从兵营中生产更多士兵");
        // 等待建造队列
        yield return new ConditionWaiter(() => brk.UnitCosntructingWaitingList.Count > 0);
        g.HideAllHints();

        var sdCnt = r.GetAllMyUnits((u) => !u.cfg.IsBuilding).Length;
        yield return new ConditionWaiter(() => r.GetAllMyUnits((u) => !u.cfg.IsBuilding).Length > sdCnt);
        g.ToClick(UIManager.Instance.World2UI(brk.Pos), "建造更多士兵");
        yield return new TimeWaiter(2000);
        g.HideAllHints();

        // 等待 5 个任意战斗单位
        yield return new ConditionWaiter(() => r.GetAllMyUnits((u) => !u.cfg.IsBuilding).Length >= 5);
        var cc1Arr = r.GetAllUnitsByPlayer(1, (u) => u.UnitType == "CommanderCenter");
        if (cc1Arr.Length > 0)
        {
            var cc1 = cc1Arr[0];

            // 提示攻击对方基地
            g.ToClick(UIManager.Instance.World2UI(cc1.Pos), "拆毁对方<u>基地</u>，获取战斗胜利");
            var sd = r.GetAllMyUnits((u) => !u.cfg.IsBuilding)[0];
            yield return new TimeWaiter(2000);
            // 等待用户指挥进攻
            yield return new ConditionWaiter(() => sd.MovePath.Count > 0 &&
                (sd.MovePath[sd.MovePath.Count - 1] - cc1.Pos).Length < 100);
            g.HideAllHints();
        }
    }
}
