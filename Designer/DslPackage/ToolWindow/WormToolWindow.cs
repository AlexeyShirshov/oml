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
    using System;
    using System.Collections;
    using Microsoft.VisualStudio.Modeling;
    using System.Windows.Forms;
    using Microsoft.VisualStudio.Modeling.Shell;
    using Microsoft.VisualStudio.Modeling.Diagrams;
    using System.Runtime.InteropServices;
    using EnvDTE;

    [Guid(Constants.WormToolWindowId)]
    internal partial class WormToolWindow : ToolWindow
    {
        private static WormToolWindow window;
        private ClassDetailsControl control;

        public WormToolWindow(global::System.IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public override IWin32Window Window
        {
            get { return (IWin32Window)control; }
        }

        public static WormToolWindow ActiveWindow
        {
            get { return window; }
        }


        public override string WindowTitle
        {
            get
            {
                return "Worm Mapping Details";
            }
        }

        protected override int BitmapResource
        {
            get
            {
                return 104;
            }
        }

        protected override int BitmapIndex
        {
            get { return 0; }
        }

        protected override void OnToolWindowCreate()
        {
            control = new ClassDetailsControl();
            control.Clear();
            window = this;
        //    DesignerExplorerToolWindow.OnSelect += new DesignerExplorerToolWindow.DesignerExplorerToolWindowSelect(DesignerExplorerToolWindow_OnSelect);
        }

      
        protected override void OnDocumentWindowChanged(ModelingDocView oldView, ModelingDocView newView)
        {

            base.OnDocumentWindowChanged(oldView, newView);

            ModelingDocData data1 = (oldView != null) ? oldView.DocData : null;
            if ((data1 != null) && data1 is DesignerDocData)
            {
                oldView.SelectionChanged -= new EventHandler(this.OnDocumentSelectionChanged);

                WormModel model = (WormModel)data1.RootElement;
                model.ModelPropertyAdded -= new ModelPropertyAddedHandler(this.OnModelPropertyAdded);
                model.ModelPropertyDeleted -= new ModelPropertyDeletedHandler(this.OnModelPropertyDeleted);
                model.ModelPropertyChanged -= new ModelPropertyChangedHandler(this.OnModelPropertyChanged);

                model.ModelSelfRelationAdded -= new ModelSelfRelationAddedHandler(this.OnModelSelfRelationAdded);
                model.ModelSelfRelationDeleted -= new ModelSelfRelationDeletedHandler(this.OnModelSelfRelationDeleted);
                model.ModelSelfRelationChanged -= new ModelSelfRelationChangedHandler(this.OnModelSelfRelationChanged);

              
            }

            ModelingDocData data2 = (newView != null) ? newView.DocData : null;
            if ((data2 != null) && data2 is DesignerDocData)
            {
                newView.SelectionChanged += new EventHandler(this.OnDocumentSelectionChanged);

                WormModel model = (WormModel)data2.RootElement;
                model.ModelPropertyAdded += new ModelPropertyAddedHandler(this.OnModelPropertyAdded);
                model.ModelPropertyDeleted += new ModelPropertyDeletedHandler(this.OnModelPropertyDeleted);
                model.ModelPropertyChanged += new ModelPropertyChangedHandler(this.OnModelPropertyChanged);

                model.ModelSelfRelationAdded += new ModelSelfRelationAddedHandler(this.OnModelSelfRelationAdded);
                model.ModelSelfRelationDeleted += new ModelSelfRelationDeletedHandler(this.OnModelSelfRelationDeleted);
                model.ModelSelfRelationChanged += new ModelSelfRelationChangedHandler(this.OnModelSelfRelationChanged);

             
            }

            OnDocumentSelectionChanged(data2, EventArgs.Empty);
        }

        public void OnModelPropertyAdded(ElementAddedEventArgs e)
        {
            Property property = e.ModelElement as Property;
            if (property != null)
                control.Display(property);
        }

        public void OnModelPropertyDeleted(ElementDeletedEventArgs e)
        {
            control.Clear();
        }

        public void OnModelPropertyChanged(ElementPropertyChangedEventArgs e)
        {
            Property property = e.ModelElement as Property;
            if (property != null)
                control.Display(property);
        }

        public void OnModelSelfRelationAdded(ElementAddedEventArgs e)
        {
            SelfRelation relation = e.ModelElement as SelfRelation;
            if (relation != null)
                control.Display(relation);
        }

        public void OnModelSelfRelationDeleted(ElementDeletedEventArgs e)
        {
            control.Clear();
        }

        public void OnModelSelfRelationChanged(ElementPropertyChangedEventArgs e)
        {
            SelfRelation relation = e.ModelElement as SelfRelation;
            if (relation != null)
                control.Display(relation);
        }

      

        public void OnDocumentSelectionChanged(object sender, EventArgs e)
        {
            if (sender != null)
            {
                ModelingDocView view = sender as ModelingDocView;
                if (view != null)
                {

                    ICollection selection = view.GetSelectedComponents();
                    if (selection.Count == 1)
                    {
                        IEnumerator enumerator = selection.GetEnumerator();

                        enumerator.MoveNext();

                        DesignerDiagram diagram = enumerator.Current as DesignerDiagram;
                        if (diagram != null)
                        {
                            control.Display((WormModel)diagram.ModelElement);
                            return;
                        }

                        EntityShape shape = enumerator.Current as EntityShape;
                        if (shape != null)
                        {
                            control.Display((Entity)shape.ModelElement);
                            return;

                        }

                        EntityConnector connector = enumerator.Current as EntityConnector;
                        if (connector != null)
                        {
                            control.Display(((EntityReferencesTargetEntities)connector.ModelElement).TargetEntity);
                            return;

                        }

                        ElementListCompartment comp = enumerator.Current as ElementListCompartment;
                        if (comp != null)
                        {
                            control.Display((Entity)comp.ParentShape.ModelElement);
                            return;

                        }

                        Entity entity = enumerator.Current as Entity;
                        if (entity != null)
                        {
                            control.Display(entity);
                            return;

                        }

                        Property property = enumerator.Current as Property;
                        if (property != null)
                        {
                            control.Display(property);

                            return;
                        }

                        SupressedProperty supressedProperty = enumerator.Current as SupressedProperty;
                        if (supressedProperty != null)
                        {
                            control.Display(supressedProperty);

                            return;
                        }

                        Table table = enumerator.Current as Table;
                        if (table != null)
                        {
                            control.Display(table);

                            return;
                        }

                        SelfRelation selfRelation = enumerator.Current as SelfRelation;
                        if (selfRelation != null)
                        {
                            control.Display(selfRelation);

                            return;
                        }
                    }
                }
                else
                {



                    DesignerDiagram diagram = sender as DesignerDiagram;
                    if (diagram != null)
                    {
                        control.Display((WormModel)diagram.ModelElement);
                        return;
                    }

                    WormModel model = sender as WormModel;
                    if (model != null)
                    {
                        control.Display(model);
                        return;
                    }

                    EntityShape shape = sender as EntityShape;
                    if (shape != null)
                    {
                        control.Display((Entity)shape.ModelElement);
                        return;

                    }

                   
                    EntityConnector connector = sender as EntityConnector;
                    if (connector != null)
                    {
                        control.Display(((EntityReferencesTargetEntities)connector.ModelElement).TargetEntity);
                        return;

                    }

                    ElementListCompartment comp = sender as ElementListCompartment;
                    if (comp != null)
                    {
                        control.Display((Entity)comp.ParentShape.ModelElement);
                        return;

                    }

                    Entity entity = sender as Entity;
                    if (entity != null)
                    {
                        control.Display(entity);
                        return;

                    }

                    Property property = sender as Property;
                    if (property != null)
                    {
                        control.Display(property);

                        return;
                    }

                    SupressedProperty supressedProperty = sender as SupressedProperty;
                    if (supressedProperty != null)
                    {
                        control.Display(supressedProperty);

                        return;
                    }

                    Table table = sender as Table;
                    if (table != null)
                    {
                        control.Display(table);

                        return;
                    }

                    SelfRelation selfRelation = sender as SelfRelation;
                    if (selfRelation != null)
                    {
                        control.Display(selfRelation);

                        return;
                    }


                }

                control.Clear();
            }
        }
    }
}