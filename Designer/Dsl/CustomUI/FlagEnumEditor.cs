using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Drawing.Design;
using System.Windows.Forms.Design;


namespace Worm.Designer
{



    // Represents an item in the checklistbox
    public class FlagCheckedListBoxItem
    {
        public FlagCheckedListBoxItem(int v, string c)
        {
            value = v;
            caption = c;
        }

        public override string ToString()
        {
            return caption;
        }

        // Returns true if the value corresponds to a single bit being set
        public bool IsFlag
        {
            get
            {
                return ((value & (value - 1)) == 0);
            }
        }

        // Returns true if this value is a member of the composite bit value
        public bool IsMemberFlag(FlagCheckedListBoxItem composite)
        {
            return (IsFlag && ((value & composite.value) == value));
        }

        public int value;
        public string caption;
    }


    // UITypeEditor for flag enums
	public class FlagEnumUIEditor : UITypeEditor
	{
        // The checklistbox
        private CheckedListBox flagEnumCB;

		public FlagEnumUIEditor()
		{
           
		}

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) 
		{
			if (context != null
				&& context.Instance != null
				&& provider != null) 
			{

				IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                string[] attributes = (context.Instance as Property).Attributes.Split(new char[] { '|' });
                ArrayList list = new ArrayList(attributes);
               
                flagEnumCB = new CheckedListBox();
                flagEnumCB.CheckOnClick = true;
                foreach (int attributeValue in Enum.GetValues(typeof(PropertyAttribute)))
                {
                    string name = Enum.GetName(typeof(PropertyAttribute), attributeValue);
                    FlagCheckedListBoxItem item = new FlagCheckedListBoxItem(attributeValue, name);
                    flagEnumCB.Items.Add(item, list.Contains(name));
                }

                flagEnumCB.BorderStyle = BorderStyle.None;

				if (edSvc != null) 
				{					
                    edSvc.DropDownControl(flagEnumCB);
                    string result = string.Empty;
                    foreach (FlagCheckedListBoxItem item in flagEnumCB.CheckedItems)
                    {
                        result += item.caption + "|";
                    }
                    return result.TrimEnd('|');

				}
			}
			return null;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
		{
			return UITypeEditorEditStyle.DropDown;			
		}


	}

}
