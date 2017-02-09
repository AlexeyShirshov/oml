
using System;
using System.Collections.Generic;

namespace CoreFramework.Collections
{
	public static partial class DictionaryExtensions
    {
            
		public static int GetInt(this IDictionary<String, String> dic, string name, int def, bool throwOnParseError = false)
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            string s;
            int i;
            if (dic.TryGetValue(name, out s))
			{
				if (throwOnParseError)
					return int.Parse(s);
				else if(int.TryParse(s, out i))
					return i;
			}

            return def;
        }

		public static int? GetInt(this IDictionary<String, String> dic, string name)
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            string s;
            int i;
            if (dic.TryGetValue(name, out s) && int.TryParse(s, out i))
                return i;

            return null;
        }

		public static int GetInt(this IDictionary<String, object> dic, string name, int def = default(int))
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            object s;
            if (dic.TryGetValue(name, out s))
			{
                return (int)Convert.ChangeType(s, typeof(int));
			}

            return def;
        }
            
		public static bool GetBool(this IDictionary<String, String> dic, string name, bool def, bool throwOnParseError = false)
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            string s;
            bool i;
            if (dic.TryGetValue(name, out s))
			{
				if (throwOnParseError)
					return bool.Parse(s);
				else if(bool.TryParse(s, out i))
					return i;
			}

            return def;
        }

		public static bool? GetBool(this IDictionary<String, String> dic, string name)
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            string s;
            bool i;
            if (dic.TryGetValue(name, out s) && bool.TryParse(s, out i))
                return i;

            return null;
        }

		public static bool GetBool(this IDictionary<String, object> dic, string name, bool def = default(bool))
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            object s;
            if (dic.TryGetValue(name, out s))
			{
                return (bool)Convert.ChangeType(s, typeof(bool));
			}

            return def;
        }
            
		public static double GetDouble(this IDictionary<String, String> dic, string name, double def, bool throwOnParseError = false)
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            string s;
            double i;
            if (dic.TryGetValue(name, out s))
			{
				if (throwOnParseError)
					return double.Parse(s);
				else if(double.TryParse(s, out i))
					return i;
			}

            return def;
        }

		public static double? GetDouble(this IDictionary<String, String> dic, string name)
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            string s;
            double i;
            if (dic.TryGetValue(name, out s) && double.TryParse(s, out i))
                return i;

            return null;
        }

		public static double GetDouble(this IDictionary<String, object> dic, string name, double def = default(double))
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            object s;
            if (dic.TryGetValue(name, out s))
			{
                return (double)Convert.ChangeType(s, typeof(double));
			}

            return def;
        }
            
		public static float GetFloat(this IDictionary<String, String> dic, string name, float def, bool throwOnParseError = false)
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            string s;
            float i;
            if (dic.TryGetValue(name, out s))
			{
				if (throwOnParseError)
					return float.Parse(s);
				else if(float.TryParse(s, out i))
					return i;
			}

            return def;
        }

		public static float? GetFloat(this IDictionary<String, String> dic, string name)
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            string s;
            float i;
            if (dic.TryGetValue(name, out s) && float.TryParse(s, out i))
                return i;

            return null;
        }

		public static float GetFloat(this IDictionary<String, object> dic, string name, float def = default(float))
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            object s;
            if (dic.TryGetValue(name, out s))
			{
                return (float)Convert.ChangeType(s, typeof(float));
			}

            return def;
        }
            
		public static short GetShort(this IDictionary<String, String> dic, string name, short def, bool throwOnParseError = false)
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            string s;
            short i;
            if (dic.TryGetValue(name, out s))
			{
				if (throwOnParseError)
					return short.Parse(s);
				else if(short.TryParse(s, out i))
					return i;
			}

            return def;
        }

		public static short? GetShort(this IDictionary<String, String> dic, string name)
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            string s;
            short i;
            if (dic.TryGetValue(name, out s) && short.TryParse(s, out i))
                return i;

            return null;
        }

		public static short GetShort(this IDictionary<String, object> dic, string name, short def = default(short))
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            object s;
            if (dic.TryGetValue(name, out s))
			{
                return (short)Convert.ChangeType(s, typeof(short));
			}

            return def;
        }
	}
}
