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
        public static object CreateInstance(this Type type)
        {
            return Activator.CreateInstance(type);
        }
        public static object CreateInstanceDyn(this Type type, dynamic @params)
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

            if (dic.Count(it => it.Value.Count(it2 => it2 == _null) == 0 && it.Key.GetParameters().Length == props) > 1)
                throw new MissingMethodException("More than one ctor match");

            var bestParams = dic.First();
            if (dic.Count > 1)
                bestParams = dic.Select(it => new { p = it, cnt = it.Value.Count(it2 => it2 != _null) }).OrderByDescending(it => it.cnt).ThenBy(it => it.p.Key.GetParameters().Length).First().p;

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

            if (dic.Count(it => it.Value.Count(it2 => it2 == _null) == 0 && it.Key.GetParameters().Length == (@params?.Count??0)) > 1)
                throw new MissingMethodException("More than one ctor match");

            var bestParams = dic.First();
            if (dic.Count > 1)
                bestParams = dic.Select(it => new { p = it, cnt = it.Value.Count(it2 => it2 != _null) }).OrderByDescending(it => it.cnt).ThenBy(it=>it.p.Key.GetParameters().Length).First().p;

            for (int i = 0; i < bestParams.Value.Length; i++)
            {
                if (bestParams.Value[i] == _null)
                    bestParams.Value[i] = null;
            }

            return bestParams.Key.Invoke(bestParams.Value);
        }
        public static object CreateInstance(this Type type, params object[] args)
        {
            return type.CreateInstance(null as Func<Type, Type, object>, args);
        }
        public static object CreateInstance(this Type type, Func<Type, Type, object> mapTypes, params object[] args)
        {
            if (type == null)
                return null;

            List<Tuple<ConstructorInfo, object[], int[]>> dic = new List<Tuple<ConstructorInfo, object[], int[]>>();
            foreach (var ctor in type.GetConstructors())
            {
                var methodParams = ctor.GetParameters();
                if (methodParams.Count() == 0 && (args == null || args.Length == 0))
                {
                    return Activator.CreateInstance(type);
                }
                else
                {
                    var params2Call = new object[methodParams.Count()];
                    var argsIdx = new List<int>();
                    for (int i = 0; i < params2Call.Length; i++)
                        params2Call[i] = _null;

                    if (args != null && args.Length > 0)
                    {
                        for (int i = 0, j = 0; i < params2Call.Length && j < args.Length; i++)
                        {
                            var p = methodParams[i];
                            var mtype = p.ParameterType;

                            object arg = args[j];

                            if (arg == null)
                            {
                                //var hasVal = false;
                                //if (mapTypes != null)
                                //{
                                //    var o = mapTypes(mtype, null);
                                //    if (o != null)
                                //    {
                                //        hasVal = true;
                                //        params2Call[i] = o;
                                //    }
                                //}

                                //if (!hasVal)
                                //{
                                //    params2Call[i] = p.DefaultValue;
                                //}

                                argsIdx.Add(j);
                                params2Call[i] = null;// Convert.ChangeType(null, mtype);
                                j++;
                            }
                            else
                            {
                                if (mtype == arg.GetType())
                                {
                                    argsIdx.Add(j);
                                    params2Call[i] = arg;
                                    j++;
                                }
                                //else if (mapTypes != null)
                                //{
                                //    params2Call[i] = mapTypes(mtype, arg.GetType());
                                //}
                                //else
                                //{
                                //    try
                                //    {
                                //        params2Call[i] = Convert.ChangeType(arg, mtype);
                                //        j++;
                                //    }
                                //    catch (Exception ex)
                                //    {
                                //        //eat an exception
                                //    }
                                //}
                            }
                        }
                    }
                    dic.Add(new Tuple<ConstructorInfo, object[], int[]>(ctor, params2Call, (from k in Enumerable.Range(0, args.Length)
                                                                                               where !argsIdx.Contains(k)
                                                                                               select k).ToArray()));
                }
            }

            var bestParams = dic.First();
            IEnumerable<Tuple<ConstructorInfo, object[], int[]>> sorted = null;
            if (dic.Count > 1)
            {
                sorted = dic.OrderByDescending(it => it.Item2.Count(it2 => it2 != _null)).ToArray();
                bestParams = sorted.First();
            }

            if (bestParams.Item2.Count(it => it != _null) == bestParams.Item1.GetParameters().Length && bestParams.Item3.Length == 0)
            {

            }
            else if (dic.Count == 1)
            {
                for (int i = 0; i < bestParams.Item2.Length; i++)
                {
                    if (bestParams.Item2[i] == _null)
                    {
                        bestParams.Item2[i] = null;
                    }
                }
            }
            else
            {
                sorted = sorted.OrderByDescending(it => it.Item2.Count(it2 => it2 != _null)).ThenBy(it => it.Item1.GetParameters().Length).ToArray();
                foreach (var candidate in sorted)
                {
                    if (candidate.Item3.Length == 0)
                    {
                        
                    }
                    else
                    {
                        for (int i = 0, j = 0; i < candidate.Item2.Length && j < candidate.Item3.Length; i++)
                        {
                            if (candidate.Item2[i] == _null)
                            {
                                var p = candidate.Item1.GetParameters()[i];
                                var mtype = p.ParameterType;
                                var arg = args[candidate.Item3[j]];
                                if (arg != null)
                                {
                                    var atype = arg.GetType();
                                    if (mtype.IsAssignableFrom(atype))
                                    {
                                        candidate.Item2[i] = arg;
                                        j++;
                                        continue;
                                    }
                                    else if (mapTypes != null)
                                    {
                                        var o = mapTypes(mtype, atype);
                                        if (o != null)
                                        {
                                            candidate.Item2[i] = o;
                                            j++;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                    }
                                }
                            }
                        }
                    }
                }

                sorted = sorted.OrderByDescending(it => it.Item2.Count(it2 => it2 != _null)).ThenBy(it => it.Item1.GetParameters().Length).ToArray();
                foreach (var candidate in sorted)
                {
                    for (int i = 0; i < candidate.Item2.Length; i++)
                    {
                        if (candidate.Item2[i] == _null)
                        {
                            candidate.Item2[i] = null;
                        }
                    }
                    bestParams = candidate;
                    break;
                }
            }
            
            return bestParams.Item1.Invoke(bestParams.Item2);
        }
    }
}
