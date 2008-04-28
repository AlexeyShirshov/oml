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
    using System.Collections.Generic;
    using System.Windows.Forms;
    using System.ComponentModel;
    using System.Resources;
    using System.Drawing;
    using Microsoft.VisualStudio.Modeling;
    using SynapticEffect.Forms;
    
    public partial class ClassDetailsControl : UserControl
    {
        private ModelElement modelElement;
        Size size = new Size(16, 16);
        ResourceManager manager;
        TreeListView treeListView;
        Label label;
 
        private ToggleColumnHeader containerListViewColumnHeader1;
        private ToggleColumnHeader containerListViewColumnHeader2;
        private ToggleColumnHeader containerListViewColumnHeader3;

      
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
                AddField(entity.IdProperty, entity.Name, entitiesNode, true);
            }

            TreeListNode tablesNode = new TreeListNode();
            tablesNode.Text = "Tables";
            tablesNode.ImageIndex = 0;
            diagramNode.Nodes.Add(tablesNode);

            foreach (Table table in diagram.Tables)
            {
                AddField(table.IdProperty, table.Name, tablesNode, true);
            }

            TreeListNode typesNode = new TreeListNode();
            typesNode.Text = "Types";
            typesNode.ImageIndex = 1;
            diagramNode.Nodes.Add(typesNode);

            foreach (WormType type in diagram.Types)
            {
                AddField(type.IdProperty, type.Name, typesNode, true);
            }
           
        }

        public void Display(Table table)
        {
            modelElement = table;
            Display(table.Entity);
        }

        public void Display(Property property)
        {
            modelElement = property;
            Display(property.Entity);
        }

        public void Display(SupressedProperty property)
        {
            modelElement = property;
            Display(property.Entity);
        }

        public void Display(EntityReferencesTargetEntities relation)
        {
            label.Visible = false;
            this.BackColor = Color.White;
            modelElement = relation;
            treeListView.Nodes.Clear();
            // modelClass = modelClassToHandle;

            if (relation != null)
            {

                TreeListNode tablesNode = new TreeListNode();
                tablesNode.Text = "Relation";
                tablesNode.ImageIndex = 0;
                treeListView.Nodes.Add(tablesNode);

                TreeListNode mappingsNode = new TreeListNode();
                mappingsNode.Text = "Maps to " + relation.Table;
                mappingsNode.ImageIndex = 1;
                tablesNode.Nodes.Add(mappingsNode);

                TreeListNode node = new TreeListNode();
                node.Text = "Left";
                node.ImageIndex = 2;
                mappingsNode.Nodes.Add(node);

                AddField(relation.LeftFieldName, "Field name", node);
                //AddField(relation.DirectAccessor, "Accessor", node);
                //AddField(relation.DirectCascadeDelete.ToString().ToLower(), "Cascade Delete", node);
                //AddField(relation.DirectAccessedEntityType, "Accessed Entity Type", node);

                node = new TreeListNode();
                node.Text = "Right";
                node.ImageIndex = 2;
                mappingsNode.Nodes.Add(node);

                AddField(relation.RightFieldName, "Field name", node);
                //AddField(relation.ReverseAccessor, "Accessor", node);
                //AddField(relation.ReverseCascadeDelete.ToString().ToLower(), "Cascade Delete", node);
                //AddField(relation.ReverseAccessedEntityType, "Accessed Entity Type", node);

                //TreeListNode newNode = new TreeListNode();
                //newNode.Text = _addNewRelation;
                //newNode.ImageIndex = 1;
                //newNode.ForeColor = Color.Gray;
                //tablesNode.Nodes.Add(newNode);

            }
           
            //treeListView.Click += new System.EventHandler(treeListView_RelationClick);
            
            treeListView.ExpandAll();
            treeListView.Visible = true;
        }

       
        public void Display(SelfRelation relation)
        {
            label.Visible = false;
            this.BackColor = Color.White;
            modelElement = relation;
            treeListView.Nodes.Clear();
           // modelClass = modelClassToHandle;

            if (relation != null)
            {

                TreeListNode tablesNode = new TreeListNode();
                tablesNode.Text = "Self relation";
                tablesNode.ImageIndex = 0;
                treeListView.Nodes.Add(tablesNode);

                foreach (SelfRelation rel in relation.Entity.SelfRelations)
                {
                    TreeListNode mappingsNode = new TreeListNode();
                    mappingsNode.Text = "Maps to " + rel.Table;
                    mappingsNode.ImageIndex = 1;
                    tablesNode.Nodes.Add(mappingsNode);

                    TreeListNode node = new TreeListNode();
                    node.Text = "Direct";
                    node.ImageIndex = 2;
                    mappingsNode.Nodes.Add(node);

                    AddField(rel.DirectFieldName, "Field name", node);
                    //AddField(relation.DirectAccessor, "Accessor", node);
                    //AddField(relation.DirectCascadeDelete.ToString().ToLower(), "Cascade Delete", node);
                    //AddField(relation.DirectAccessedEntityType, "Accessed Entity Type", node);

                    node = new TreeListNode();
                    node.Text = "Reverse";
                    node.ImageIndex = 2;
                    mappingsNode.Nodes.Add(node);

                    AddField(rel.ReverseFieldName, "Field name", node);
                    //AddField(relation.ReverseAccessor, "Accessor", node);
                    //AddField(relation.ReverseCascadeDelete.ToString().ToLower(), "Cascade Delete", node);
                    //AddField(relation.ReverseAccessedEntityType, "Accessed Entity Type", node);
                }
                 TreeListNode newNode = new TreeListNode();
                 newNode.Text = _addNewSelfRelation;
                 newNode.ImageIndex = 1;
                 newNode.ForeColor = Color.Gray;
                 tablesNode.Nodes.Add(newNode);
            }
            treeListView.Click += new System.EventHandler(treeListView_SelfRelationClick);
            treeListView.ExpandAll();
            treeListView.Visible = true;
        }

        void treeListView_SelfRelationClick(object sender, System.EventArgs e)
        {
            if (treeListView.SelectedNodes.Count > 0 && treeListView.SelectedNodes[0].Text == _addNewSelfRelation)
            {
                using (Transaction txAdd = ((SelfRelation)modelElement).Entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                {
                    SelfRelation relation = new SelfRelation(((SelfRelation)modelElement).Entity.WormModel.Store);
                    string name = "SelfRelation1";
                    int i = 1;
                    while (((SelfRelation)modelElement).Entity.SelfRelations.FindAll(s => s.Name == name).Count > 0)
                    {
                        name = "SelfRelation" + (++i);
                    }
                    relation.Name = name;
                    relation.Entity = ((SelfRelation)modelElement).Entity;
                    txAdd.Commit();
                }
                Display((SelfRelation)modelElement);
            }
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

         void treeListView_EntityClick(object sender, System.EventArgs e)
        {
            if (treeListView.SelectedNodes.Count > 0 && treeListView.SelectedNodes[0].Text == _addNewTableMapping)
            {
                Entity entity = null;
                if (modelElement is Entity)
                {
                    entity = ((Entity)modelElement);
                }
                else if (modelElement is Table)
                {
                    entity = ((Table)modelElement).Entity;
                }
                else if (modelElement is Property)
                {
                    entity = ((Property)modelElement).Entity;
                }
                else if (modelElement is SupressedProperty)
                {
                    entity = ((SupressedProperty)modelElement).Entity;
                }
                else
                {
                    return;
                }
                using (Transaction txAdd = entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                {
                    Table table = new Table(entity.WormModel.Store);
                   // relation. = "RelationNew";
                    table.Entity = entity;
                    string name = "Table1";
                    int i = 1;
                    while (entity.Tables.FindAll(t => t.Name == name).Count > 0)
                    {
                        name = "Table" + (++i);
                    }

                    table.Name = name;

                    entity.WormModel.Tables.Add(table);
                    txAdd.Commit();
                    
                }
                Display(entity);
            }
        }
        
        

        public void Display(Entity entity)
        {
            label.Visible = false;
            this.BackColor = Color.White;
            treeListView.Nodes.Clear();

            if (modelElement == null)
            {
                modelElement = entity;
            }
            if (modelElement != null)
            {
                TreeListNode tablesNode = new TreeListNode();
                tablesNode.Text = "Tables";
                tablesNode.ImageIndex = 0;
                treeListView.Nodes.Add(tablesNode);

                foreach (Table table in entity.Tables)
                {
                    TreeListNode node = new TreeListNode();
                    node.Text = "Maps to " + table.Name;
                    node.ImageIndex = 1;
                    tablesNode.Nodes.Add(node);

                    TreeListNode mappingsNode = new TreeListNode();
                    mappingsNode.Text = "Column Mappings";
                    mappingsNode.ImageIndex = 2;
                    node.Nodes.Add(mappingsNode);

                    IList<Property> properties = entity.Properties.FindAll(p => p.Table == table.Name);
                    foreach (Property property in properties)
                    {
                        AddField(property.Name, property.FieldName, mappingsNode);
                    }
                    
                }
                TreeListNode newTablesNode = new TreeListNode();
                newTablesNode.Text = _addNewTableMapping;
                newTablesNode.ImageIndex = 1;
                newTablesNode.ForeColor = Color.Gray;
                tablesNode.Nodes.Add(newTablesNode);
            }
            treeListView.Click += new System.EventHandler(treeListView_EntityClick);
            treeListView.ExpandAll();
            treeListView.Visible = true;
        }

        private void AddField(string value, string name, TreeListNode node, bool readOnly)
        {
            TreeListNode fieldNode = new TreeListNode();
            fieldNode.Text = name;
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
            text.TextChanged += new System.EventHandler(text_TextChanged);
            fieldNode.SubItems.Add(text);

            node.Nodes.Add(fieldNode);
        }

        private  void AddField(string value, string name, TreeListNode node)
        {
            AddField(value, name, node, false);
        }

        void text_TextChanged(object sender, System.EventArgs e)
        {
            //using (Transaction txAdd = modelClass.WormModel.Store.TransactionManager.BeginTransaction
            //    ("Add property"))
            //{
            //    Property property = new Property(modelClass.WormModel.Store);
            //    property.Name = e.Value.ToString();
            //    property.Entity = modelClass;
            //    txAdd.Commit();
            //}
            if (modelElement is SelfRelation)
            {
                SetSelfRelation((TextBox)sender);
            }
            if (modelElement is EntityReferencesTargetEntities)
            {
                SetRelation((TextBox)sender);
            }
            else if(!(modelElement is WormModel))
            {
                SetProperty((TextBox)sender);
            }
        }

        private void SetProperty(TextBox textBox)
        {
            foreach(TreeListNode mappingNode in treeListView.Nodes[0].Nodes)
            {
                foreach (TreeListNode node in mappingNode.Nodes[0].Nodes)
                {
                    if (node.SubItems[1].ItemControl == textBox)
                    {
                        if (modelElement is Property)
                        {
                            foreach (Property property in ((Property)modelElement).Entity.Properties)
                            {
                                if (property.FieldName == node.Text)
                                {
                                    using (Transaction txAdd = property.Entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                                    {
                                        property.Name = textBox.Text;
                                        txAdd.Commit();
                                    }
                                }
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
                                            //case "Accessor":
                                            //    relation.DirectAccessor = textBox.Text;
                                            //    break;
                                            //case "Cascade Delete":
                                            //    bool val;
                                            //    bool.TryParse(textBox.Text, out val);
                                            //    relation.DirectCascadeDelete = val;
                                            //    break;
                                            //case "Accessed Entity Type":
                                            //    relation.DirectAccessedEntityType = textBox.Text;
                                            //    break;
                                        }
                                        break;
                                    case 1:
                                        switch (fieldNode.Text)
                                        {
                                            case "Field name":
                                                relation.ReverseFieldName = textBox.Text;
                                                break;
                                            //case "Accessor":
                                            //    relation.ReverseAccessor = textBox.Text;
                                            //    break;
                                            //case "Cascade Delete":
                                            //    bool val;
                                            //    bool.TryParse(textBox.Text, out val);
                                            //    relation.ReverseCascadeDelete = val;
                                            //    break;
                                            //case "Accessed Entity Type":
                                            //    relation.ReverseAccessedEntityType = textBox.Text;
                                            //    break;
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

        private void AddBoolDropDown(bool value, string name, TreeListNode node)
        {
            TreeListNode fieldNode = new TreeListNode();
            fieldNode.Text = name;
            fieldNode.ImageIndex = 3;

            Label label = new Label();
            label.ImageList = new ImageList();
            label.ImageList.Images.Add(new Icon((Icon)manager.GetObject("5293"), size));
            label.ImageIndex = 0;
            fieldNode.SubItems.Add(label);

            TextBox text = new TextBox();
            text.BorderStyle = BorderStyle.None;
            text.Text = value.ToString().ToLower();
            fieldNode.SubItems.Add(Text);
            node.Nodes.Add(fieldNode);
        }

        

       
        
        private void SetupControls()
        {
            this.treeListView = new TreeListView();
            this.containerListViewColumnHeader1 = new ToggleColumnHeader();
            this.containerListViewColumnHeader2 = new ToggleColumnHeader();
            this.containerListViewColumnHeader3 = new ToggleColumnHeader();

            SuspendLayout();
            // 
            // containerListView1
            // 
            this.treeListView.AllowColumnReorder = false;            
            this.treeListView.Columns.AddRange(new ToggleColumnHeader[] {
            this.containerListViewColumnHeader1,
            this.containerListViewColumnHeader2,
            this.containerListViewColumnHeader3});
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
            containerListViewColumnHeader1.Width = (int)(this.DisplayRectangle.Width *0.4);
            
            // 
            // containerListViewColumnHeader2
            // 
            
            this.containerListViewColumnHeader2.Text = "Operator";
            containerListViewColumnHeader2.Width = (int)(this.DisplayRectangle.Width * 0.2);
            
            // 
            // containerListViewColumnHeader3
            // 
            
            this.containerListViewColumnHeader3.Text = "Value/Property";
            containerListViewColumnHeader3.Width = (int)(this.DisplayRectangle.Width * 0.4);
            
            treeListView.Items.Clear();
            treeListView.SmallImageList = new ImageList();
            
                
           
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2601"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2621"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2022"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2651"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2671"), size));


            label = new Label();
            label.Text = "Select Entity or Relation on the Entity Designer to edit its mappings";
            label.ForeColor = Color.DarkGray;
            label.AutoSize = true;

            this.Controls.Add(label);
            label.Location = new Point(label.Parent.Width / 2 - label.Width / 2, label.Parent.Height / 2 - label.Height / 2);
            
            ResumeLayout(false);
        }

        #region DataGridView Virtual Mode Events

        private void propertyGrid_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
        //    if (e.RowIndex == propertyGrid.RowCount - 1)
        //    {
        //        if (e.ColumnIndex == 0)
        //        {
        //            e.Value = _addNewCellValue;
        //        }
        //    }
        //    else if (modelClass != null && modelClass.Properties.Count > 0)
        //    {
        //       Property property = modelClass.Properties[e.RowIndex];
        //        switch (e.ColumnIndex)
        //        {
        //            case 0:
        //                e.Value = property.Name;
        //                break;
                    //case 1:
                    //    e.Value = property.ColumnType.ToString();
                    //    break;
                    //case 2:
                    //    e.Value = property.Column;
                    //    break;
                    //case 3:
                    //    e.Value = property.KeyType.ToString();
                    //    break;
                    //case 4:
                    //    e.Value = property.Description;
            //        //    break;
            //        default:
            //            e.Value = null;
            //            break;
            //    }
            //}
        }

               #endregion

    }
}

