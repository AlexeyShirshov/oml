using System;
using System.Text.RegularExpressions;
using Worm.CodeGen.Core.Descriptors;

namespace Worm.CodeGen.Core
{
    internal static class OrmCodeGenNameHelper
    {

        public static event OrmCodeDomGenerator.GetSettingsDelegate OrmCodeDomGeneratorSettingsRequied;

        public static string GetPrivateMemberName(string name)
        {
            OrmCodeDomGeneratorSettings settings = GetSettings();

            if (string.IsNullOrEmpty(name))
                return string.Empty;
            return settings.PrivateMembersPrefix + name.Substring(0, 1).ToLower() + name.Substring(1);
        }

        private static OrmCodeDomGeneratorSettings GetSettings()
        {
            OrmCodeDomGeneratorSettings settings = null;
            var h = OrmCodeDomGeneratorSettingsRequied;
            if(h != null)
                settings = h();
            if (settings == null) throw new Exception("OrmCodeDomGeneratorSettings requied.");
            return settings;
        }

        public static string GetSafeName(string p)
        {
            // todo: ������� ����� ��������������� ���
            Regex regex = new Regex("[\\W]+");
            return regex.Replace(p, "_");
        }


        public static string GetEntityFileName(EntityDescription entity)
        {
            OrmCodeDomGeneratorSettings settings = GetSettings();
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
            OrmCodeDomGeneratorSettings settings = GetSettings();
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
    		return GetEntityClassName(entity, false);
    	}

    	/// <summary>
		/// Gets class name of the entity using settings
		/// </summary>
		/// <param name="entity">The entity.</param>
		/// <param name="qualified">if set to <c>true</c> return qualified name.</param>
		/// <returns></returns>
        public static string GetEntityClassName(EntityDescription entity, bool qualified)
        {
            OrmCodeDomGeneratorSettings settings = GetSettings();

			string className =
				// prefix from settings for class name
				settings.ClassNamePrefix +
				// entity's class name
				entity.Name +
				// suffix from settings for class name
				settings.ClassNameSuffix;

			string ns = string.Empty;
			if (qualified && !string.IsNullOrEmpty(entity.Namespace))
				ns += entity.Namespace + ".";
			return ns + className;

             
               
        }

        /// <summary>
        /// Gets the name of the schema definition class for entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public static string GetEntitySchemaDefClassName(EntityDescription entity)
        {
            OrmCodeDomGeneratorSettings settings = GetSettings();
            return 
                // name of the entity class name
                GetEntityClassName(entity) + 
                // entity
                settings.EntitySchemaDefClassNameSuffix;
        }

        public static string GetEntitySchemaDefClassQualifiedName(EntityDescription entity)
        {
            return string.Format("{0}.{1}", GetEntityClassName(entity, true), GetEntitySchemaDefClassName(entity));
        }

    	public static string GetEntityInterfaceName(EntityDescription entity)
    	{
    		return GetEntityInterfaceName(entity, null, null, false);
    	}

    	public static string GetEntityInterfaceName(EntityDescription entity, string prefix, string suffix, bool qualified)
    	{
    		string interfaceName = "I" + (prefix ?? string.Empty) + GetEntityClassName(entity, false) + (suffix ?? string.Empty);

    		string ns = string.Empty;
    		if (qualified && !string.IsNullOrEmpty(entity.Namespace))
    		{
				ns += entity.Namespace + ".";
    		}
    		return ns + interfaceName;
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
