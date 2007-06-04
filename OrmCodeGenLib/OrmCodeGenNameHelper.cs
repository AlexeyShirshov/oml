using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using OrmCodeGenLib.Descriptors;

namespace OrmCodeGenLib
{
    static class OrmCodeGenNameHelper
    {
        public static string GetPrivateMemberName(string name, OrmCodeDomGeneratorSettings settings)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;
            return settings.PrivateMembersPrefix + name.Substring(0, 1).ToLower() + name.Substring(1);
        }

        public static string GetSafeName(string p)
        {
            Regex regex = new Regex("[\\W]+");
            return regex.Replace(p, "_");
        }

        /// <summary>
        /// Полное имя сущности которая должна быть сгенерированна
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static string GetQualifiedEntityName(EntityDescription entity, OrmCodeDomGeneratorSettings settings, bool final)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(entity.OrmObjectsDef.Namespace))
                result += entity.OrmObjectsDef.Namespace;
            if (!string.IsNullOrEmpty(entity.Namespace))
                result += (string.IsNullOrEmpty(result) ? string.Empty : ".") + entity.Name;
            result += (string.IsNullOrEmpty(result) ? string.Empty : ".") + GetEntityClassName(entity, settings);
            return result;
        }

        public static string GetEntityFileName(EntityDescription entity, OrmCodeDomGeneratorSettings settings)
        {
            string baseName = settings.FileNamePrefix + GetEntityClassName(entity, settings) +
                              settings.FileNameSuffix;
            return baseName;
        }

        public static string GetEntitySchemaDefFileName(EntityDescription entity, OrmCodeDomGeneratorSettings settings)
        {
            string baseName = settings.FileNamePrefix + GetEntitySchemaDefClassName(entity, settings, false) +
                              settings.FileNameSuffix;
            return baseName;
        }

        /// <summary>
        /// Имя класа сущности которая должна быть сгенерированна
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static string GetEntityClassName(EntityDescription entity, OrmCodeDomGeneratorSettings settings)
        {
            //if (settings.Behaviour == OrmObjectGeneratorBehaviour.BaseObjects && !final)
            //    return entity.Name + settings.BaseClassNameSuffix;
            //else
            return settings.ClassNamePrefix + entity.Name + settings.ClassNameSuffix;
        }

        public static string GetEntitySchemaDefClassName(EntityDescription entity, OrmCodeDomGeneratorSettings settings, bool final)
        {
            return GetEntityClassName(entity, settings) + settings.EntitySchemaDefClassNameSuffix;
        }

        public static string GetEntitySchemaDefClassQualifiedName(EntityDescription entity, OrmCodeDomGeneratorSettings settings, bool final)
        {
            return string.Format("{0}.{1}", GetQualifiedEntityName(entity, settings, final), GetEntitySchemaDefClassName(entity, settings, final));
        }

        public static string GetMultipleForm(string name)
        {
            if (name.EndsWith("s"))
                return name + "es";
            if (name.EndsWith("y"))
                return name.Substring(0, name.Length - 1) + "ies";
            return name + "s";
        }
    }
}
