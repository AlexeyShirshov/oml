// Copyright 2006 Gokhan Altinoren - http://altinoren.com/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Worm.Designer
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using System.ComponentModel;
    using System.Resources;
    using System.Drawing;
    using Microsoft.VisualStudio.Modeling;
    using global::Microsoft.VisualStudio.Modeling.Shell;
    using SynapticEffect.Forms;
    using Worm.CodeGen.Core;

    public partial class ClassDetailsControl : UserControl
    {
        private ModelElement modelElement;
        Size size = new Size(16, 16);
        ResourceManager manager;
        TreeListView treeListView;
        Label label;
        private string focusedTable;

        private ToggleColumnHeader containerListViewColumnHeader1;
        private ToggleColumnHeader containerListViewColumnHeader2;
        private ToggleColumnHeader containerListViewColumnHeader3;
        private ToggleColumnHeader containerListViewColumnHeader4;


        private const string _addNewSelfRelation = "<Add New Self Relation>";

        private const string _addNewTableMapping = "<Add New Table Mapping>";


        public ClassDetailsControl()
        {
            InitializeComponent();
            manager = new ResourceManager("Worm.Designer.VSPackage",
                                    typeof(ClassDetailsControl).Assembly);
            SetupControls();
        }

        protected override bool ProcessDialogChar(char charCode)
        {
            if (charCode != ' ' && ProcessMnemonic(charCode))
            {
                return true;
            }
            return base.ProcessDialogChar(charCode);
        }

        public void Clear()
        {
            modelElement = null;
            treeListView.Nodes.Clear();
            treeListView.Visible = false;

            this.BackColor = Color.FromArgb(212, 208, 200);
            label.Visible = true;

        }

        public void Display(WormModel diagram)
        {
            label.Visible = false;
            this.BackColor = Color.White;
            treeListView.Nodes.Clear();

            this.containerListViewColumnHeader1.Text = "Column";
            this.containerListViewColumnHeader2.Text = "Operator";
            if (treeListView.Columns.Count == 4)
            {
                treeListView.Columns.RemoveAt(3);
            }
            modelElement = diagram;
            TreeListNode diagramNode = new TreeListNode();
            diagramNode.Text = "Worm Model";
            diagramNode.ImageIndex = 4;
            treeListView.Nodes.Add(diagramNode);



            TreeListNode entitiesNode = new TreeListNode();
            entitiesNode.Text = "Entities";
            entitiesNode.ImageIndex = 0;
            diagramNode.Nodes.Add(entitiesNode);

            foreach (Entity entity in diagram.Entities)
            {
                AddField("e+" + entity.Name, entity.IdProperty, entity.Name, entitiesNode, true);
            }

            TreeListNode tablesNode = new TreeListNode();
            tablesNode.Text = "Tables";
            tablesNode.ImageIndex = 0;
            diagramNode.Nodes.Add(tablesNode);

            foreach (Table table in diagram.Tables)
            {
                AddField("ta+" + table.Name, table.IdProperty, table.Name, tablesNode, true);
            }

            TreeListNode typesNode = new TreeListNode();
            typesNode.Text = "Types";
            typesNode.ImageIndex = 1;
            diagramNode.Nodes.Add(typesNode);

            foreach (WormType type in diagram.Types)
            {
                AddField("ty+" + type.Name, type.IdProperty, type.Name, typesNode, true);
            }
            treeListView.ExpandAll();
            treeListView.Visible = true;
            treeListView.Click += new System.EventHandler(treeListView_Click);
        }

        public void Display(Table table)
        {
            if (table.Entities.Count > 0)
            {
                Display(table.Entities[0]);
            }
        }

        public void Display(Property property)
        {
            Display(property.Entity);
        }

        public void Display(SupressedProperty property)
        {
            Display(property.Entity);
        }



        void Table_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            EntityReferencesTargetEntities relation = modelElement as EntityReferencesTargetEntities;

            using (Transaction txAdd = relation.TargetEntity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
            {
                relation.Table = ((ComboBox)(sender)).SelectedItem.ToString();
                txAdd.Commit();
            }

            //Display(relation);

        }

        void LeftField_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            EntityReferencesTargetEntities relation = modelElement as EntityReferencesTargetEntities;
            using (Transaction txAdd = relation.TargetEntity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
            {
                relation.LeftFieldName = ((ComboBox)(sender)).SelectedItem.ToString();
                txAdd.Commit();
            }
        }

        void RightField_SelectedIndexChanged(object sender, System.EventArgs e)
        {

            EntityReferencesTargetEntities relation = modelElement as EntityReferencesTargetEntities;
            using (Transaction txAdd = relation.TargetEntity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
            {
                relation.RightFieldName = ((ComboBox)(sender)).SelectedItem.ToString();
                txAdd.Commit();
            }
        }


        public void Display(SelfRelation relation)
        {
            Display(relation.Entity);
        }



        // void treeListView_RelationClick(object sender, System.EventArgs e)
        //{
        //    if (treeListView.SelectedNodes[0].Text == _addNewRelation)
        //    {
        //        using (Transaction txAdd = ((EntityReferencesTargetEntities)modelElement).TargetEntity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
        //        {
        //            EntityReferencesTargetEntities relation = new EntityReferencesTargetEntities
        //                (((EntityReferencesTargetEntities)modelElement).TargetEntity.WormModel.Store);
        //           // relation. = "RelationNew";
        //            relation.TargetEntity = ((EntityReferencesTargetEntities)modelElement).TargetEntity;
        //            relation.SourceEntity = ((EntityReferencesTargetEntities)modelElement).SourceEntity;
        //            txAdd.Commit();

        //        }
        //        Display((EntityReferencesTargetEntities)modelElement);
        //    }
        //}

        void treeListView_Click(object sender, System.EventArgs e)
        {
            if (modelElement is Entity)
            {
                EntityClick();
            }
           
            else if (modelElement is WormModel)
            {
                ModelClick();
            }

        }

        private void ModelClick()
        {
            ExplorerTreeNode node = null;
            if (treeListView.SelectedNodes.Count > 0 && !string.IsNullOrEmpty((string)treeListView.SelectedNodes[0].Tag))
            {
                string[] ar = ((string)treeListView.SelectedNodes[0].Tag).Split(new char[] { '+' });
                switch (ar[0])
                {
                    case "e":
                        Entity entity = GetModel().Entities.Find(e => e.Name == ar[1]);
                        if (entity != null)
                        {
                            node = DesignerExplorerToolWindow.ActiveWindow.TreeContainer.
                                FindNodeForElement(entity);
                            if (node != null)
                            {
                                node.TreeView.SelectedNode = node;
                                DesignerExplorerToolWindow.ActiveWindow.Show();
                            }
                        }
                        break;
                    case "ta":
                        Table table = GetModel().Tables.Find(t => t.Name == ar[1]);
                        if (table != null)
                        {
                            node = DesignerExplorerToolWindow.ActiveWindow.TreeContainer.
                                FindNodeForElement(table);
                            if (node != null)
                            {
                                node.TreeView.SelectedNode = node;
                                DesignerExplorerToolWindow.ActiveWindow.Show();
                            }
                        }
                        break;
                    case "ty":
                        WormType type = GetModel().Types.Find(t => t.Name == ar[1]);
                        if (type != null)
                        {
                            node = DesignerExplorerToolWindow.ActiveWindow.TreeContainer.
                                FindNodeForElement(type);
                            if (node != null)
                            {
                                node.TreeView.SelectedNode = node;
                                DesignerExplorerToolWindow.ActiveWindow.Show();
                            }
                        }
                        break;
                }
            }
        }


        private void EntityClick()
        {
            if (treeListView.SelectedNodes.Count > 0 && DesignerExplorerToolWindow.ActiveWindow != null)
            {
                SelectInExplorer();
            }
        }

        private void SelectInExplorer()
        {
            ExplorerTreeNode node = null;
            TreeListNode selectedNode = null;
            foreach (TreeListNode propertyNode in treeListView.Nodes[0].Nodes)
            {
                if (treeListView.SelectedNodes[0].ImageIndex >= 5
                    && treeListView.SelectedNodes[0].ImageIndex < 15
                   && treeListView.SelectedNodes[0].Tag == propertyNode.Tag
                   && treeListView.SelectedNodes[0].Text == propertyNode.Text
                   )
                {
                    selectedNode = propertyNode;
                    break;
                }
            }


            if (selectedNode != null)
            {
                WormModel model = GetModel();
                if (model != null)
                {

                    Entity entity = (Entity)modelElement;
                    if (entity != null)
                    {
                        Property property = entity.Properties.Find(p => p.Name == selectedNode.Text);
                        if (property != null)
                        {
                            node = DesignerExplorerToolWindow.ActiveWindow.TreeContainer.
                                FindNodeForElement(property);
                            if (node != null)
                            {
                                node.TreeView.SelectedNode = node;
                                DesignerExplorerToolWindow.ActiveWindow.Show();
                            }

                        }
                    }

                }
            }
        }



        private WormModel GetModel()
        {
            WormModel model = null;
            if (modelElement is WormModel)
            {
                model = ((WormModel)modelElement);
            }
            else if (modelElement is Entity)
            {
                model = ((Entity)modelElement).WormModel;
            }
            else if (modelElement is Table)
            {
                model = ((Table)modelElement).WormModel;
            }
            else if (modelElement is Property)
            {
                model = ((Property)modelElement).Entity.WormModel;
            }
            else if (modelElement is SupressedProperty)
            {
                model = ((SupressedProperty)modelElement).Entity.WormModel;
            }

            return model;


        }



        private void AddActionField(int iconIndex, string tag, string value, string name, TreeListNode node,
            string[] values, System.EventHandler handler, System.EventHandler focusHandler)
        {
            TreeListNode fieldNode = new TreeListNode();
            fieldNode.Text = name;
            fieldNode.Tag = tag;
            fieldNode.ImageIndex = iconIndex;

            ComboBox combo = new ComboBox();
            foreach (string t in values)
            {
                combo.Items.Add(t);
            }
            List<string> list = new List<string>(values);

            fieldNode.SubItems.Add(combo);
            combo.SelectedIndex = list.FindIndex(l => l == tag);
            combo.SelectedIndexChanged += handler;
            combo.GotFocus += focusHandler;

            TextBox text = new TextBox();
            text.BorderStyle = BorderStyle.None;
            text.Text = value;
            text.Leave += new System.EventHandler(text_TextChanged);
            fieldNode.SubItems.Add(text);

            Label deleteLabel = new Label();
            deleteLabel.ImageList = new ImageList();
            deleteLabel.ImageList.Images.Add(new Icon((Icon)manager.GetObject("123"), size));
            deleteLabel.ImageIndex = 0;
            deleteLabel.Tag = value;
            deleteLabel.Click += new System.EventHandler(deleteLabel_Click);
            fieldNode.SubItems.Add(deleteLabel);


            node.Nodes.Add(fieldNode);
        }

        public void Display(Entity entity)
        {
            label.Visible = false;
            this.BackColor = Color.White;
            treeListView.Nodes.Clear();
            this.containerListViewColumnHeader1.Text = "Property";
            this.containerListViewColumnHeader2.Text = "Table";

            if (treeListView.Columns.Count == 3)
            {
                treeListView.Columns.Add(containerListViewColumnHeader4);
            }
            modelElement = entity;

            if (modelElement != null)
            {
                List<string> tables = new List<string>();
                foreach (Table t in entity.WormModel.Tables)
                {
                    tables.Add(t.Name);
                }

                TreeListNode tablesNode = new TreeListNode();
                tablesNode.Text = entity.Name;
                tablesNode.ImageIndex = 0;
                treeListView.Nodes.Add(tablesNode);

                foreach (Property property in entity.Properties)
                {
                    AddActionField(GetIconIndex(property), property.Table, property.FieldName, property.Name, tablesNode,
                        tables.ToArray(), new System.EventHandler(EntityTable_SelectedIndexChanged),
                         new System.EventHandler(EntityTable_GotFocus));
                }
            }
            treeListView.Click += new System.EventHandler(treeListView_Click);
            treeListView.ExpandAll();
            treeListView.Visible = true;
        }


        /*
                public void Display(Entity entity)
                {
                    label.Visible = false;
                    this.BackColor = Color.White;
                    treeListView.Nodes.Clear();
                    this.containerListViewColumnHeader1.Text = "Property";

                    if (treeListView.Columns.Count == 3)
                    {
                        treeListView.Columns.Add(containerListViewColumnHeader4);
                    }
                    modelElement = entity;

                    if (modelElement != null)
                    {
                        List<string> tables = new List<string>();
                        foreach (Table t in entity.WormModel.Tables)
                        {
                            tables.Add(t.Name);
                        }

                        TreeListNode tablesNode = new TreeListNode();
                        tablesNode.Text = "Tables";
                        tablesNode.ImageIndex = 0;
                        treeListView.Nodes.Add(tablesNode);
                        int i = 0;
                        foreach (Table table in entity.Tables)
                        {
                            AddCombo(table.Name, table.Name, "Maps to " + table.Name, tablesNode, tables.ToArray(),
                                new System.EventHandler(EntityTable_SelectedIndexChanged),
                                 new System.EventHandler(EntityTable_GotFocus));
                            TreeListNode node = tablesNode.Nodes[i];
                            node.ImageIndex = 1;

                            TreeListNode mappingsNode = new TreeListNode();
                            mappingsNode.Text = "Column Mappings";
                            mappingsNode.ImageIndex = 2;
                            mappingsNode.Tag = table.Name;
                            node.Nodes.Add(mappingsNode);

                            IList<Property> properties = entity.Properties.FindAll(p => p.Table == table.Name);
                            foreach (Property property in properties)
                            {
                                AddActionField(GetIconIndex(property), table.Name, property.Name, property.FieldName, mappingsNode, false);
                            }
                            i++;
                        }
                        // Add non mapped properties
                        {
                            AddCombo("", "", "Unmapped properties", tablesNode, tables.ToArray(),
                                new System.EventHandler(EntityTable_SelectedIndexChanged),
                                 new System.EventHandler(EntityTable_GotFocus));
                            TreeListNode node = tablesNode.Nodes[i];
                            node.ImageIndex = 1;

                            TreeListNode mappingsNode = new TreeListNode();
                            mappingsNode.Text = "Column Mappings";
                            mappingsNode.ImageIndex = 2;
                            mappingsNode.Tag = "";
                            node.Nodes.Add(mappingsNode);

                            IList<Property> properties = entity.Properties.FindAll(p => p.Table == "");
                            foreach (Property property in properties)
                            {
                                AddActionField(GetIconIndex(property), "", property.Name, property.FieldName, mappingsNode, false);
                            }
                            i++;
                        }
                        TreeListNode newTablesNode = new TreeListNode();
                        newTablesNode.Text = _addNewTableMapping;
                        newTablesNode.ImageIndex = 1;
                        newTablesNode.ForeColor = Color.Gray;
                        tablesNode.Nodes.Add(newTablesNode);
                    }
                    treeListView.Click += new System.EventHandler(treeListView_Click);
                    treeListView.ExpandAll();
                    treeListView.Visible = true;
                }*/

        private int GetIconIndex(Property property)
        {
            int index = 3;
            if (property.PrimaryKey == BooleanEnum.True)
            {
                switch (property.AccessLevel)
                {
                    case AccessLevel.Public:
                        index = 6;
                        break;
                    case AccessLevel.Private:
                        index = 8;
                        break;
                    case AccessLevel.Assembly:
                        index = 12;
                        break;
                    case AccessLevel.Family:
                        index = 10;
                        break;
                    case AccessLevel.FamilyOrAssembly:
                        index = 14;
                        break;
                }


            }
            else
            {
                switch (property.AccessLevel)
                {
                    case AccessLevel.Public:
                        index = 5;
                        break;
                    case AccessLevel.Private:
                        index = 7;
                        break;
                    case AccessLevel.Assembly:
                        index = 11;
                        break;
                    case AccessLevel.Family:
                        index = 9;
                        break;
                    case AccessLevel.FamilyOrAssembly:
                        index = 13;
                        break;
                }

            }
            return index;
        }


        void EntityTable_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            Entity entity = modelElement as Entity;

            using (Transaction txAdd = entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
            {
                string tableName = ((ComboBox)(sender)).SelectedItem.ToString();
                if (entity.Tables.Find(t => t.Name == tableName) == null)
                {
                    if (!string.IsNullOrEmpty(focusedTable))
                    {
                        int count = entity.Properties.FindAll(p => p.Table == focusedTable).Count;
                        if (count <= 1)
                        {
                            Table fTable = entity.WormModel.Tables.Find(t => t.Name == focusedTable);
                            fTable.Entities.Remove(entity);
                        }
                    }
                    Table table = entity.WormModel.Tables.Find(t => t.Name == tableName);
                    if (table != null && !table.Entities.Contains(entity))
                    {
                        table.Entities.Add(entity);
                    }
                    if (treeListView.SelectedNodes.Count > 0)
                    {
                        Property property = entity.Properties.Find(p => p.Name == treeListView.SelectedNodes[0].Text);
                        if (property != null)
                        {
                            property.Table = table.Name;
                        }
                    }
                    txAdd.Commit();
                }
            }

            Display(entity);

        }

        void EntityTable_GotFocus(object sender, System.EventArgs e)
        {
            if (((ComboBox)(sender)).SelectedItem != null)
            {
                focusedTable = ((ComboBox)(sender)).SelectedItem.ToString();
            }
        }

        private void AddField(string tag, string value, string name, TreeListNode node, bool readOnly)
        {
            TreeListNode fieldNode = new TreeListNode();
            fieldNode.Text = name;
            fieldNode.Tag = tag;
            fieldNode.ImageIndex = 3;

            Label label = new Label();
            label.ImageList = new ImageList();
            label.ImageList.Images.Add(new Icon((Icon)manager.GetObject("5293"), size));
            label.ImageIndex = 0;
            fieldNode.SubItems.Add(label);

            TextBox text = new TextBox();
            text.BorderStyle = BorderStyle.None;
            text.Text = value;
            if (readOnly)
            {
                text.ReadOnly = true;
                text.BackColor = Color.White;
            }
            text.Leave += new System.EventHandler(text_TextChanged);
            fieldNode.SubItems.Add(text);

            node.Nodes.Add(fieldNode);
        }


        void deleteLabel_Click(object sender, System.EventArgs e)
        {
            if (MessageBox.Show("Do you want to delete this item?", "Confirmation", MessageBoxButtons.YesNo,
                 MessageBoxIcon.Question) == DialogResult.Yes)
            {

                Entity entity = modelElement as Entity;
                Property property = entity.Properties.Find(p => p.Name == ((Label)sender).Tag.ToString());
                if (property != null)
                {
                    using (Transaction txAdd = entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                    {
                        property.Delete();
                        txAdd.Commit();
                    }
                }
            }
        }

        private void AddCombo(string tag, string value, string name, TreeListNode node, string[] values, System.EventHandler handler)
        {
            TreeListNode fieldNode = new TreeListNode();
            fieldNode.Text = name;
            fieldNode.Tag = tag;
            fieldNode.ImageIndex = 3;

            Label label = new Label();
            label.ImageList = new ImageList();
            label.ImageList.Images.Add(new Icon((Icon)manager.GetObject("5293"), size));
            label.ImageIndex = 0;
            fieldNode.SubItems.Add(label);

            ComboBox text = new ComboBox();
            foreach (string t in values)
            {
                text.Items.Add(t);
            }
            List<string> list = new List<string>(values);

            fieldNode.SubItems.Add(text);

            node.Nodes.Add(fieldNode);
            text.SelectedIndex = list.FindIndex(l => l == value);
            text.SelectedIndexChanged += handler;
        }






        void text_TextChanged(object sender, System.EventArgs e)
        {
            if (modelElement is SelfRelation)
            {
                SetSelfRelation((TextBox)sender);
            }
            if (modelElement is EntityReferencesTargetEntities)
            {
                SetRelation((TextBox)sender);
            }
            else if (!(modelElement is WormModel))
            {
                SetProperty((TextBox)sender);
            }
        }

        private void SetProperty(TextBox textBox)
        {
            foreach (TreeListNode mappingNode in treeListView.Nodes[0].Nodes)
            {
                if (mappingNode.SubItems[1].ItemControl == textBox && modelElement != null)
                {

                    foreach (Property property in ((Entity)modelElement).Properties)
                    {
                        if (property.Name == mappingNode.Text && property.FieldName != textBox.Text)
                        {
                            using (Transaction txAdd = property.Entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                            {
                                property.FieldName = textBox.Text;
                                txAdd.Commit();
                                return;
                            }
                        }
                    }

                }
            }
        }

        private void SetSelfRelation(TextBox textBox)
        {
            SelfRelation relation = modelElement as SelfRelation;
            foreach (TreeListNode mappingNode in treeListView.Nodes[0].Nodes)
            {
                if (mappingNode.Text == _addNewSelfRelation)
                {
                    continue;
                }
                for (int i = 0; i < mappingNode.Nodes.Count; i++)
                {
                    foreach (TreeListNode fieldNode in mappingNode.Nodes[i].Nodes)
                    {
                        if (fieldNode.SubItems[1].ItemControl == textBox)
                        {
                            using (Transaction txAdd = relation.Entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                            {
                                switch (i)
                                {
                                    case 0:
                                        switch (fieldNode.Text)
                                        {
                                            case "Field name":
                                                relation.DirectFieldName = textBox.Text;
                                                break;

                                        }
                                        break;
                                    case 1:
                                        switch (fieldNode.Text)
                                        {
                                            case "Field name":
                                                relation.ReverseFieldName = textBox.Text;
                                                break;

                                        }
                                        break;
                                }

                                txAdd.Commit();
                            }
                        }
                    }
                }
            }
        }

        private void SetRelation(TextBox textBox)
        {
            EntityReferencesTargetEntities relation = modelElement as EntityReferencesTargetEntities;
            foreach (TreeListNode mappingNode in treeListView.Nodes[0].Nodes)
            {
                for (int i = 0; i < mappingNode.Nodes.Count; i++)
                {
                    foreach (TreeListNode fieldNode in mappingNode.Nodes[i].Nodes)
                    {
                        if (fieldNode.SubItems[1].ItemControl == textBox)
                        {
                            using (Transaction txAdd = relation.TargetEntity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                            {
                                switch (i)
                                {
                                    case 0:
                                        switch (fieldNode.Text)
                                        {
                                            case "Field name":
                                                relation.LeftFieldName = textBox.Text;
                                                break;

                                        }
                                        break;
                                    case 1:
                                        switch (fieldNode.Text)
                                        {
                                            case "Field name":
                                                relation.RightFieldName = textBox.Text;
                                                break;
                                        }
                                        break;
                                }

                                txAdd.Commit();
                            }
                        }
                    }
                }
            }
        }





        private void SetupControls()
        {
            this.treeListView = new TreeListView();
            this.containerListViewColumnHeader1 = new ToggleColumnHeader();
            this.containerListViewColumnHeader2 = new ToggleColumnHeader();
            this.containerListViewColumnHeader3 = new ToggleColumnHeader();
            containerListViewColumnHeader4 = new ToggleColumnHeader();

            SuspendLayout();
            // 
            // containerListView1
            // 
            this.treeListView.AllowColumnReorder = false;
            this.treeListView.Columns.AddRange(new ToggleColumnHeader[] {
            this.containerListViewColumnHeader1,
            this.containerListViewColumnHeader2,
            this.containerListViewColumnHeader3,
            this.containerListViewColumnHeader4});
            //this.containerListView.ItemContextMenu = this.contextMenuStrip1;
            this.treeListView.Location = new System.Drawing.Point(0, 0);
            this.treeListView.Name = "containerListView1";
            this.treeListView.Dock = DockStyle.Fill;
            this.treeListView.TabIndex = 0;
            this.treeListView.BorderStyle = BorderStyle.FixedSingle;
            treeListView.ShowLines = true;
            treeListView.ShowRootLines = true;
            treeListView.ShowPlusMinus = true;
            treeListView.GridLines = true;
            treeListView.GridLineColor = Color.FromArgb(212, 208, 200);
            treeListView.LabelEdit = true;
            treeListView.RowSelectColor = Color.FromArgb(212, 208, 200); ;
            this.Controls.Add(this.treeListView);


            this.containerListViewColumnHeader1.Text = "Column";
            containerListViewColumnHeader1.Width = 300;

            // 
            // containerListViewColumnHeader2
            // 

            this.containerListViewColumnHeader2.Text = "Operator";
            containerListViewColumnHeader2.Width = 100;

            // 
            // containerListViewColumnHeader3
            // 

            this.containerListViewColumnHeader3.Text = "Value/Property";
            containerListViewColumnHeader3.Width = 200;

            this.containerListViewColumnHeader4.Text = "Action";
            containerListViewColumnHeader4.Width = 100;

            treeListView.Items.Clear();
            treeListView.SmallImageList = new ImageList();
            treeListView.HoverSelection = true;


            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2601"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2621"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2022"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2651"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2671"), size));
            //properties starting = 5
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("public"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("kpublic"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("private"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("kprivate"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("family"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("kfamily"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("assembly"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("kassembly"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("familyassembly"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("kfamilyassembly"), size));




            label = new Label();
            label.Text = "Select Entity or Relation on the Entity Designer to edit its mappings";
            label.ForeColor = Color.DarkGray;
            label.AutoSize = true;

            this.Controls.Add(label);
            int width = label.Parent.Width / 2 - label.Width / 2;
            label.Location = new Point(width < 0 ? 50 : width
                , label.Parent.Height / 2 - label.Height / 2);

            ResumeLayout(false);
        }

    }
}

