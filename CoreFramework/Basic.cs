using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreFramework
{
    public static class Basic
    {
        public static DateTime LeftTime(DateTime? dt)
        {
            if (!dt.HasValue)
            {
                return new DateTime(1800, 1, 1);
            }

            return dt.Value;
        }

        public static DateTime RightTime(DateTime? dt)
        {
            if (!dt.HasValue)
            {
                return new DateTime(5800, 1, 1);
            }

            return dt.Value;
        }

        public static bool IsDtEqual(DateTime? dt1, DateTime? dt2)
        {
            if (dt1.HasValue)
            {
                if (dt2.HasValue)
                {
                    return dt1.Value == dt2.Value;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (dt2.HasValue)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsEqual<T>(T? dt1, T? dt2) where T : struct
        {
            if (dt1.HasValue)
            {
                if (dt2.HasValue)
                {
                    return Equals(dt1.Value, dt2.Value);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (dt2.HasValue)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Between<T>(this T target, T beg, T end) where T : IComparable<T>
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            if (beg == null)
            {
                throw new ArgumentNullException("beg");
            }

            if (end == null)
            {
                throw new ArgumentNullException("end");
            }

            return beg.CompareTo(target) <= 0 && target.CompareTo(end) <= 0;
        }
    }
}
