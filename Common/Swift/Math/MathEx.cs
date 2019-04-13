using System;

namespace Swift.Math
{
    // 零碎扩展
    public static class MathEx
    {
        public static readonly Fix64 Pi = Fix64.Pi;
        public static readonly Fix64 HalfPi = Pi / 2;
        public static readonly Fix64 Pi2 = Pi * 2;

        public static readonly Fix64 Left = Fix64.Pi;
        public static readonly Fix64 Right = Fix64.Zero;
        public static readonly Fix64 Up = HalfPi;
        public static readonly Fix64 Down = HalfPi + Pi;

        public static readonly Fix64 Rad2Deg = 180 / Pi;
        public static readonly Fix64 Deg2Rad = Pi / 180;

        // 向量所指方向对应的欧拉角(0-2pi)
        public static Fix64 Arc(this Vec2 v)
        {
            return MathEx.Atan(v.y, v.x);
        }

        public static int Dir(this Vec2 v)
        {
            return (int)(v.Arc() * 180 / Pi);
        }

        // 截断到给定范围
        public static float Clamp(this float v, float min, float max)
        {
            if (v < min)
                return min;
            else if (v > max)
                return max;
            else
                return v;
        }

        // 截断到给定范围
        public static Fix64 Clamp(this Fix64 v, Fix64 min, Fix64 max)
        {
            if (v < min)
                return min;
            else if (v > max)
                return max;
            else
                return v;
        }

        // 截断到给定范围
        public static Vec2 Clamp(this Vec2 v, Vec2 max) { return v.Clamp(Vec2.Zero, max); }
        public static Vec2 Clamp(this Vec2 v, Vec2 min, Vec2 max)
        {
            if (v.InRect(min, max))
                return v;

            return new Vec2(v.x.Clamp(min.x, max.x), v.y.Clamp(min.y, max.y));
        }

        // 计算转向，从当前朝向转向目标方向，并限制最大转动角度
        public static Fix64 CalcArcTurn2(Vec2 nowDirVec, Vec2 turn2DirVec, Fix64 maxAbs)
        {
            var arcFrom = nowDirVec.Arc();
            var arcTo = turn2DirVec.Arc();
            return CalcArc4Turn2(arcFrom, arcTo, maxAbs);
        }

        // 计算转向，从当前朝向转向目标方向，并限制最大转动角度
        public static Fix64 CalcDir4Turn2(Fix64 nowDir, Fix64 turn2Dir, Fix64 maxAbs)
        {
            return CalcArc4Turn2(nowDir.Dir2Arc(), turn2Dir.Dir2Arc(), maxAbs.Dir2Arc()).Arc2Dir();
        }

        // 计算转向，从当前朝向转向目标方向，并限制最大转动角度
        public static Fix64 CalcArc4Turn2(Fix64 nowArc, Fix64 turn2Arc, Fix64 maxAbs)
        {
            var tv = (turn2Arc - nowArc).RangeInPi();
            if (Fix64.Abs(maxAbs) < Fix64.Abs(tv))
                return tv > 0 ? maxAbs : -maxAbs;
            else
                return tv;
        }

        // 将指定角度规范到 [-180, 180)
        public static Fix64 RangeIn180(this Fix64 dir)
        {
            var d = dir;
            while (d >= 180)
                d -= 360;

            while (d < -180)
                d += 360;

            return d;
        }

        // 将指定角度规范到 [-Pi, Pi)
        public static Fix64 RangeInPi(this Fix64 arc)
        {
            var d = arc;
            while (d >= Pi)
                d -= Pi2;

            while (d < -Pi)
                d += Pi2;

            return d;
        }

        // 取绝对值
        public static Fix64 Abs(this Fix64 v)
        {
            return v >= 0 ? v : -v;
        }

        // 计算三角函数

        public static Fix64 Cos(Fix64 arc)
        {
            return Fix64.Cos(arc);
        }

        public static Fix64 Sin(Fix64 arc)
        {
            return Fix64.Sin(arc);
        }

        public static Fix64 Tan(Fix64 arc)
        {
            return Fix64.Tan(arc);
        }

        public static Fix64 Atan(Fix64 y, Fix64 x)
        {
            return Fix64.Atan2(y, x);
        }

        public static Fix64 Sqrt(Fix64 v)
        {
            return Fix64.Sqrt(v);
        }

        public static void Write(this IWriteableBuffer buff, Vec2 v2)
        {
            buff.Write(v2.x.RawValue);
            buff.Write(v2.y.RawValue);
        }

        public static void Write(this IWriteableBuffer buff, Vec2[] arr)
        {
            if (arr == null)
                buff.Write(-1);
            else
            {
                buff.Write(arr.Length);
                foreach (var v in arr)
                    buff.Write(v);
            }
        }

        public static Fix64 ReadFix64(this IReadableBuffer data)
        {
            return Fix64.FromRaw(data.ReadLong());
        }

        public static void Write(this IWriteableBuffer buff, Fix64 v)
        {
            buff.Write(v.RawValue);
        }

        public static Vec2 ReadVec2(this IReadableBuffer data)
        {
            var x = data.ReadFix64();
            var y = data.ReadFix64();
            var v2 = new Vec2(x, y);
            return v2;
        }

        public static Vec2[] ReadVec2Arr(this IReadableBuffer data)
        {
            var cnt = data.ReadInt();
            if (cnt < 0)
                return null;

            var arr = new Vec2[cnt];
            FC.For(cnt, (i) => { var v = data.ReadVec2(); arr[i] = v; });
            return arr;
        }

        public static Fix64 Max(Fix64 a, Fix64 b)
        {
            return a >= b ? a : b;
        }

        public static Fix64 Min(Fix64 a , Fix64 b)
        {
            return a <= b ? a : b;
        }

        public static Fix64 Arc2Dir(this Fix64 arc)
        {
            return arc * 180 / Pi;
        }

        public static Fix64 Dir2Arc(this Fix64 dir)
        {
            return dir * Pi / 180;
        }
    }
}
