using System;
using System.Collections.Generic;
using Swift.Math;

namespace Swift
{
    /// <summary>
    /// Math related Utils
    /// </summary>
    public static class MU
    {
        // 截断 (min, max)
        public static int ClampLR(this int v, int min, int max)
        {
            return v.Clamp(min + 1, max - 1);
        }

        // 截断 (min, max]
        public static int ClampR(this int v, int min, int max)
        {
            return v.Clamp(min + 1, max);
        }

        // 截断 [min, max)
        public static int ClampL(this int v, int min, int max)
        {
            return v.Clamp(min, max - 1);
        }

        // 截断 [min, max]
        public static int Clamp(this int v, int min, int max)
        {
            if (v < min)
                return min;
            else if (v > max)
                return max - 1;
            else
                return v;
        }

        // 根据给定向量方向计算对应角度
        public static Fix64 v2Degree(Fix64 y, Fix64 x)
        {
            var arc = Fix64.Atan2(y, x);
            return arc * 180 / Fix64.Pi;
        }

        // 判断给定扇形是否与指定圆形相交
        public static bool IsFunOverlappedCircle(Vec2 circleCenter, Fix64 circleR, Vec2 fanCenter, Fix64 fanR, Fix64 fanDir, Fix64 fanAngle)
        {
            // 先判断圆心距离
            var dc = circleCenter - fanCenter;
            var r = dc.Length;
            if (r > circleR + fanR)
                return false;

            // 在判断方向角度
            var dir = MU.v2Degree(dc.x, dc.y);
            var dd = (dir - fanDir).RangeIn180();
            return Fix64.Abs(dd) <= fanAngle / 2;
        }

        // 对给定的 Vec2 在指定范围内做镜像
        public static void MirroClamp(this Vec2 v, Vec2 min, Vec2 max)
        {
            if (v.x < min.x)
                v.x = 2 * min.x - v.x;
            else if (v.x > max.x)
                v.x = 2 * max.x - v.x;

            if (v.y < min.y)
                v.y = 2 * min.y - v.y;
            else if (v.y > max.y)
                v.y = 2 * max.y - v.y;
        }

        // 对给定的 Vec2 在指定范围内做镜像
        public static void MirroClamp(this Vec2 v, Vec2 max)
        {
            v.MirroClamp(Vec2.Zero, max);
        }

        // 判断给定左边是否在指定矩形范围内
        public static bool InRect(this Vec2 v, Vec2 min, Vec2 max)
        {
            return v.x >= min.x && v.x <= max.x && v.y >= min.y && v.y <= max.y;
        }

        // 判断给定左边是否在指定矩形范围内
        public static bool InRect(this Vec2 v, Vec2 max)
        {
            return v.InRect(Vec2.Zero, max);
        }
    }
}