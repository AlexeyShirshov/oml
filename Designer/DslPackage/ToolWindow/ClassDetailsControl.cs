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
        TreeListView treeListViewDiagram;
        TreeListView treeListViewTable;
        TreeListView treeListViewRelation;
        TreeListView treeListViewSelfRelation;
        Label label;

        private ToggleColumnHeader containerListViewColumnHeader1Diagram;
        private ToggleColumnHeader containerListViewColumnHeader2Diagram;
        private ToggleColumnHeader containerListViewColumnHeader3Diagram;

        private ToggleColumnHeader containerListViewColumnHeader1Table;
        private ToggleColumnHeader containerListViewColumnHeader2Table;
        private ToggleColumnHeader containerListViewColumnHeader3Table;

        private ToggleColumnHeader containerListViewColumnHeader1Relation;
        private ToggleColumnHeader containerListViewColumnHeader2Relation;
        private ToggleColumnHeader containerListViewColumnHeader3Relation;

        private ToggleColumnHeader containerListViewColumnHeader1SelfRelation;
        private ToggleColumnHeader containerListViewColumnHeader2SelfRelation;
        private ToggleColumnHeader containerListViewColumnHeader3SelfRelation;

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPageDiagram;
        private System.Windows.Forms.TabPage tabPageTable;
        private System.Windows.Forms.TabPage tabPageSelfRelation;
        private System.Windows.Forms.TabPage tabPageRelation;

      
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
            treeListViewDiagram.Nodes.Clear();
            treeListViewDiagram.Visible = false;
            treeListViewTable.Nodes.Clear();
            treeListViewTable.Visible = false;
            treeListViewSelfRelation.Nodes.Clear();
            treeListViewSelfRelation.Visible = false;
            treeListViewRelation.Nodes.Clear();
            treeListViewRelation.Visible = false;
            
            this.BackColor = Color.FromArgb(212, 208, 200);
            label.Visible = true;
            
        }
        public void Display(Table table)
        {
            modelElement = table;
            Display(table.Entity.WormModel);
        }

        public void Display(Property property)
        {
            modelElement = property;
            Display(property.Entity.WormModel);
        }

        public void Display(SupressedProperty property)
        {
            modelElement = property;
            Display(property.Entity.WormModel);
        }

        public void Display(WormModel diagram)
        {
            label.Visible = false;
            this.BackColor = Color.White;
            treeListViewDiagram.Nodes.Clear();
            treeListViewDiagram.Visible = true;
            treeListViewTable.Nodes.Clear();
            treeListViewTable.Visible = true;
            treeListViewSelfRelation.Nodes.Clear();
            treeListViewSelfRelation.Visible = true;
            treeListViewRelation.Nodes.Clear();
            treeListViewRelation.Visible = true;

            modelElement = diagram;
            TreeListNode diagramNode = new TreeListNode();
            diagramNode.Text = "Worm Model";
            diagramNode.ImageIndex = 4;
            treeListViewDiagram.Nodes.Add(diagramNode);

           

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
            if (diagram.Entities.Count > 0)
            {
                Display(diagram.Entities[0]);
            }
        }

        public void Display(EntityReferencesTargetEntities relation)
        {
            label.Visible = false;
            this.BackColor = Color.White;
            modelElement = relation;
            treeListViewRelation.Nodes.Clear();
            // modelClass = modelClassToHandle;

            if (relation != null)
            {

                TreeListNode tablesNode = new TreeListNode();
                tablesNode.Text = "Relation";
                tablesNode.ImageIndex = 0;
                treeListViewRelation.Nodes.Add(tablesNode);

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
            
            treeListViewRelation.ExpandAll();
            treeListViewRelation.Visible = true;
        }

       
        public void Display(SelfRelation relation)
        {
            label.Visible = false;
            this.BackColor = Color.White;
            modelElement = relation;
            treeListViewSelfRelation.Nodes.Clear();
           // modelClass = modelClassToHandle;

            if (relation != null)
            {

                TreeListNode tablesNode = new TreeListNode();
                tablesNode.Text = "Self relation";
                tablesNode.ImageIndex = 0;
                treeListViewSelfRelation.Nodes.Add(tablesNode);

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
            treeListViewSelfRelation.Click += new System.EventHandler(treeListView_SelfRelationClick);
            treeListViewSelfRelation.ExpandAll();
            treeListViewSelfRelation.Visible = true;
        }

        void treeListView_SelfRelationClick(object sender, System.EventArgs e)
        {
            if (treeListViewSelfRelation.SelectedNodes.Count > 0 && treeListViewSelfRelation.SelectedNodes[0].Text == _addNewSelfRelation)
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
            if (treeListViewTable.SelectedNodes.Count > 0 && treeListViewTable.SelectedNodes[0].Text == _addNewTableMapping)
            {
                Entity entity = null;
                if (modelElement is WormModel)
                {
                    entity = ((Entity)modelElement);
                }
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
            treeListViewTable.Nodes.Clear();

            if (modelElement == null)
            {
                modelElement = entity;
            }
            if (modelElement != null)
            {
                TreeListNode tablesNode = new TreeListNode();
                tablesNode.Text = "Tables";
                tablesNode.ImageIndex = 0;
                treeListViewTable.Nodes.Add(tablesNode);

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
            treeListViewTable.Click += new System.EventHandler(treeListView_EntityClick);
            treeListViewTable.ExpandAll();
            treeListViewTable.Visible = true;
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
            foreach(TreeListNode mappingNode in treeListViewTable.Nodes[0].Nodes)
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
            foreach (TreeListNode mappingNode in treeListViewSelfRelation.Nodes[0].Nodes)
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
            foreach (TreeListNode mappingNode in treeListViewRelation.Nodes[0].Nodes)
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPageDiagram = new System.Windows.Forms.TabPage();
            this.tabPageRelation = new System.Windows.Forms.TabPage();
            this.tabPageSelfRelation = new System.Windows.Forms.TabPage();
            this.tabPageTable = new System.Windows.Forms.TabPage();
            this.tabControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl.Controls.Add(this.tabPageDiagram);
            this.tabControl.Controls.Add(this.tabPageTable);
            this.tabControl.Controls.Add(this.tabPageSelfRelation);
            this.tabControl.Controls.Add(this.tabPageRelation);
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "Mapping details";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(493, 100);
            this.tabControl.TabIndex = 0;
            this.tabControl.Dock = DockStyle.Fill;
            // 
            // tabPage1
            // 
            this.tabPageDiagram.Location = new System.Drawing.Point(4, 22);
            this.tabPageDiagram.Name = "tabPageDiagram";
            this.tabPageDiagram.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageDiagram.Size = new System.Drawing.Size(485, 74);
            this.tabPageDiagram.TabIndex = 0;
            this.tabPageDiagram.Text = "Worm Model";
            this.tabPageDiagram.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPageTable.Location = new System.Drawing.Point(4, 22);
            this.tabPageTable.Name = "tabPageTable";
            this.tabPageTable.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageTable.Size = new System.Drawing.Size(485, 74);
            this.tabPageTable.TabIndex = 1;
            this.tabPageTable.Text = "Table mappings";
            this.tabPageTable.UseVisualStyleBackColor = true;

            this.tabPageRelation.Location = new System.Drawing.Point(4, 22);
            this.tabPageRelation.Name = "tabPageRelation";
            this.tabPageRelation.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageRelation.Size = new System.Drawing.Size(485, 74);
            this.tabPageRelation.TabIndex = 1;
            this.tabPageRelation.Text = "Relation mappings";
            this.tabPageRelation.UseVisualStyleBackColor = true;


            this.tabPageSelfRelation.Location = new System.Drawing.Point(4, 22);
            this.tabPageSelfRelation.Name = "tabPageTable";
            this.tabPageSelfRelation.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSelfRelation.Size = new System.Drawing.Size(485, 74);
            this.tabPageSelfRelation.TabIndex = 1;
            this.tabPageSelfRelation.Text = "Self relation mappings";
            this.tabPageSelfRelation.UseVisualStyleBackColor = true;
            // 
            // ClassDetailsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.Controls.Add(this.tabControl);
            this.Name = "ClassDetailsControl";
            this.Size = new System.Drawing.Size(754, 153);
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);


            this.treeListViewDiagram = new TreeListView();
            this.containerListViewColumnHeader1Diagram = new ToggleColumnHeader();
            this.containerListViewColumnHeader2Diagram = new ToggleColumnHeader();
            this.containerListViewColumnHeader3Diagram = new ToggleColumnHeader();
            this.treeListViewTable= new TreeListView();
            this.containerListViewColumnHeader1Table = new ToggleColumnHeader();
            this.containerListViewColumnHeader2Table = new ToggleColumnHeader();
            this.containerListViewColumnHeader3Table = new ToggleColumnHeader();
            this.treeListViewRelation = new TreeListView();
            this.containerListViewColumnHeader1Relation = new ToggleColumnHeader();
            this.containerListViewColumnHeader2Relation = new ToggleColumnHeader();
            this.containerListViewColumnHeader3Relation = new ToggleColumnHeader();
            this.treeListViewSelfRelation = new TreeListView();
            this.containerListViewColumnHeader1SelfRelation = new ToggleColumnHeader();
            this.containerListViewColumnHeader2SelfRelation = new ToggleColumnHeader();
            this.containerListViewColumnHeader3SelfRelation = new ToggleColumnHeader();

            SuspendLayout();
            // 
            // containerListView1
            // 
            CreateTree("treeListViewDiagram", treeListViewDiagram, containerListViewColumnHeader1Diagram, containerListViewColumnHeader2Diagram, containerListViewColumnHeader3Diagram);
            CreateTree("treeListViewTable", treeListViewTable, containerListViewColumnHeader1Table, containerListViewColumnHeader2Table, containerListViewColumnHeader3Table);
            CreateTree("treeListViewSelfRelation", treeListViewSelfRelation, containerListViewColumnHeader1SelfRelation, containerListViewColumnHeader2SelfRelation, containerListViewColumnHeader3SelfRelation);
            CreateTree("treeListViewRelation", treeListViewRelation, containerListViewColumnHeader1Relation, containerListViewColumnHeader2Relation, containerListViewColumnHeader3Relation);

            
            
           
            //this.Controls.Add(this.treeListView);
            tabPageDiagram.Controls.Add(this.treeListViewDiagram);
            tabPageTable.Controls.Add(this.treeListViewTable);
            tabPageRelation.Controls.Add(this.treeListViewRelation);
            tabPageSelfRelation.Controls.Add(this.treeListViewSelfRelation);

            label = new Label();
            label.Text = "Select Entity or Relation on the Entity Designer to edit its mappings";
            label.ForeColor = Color.DarkGray;
            label.AutoSize = true;

            this.Controls.Add(label);
            label.Location = new Point(label.Parent.Width / 2 - label.Width / 2, label.Parent.Height / 2 - label.Height / 2);
            
            ResumeLayout(false);
        }

        private void CreateTree(string name, TreeListView treeListView, ToggleColumnHeader col1, ToggleColumnHeader col2, ToggleColumnHeader col3)
        {
          treeListView.AllowColumnReorder = false;
          treeListView.Columns.AddRange(new ToggleColumnHeader[] {
          col1,
          col2,
          col3});
          
          treeListView.Location = new System.Drawing.Point(0, 0);
          treeListView.Name = name;
          treeListView.Dock = DockStyle.Fill;
          treeListView.TabIndex = 0;
            treeListView.BorderStyle = BorderStyle.FixedSingle;
            treeListView.ShowLines = true;
            treeListView.ShowRootLines = true;
            treeListView.ShowPlusMinus = true;
            treeListView.GridLines = true;
            treeListView.GridLineColor = Color.FromArgb(212, 208, 200);
            treeListView.LabelEdit = true;
            treeListView.RowSelectColor = Color.FromArgb(212, 208, 200); ;


            col1.Text = "Column";
            col1.Width = (int)(this.DisplayRectangle.Width * 0.4);

            // 
            // containerListViewColumnHeader2
            // 

            col2.Text = "Operator";
            col2.Width = (int)(this.DisplayRectangle.Width * 0.2);

            // 
            // containerListViewColumnHeader3
            // 

            col3.Text = "Value/Property";
            col3.Width = (int)(this.DisplayRectangle.Width * 0.4);

            treeListView.Items.Clear();

            treeListView.SmallImageList = new ImageList();



            treeListViewDiagram.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2601"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2621"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2022"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2651"), size));
            treeListView.SmallImageList.Images.Add(new Icon((Icon)manager.GetObject("2671"), size));
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

