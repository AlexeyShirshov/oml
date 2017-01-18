using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CoreFramework
{
    public static class ActivatorHelpers
    {
        private static readonly object _null = new object();
        public static object CreateInstance(this Type type, dynamic @params)
        {
            if (type == null)
                return null;

            Type t = null;
            int props = 0;
            if (@params != null)
            {
                t = @params.GetType();
                props = t.GetProperties().Length;
            }

            Dictionary<ConstructorInfo, object[]> dic = new Dictionary<ConstructorInfo, object[]>();

            foreach (var ctor in type.GetConstructors())
            {
                var methodParams = ctor.GetParameters();
                if (methodParams.Count() == 0 && props == 0)
                {
                    return Activator.CreateInstance(type);
                }
                else
                {
                    var params2Call = new object[methodParams.Count()];

                    if (props == 0)
                        return Activator.CreateInstance(type, params2Call);

                    for (int i = 0; i < params2Call.Length; i++)
                        params2Call[i] = _null;

                    for (int i = 0; i < params2Call.Length; i++)
                    {
                        var p = methodParams[i];
                        //var mtype = p.ParameterType;
                        var prop = t.GetProperty(p.Name);

                        if (prop != null)
                        {
                            params2Call[i] = prop.GetValue(@params, new object[] { });
                        }                        
                    }

                    dic[ctor] = params2Call;
                }
            }

            var bestParams = dic.First();
            if (dic.Count > 1)
                bestParams = dic.Select(it => new { p = it, cnt = it.Value.Count(it2 => it2 != _null) }).OrderByDescending(it => it.cnt).First().p;

            for (int i = 0; i < bestParams.Value.Length; i++)
            {
                if (bestParams.Value[i] == _null)
                    bestParams.Value[i] = null;
            }

            return bestParams.Key.Invoke(bestParams.Value);
        }
        public static object CreateInstance(this Type type, IDictionary<string, object> @params)
        {
            if (type == null)
                return null;

            var noParams = @params == null || @params.Count == 0;
            Dictionary<ConstructorInfo, object[]> dic = new Dictionary<ConstructorInfo, object[]>();

            foreach (var ctor in type.GetConstructors())
            {
                var methodParams = ctor.GetParameters();
                if (methodParams.Count() == 0 && noParams)
                {
                    return Activator.CreateInstance(type);
                }
                else
                {
                    var params2Call = new object[methodParams.Count()];

                    if (noParams)
                        return Activator.CreateInstance(type, params2Call);

                    for (int i = 0; i < params2Call.Length; i++)
                        params2Call[i] = _null;

                    for (int i = 0; i < params2Call.Length; i++)
                    {
                        var p = methodParams[i];
                        //var mtype = p.ParameterType;

                        object prop;
                        if (@params.TryGetValue(p.Name, out prop))
                        {
                            params2Call[i] = prop;
                        }
                    }

                    dic[ctor] = params2Call;
                }
            }

            var bestParams = dic.First();
            if (dic.Count > 1)
                bestParams = dic.Select(it => new { p = it, cnt = it.Value.Count(it2 => it2 != _null) }).OrderByDescending(it => it.cnt).First().p;

            for (int i = 0; i < bestParams.Value.Length; i++)
            {
                if (bestParams.Value[i] == _null)
                    bestParams.Value[i] = null;
            }

            return bestParams.Key.Invoke(bestParams.Value);
        }
        //public static object CreateInstance(this Type type, params object[] args)
        //{
        //    return type.CreateInstance(null as Func<Type, Type, object>, args);
        //}
        //public static object CreateInstance(this Type type, Func<Type,Type, object> mapTypes, params object[] args)
        //{
        //    if (type == null)
        //        return null;

        //    foreach (var method in type.GetConstructors())
        //    {
        //        var methodParams = method.GetParameters();
        //        if (methodParams.Count() == 0)
        //        {
        //            return Activator.CreateInstance(type);
        //        }
        //        else
        //        {
        //            var params2Call = new object[methodParams.Count()];
        //            int j = 0;
        //            for (int i = 0; i < params2Call.Length; i++)
        //            {
        //                var p = methodParams[i];
        //                var mtype = p.ParameterType;
        //                object arg = null;
        //                if (args != null && args.Length > j)
        //                    arg = args[j];

        //                if (arg == null)
        //                {
        //                    var hasVal = false;
        //                    if (mapTypes != null)
        //                    {
        //                        var o = mapTypes(mtype, null);
        //                        if (o != null)
        //                        {
        //                            hasVal = true;
        //                            params2Call[i] = o;
        //                        }
        //                    }

        //                    if (!hasVal)
        //                    {
        //                        params2Call[i] = p.DefaultValue;
        //                    }

        //                    if (args != null && args.Length > j)
        //                        j++;
        //                }
        //                else
        //                {
        //                    if (mtype == arg.GetType())
        //                    {
        //                        params2Call[i] = arg;
        //                        j++;
        //                    }
        //                    else if (mapTypes != null)
        //                    {
        //                        params2Call[i] = mapTypes(mtype, arg.GetType());
        //                    }
        //                    else
        //                    {
        //                        try
        //                        {
        //                            params2Call[i] = Convert.ChangeType(arg, mtype);
        //                            j++;
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            //eat an exception
        //                        }
        //                    }
        //                }
        //            }
        //            return Activator.CreateInstance(type, params2Call);
        //        }
        //    }

        //    return null;
        //}
    }
}
