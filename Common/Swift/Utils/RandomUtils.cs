using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using Swift.Math;
using System.Collections;

namespace Swift
{
    /// <summary>
    /// 工具类
    /// </summary>
    public class RandomUtils
    {
        // 获取一个不重复的随机字符串
        public static string GetRandomString(string prefix)
        {
            if (prefix != null)
                return prefix + r.Next(0, int.MaxValue) + seq++;
            else
                return r.Next(0, int.MaxValue).ToString() + seq++;
        }

        // 从一个数组中随机多个，数组中装的是每一个元素的概率
        // canRepeat：能否重复
        // 如果能重复，则 arr 里至少要有 1 个 > 0 的数，否则异常！
        // 如果不能重复，则 arr 里至少要有 count 个 > 0 的数，否则异常！
        // 返回索引列表
        public static List<int> RandomMulti(int[] rates, int count, bool canRepeat)
        {
            List<int> lst = new List<int>();
            lst.AddRange(rates);
            List<int> re = RandomMulti(lst, count, canRepeat);
            return re;
        }
        public static List<int> RandomMulti(List<int> rates, int count, bool canRepeat)
        {
            List<int> ratesCopy = rates;

            // 先复制一份rates，不要修改输入参数
            if (!canRepeat)
            {
                ratesCopy = new List<int>();
                ratesCopy.AddRange(rates);
                rates = null;
            }

            List<int> re = new List<int>();
            for (int i = 0; i < count; i++)
            {
                int index = RandomOne(ratesCopy);
                re.Add(index);

                // 如果不能重复，把此元素概率设置为0，不可以直接移除，否则一会索引就不对了
                if (!canRepeat)
                {
                    ratesCopy[index] = 0;
                }
            }
            return re;
        }

        // 从一个数组中随机一个，数组中装的是每一个元素的概率
        // 返回索引
        // 比如 RandomOne([80, 15, 4, 1]);
        // 80% 返回 0
        // 15% 返回 1
        // 4% 返回 2
        // 1% 返回 3
        public static int RandomOne(int[] arr)
        {
            return RandomOne(arr.Length, (i) => arr[i]);
        }
        // 同上
        public static int RandomOne(List<int> arr)
        {
            return RandomOne(arr.Count, (i) => arr[i]);
        }

        public static int RandomOne(int L, Func<int, int> funGetProbability)
        {
            int sum = 0;

            // 先把掉落组里的数值全部加起来，一会做随机
            for (int i = 0; i < L; i++)
                sum += funGetProbability(i);

            int res = RandomUtils.Random(0, sum);
            int r = 0;
            for (int i = 0; i < L; i++)
            {
                r += funGetProbability(i);
                if (res < r)
                    return i;
            }

            // 不会走到这里！
            throw new Exception("Shouldn't reach here");
        }

        // 获取一个随机数，默认区间
        public static int RandomNext()
        {
            return r.Next();
        }

        // 获取一个随机数，区间为 [min, max)
        public static int RandomNext(int min, int max)
        {
            return r.Next(min, max);
        }
        public static int Random0Or1()
        {
            return RandomNext(0, 2);
        }

        // r 是[0,100]的一个数，表示一个概率
        // 随机一下看是否命中
        public static bool Hit100Rate(int r)
        {
            if (r <= 0) return false;
            if (r >= 100) return true;
            return RandomNext(1, 101) <= r;
        }

        // r 是[0, 10000]的一个数，表示一个概率
        // 随机一下看是否命中
        public static bool Hit10000Rate(int r)
        {
            if (r == 0) return false;
            if (r == 10000) return true;
            return RandomNext(1, 10001) <= r;
        }

        // 获取随机数，区间为 [min, max)
        public static int Random(int min, int max)
        {
            return r.Next(min, max);
        }

        // 获取 n 个不重复的随机数，区间为 [min, max)
        // TODO
        // 感觉好像不太对
        public static int[] RandomNextN(int min, int max, int n)
        {
            List<int> lst = new List<int>();
            int cnt = max - min;

            if (cnt < n)
                throw new Exception("the range is not big enough");

            while (lst.Count < n && n <= cnt)
            {
                int num = r.Next(min, max);
                if (lst.IndexOf(num) < 0)
                    lst.Add(num);
                else
                {
                    // 如果遇到重复的，就取下一个数
                    for (int i = 0; i < cnt && lst.IndexOf(num) >= 0; i++)
                    {
                        num++;
                        if (num >= max)
                            num = min;
                    }

                    lst.Add(num);
                }
            }
            return lst.ToArray();
        }

        // 随机一个 [min, max) 的浮点数，最小粒度为 0.0001
        public static Fix64 RandomNext(Fix64 min, Fix64 max)
        {
            return r.Next((int)(min * 10000), (int)(max * 10000)) * 0.0001f;
        }

        // 计算两个集合是否有任何重复的元素
        public static bool AnyDuplicated<T>(IEnumerable<T> s1, IEnumerable<T> s2)
        {
            foreach (T e1 in s1)
            {
                foreach (T e2 in s2)
                {
                    if (e1.Equals(e2))
                        return true;
                }
            }

            return false;
        }
        
        // 在给定列表中随机选取一个满足条件的对象
        public static T RandomSelect<T>(IEnumerable<T> list, Func<T, bool> f)
        {
            List<T> candidates = new List<T>();
            foreach (T obj in list)
            {
                if (f == null || f(obj))
                    candidates.Add(obj);
            }

            int cnt = candidates.Count;
            if (cnt == 0)
                return default(T);
            else
                return candidates[r.Next(0, cnt)];
        }

        // 对指定概率做一次随机，判断是否击中概率，目标概率应该位于 [0, 1]
        public static bool HitChance(Fix64 ratio)
        {
            if (ratio <= 0)
                return false;
            else if (ratio >= 1)
                return true;
            else
                return r.NextDouble() <= ratio;
        }

		// 选取给定数据的最大一个
		public static T Max<T>(params T[] arr)
		{
			T maxOne = arr[0];
			foreach (T d in arr)
			{
				if (Comparer<T>.Default.Compare(d, maxOne) > 0)
					maxOne = d;
			}

			return maxOne;
		}

		// 选取给定数据的最小一个
		public static T Min<T>(params T[] arr)
		{
			T maxOne = arr[0];
			foreach (T d in arr)
			{
				if (Comparer<T>.Default.Compare(d, maxOne) < 0)
					maxOne = d;
			}
			
			return maxOne;
		}

        // 计算名字长度
        // 中文算2个字符
        public static int NameLength(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            int L = 0;
            foreach (var c in str)
            {
                if (c > 127)
                    L += 2;
                else
                    L += 1;
            }
            return L;
        }

        // 名字长度裁剪，只保留指定数量的字符
        // 中文算2个字符
        public static string NameLengthCut(string str, int length)
        {
            if (NameLength(str) <= length)
                return str;
            string s = "";
            int L = 0;
            foreach (var c in str)
            {
                if (c > 127)
                    L += 2;
                else
                    L += 1;

                if (L <= length)
                    s += c;
                else
                    break;
            }
            return s;
        }

        #region 保护部分

        // 递增序号
        static ulong seq = 0;

        // 随机种子
        public static SRandom r = new SRandom();

        #endregion
    }

    public static class SwiftSortClassExt
    {

        #region 冒泡排序

        public static T[] SwiftSort<T>(this T[] arr, Func<T, T, int> comp)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                for (int j = i + 1; j < arr.Length; j++)
                {
                    int r = comp(arr[i], arr[j]);
                    if (r > 0)
                    {
                        T t = arr[i];
                        arr[i] = arr[j];
                        arr[j] = t;
                    }
                }
            }

            return arr;
        }

        public static T[] SwiftSort<T, CT>(this T[] arr, Func<T, CT> valueFun) where CT : IComparable<CT>
        {
            return arr.SwiftSort((T x, T y) =>
            {
                var vx = valueFun(x);
                var vy = valueFun(y);
                return vx.CompareTo(vy);
            });
        }

        public static List<T> SwiftSort<T>(this List<T> lst, Func<T, T, int> comp)
        {
            T[] arr = lst.ToArray();
            arr.SwiftSort(comp);
            lst.Clear();
            lst.AddRange(arr);
            return lst;
        }

        public static List<T> SwiftSort<T>(this List<T> lst, Func<T, int> valueFun)
        {
            T[] arr = lst.ToArray();
            arr.SwiftSort(valueFun);
            lst.Clear();
            lst.AddRange(arr);
            return lst;
        }

        public static int[] SwiftSort(this int[] arr)
        {
            return arr.SwiftSort((int x, int y) => { if (x < y) return -1; else if (x == y) return 0; else return 1; });
        }

        public static T[] SwiftRandomSort<T>(this T[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                int n = RandomUtils.RandomNext(i, arr.Length);
                T tmp = arr[n];
                arr[n] = arr[i];
                arr[i] = tmp;
            }

            return arr;
        }

        public static List<T> SwiftRandomSort<T>(this List<T> lst)
        {
            for (int i = 0; i < lst.Count; i++)
            {
                int n = RandomUtils.RandomNext(i, lst.Count);
                T tmp = lst[n];
                lst[n] = lst[i];
                lst[i] = tmp;
            }

            return lst;
        }

        #endregion
    }
}
