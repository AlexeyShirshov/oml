using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;

namespace Worm.Designer
{
    internal partial class DesignerEditorFactory : IVsEditorFactoryNotify
    {
        #region IVsEditorFactoryNotify Members

        public int NotifyDependentItemSaved(IVsHierarchy pHier, uint itemidParent, string pszMkDocumentParent, uint itemidDpendent, string pszMkDocumentDependent)
        {
            return 0;
        }

        public int NotifyItemAdded(uint grfEFN, IVsHierarchy pHier, uint itemid, string pszMkDocument)
        {
            try
            {
                int hr = SafeNotifyItemAdded(grfEFN, pHier, itemid, pszMkDocument);
                return 0;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        private int SafeNotifyItemAdded(uint grfEFN, IVsHierarchy pHier, uint itemid, string pszMkDocument)
        {
            object itemObject;
            int hr = pHier.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_ExtObject, out itemObject);

            ProjectItem item = itemObject as ProjectItem;
            if (item == null)
            {
                return -1;
            }

            // Place here the name of the custom tool you want to run
            item.Properties.Item("CustomTool").Value = "WormCodeGenerator";
            return 0;
        }

        public int NotifyItemRenamed(IVsHierarchy pHier, uint itemid, string pszMkDocumentOld, string pszMkDocumentNew)
        {
            return 0;
        }

        #endregion
    }
}
