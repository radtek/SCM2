using System;
using System.Collections.Generic;
using Swift;
using SCM;

namespace Server
{
    /// <summary>
    /// 用户管理器
    /// </summary>
    public class UserManager : Component
    {
        UserPort UP;

        public override void Init()
        {
            UP = GetCom<UserPort>();

            UP.OnMessage("ModifyUsrName", OnModifyUsrName);
            UP.OnMessage("ModifyUsrIntegration", OnModifyUsrIntegration);
            UP.OnMessage("ModifyUsrIntegrationCost", OnModifyUsrIntegrationCost);
            UP.OnMessage("ModifyUsrVariants", OnModifyUsrVariants);
            UP.OnMessage("ModifyUsrUUlocks", OnModifyUsrUUlocks);
            UP.OnMessage("ModifyUsrAvatars", OnModifyUsrAvatars);
            UP.OnMessage("ModifyUsrCurAvatar", OnModifyUsrCurAvatar);
            UP.OnMessage("ModifyUsrUnits", OnModifyUsrUnits);
        }

        public static void SetDefaultInfo(User usr)
        {
            usr.Info.Name = "[NoName]";
            usr.Info.Avatars.Add("Default", true);
            usr.Info.CurAvator = "Default";

            var originUnits = UnitConfiguration.AllOriginalUnitTypes;

            foreach (var type in originUnits)
            {
                usr.Info.Units[type] = true;
            }
            usr.Info.Units["SoldierWithDog"] = true;
            usr.Info.Units["FirebatAOE"] = true;
            usr.Info.Units["FireGuardAir"] = true;
        }

        void OnModifyUsrName(Session s, IReadableBuffer data)
        {
            s.Usr.Info.Name = data.ReadString();
            s.Usr.Update();
        }

        void OnModifyUsrIntegration(Session s, IReadableBuffer data)
        {
            s.Usr.Info.Integration = data.ReadInt();
            s.Usr.Update();
        }

        void OnModifyUsrIntegrationCost(Session s, IReadableBuffer data)
        {
            s.Usr.Info.IntegrationCost = data.ReadInt();
            s.Usr.Update();
        }

        void OnModifyUsrVariants(Session s, IReadableBuffer data)
        {
            var dic = new Dictionary<string, string>();

            var cnt = data.ReadInt();

            for (int i = 0; i < cnt; i++)
            {
                var key = data.ReadString();
                var val = data.ReadString();

                dic[key] = val;
            }

            s.Usr.Info.Variants = dic;
            s.Usr.Update();
        }

        void OnModifyUsrUUlocks(Session s, IReadableBuffer data)
        {
            var dic = new Dictionary<int, bool>();

            var cnt = data.ReadInt();

            for (int i = 0; i < cnt; i++)
            {
                var key = data.ReadInt();
                var val = data.ReadBool();

                dic[key] = val;
            }

            s.Usr.Info.UUnlocks = dic;
            s.Usr.Update();
        }

        void OnModifyUsrAvatars(Session s, IReadableBuffer data)
        {
            var dic = new Dictionary<string, bool>();

            var cnt = data.ReadInt();

            for (int i = 0; i < cnt; i++)
            {
                var key = data.ReadString();
                var val = data.ReadBool();

                dic[key] = val;
            }

            s.Usr.Info.Avatars = dic;
            s.Usr.Update();
        }

        void OnModifyUsrCurAvatar(Session s, IReadableBuffer data)
        {
            s.Usr.Info.CurAvator = data.ReadString();
            s.Usr.Update();
        }

        void OnModifyUsrUnits(Session s, IReadableBuffer data)
        {
            var dic = new Dictionary<string, bool>();

            var cnt = data.ReadInt();

            for (int i = 0; i < cnt; i++)
            {
                var key = data.ReadString();
                var val = data.ReadBool();

                dic[key] = val;
            }

            s.Usr.Info.Units = dic;
            s.Usr.Update();
        }
    }
}
