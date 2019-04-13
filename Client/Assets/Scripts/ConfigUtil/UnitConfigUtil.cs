using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swift;
using Swift.Math;
using SCM;

public class UnitConfigUtil : Component
{
    ServerPort sp;

    public static bool IsFirstGetUnitCfgsFromServer = true;

    public static Action OnBuildUnitCfgsFromServer = null;
    public static Action OnRefreshUnitCfgsFromServer = null;

    public static void GetUnitCfgsFromServer()
    {
        var conn = GameCore.Instance.ServerConnection;
        var buff = conn.Request2Srv("GetUnitCfgs", (data) =>
        {
            UnitConfiguration.GetUnitCfgsFromServer(data);

            UserManager.SyncVariantsFromCfg();

            if (IsFirstGetUnitCfgsFromServer)
            {
                if (OnBuildUnitCfgsFromServer != null)
                {
                    OnBuildUnitCfgsFromServer();
                    IsFirstGetUnitCfgsFromServer = false;
                }
            }
            else if (OnRefreshUnitCfgsFromServer != null)
                OnRefreshUnitCfgsFromServer();
        });
        conn.End(buff);
    }
}
