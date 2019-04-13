using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;

namespace Server
{
    /// <summary>
    /// 数据信息
    /// </summary>
    public class DataAnalysisInfo : SerializableData
    {
        public string Length;
        public DateTime Date;
        public string User1;
        public string User2;
        public string UserName1;
        public string UserName2;

        public string Winner;

        public int DogCount1;
        public int DogCount2;

        public int SoldierCount1;
        public int SoldierCount2;

        public int FirebatCount1;
        public int FirebatCount2;

        public int MagSpiderCount1;
        public int MagSpiderCount2;

        public int GhostCount1;
        public int GhostCount2;

        public int RobotCount1;
        public int RobotCount2;

        public int TankCount1;
        public int TankCount2;

        public int ThorCount1;
        public int ThorCount2;

        public int HammerCount1;
        public int HammerCount2;

        public int SoldierCarrierCount1;
        public int SoldierCarrierCount2;

        public int WarplaneCount1;
        public int WarplaneCount2;

        public int MotherShipCount1;
        public int MotherShipCount2;

        protected override void Sync()
        {
            BeginSync();
            SyncString(ref Length);
            SyncString(ref Winner);
            //SyncString(ref Date.ToString());
            SyncString(ref User1);
            SyncString(ref User2);
            SyncInt(ref DogCount1);
            SyncInt(ref DogCount2);
            SyncInt(ref SoldierCount1);
            SyncInt(ref SoldierCount2);
            SyncInt(ref FirebatCount1);
            SyncInt(ref FirebatCount2);
            SyncInt(ref MagSpiderCount1);
            SyncInt(ref MagSpiderCount2);
            SyncInt(ref GhostCount1);
            SyncInt(ref GhostCount2);
            SyncInt(ref RobotCount1);
            SyncInt(ref RobotCount2);
            SyncInt(ref TankCount1);
            SyncInt(ref TankCount2);
            SyncInt(ref ThorCount1);
            SyncInt(ref ThorCount2);
            SyncInt(ref HammerCount1);
            SyncInt(ref HammerCount2);
            SyncInt(ref SoldierCarrierCount1);
            SyncInt(ref SoldierCarrierCount2);
            SyncInt(ref WarplaneCount1);
            SyncInt(ref WarplaneCount2);
            SyncInt(ref MotherShipCount1);
            SyncInt(ref MotherShipCount2);
            EndSync();
        }
    }
}
