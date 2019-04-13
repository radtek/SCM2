using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;
using System.Linq;

namespace SCM
{
    public class UserManager : Component
    {
        public static Action onSyncUnits2Server = null;
        public static Action onSyncUUnlocksFromCfg = null;

        // 初始化
        public override void Init()
        {
            UnitConfigUtil.OnBuildUnitCfgsFromServer += SyncUnitsFromCfg;

            onSyncUnits2Server += SyncUUnlocksFromCfg;
        }

        // 从配置表同步头像解锁信息
        public static void SyncAvatarsFromCfg()
        {
            var allAvas = AvatarConfiguration.Cfgs;

            var myAvas = GameCore.Instance.MeInfo.Avatars;

            for (int i = 0; i < allAvas.Count; i++)
            {
                if (!myAvas.ContainsKey(allAvas[i]))
                {
                    myAvas.Add(allAvas[i], false);
                }
            }

            SyncAvatars2Server();
        }

        // 同步服务器头像解锁信息
        public static void SyncAvatars2Server()
        {
            var info = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrAvatars");

            var aKeys = info.Avatars.Keys.ToArray();

            buff.Write(aKeys.Length);

            foreach (var a in aKeys)
            {
                buff.Write(a);
                buff.Write(info.Avatars[a]);
            }

            conn.End(buff);
        }

        // 同步服务器当前头像信息
        public static void SyncCurAvatar2Server()
        {
            var meInfo = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrCurAvatar");

            buff.Write(meInfo.CurAvator);

            conn.End(buff);
        }

        // 从配置表同步单位解锁信息
        public static void SyncUnitsFromCfg()
        {
            var meInfo = GameCore.Instance.MeInfo;
            var keys = UnitConfiguration.AllUnitTypes;

            for (int i = 0; i < keys.Length; i++)
            {
                if (!meInfo.Units.ContainsKey(keys[i]))
                {
                    meInfo.Units[keys[i]] = false;
                }
            }

            SyncUnits2Server();
        }

        // 同步服务器单位解锁信息
        public static void SyncUnits2Server()
        {
            var meInfo = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrUnits");

            var uKeys = meInfo.Units.Keys.ToArray();

            buff.Write(uKeys.Length);

            foreach (var u in uKeys)
            {
                buff.Write(u);
                buff.Write(meInfo.Units[u]);
            }

            conn.End(buff);

            if (onSyncUnits2Server != null)
                onSyncUnits2Server();
        }

        // 从配置表同步变种单位信息
        public static void SyncVariantsFromCfg()
        {
            var info = GameCore.Instance.MeInfo;
            var orgKeys = UnitConfiguration.AllOriginalUnitTypes;

            for (int i = 0; i < orgKeys.Length; i++)
            {
                if (!info.Variants.ContainsKey(orgKeys[i]))
                {
                    info.Variants[orgKeys[i]] = orgKeys[i];
                }
                else
                {
                    var vcfg = UnitConfiguration.GetDefaultConfig(info.Variants[orgKeys[i]]);

                    if ((null == vcfg) || (vcfg.OriginalType != orgKeys[i]))
                    {
                        info.Variants[orgKeys[i]] = orgKeys[i];
                    }
                }
            }

            SyncVariants2Server();
        }

        // 同步服务器变种单位信息
        public static void SyncVariants2Server()
        {
            var meInfo = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrVariants");

            var vkeys = meInfo.Variants.Keys.ToArray();

            buff.Write(vkeys.Length);

            foreach (var k in vkeys)
            {
                buff.Write(k);
                buff.Write(meInfo.Variants[k]);
            }

            conn.End(buff);
        }

        // 从配置表同步单位解锁条件
        public static void SyncUUnlocksFromCfg()
        {
            var uUlocks = GameCore.Instance.MeInfo.UUnlocks;
            var ulcfgs = UnitConfiguration.Ulcfgs;

            for (int i = 0; i < ulcfgs.Count; i++)
            {
                if (!uUlocks.ContainsKey(ulcfgs[i]))
                {
                    uUlocks.Add(ulcfgs[i], false);
                }
            }

            SyncUUnlocks2Server();
        }

        // 同步服务器单位解锁条件
        public static void SyncUUnlocks2Server()
        {
            var info = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrUUlocks");

            var uKeys = info.UUnlocks.Keys.ToArray();

            buff.Write(uKeys.Length);

            foreach (var a in uKeys)
            {
                buff.Write(a);
                buff.Write(info.UUnlocks[a]);
            }

            conn.End(buff);
        }

        // 概率解锁一个随机头像
        public static string UnlockOneAvatarAtRandom()
        {
            //成功概率100% 区间(0-99)
            var successRate = 9;

            var num = new Random().Next(100);

            if (num > successRate)
                return null;

            // 构造未解锁的头像列表
            var info = GameCore.Instance.MeInfo;
            var aKeys = info.Avatars.Keys.ToArray();

            List<string> lst = new List<string>();

            foreach (var a in aKeys)
            {
                if (!info.Avatars[a])
                    lst.Add(a);
            }

            if (lst.Count == 0)
                return null;

            var index = new Random().Next(0, lst.Count - 1);

            info.Avatars[lst[index]] = true;
            SyncAvatars2Server();

            return lst[index];
        }

        // 解锁单位
        public static bool UnlockUnit(string type)
        {
            var meInfo = GameCore.Instance.MeInfo;
            var ulcfgs = UnitConfiguration.Ulcfgs;

            for (int i = 0; i < ulcfgs.Count; i++)
            {
                if (!meInfo.UUnlocks[ulcfgs[i]])
                {
                    var left = meInfo.Integration - meInfo.IntegrationCost;
                    var need = ulcfgs[i] - ulcfgs[i - 1];

                    if (left >= need)
                    {
                        // 足额解锁成功
                        meInfo.Units[type] = true;
                        meInfo.UUnlocks[ulcfgs[i]] = true;

                        SyncUnits2Server();
                        SyncUUnlocks2Server();

                        return true;
                    }
                    else
                        return false;
                }
            }

            return false;
        }

        // 尝试触发单位解锁
        public static string TryTriggerUUnlock()
        {
            var meInfo = GameCore.Instance.MeInfo;
            var ulcfgs = UnitConfiguration.Ulcfgs;
            var cfgs = UnitConfiguration.AllUnitTypes;

            // 构造未解锁单位列表
            var lst = new List<string>();

            foreach (var k in cfgs)
            {
                if (!meInfo.Units[k])
                    lst.Add(k);
            }

            if (lst.Count == 0)
                return null;

            for (int i = 0; i < ulcfgs.Count; i++)
            {
                if (meInfo.Integration >= ulcfgs[i] && !meInfo.UUnlocks[ulcfgs[i]])
                {
                    var index = new Random().Next(0, lst.Count - 1);

                    meInfo.Units[lst[index]] = true;
                    meInfo.UUnlocks[ulcfgs[i]] = true;

                    SyncUnits2Server();
                    SyncUUnlocks2Server();
                    return lst[index];
                }
            }

            return null;
        }

        // 同步服务器名字信息
        public static void SyncName2Server()
        {
            var meInfo = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrName");
            buff.Write(meInfo.Name);
            conn.End(buff);
        }

        // 同步服务器积分值
        public static void SyncIntegration2Server()
        {
            var meInfo = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrIntegration");
            buff.Write(meInfo.Integration);
            conn.End(buff);
        }

        // 同步服务器已花费积分值
        public static void SyncIntegrationCost2Server()
        {
            var meInfo = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrIntegrationCost");
            buff.Write(meInfo.IntegrationCost);
            conn.End(buff);
        }
    }
}