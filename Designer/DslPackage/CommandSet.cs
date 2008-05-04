using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.ComponentModel.Design;
using global::Microsoft.VisualStudio.Modeling.Shell;
using global::Microsoft.VisualStudio.Shell;
using global::Microsoft.VisualStudio.Modeling;
using global::Microsoft.VisualStudio.Modeling.Diagrams;
using global::Microsoft.VisualStudio.Modeling.Validation;
using DslModeling = global::Microsoft.VisualStudio.Modeling;
using EnvDTE;
using EnvDTE80;

namespace Worm.Designer
{

    internal partial class DesignerExplorer
    {
        protected override void ProcessOnMenuDeleteCommand()
        {
            if (MessageBox.Show("Do you want to delete this item?", "Confirmation", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                base.ProcessOnMenuDeleteCommand();
            }
        }

     
       
    }
 /// <summary>
	/// Double-derived class to allow easier code customization.
	/// </summary>
	internal partial class DesignerCommandSet 
    {
        private const int cmdAddEntity = 0x801;
        /// <summary>
        /// Provide the menu commands that this command set handles
        /// </summary>
        protected override global::System.Collections.Generic.IList<global::System.ComponentModel.Design.MenuCommand> GetMenuCommands()
        {
            // Get the standard commands
            IList<MenuCommand> commands = base.GetMenuCommands();
            commands.Add(new DynamicStatusMenuCommand(
            new EventHandler(OnStatusEntity),
            new EventHandler(OnMenuEntity),    				
            new CommandID(new Guid(Constants.DesignerCommandSetId),	cmdAddEntity)
            ));
            // AW Class Details ToolWindow
            MenuCommand toolWindowMenuCommand = new CommandContextBoundMenuCommand(this.ServiceProvider,
                new EventHandler(OnMenuViewClassDetails),
                Constants.ViewClassDetailsCommand,
                typeof(DesignerEditorFactory).GUID);
            commands.Add(toolWindowMenuCommand);

            return commands;
        }

        internal void OnMenuViewClassDetails(object sender, EventArgs e)
        {
            WormToolWindow classDetails = this.WormToolWindow;
            if (classDetails != null)
            {
                classDetails.Show();
            }
        }

        protected WormToolWindow WormToolWindow
        {
            get
            {
                WormToolWindow classDetails = null;
                ModelingPackage package = this.ServiceProvider.GetService
                    (typeof(Microsoft.VisualStudio.Shell.Package)) as ModelingPackage;

                if (package != null)
                {
                    classDetails = package.GetToolWindow(typeof(WormToolWindow), true) as WormToolWindow;
                }

                return classDetails;
            }
        }

        protected override void ProcessOnMenuDeleteCommand()
        {

            if (MessageBox.Show("Do you want to delete this item?", "Confirmation", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                base.ProcessOnMenuDeleteCommand();
            }
        }

        internal void OnStatusEntity(object sender, EventArgs e)
        {
            MenuCommand command = sender as MenuCommand;
            command.Visible = command.Enabled = false;

            foreach (object selectedObject in this.CurrentSelection)
            {
                DesignerDiagram diagram = selectedObject as DesignerDiagram;
                if (diagram != null)
                {
                    command.Visible = command.Enabled = true;
                    break;
                }
            }
        }

        internal void OnMenuEntity(object sender, EventArgs e)
        {
            MenuCommand command = sender as MenuCommand;
            using (Transaction transaction =
               this.CurrentDesignerDocData.Store.TransactionManager.
                  BeginTransaction("Add Entity Command"))
            {
                foreach (object selectedObject in this.CurrentSelection)
                {
                    DesignerDiagram diagram = selectedObject as DesignerDiagram;
                    if (diagram != null)
                    {

                        // This handler is shared between several commands
                        switch (command.CommandID.ID)
                        {
                            case cmdAddEntity:
                               ((WormModel)( this.CurrentDesignerDocData.RootElement))
                                    .Entities.Add(new Entity(diagram.Store));
                                break;
                            
                            // … other cases …
                        }

                    }
                }
                transaction.Commit();
            }
        }

    }

    internal partial class DesignerExplorerToolWindow
    {
    

           protected override void OnSelectionChanged(EventArgs e)
           {
               base.OnSelectionChanged(e);
               DslModeling::ModelElement selectedElement = this.PrimarySelection as DslModeling::ModelElement;
               if (WormToolWindow.ActiveWindow != null)
               {
                   WormToolWindow.ActiveWindow.OnDocumentSelectionChanged(selectedElement, e);
               }
           }
    }
}
