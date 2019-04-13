using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Swift;
using Swift.Math;
using System.Data;
using System.Data.Common;
using System.Data.Sql;
using System.Text;
using MySql;
using MySql.Data;
using MySql.Data.MySqlClient;
using SCM;

namespace Server
{
    class DataAnalysisMgr : Component
    {
        ConsoleInputDataAnalysis cida;
        DataAnalysisContainer dac;
        string DbName = "scm_da";

        public override void Init()
        {
            cida = GetCom<ConsoleInputDataAnalysis>();
            dac = GetCom<DataAnalysisContainer>();

            //cida.OnCommand("create", (ps) =>
            //{
            //    if (ps == null || ps.Length != 2)
            //        return "parameter error";

            //    if (ps[0] == "db")
            //    {
            //        if (dac != null)
            //            return "only a new database can be create";

            //        CreateDatabase(DbName);
            //    }
            //    else
            //        return "parameter error";

            //    return "success!";
            //});

            cida.OnCommand("update", (ps) =>
            {
                if (dac == null)
                    return "you have to create a new database and then table will automatically generate";

                UpdateTable();
                return "success!";
            });

            cida.OnCommand("cusers", (ps) =>
            {
                var cnt = CreateUserTable();
                return cnt + " users";
            });

            //cida.OnCommand("drop", (ps) =>
            //{
            //    if (ps == null || ps.Length != 2)
            //        return "parameter error";

            //    if (ps[0] == "db")
            //    {
            //        DropDatabase(ps[1]);
            //    }
            //    else
            //        return "parameter error";

            //    return "success!";
            //});

            CreateTable();
        }

        public void CreateDatabase(string dbName)
        {
            MySqlConnection conn = new MySqlConnection("Data Source=localhost;Persist Security Info=yes;UserId=root; PWD=123456;");
            MySqlCommand cmd = new MySqlCommand(string.Format("CREATE DATABASE {0};", dbName), conn);

            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();

            DbName = dbName;
        }

        public void DropDatabase(string dbName)
        {
            MySqlConnection conn = new MySqlConnection("Data Source=localhost;Persist Security Info=yes;UserId=root; PWD=123456;");
            MySqlCommand cmd = new MySqlCommand(string.Format("DROP DATABASE {0};", dbName), conn);

            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void CreateTable()
        {
            dac = new DataAnalysisContainer(new MySqlDbPersistence<DataAnalysis, string>(
                DbName, "127.0.0.1", "root", "123456",
                @"Replays", "CREATE TABLE Replays(ID VARCHAR(100) BINARY, Data MediumBlob,"
                + "Date DATETIME, Length VARCHAR(20),"
                + "User1 VARCHAR(100), User2 VARCHAR(100),"
                + "UserName1 VARCHAR(100), UserName2 VARCHAR(100),"
                + "Winner VARCHAR(100),"
                + "DogCount1 INT, DogCount2 INT,"
                + "SoldierCount1 INT, SoldierCount2 INT,"
                + "FirebatCount1 INT, FirebatCount2 INT,"
                + "MagSpiderCount1 INT, MagSpiderCount2 INT,"
                + "GhostCount1 INT, GhostCount2 INT,"
                + "RobotCount1 INT, RobotCount2 INT,"
                + "TankCount1 INT, TankCount2 INT,"
                + "ThorCount1 INT, ThorCount2 INT,"
                + "HammerCount1 INT, HammerCount2 INT,"
                + "SoldierCarrierCount1 INT, SoldierCarrierCount2 INT,"
                + "WarplaneCount1 INT, WarplaneCount2 INT,"
                + "MotherShipCount1 INT, MotherShipCount2 INT,"
                + "PRIMARY KEY(ID ASC));",
                 new string[] 
                 { 
                     "Date", "Length",
                     "User1", "User2",
                     "UserName1", "UserName2",
                     "Winner",
                     "DogCount1", "DogCount2",
                     "SoldierCount1", "SoldierCount2", 
                     "FirebatCount1", "FirebatCount2", 
                     "MagSpiderCount1", "MagSpiderCount2",
                     "GhostCount1", "GhostCount2",
                     "RobotCount1", "RobotCount2",
                     "TankCount1", "TankCount2",
                     "ThorCount1", "ThorCount2",
                     "HammerCount1", "HammerCount2",
                     "SoldierCarrierCount1", "SoldierCarrierCount2",
                     "WarplaneCount1", "WarplaneCount2",
                     "MotherShipCount1", "MotherShipCount2",
                 }, (da) =>
                 {
                     var buff = new WriteBuffer();
                     da.Serialize(buff);
                     return buff.Data;
                 }, (data) =>
                 {
                     var rb = new RingBuffer(data);
                     var da = new DataAnalysis();
                     da.Deserialize(rb);
                     return da;
                 }, (DataAnalysis da, string col) =>
                 {
                     switch (col)
                     {
                         case "Date":
                             return da.Info.Date;
                         case "Length":
                             return da.Info.Length;
                         case "User1":
                             return da.Info.User1;
                         case "User2":
                             return da.Info.User2;
                         case "UserName1":
                             return da.Info.UserName1;
                         case "UserName2":
                             return da.Info.UserName2;
                         case "Winner":
                             return da.Info.Winner;
                         case "DogCount1":
                             return da.Info.DogCount1;
                         case "DogCount2":
                             return da.Info.DogCount2;
                         case "SoldierCount1":
                             return da.Info.SoldierCount1;
                         case "SoldierCount2":
                             return da.Info.SoldierCount2;
                         case "FirebatCount1":
                             return da.Info.FirebatCount1;
                         case "FirebatCount2":
                             return da.Info.FirebatCount2;
                         case "MagSpiderCount1":
                             return da.Info.MagSpiderCount1;
                         case "MagSpiderCount2":
                             return da.Info.MagSpiderCount2;
                         case "GhostCount1":
                             return da.Info.GhostCount1;
                         case "GhostCount2":
                             return da.Info.GhostCount2;
                         case "RobotCount1":
                             return da.Info.RobotCount1;
                         case "RobotCount2":
                             return da.Info.RobotCount2;
                         case "TankCount1":
                             return da.Info.TankCount1;
                         case "TankCount2":
                             return da.Info.TankCount2;
                         case "ThorCount1":
                             return da.Info.ThorCount1;
                         case "ThorCount2":
                             return da.Info.ThorCount2;
                         case "HammerCount1":
                             return da.Info.HammerCount1;
                         case "HammerCount2":
                             return da.Info.HammerCount2;
                         case "SoldierCarrierCount1":
                             return da.Info.SoldierCarrierCount1;
                         case "SoldierCarrierCount2":
                             return da.Info.SoldierCarrierCount2;
                         case "WarplaneCount1":
                             return da.Info.WarplaneCount1;
                         case "WarplaneCount2":
                             return da.Info.WarplaneCount2;
                         case "MotherShipCount1":
                             return da.Info.MotherShipCount1;
                         case "MotherShipCount2":
                             return da.Info.MotherShipCount2;
                     }
                     return null;
                 }));
            cida.Srv.Add("DataAnalysisContainer", dac);
        }

        public void UpdateTable()
        {
            Room4Server.LoadAllPVPReplays();

            var replayKeys = Room4Server.AllReplayTitles;

            if (replayKeys == null || replayKeys.Length == 0)
                return;

            for (int i = 0; i < replayKeys.Length; i++)
            {
                var r = Room4Server.GetReplay(replayKeys[i]);

                var da = new DataAnalysis();

                DeserializeReplay(da, r);

                dac.Retrieve(da.ID, (data) =>
                {
                    var isNew = data == null;

                    if (isNew)
                    {
                        dac.AddNew(da);
                        dac.Close();
                    }
                });
            }
        }

        public int CreateUserTable()
        {
            var p = new MySqlDbPersistence<DataAnalysisUser, string>(
                DbName, "127.0.0.1", "root", "123456",
                @"UserCount", "CREATE TABLE UserCount(ID VARCHAR(100) BINARY, Data MediumBlob, Count INT,"
                + "PRIMARY KEY(ID ASC));",
                 new string[]
                 {
                     "Count",
                 }, (da) =>
                 {
                     var buff = new WriteBuffer();
                     da.Serialize(buff);
                     return buff.Data;
                 }, (data) =>
                 {
                     var rb = new RingBuffer(data);
                     var da = new DataAnalysisUser();
                     da.Deserialize(rb);
                     return da;
                 }, (DataAnalysisUser da, string col) =>
                 {
                     switch (col)
                     {
                         case "Count":
                             return da.Info.Count;
                     }
                     return null;
                 });

            var userCnt = new Dictionary<string, int>();
            dac.P.LoadAll((daArr) =>
            {
                foreach (var da in daArr)
                {
                    var usr1 = da.Info.User1;
                    var usr2 = da.Info.User2;

                    if (!userCnt.ContainsKey(usr1))
                        userCnt[usr1] = 1;
                    else
                        userCnt[usr1]++;

                    if (!userCnt.ContainsKey(usr2))
                        userCnt[usr2] = 1;
                    else
                        userCnt[usr2]++;
                }
            });
            dac.P.Wait4CompoleteAll();

            foreach (var usr in userCnt.Keys)
            {
                var dau = new DataAnalysisUser();
                dau.ID = usr;
                dau.Info.Id = usr;
                dau.Info.Count = userCnt[usr];
                p.AddNew(dau);
            }
            p.Wait4CompoleteAll();

            return userCnt.Count;
        }

        public void DeserializeReplay(DataAnalysis da, BattleReplay r)
        {
            da.ID = r.ID;

            var info = new DataAnalysisInfo();
            info.User1 = r.Usr1;
            info.User2 = r.Usr2;
            info.UserName1 = r.UsrName1;
            info.UserName2 = r.UsrName2;

            info.Date = r.Date;

            var secs = r.Length * 100 / 1000;
            var min = secs / 60;
            secs = secs - min * 60;
            var time = (min.ToString().PadLeft(2, '0')) + ":" + (secs.ToString().PadLeft(2, '0'));
            info.Length = time;

            foreach (var para in r.Msgs)
            {
                if (para.Value.Available <= 0)
                    continue;

                if (para.Key == "BattleEnd")
                {
                    info.Winner = para.Value.ReadString();
                }
                else if (para.Key == "DropSoldierFromCarrier")
                {
                    var player = para.Value.ReadInt();
                    if (player == 1)
                        info.SoldierCarrierCount1++;
                    else if (player == 2)
                        info.SoldierCarrierCount2++;
                }
                else if (para.Key == "AddBattleUnitAt")
                    DeserializeReplayMag(info, para.Value);
            }

            da.Info = info;
        }

        private void DeserializeReplayMag(DataAnalysisInfo info, IReadableBuffer buff)
        {
            var player = buff.ReadInt();
            var type = buff.ReadString();

            switch (type)
            {
                case "Dog":
                    if (player == 1)
                        info.DogCount1++;
                    else if (player == 2)
                        info.DogCount2++;
                    break;
                case "Soldier":
                    if (player == 1)
                        info.SoldierCount1++;
                    else if (player == 2)
                        info.SoldierCount2++;
                    break;
                case "Firebat":
                    if (player == 1)
                        info.FirebatCount1++;
                    else if (player == 2)
                        info.FirebatCount2++;
                    break;
                case "MagSpider":
                    if (player == 1)
                        info.MagSpiderCount1++;
                    else if (player == 2)
                        info.MagSpiderCount2++;
                    break;
                case "Robot":
                    if (player == 1)
                        info.RobotCount1++;
                    else if (player == 2)
                        info.RobotCount2++;
                    break;
                case "Tank":
                    if (player == 1)
                        info.TankCount1++;
                    else if (player == 2)
                        info.TankCount2++;
                    break;
                case "Thor":
                    if (player == 1)
                        info.ThorCount1++;
                    else if (player == 2)
                        info.ThorCount2++;
                    break;
                case "Hammer":
                    if (player == 1)
                        info.HammerCount1++;
                    else if (player == 2)
                        info.HammerCount2++;
                    break;
                case "Warplane":
                    if (player == 1)
                        info.WarplaneCount1++;
                    else if (player == 2)
                        info.WarplaneCount2++;
                    break;
                case "MotherShip":
                    if (player == 1)
                        info.MotherShipCount1++;
                    else if (player == 2)
                        info.MotherShipCount2++;
                    break;
            }
        }
    }
}
