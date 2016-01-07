using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreFramework.Text
{
    public class Declines
    {
        public static bool IsPluralGenitive(int digits)
        {
            if ((digits >= 5 && digits <= 20) || digits == 0)
                return true;

            return false;
        }
        public static bool IsSingularGenitive(int digits)
        {
            if (digits >= 2 && digits <= 4)
                return true;

            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="s">
        /// s[0] Single - рубль
        /// s[1] Singular - рубля
        /// s[2] Plural - рублей
        /// </param>
        /// <returns></returns>
        public static string DeclineAny(int n, params string[] s)
        {            
            if (IsPluralGenitive(n))
                return s[2];
            else if (IsSingularGenitive(n))
                return s[1];
            else if (n == 1)
                return s[0];

            return DeclineAny(n % 10, s);
        }

        public static string DeclineAny(int n, DigitItems s)
        {
            return DeclineAny(n, s.ToArray());
        }
    }
    
    public class DigitItems
    {
        public string Single { get; set; }
        public string Singular { get; set; }
        public string Plural { get; set; }
        public string Decline(int n)
        {
            return Declines.DeclineAny(n, this);
        }
        public string[] ToArray()
        {
            return new[] { Single, Singular, Plural};
        }
        public static DigitItems FromArray(string[] arr)
        {
            if (arr == null)
                return null;

            var r = new DigitItems();

            if (arr.Length > 0)
                r.Single = arr[0];

            if (arr.Length > 1)
                r.Singular = arr[1];

            if (arr.Length > 2)
                r.Plural = arr[2];

            return r;
        }
        public static DigitItems RuRubles()
        {
            return new DigitItems() { Single = "рубль", Singular="рубля", Plural="рублей" };
        }
        public static DigitItems RuCopecks()
        {
            return new DigitItems() { Single = "копейка", Singular = "копейки", Plural = "копеек" };
        }
        public static DigitItems RuDollars()
        {
            return new DigitItems() { Single = "доллар", Singular = "доллара", Plural = "долларов" };
        }
        public static DigitItems RuEuros()
        {
            return new DigitItems() { Single = "евро", Singular = "евро", Plural = "евро" };
        }
        public static DigitItems RuDays()
        {
            return new DigitItems() { Single = "день", Singular = "дня", Plural = "дней" };
        }
        public static DigitItems RuNights()
        {
            return new DigitItems() { Single = "ночь", Singular = "ночи", Plural = "ночей" };
        }
    }
}
