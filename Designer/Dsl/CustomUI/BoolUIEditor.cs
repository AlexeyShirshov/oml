using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Globalization;
using System.Windows.Forms.Design;

namespace Worm.Designer
{
    /// <summary> 
    /// Implements the Table domain attribute property 
    /// editor. 
    /// </summary> 
    public class BoolUIEditor : UITypeEditor
    {
        #region Members
        private IWindowsFormsEditorService formsEditorService;
        #endregion

        #region Public Methods
        /// <summary> 
        /// Gets the editor style used by the EditValue method. 
        /// </summary> 
        /// <param name="context">Additional context information. 
        /// </param> 
        /// <returns> 
        /// A value that indicates the style of editor used by the 
        /// EditValue method. 
        /// If the UITypeEditor does not support this method, then 
        /// GetEditStyle will return None. 
        /// </returns> 
        public override UITypeEditorEditStyle GetEditStyle
            (ITypeDescriptorContext context)
        {
            if (context != null)
            {
                return UITypeEditorEditStyle.DropDown;
            }
            return base.GetEditStyle(context);
        }


        /// <summary> 
        /// Edits the specified object's value using the editor style 
        /// indicated by the GetEditStyle method. 
        /// </summary> 
        /// <param name="context">Additional context information.</param> 
        /// <param name="provider">A provider that this editor can use to 
        /// obtain services.</param> 
        /// <param name="value">The object to edit.</param> 
        /// <returns> 
        /// The new value of the object. If the value of the object has not 
        /// changed, this should return the same object it was passed. 
        /// </returns> 
        public override object EditValue(ITypeDescriptorContext context,
            IServiceProvider provider, object value)
        {
            // Default behavior 
            if ((context == null) ||
                (provider == null) || (context.Instance == null))
            {
                return base.EditValue(context, provider, value);
            } // Get current entity 
           
           
            // Build list box 
            ListBox listBox = new ListBox();
            listBox.Sorted = true;
            listBox.Click += new EventHandler(listBox_Click);
            listBox.BorderStyle = BorderStyle.None;

           
                listBox.Items.Add("False");
                listBox.Items.Add("True");

                listBox.SelectedItem = value.ToString();

            // Handle the service 
            this.formsEditorService =
                (IWindowsFormsEditorService)provider.
                GetService(typeof(IWindowsFormsEditorService));
            this.formsEditorService.DropDownControl(listBox);

            // Result 
            return listBox.SelectedItem;
              

        }

        
        #endregion

        #region Private Methods
        /// <summary> 
        /// Handles the Click event of the listBox control. 
        /// </summary> 
        /// <param name="sender">The source of the event.</param> 
        /// <param name="e">The <see cref="System.EventArgs"/> instance 
        /// containing the event data.</param> 
        private void listBox_Click(object sender, EventArgs e)
        {
            if (this.formsEditorService != null)
            {
                this.formsEditorService.CloseDropDown();
            }
        }
        #endregion
    }
}


