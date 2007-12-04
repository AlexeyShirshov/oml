using System;
using System.Collections.Generic;
using System.Text;

namespace OrmCodeGenLib
{
    public class SettingsManager : IDisposable
    {
        private static SettingsManager s_currentManager;

        private readonly SettingsManager m_previousManager;

        private readonly OrmCodeDomGeneratorSettings m_ormCodeDomGeneratorSettings;
        private readonly OrmXmlGeneratorSettings m_ormXmlGeneratorSettings;

        public SettingsManager(OrmCodeDomGeneratorSettings ormCodeDomGeneratorSettings, OrmXmlGeneratorSettings ormXmlGeneratorSettings)
        {
            m_ormCodeDomGeneratorSettings = ormCodeDomGeneratorSettings;
            m_ormXmlGeneratorSettings = ormXmlGeneratorSettings;

            m_previousManager = s_currentManager;
            s_currentManager = this;
        }

        public static SettingsManager CurrentManager
        {
            get { return s_currentManager; }
        }

        public OrmCodeDomGeneratorSettings OrmCodeDomGeneratorSettings
        {
            get { return m_ormCodeDomGeneratorSettings; }
        }

        public OrmXmlGeneratorSettings OrmXmlGeneratorSettings
        {
            get { return m_ormXmlGeneratorSettings; }
        }

        #region IDisposable members
        private bool m_disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    s_currentManager = m_previousManager;
                }
                m_disposed = true;
            }
        }

        ~SettingsManager()
        {
            Dispose(false);
        } 
        #endregion
    }
}
