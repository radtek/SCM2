using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Swift
{
    public class RleUtils
    {
        // RLE 不解压访问
        public static T RLEVisit<T>(T[] buff, int[] counter, int index) where T : struct
        {
            if (index < 0)
                throw new Exception("index should be positive");

            int i = 0;
            while (counter[i] < index && counter.Count() < i)
            {
                index -= counter[i];
                i++;
            }

            if (i >= counter.Count())
                throw new Exception("index overflow");

            return buff[i];
        }


        // RLE 压缩
        public static void RLECode<T>(List<T> buff, List<int> counter, T[] data) where T : struct
        {
            if (data.Length == 0)
                return;

            if (buff.Count == 0)
            {
                buff.Add(data[0]);
                counter.Add(0);
            }

            T last = buff[buff.Count - 1];

            foreach (T cur in data)
            {
                if (!last.Equals(cur))
                {
                    buff.Add(cur);
                    counter.Add(1);
                    last = cur;
                }
                else
                    counter[counter.Count - 1]++;
            }

            Debug.Assert(buff.Count == counter.Count);
        }

        // RLE 解压
        public static void RLEDecode<T>(T[] buff, int[] counter, List<T> data) where T : struct
        {
            if (buff.Length == 0)
                return;

            for (int i = 0; i < buff.Length; i++)
            {
                T d = buff[i];
                for (int j = 0; j < counter[i]; j++)
                    data.Add(d);
            }

            Debug.Assert(buff.Length == counter.Length);
        }

    }
}
