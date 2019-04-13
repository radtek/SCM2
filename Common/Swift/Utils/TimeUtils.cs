using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Swift
{
    public class TimeUtils
    {
        // 当前本地时间（毫秒）
        public static long Now
        {
            get
            {
                return DateTime.Now.Ticks / 10000;
            }
        }

        // 当前本地日期
        public static DateTime NowDate
        {
            get
            {
                return DateTime.Now;
            }
        }

        // 当前本地时间（毫秒）
        public static long NowSecond
        {
            get
            {
                return DateTime.Now.Ticks / 10000000;
            }
        }

        // 判断2个日期间隔几天，同一天返回0
        // OnHours：以几点为界
        // 注意：可能返回负数，如果想得到正数，dt2要比dt1大！
        public static int DaysBetweenTwoDateTime(DateTime dt1, DateTime dt2, int OnHours)
        {
            dt1 = dt1.AddHours(24 - OnHours);
            dt2 = dt2.AddHours(24 - OnHours);

            DateTime DT1 = new DateTime(dt1.Year, dt1.Month, dt1.Day);
            DateTime DT2 = new DateTime(dt2.Year, dt2.Month, dt2.Day);

            TimeSpan span = DT2.Subtract(DT1);
            return span.Days;
        }

        public static bool IsDifferentDay_0Oclock(DateTime dt1, DateTime dt2)
        {
            return IsDifferentDay_OnClock(dt1, dt2, 0);
        }

        // 是否跨天，以早上5点为界
        public static bool IsDifferentDay_5OClock(DateTime dt1, DateTime dt2)
        {
            return IsDifferentDay_OnClock(dt1, dt2, 5);
        }

        // 是否跨周(OnHours=0以周日24点为界限)
        public static bool IsDifferentWeek(DateTime dt1, DateTime dt2, int OnHours)
        {
            int days = DaysBetweenTwoDateTime(dt1, dt2, OnHours);
            dt1 = dt1.AddHours(-OnHours);
            for (int i = 0; i < days + 1; i++)
            {
                if (dt1.DayOfWeek == DayOfWeek.Monday)
                {
                    return true;
                }
                dt1 = dt1.AddDays(1);
            }

            return false;
        }

        // 是否跨天，以一天中的指定时间为准（24 小时制度）
        public static bool IsDifferentDay_OnClock(DateTime dt1, DateTime dt2, int OnHours)
        {
            DateTime DT1 = dt1.AddHours(24 - OnHours);
            DateTime DT2 = dt2.AddHours(24 - OnHours);

            return (DT1.Day != DT2.Day || DT1.Month != DT2.Month || DT1.Year != DT2.Year);
        }

        // 是否跨天，以一天中的指定时间为准（24 小时制度）
        public static bool IsDifferentDay_OnClock(DateTime dt1, DateTime dt2, int OnHours, int OnMin)
        {
            DateTime DT1 = dt1.AddMinutes(60 - OnMin);
            DateTime DT2 = dt1.AddMinutes(60 - OnMin);
            DT1 = dt1.AddHours(24 - OnHours - 1);
            DT2 = dt2.AddHours(24 - OnHours - 1);

            return (DT1.Day != DT2.Day || DT1.Month != DT2.Month || DT1.Year != DT2.Year);
        }

        // 判断两个日期是否在同一周
        public static bool IsInSameWeek(DateTime dtmS, DateTime dtmE)
        {
            TimeSpan ts = dtmE - dtmS;
            double dbl = ts.TotalDays;
            int intDow = Convert.ToInt32(dtmE.DayOfWeek);
            if (intDow == 0) intDow = 7;
            if (dbl >= 7 || dbl >= intDow) return false;
            else return true;
        }
    }
}
