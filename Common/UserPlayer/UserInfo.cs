using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;

namespace SCM
{
    /// <summary>
    /// 用户信息
    /// </summary>
    public class UserInfo : SerializableData
    {
        // 胜场次数
        public int WinCount;

        // 败场次数
        public int LoseCount;

        // 名字
        public string Name;

        // 用户权限等级, 0 是普通用户
        public int AuthLv = 0;

        // 自己的录像
        public List<string> MyReplays = new List<string>();

        // 设备型号
        public string DeviceModel;

        // 变种关系
        public Dictionary<string, string> Variants = new Dictionary<string, string>();

        // 当前头像
        public string CurAvator;

        // 头像解锁
        public Dictionary<string, bool> Avatars = new Dictionary<string, bool>();

        // 单位解锁
        public Dictionary<string, bool> Units = new Dictionary<string, bool>();

        // 单位解锁触发条件(积分值)
        public Dictionary<int, bool> UUnlocks = new Dictionary<int, bool>();

        // 获得的总积分
        public int Integration;

        // 已花费积分
        public int IntegrationCost;

        // PVP场次
        public int PVPCount;

        public void AddMyReplay(string r)
        {
            MyReplays.Add(r);
            while (MyReplays.Count > 10)
                MyReplays.RemoveAt(0);
        }

        protected override void Sync()
        {
            BeginSync();
            SyncInt(ref WinCount);
            SyncInt(ref LoseCount);
            SyncString(ref Name);
            SyncInt(ref AuthLv);
            SyncListString(ref MyReplays);
            SyncString(ref DeviceModel);
            SyncDictSS(ref Variants);
            SyncString(ref CurAvator);
            SyncDictSB(ref Avatars);
            SyncDictSB(ref Units);
            SyncDictIB(ref UUnlocks);
            SyncInt(ref Integration);
            SyncInt(ref IntegrationCost);
            SyncInt(ref PVPCount);
            EndSync();
        }
    }
}
