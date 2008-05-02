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
    public class TableUIEditor : UITypeEditor
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

            Entity entity = null;
            if (context.Instance is Property)
            {
                entity = (context.Instance as Property).Entity;
            }
            if (context.Instance is SelfRelation)
            {
                entity = (context.Instance as SelfRelation).Entity;
            }
           

            if (entity != null)
            {
                // Current entity name 
                string entityName = entity.Name;
                // Current graph 
                IList<Table> tables = entity.Tables;
                if (tables == null)
                {
                    tables = entity.WormModel.Tables;
                }
                if (tables != null)
                {
                    // Build list box 
                    ListBox listBox = new ListBox();
                    listBox.Sorted = true;
                    listBox.Click += new EventHandler(listBox_Click);
                    listBox.BorderStyle = BorderStyle.None;

                    foreach (Table childTable in tables)
                    {
                        listBox.Items.Add(childTable.Name);
                    }

                    listBox.SelectedItem = value;

                    // Handle the service 
                    this.formsEditorService =
                        (IWindowsFormsEditorService)provider.
                        GetService(typeof(IWindowsFormsEditorService));
                    this.formsEditorService.DropDownControl(listBox);

                    // Result 
                    return listBox.SelectedItem;
                }
                


            }
             if (context.Instance is EntityConnector)
            {
                EntityConnector relation = (EntityConnector)context.Instance;
                ListBox listBox = new ListBox();
                listBox.Sorted = true;
                listBox.Click += new EventHandler(listBox_Click);
                listBox.BorderStyle = BorderStyle.None;
     
                // Current graph 
                IList<Table> tables = ((Entity)relation.FromShape.ModelElement).Tables;
                if (tables != null)
                {
       
                    foreach (Table childTable in tables)
                    {
                        listBox.Items.Add(childTable.Name);
                    }
               }
                tables = ((Entity)relation.ToShape.ModelElement).Tables;
                if (tables != null)
                {

                    foreach (Table childTable in tables)
                    {
                        listBox.Items.Add(childTable.Name);
                    }
                }
                listBox.SelectedItem = value;

                // Handle the service 
                this.formsEditorService =
                    (IWindowsFormsEditorService)provider.
                    GetService(typeof(IWindowsFormsEditorService));
                this.formsEditorService.DropDownControl(listBox);

                // Result 
                return listBox.SelectedItem;
            }

            // Default behavior 
            return base.EditValue(context, provider, value);
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


