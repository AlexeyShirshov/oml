using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

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
            if (Object.Equals(target, default(T)))
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (Object.Equals(beg, default(T)))
            {
                throw new ArgumentNullException(nameof(beg));
            }

            if (Object.Equals(end, default(T)))
            {
                throw new ArgumentNullException(nameof(end));
            }

            return beg.CompareTo(target) <= 0 && target.CompareTo(end) <= 0;
        }

        public static T Do<T>(this T item, Action<T> work)
        {
            work(item);
            return item;
        }
        public static T Do<T>(this T item, Func<T, T> work)
        {
            return work(item);
        }

        public static string ToXml(this object obj)
        {
            if (obj != null)
            {

                XmlSerializer s = new XmlSerializer(obj.GetType());
                using (var sw = new StringWriter())
                {
                    s.Serialize(sw, obj);
                    return sw.ToString();
                }
            }
            return null;
        }
        public static string ToXml(this object obj, XmlWriterSettings settings)
        {
            if (obj != null)
            {
                XmlSerializer s = new XmlSerializer(obj.GetType());
                using (StringWriter sw = new StringWriter())
                {
                    using (dynamic xw = XmlWriter.Create(sw, settings))
                    {
                        s.Serialize(xw, obj);
                        return sw.ToString();
                    }
                }
            }

            return null;
        }
        public static XElement ToXElement(this object obj)
        {
            if (obj != null)
            {
                XmlSerializer s = new XmlSerializer(obj.GetType());
                using (MemoryStream ms = new MemoryStream())
                {
                    s.Serialize(ms, obj);
                    ms.Seek(0, SeekOrigin.Begin);
                    return XElement.Load(ms);
                }
            }

            return null;
        }

    }
}
