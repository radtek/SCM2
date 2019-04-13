using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Swift
{
    /// <summary>
    /// Flow Control
    /// </summary>
    public static class FC
    {
        #region For

        public static void For(int end, Action<int> f, Func<bool> continueCondition = null)
        {
            For(0, end, f, continueCondition);
        }

        public static void For(int start, int end, Action<int> f, Func<bool> continueCondition = null)
        {
            for (int i = start; i < end && (continueCondition == null || continueCondition()); i++)
                f(i);
        }

        public static void ForFromTo(int start, int end, Action<int> f, Func<bool> continueCondition = null)
        {
            if (start <= end)
                for (int i = start; i < end && (continueCondition == null || continueCondition()); i++)
                    f(i);
            else
                for (int i = start; i > end && (continueCondition == null || continueCondition()); i--)
                    f(i);
        }

        public static void ForFromTo2(int start1, int end1, int start2, int end2, Action<int, int> f, Func<bool> continueCondition = null)
        {
            ForFromTo(start1, end1, (i) =>
            {
                ForFromTo(end1, end2, (j) =>
                {
                    f(i, j);
                }, continueCondition);
            }, continueCondition);
        }

        public static void For2(int start1, int end1, int start2, int end2, Action<int, int> f, Func<bool> continueCondition = null)
        {
            For(start1, end1, (i) =>
            {
                For(start2, end2, (j) =>
                {
                    f(i, j);
                }, continueCondition);
            }, continueCondition);
        }

        public static void For2(int end1, int end2, Action<int, int> f, Func<bool> continueCondition = null)
        {
            For2(0, end1, 0, end2, f, continueCondition);
        }

        public static void ForEach<T>(IEnumerable<T> set, Action<int, T> f, Func<bool> continueCondition = null)
        {
            if (set == null)
                return;

            var i = 0;
            foreach (var d in set)
            {
                if (continueCondition != null && !continueCondition())
                    break;

                f(i++, d);
            }
        }

        public static void RectForCenterAt(int cx, int cy, int halfW, int halfH, Action<int, int> f, Func<bool> continueCondition = null)
        {
            var left = cx - halfW;
            var right = cx + halfW;
            var top = cy - halfH;
            var bottom = cy + halfH;
            RectFor(left, right, top, bottom, f, continueCondition);
        }

        // top > bottom
        public static void RectFor(int left, int right, int bottom, int top, Action<int,int> f, Func<bool> continueCondition = null)
        {
            // 左右(包括上下左右4个角落)
            For(bottom, top + 1, (_y) =>
            {
                f(left, _y);
                f(right, _y);
            }, continueCondition);

            // 上下(不包括4个角落)
            For(left + 1, right, (_x) =>
            {
                f(_x, bottom);
                f(_x, top);
            }, continueCondition);
        }

        public enum SquareForSeq
        {
            Default,
            PerpendicularFirst, // 上下左右4个位置优先
        }

        // 方形遍历，只遍历方形的4条边
        // cx,cy=中心点坐标
        // r=半径，至少1
        // 以r==2为例，seq == Default, 遍历顺序如下
        // 9CEGA
        // 7   8
        // 5 * 6
        // 3   4
        // 1BDF2
        public static void SquareFor(int cx, int cy, int r, Action<int, int> f, 
            SquareForSeq seq = SquareForSeq.Default, Func<bool> continueCondition = null)
        {
            if (r < 0 || (continueCondition != null && !continueCondition()))
                return;

            if (r == 0)
            {
                f(cx, cy);
                return;
            }

            if (seq == SquareForSeq.PerpendicularFirst)
            {
                f(cx - r, cy);  // 左
                f(cx + r, cy);  // 右
                f(cx, cy + r);  // 上
                f(cx, cy - r);  // 下

                // 左右(包括上下左右4个角落)
                For(cy - r, cy + r + 1, (_y) =>
                {
                    if (_y == cy)
                        return;

                    f(cx - r, _y);
                    f(cx + r, _y);
                }, continueCondition);
                // 上下(不包括4个角落)
                For(cx - r + 1, cx + r, (_x) =>
                {
                    if (_x == cx)
                        return;

                    f(_x, cy - r);
                    f(_x, cy + r);
                }, continueCondition);
            }
            else
            {
                // 左右(包括上下左右4个角落)
                For(cy - r, cy + r + 1, (_y) =>
                {
                    f(cx - r, _y);
                    f(cx + r, _y);
                }, continueCondition);
                // 上下(不包括4个角落)
                For(cx - r + 1, cx + r, (_x) =>
                {
                    f(_x, cy - r);
                    f(_x, cy + r);
                }, continueCondition);
            }
        }
        public static void SquareFor2(int cx, int cy, int start_r, int end_r, Action<int, int> f, SquareForSeq seq = SquareForSeq.Default, Func<bool> continueCondition = null)
        {
            for (int r = start_r; r < end_r && (continueCondition == null || continueCondition()); r++)
                SquareFor(cx, cy, r, f, seq, continueCondition);
        }
        
        // 斜的一圈一圈遍历，也即距离相等的一圈遍历
        public static void ObliqueSquareFor(int cx, int cy, int r, Action<int, int> f, Func<bool> continueCondition = null)
        {
            if (r < 0 || (continueCondition != null && !continueCondition()))
                return;

            if (r == 0)
            {
                f(cx, cy);
                return;
            }

            int left = cx - r;
            int right = cx + r;
            int top = cy + r;
            int bottom = cy - r;

            f(left, cy);  // 左
            f(right, cy);  // 右
            f(cx, top);  // 上
            f(cx, bottom);  // 下

            for (int i = 1; i < r && (continueCondition == null || continueCondition()); i++)
            {
                f(left + i, cy + i);     // 左-->上
                f(left + i, cy - i);    // 左-->下

                f(right - i, cy + i);    // 右-->上
                f(right - i, cy - i);   // 右-->下
            }
        }
        public static void ObliqueSquareFor2(int cx, int cy, int start_r, int end_r, Action<int, int> f, Func<bool> continueCondition = null)
        {
            for (int r = start_r; r < end_r && (continueCondition == null || continueCondition()); r++)
                ObliqueSquareFor(cx, cy, r, f, continueCondition);
        }

        public static bool Contains<T>(this IEnumerable<T> itor, Func<T, bool> f)
        {
            foreach (var it in itor)
            {
                if (f(it))
                    return true;
            }

            return false;
        }

        // 满足条件的对象映射为新列表
        public static List<T> Select<T>(this IEnumerable<T> origin, Func<T, bool> filter)
        {
            var lst = new List<T>();
            foreach (var e in origin)
                if (filter(e))
                    lst.Add(e);

            return lst;
        }

        public static void Travel<T>(this IEnumerable<T> src, Action<T> fun)
        {
            if (src == null || fun == null)
                return;

            foreach (var e in src)
                fun(e);
        }

        #endregion

        #region Action SafeCall

        public static void SC(this Action a)
        {
            if (a != null)
                a();
        }

        public static void SC<T>(this Action<T> a, T p)
        {
            if (a != null)
                a(p);
        }

        public static void SC<T1, T2>(this Action<T1, T2> a, T1 p1, T2 p2)
        {
            if (a != null)
                a(p1, p2);
        }

        public static void SC<T1, T2, T3>(this Action<T1, T2, T3> a, T1 p1, T2 p2, T3 p3)
        {
            if (a != null)
                a(p1, p2, p3);
        }

        public static void SC<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> a, T1 p1, T2 p2, T3 p3, T4 p4)
        {
            if (a != null)
                a(p1, p2, p3, p4);
        }

        #endregion

        public static TDst[] ToArray<TSrc, TDst>(this IEnumerable<TSrc> src, Func<int, TSrc, Action, TDst> mapFun)
        {
            if (src == null)
                return null;

            var lst = new List<TDst>();
            var i = 0;
            foreach (var e in src)
            {
                var skip = false;
                var elem = mapFun(i++, e, () => { skip = true; });
                if (!skip)
                    lst.Add(elem);
            }

            return lst.ToArray();
        }

        public static int FirstIndexOf<T>(this T[] arr, T v)
        {
            if (arr == null || arr.Length == 0)
                return -1;

            for (var i = 0; i < arr.Length; i++)
            {
                if (EqualityComparer<T>.Default.Equals(arr[i], v))
                    return i;
            }

            return -1;
        }

        public static T[] SubArray<T>(this T[] arr, int start, int len)
        {
            T[] subArr = new T[len];
            FC.For(len, (i) => { subArr[i] = arr[start + i]; });
            return subArr;
        }
    }
}
