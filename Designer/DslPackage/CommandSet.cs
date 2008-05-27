using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using System.Drawing;
using System.Resources;
using System.ComponentModel.Design;
using global::Microsoft.VisualStudio.Modeling.Shell;
using global::Microsoft.VisualStudio.Shell;
using global::Microsoft.VisualStudio.Modeling;
using global::Microsoft.VisualStudio.Modeling.Diagrams;
using global::Microsoft.VisualStudio.Modeling.Validation;
using DslModeling = global::Microsoft.VisualStudio.Modeling;
using EnvDTE;
using EnvDTE80;
using Worm.CodeGen.Core;
using Microsoft.VisualStudio.CommandBars;

namespace Worm.Designer
{


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
            new CommandID(new Guid(Constants.DesignerCommandSetId), cmdAddEntity)
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
                                ((WormModel)(this.CurrentDesignerDocData.RootElement))
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


            if (DesignerDocView.ActiveWindow != null)
            {
                ArrayList list = new ArrayList(DesignerDocView.ActiveWindow.GetSelectedComponents());
                if (!list.Contains(selectedElement))
                {
                    ArrayList collection = new ArrayList();
                    collection.Add(selectedElement);
                    DesignerDocView.ActiveWindow.SetSelectedComponents(collection);
                }
            }
        }



        private static DesignerExplorerToolWindow window;
        protected override void OnToolWindowCreate()
        {
            base.OnToolWindowCreate();
            window = this;
        }

        public static DesignerExplorerToolWindow ActiveWindow
        {
            get { return window; }
        }

    }

    internal partial class DesignerDocView
    {
        private static DesignerDocView window;


        protected override void OnCreate()
        {
            window = this;
            base.OnCreate();
        }



        public static DesignerDocView ActiveWindow
        {
            get { return window; }
        }

        protected override bool LoadView()
        {
            bool load = base.LoadView();
            ModelingDocData data = this.DocData;
            if ((data != null) && data is DesignerDocData)
            {
                WormModel model = (WormModel)data.RootElement;
                model.ModelPropertyChanged += new ModelPropertyChangedHandler(this.OnModelPropertyChanged);

                if (DesignerPackage.ImportData != null)
                {
                    try
                    {
                        XmlHelper.Import(model, (OrmObjectsDef)DesignerPackage.ImportData);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Cannot import data: " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        DesignerPackage.ImportData = null;
                    }
                }
            }
            return load;
        }


        public void OnModelPropertyChanged(ElementPropertyChangedEventArgs e)
        {
            Property property = e.ModelElement as Property;
            if (property != null)
            {
                if (DesignerExplorerToolWindow.ActiveWindow != null && this.SelectedElements.Count > 0)
                {
                    ExplorerTreeNode node = DesignerExplorerToolWindow.ActiveWindow.TreeContainer.
                            FindNodeForElement((ModelElement)this.SelectedElements[0]);
                }
                this.CurrentDesigner.Refresh();
            }
        }

        protected override void OnSelectionChanged(EventArgs e)
        {
            base.OnSelectionChanged(e);
            if (DesignerExplorerToolWindow.ActiveWindow != null && this.SelectedElements.Count > 0)
            {
                ExplorerTreeNode node = null;
                if (this.SelectedElements[0] is EntityShape)
                {
                    node = DesignerExplorerToolWindow.ActiveWindow.TreeContainer.
                        FindNodeForElement(((EntityShape)this.SelectedElements[0]).ModelElement);
                }
                else if (this.SelectedElements[0] is Property)
                {
                    node = DesignerExplorerToolWindow.ActiveWindow.TreeContainer.
                         FindNodeForElement((ModelElement)this.SelectedElements[0]);
                }
                else if (this.SelectedElements[0] is DesignerDiagram)
                {
                    node = DesignerExplorerToolWindow.ActiveWindow.TreeContainer.
                         FindNodeForElement(((DesignerDiagram)this.SelectedElements[0]).ModelElement);
                }


                if (node != null)
                {
                    node.TreeView.SelectedNode = node;
                }
            }
        }


    }

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

        public override ExplorerTreeNode FindNodeForElement(ModelElement element)
        {
            ExplorerTreeNode node = base.FindNodeForElement(element);
            if (node is ModelElementTreeNode)
            {
                if (((ModelElementTreeNode)node).ModelElement is Property)
                {
                    node.ImageKey = Utils.GetIconIndex((Property)((ModelElementTreeNode)node).ModelElement);
                    node.SelectedImageKey = node.ImageKey;
                }
            }
            return node;
        }



        public override void InsertTreeNode(TreeNodeCollection siblingNodes, ExplorerTreeNode node)
        {
            base.InsertTreeNode(siblingNodes, node);

            if (node.TreeView.ImageList.Images.Count < 16)
            {
                ResourceManager manager = new ResourceManager("Worm.Designer.VSPackage", typeof(DesignerExplorer).Assembly);
                Size size = new Size(16, 16);
                node.TreeView.ImageList.Images.Add("public", new Icon((Icon)manager.GetObject("public"), size));
                node.TreeView.ImageList.Images.Add("kpublic", new Icon((Icon)manager.GetObject("kpublic"), size));
                node.TreeView.ImageList.Images.Add("private", new Icon((Icon)manager.GetObject("private"), size));
                node.TreeView.ImageList.Images.Add("kprivate", new Icon((Icon)manager.GetObject("kprivate"), size));
                node.TreeView.ImageList.Images.Add("family", new Icon((Icon)manager.GetObject("family"), size));
                node.TreeView.ImageList.Images.Add("kfamily", new Icon((Icon)manager.GetObject("kfamily"), size));
                node.TreeView.ImageList.Images.Add("assembly", new Icon((Icon)manager.GetObject("assembly"), size));
                node.TreeView.ImageList.Images.Add("kassembly", new Icon((Icon)manager.GetObject("kassembly"), size));
                node.TreeView.ImageList.Images.Add("familyassembly", new Icon((Icon)manager.GetObject("familyassembly"), size));
                node.TreeView.ImageList.Images.Add("kfamilyassembly", new Icon((Icon)manager.GetObject("kfamilyassembly"), size));
            }

            if (node is ModelElementTreeNode)
            {
                if (((ModelElementTreeNode)node).ModelElement is Property)
                {
                    node.ImageKey = Utils.GetIconIndex((Property)((ModelElementTreeNode)node).ModelElement);
                    node.SelectedImageKey = node.ImageKey;
                }
            }
        }
    }


}
