using System;
using UnityEngine;
using Swift;

namespace SCM
{
    public class Utils
    {
        // 递归遍历所有子节点
        public static bool TravelAllChild(Transform t, Func<Transform, bool> fun)
        {
            var continueTravel = fun(t);
            if (!continueTravel)
                return false;

            var cnt = t.childCount;
            FC.For(cnt, (i) =>
            {
                var child = t.GetChild(i);
                continueTravel = TravelAllChild(child, fun);
            }, () => continueTravel);

            return continueTravel;
        }
    }
}
