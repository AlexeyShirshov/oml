﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using DslModeling = global::Microsoft.VisualStudio.Modeling;
using DslDesign = global::Microsoft.VisualStudio.Modeling.Design;
using DslDiagrams = global::Microsoft.VisualStudio.Modeling.Diagrams;

namespace Worm.Designer
{
	/// <summary>
	/// Helper class used to create and initialize toolbox items for this DSL.
	/// </summary>
	/// <remarks>
	/// Double-derived class to allow easier code customization.
	/// </remarks>
	public partial class DesignerToolboxHelper : DesignerToolboxHelperBase 
	{
		/// <summary>
		/// Constructs a new DesignerToolboxHelper.
		/// </summary>
		public DesignerToolboxHelper(global::System.IServiceProvider serviceProvider)
			: base(serviceProvider) { }
	}
	
	/// <summary>
	/// Helper class used to create and initialize toolbox items for this DSL.
	/// </summary>
	public abstract class DesignerToolboxHelperBase
	{
		/// <summary>
		/// Toolbox item filter string used to identify Designer toolbox items.  
		/// </summary>
		/// <remarks>
		/// See the MSDN documentation for the ToolboxItemFilterAttribute class for more information on toolbox
		/// item filters.
		/// </remarks>
		public const string ToolboxFilterString = "Designer.1.0";
		/// <summary>
		/// Toolbox item filter string used to identify Relation connector tool.
		/// </summary>
		public const string RelationFilterString = "Relation.1.0";

		private global::System.IServiceProvider sp;
		
		/// <summary>
		/// Constructs a new DesignerToolboxHelperBase.
		/// </summary>
		protected DesignerToolboxHelperBase(global::System.IServiceProvider serviceProvider)
		{
			if(serviceProvider == null) throw new global::System.ArgumentNullException("serviceProvider");
			
			this.sp = serviceProvider;
		}
		
		/// <summary>
		/// Serivce provider used to access services from the hosting environment.
		/// </summary>
		protected global::System.IServiceProvider ServiceProvider
		{
			get
			{
				return this.sp;
			}
		}

		/// <summary>
		/// Returns the display name of the tab that should be opened by default when the editor is opened.
		/// </summary>
		public static string DefaultToolboxTabName
		{
			get
			{
				return global::Worm.Designer.DesignerDomainModel.SingletonResourceManager.GetString("Worm DesignerToolboxTab", global::System.Globalization.CultureInfo.CurrentUICulture);
			}
		}
		
		
		/// <summary>
		/// Returns the toolbox items count in the default tool box tab.
		/// </summary>
		public static int DefaultToolboxTabToolboxItemsCount
		{
			get
			{
				return 2;
			}
		}
		

		/// <summary>
		/// Returns a list of toolbox items for use with this DSL.
		/// </summary>
		public virtual global::System.Collections.Generic.IList<DslDesign::ModelingToolboxItem> CreateToolboxItems()
		{
			global::System.Collections.Generic.List<DslDesign::ModelingToolboxItem> toolboxItems = new global::System.Collections.Generic.List<DslDesign::ModelingToolboxItem>();
			
			// Create store and load domain models.
			using(DslModeling::Store store = new DslModeling::Store(this.ServiceProvider))
			{
				store.LoadDomainModels(typeof(DslDiagrams::CoreDesignSurfaceDomainModel),
					typeof(global::Worm.Designer.DesignerDomainModel));
				global::System.Resources.ResourceManager resourceManager = global::Worm.Designer.DesignerDomainModel.SingletonResourceManager;
				global::System.Globalization.CultureInfo resourceCulture = global::System.Globalization.CultureInfo.CurrentUICulture;
			
				// Open transaction so we can create model elements corresponding to toolbox items.
				using(DslModeling::Transaction t = store.TransactionManager.BeginTransaction("CreateToolboxItems"))
				{

					// Add EntityClass shape tool.
					toolboxItems.Add(new DslDesign::ModelingToolboxItem(
						"EntityClassToolboxItem", // Unique identifier (non-localized) for the toolbox item.
						1, // Position relative to other items in the same toolbox tab.
						resourceManager.GetString("EntityClassToolboxItem", resourceCulture), // Localized display name for the item.
						(global::System.Drawing.Bitmap)DslDiagrams::ImageHelper.GetImage(resourceManager.GetObject("EntityClassToolboxBitmap", resourceCulture)), // Image displayed next to the toolbox item.
						"Worm DesignerToolboxTab", // Unique identifier (non-localized) for the toolbox item tab.
						resourceManager.GetString("Worm DesignerToolboxTab", resourceCulture), // Localized display name for the toolbox tab.
						"EntityClass", // F1 help keyword for the toolbox item.
						resourceManager.GetString("EntityClassToolboxTooltip", resourceCulture), // Localized tooltip text for the toolbox item.
						CreateElementToolPrototype(store, global::Worm.Designer.Entity.DomainClassId), // ElementGroupPrototype (data object) representing model element on the toolbox.
						new global::System.ComponentModel.ToolboxItemFilterAttribute[] { // Collection of ToolboxItemFilterAttribute objects that determine visibility of the toolbox item.
							new global::System.ComponentModel.ToolboxItemFilterAttribute(ToolboxFilterString, global::System.ComponentModel.ToolboxItemFilterType.Require) 
						}));

					// Add Relation connector tool.
					toolboxItems.Add(new DslDesign::ModelingToolboxItem(
						"RelationToolboxItem", // Unique identifier (non-localized) for the toolbox item.
						2, // Position relative to other items in the same toolbox tab.
						resourceManager.GetString("RelationToolboxItem", resourceCulture), // Localized display name for the item.
						(global::System.Drawing.Bitmap)DslDiagrams::ImageHelper.GetImage(resourceManager.GetObject("RelationToolboxBitmap", resourceCulture)), // Image displayed next to the toolbox item.				
						"Worm DesignerToolboxTab", // Unique identifier (non-localized) for the toolbox item tab.
						resourceManager.GetString("Worm DesignerToolboxTab", resourceCulture), // Localized display name for the toolbox tab.
						"Relation", // F1 help keyword for the toolbox item.
						resourceManager.GetString("RelationToolboxTooltip", resourceCulture), // Localized tooltip text for the toolbox item.
						null, // Connector toolbox items do not have an underlying data object.
						new global::System.ComponentModel.ToolboxItemFilterAttribute[] { // Collection of ToolboxItemFilterAttribute objects that determine visibility of the toolbox item.
							new global::System.ComponentModel.ToolboxItemFilterAttribute(ToolboxFilterString, global::System.ComponentModel.ToolboxItemFilterType.Require), 
							new global::System.ComponentModel.ToolboxItemFilterAttribute(RelationFilterString)
						}));

					t.Rollback();
				}
			}

			return toolboxItems;
		}
		
		/// <summary>
		/// Creates an ElementGroupPrototype for the element tool corresponding to the given domain class id.
		/// Default behavior is to create a prototype containing an instance of the domain class.
		/// Derived classes may override this to add additional information to the prototype.
		/// </summary>
		protected virtual DslModeling::ElementGroupPrototype CreateElementToolPrototype(DslModeling::Store store, global::System.Guid domainClassId)
		{
			DslModeling::ModelElement element = store.ElementFactory.CreateElement(domainClassId);
			DslModeling::ElementGroup elementGroup = new DslModeling::ElementGroup(store.DefaultPartition);
			elementGroup.AddGraph(element, true);
			return elementGroup.CreatePrototype();
		}
	}
}
