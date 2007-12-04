using System.Text.RegularExpressions;
using OrmCodeGenLib.Descriptors;

namespace OrmCodeGenLib
{
    internal static class OrmCodeGenNameHelper
    {
        public static string GetPrivateMemberName(string name)
        {
            OrmCodeDomGeneratorSettings settings = SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings;
            if (string.IsNullOrEmpty(name))
                return string.Empty;
            return settings.PrivateMembersPrefix + name.Substring(0, 1).ToLower() + name.Substring(1);
        }

        public static string GetSafeName(string p)
        {
            // todo: сделать более интелектуальным его
            Regex regex = new Regex("[\\W]+");
            return regex.Replace(p, "_");
        }

        /// <summary>
        /// Gets the qualified class name of the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public static string GetQualifiedEntityName(EntityDescription entity)
        {
            OrmCodeDomGeneratorSettings settings = SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings;
            string result = string.Empty;
            if (!string.IsNullOrEmpty(entity.Namespace))
                result += entity.Namespace;
            result += ((string.IsNullOrEmpty(result) ? string.Empty : ".") + GetEntityClassName(entity));
            return result;
        }

        public static string GetEntityFileName(EntityDescription entity)
        {
            OrmCodeDomGeneratorSettings settings = SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings;
            string baseName = 
                // prefix for file name
                settings.FileNamePrefix + 
                // class name of the entity
                GetEntityClassName(entity) +
                // suffix for file name
                settings.FileNameSuffix;
            return baseName;
        }

        public static string GetEntitySchemaDefFileName(EntityDescription entity)
        {
            OrmCodeDomGeneratorSettings settings = SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings;
            string baseName = 
                settings.FileNamePrefix + 
                GetEntitySchemaDefClassName(entity) +
                settings.FileNameSuffix;
            return baseName;
        }

        /// <summary>
        /// Gets class name of the entity using settings
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public static string GetEntityClassName(EntityDescription entity)
        {
            OrmCodeDomGeneratorSettings settings = SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings;
            return 
                // prefix from settings for class name
                settings.ClassNamePrefix + 
                // entity's class name
                entity.Name +
                // suffix from settings for class name
                settings.ClassNameSuffix;
        }

        /// <summary>
        /// Gets the name of the schema definition class for entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public static string GetEntitySchemaDefClassName(EntityDescription entity)
        {
            OrmCodeDomGeneratorSettings settings = SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings;
            return 
                // name of the entity class name
                GetEntityClassName(entity) + 
                // entity
                settings.EntitySchemaDefClassNameSuffix;
        }

        public static string GetEntitySchemaDefClassQualifiedName(EntityDescription entity)
        {
            return string.Format("{0}.{1}", GetQualifiedEntityName(entity), GetEntitySchemaDefClassName(entity));
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
