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
	/// DomainClass EntityShape
	/// Entity
	/// </summary>
	[DslDesign::DisplayNameResource("Worm.Designer.EntityShape.DisplayName", typeof(global::Worm.Designer.DesignerDomainModel), "Worm.Designer.GeneratedCode.DomainModelResx")]
	[DslDesign::DescriptionResource("Worm.Designer.EntityShape.Description", typeof(global::Worm.Designer.DesignerDomainModel), "Worm.Designer.GeneratedCode.DomainModelResx")]
	[global::System.CLSCompliant(true)]
	[DslModeling::DomainObjectId("1ac7c68e-23be-47ec-a6fe-2ece8f63e49c")]
	public partial class EntityShape : DslDiagrams::CompartmentShape
	{
		#region DiagramElement boilerplate
		private static DslDiagrams::StyleSet classStyleSet;
		private static global::System.Collections.Generic.IList<DslDiagrams::ShapeField> shapeFields;
		private static global::System.Collections.Generic.IList<DslDiagrams::Decorator> decorators;
		
		/// <summary>
		/// Per-class style set for this shape.
		/// </summary>
		protected override DslDiagrams::StyleSet ClassStyleSet
		{
			get
			{
				if (classStyleSet == null)
				{
					classStyleSet = CreateClassStyleSet();
				}
				return classStyleSet;
			}
		}
		
		/// <summary>
		/// Per-class ShapeFields for this shape.
		/// </summary>
		public override global::System.Collections.Generic.IList<DslDiagrams::ShapeField> ShapeFields
		{
			get
			{
				if (shapeFields == null)
				{
					shapeFields = CreateShapeFields();
				}
				return shapeFields;
			}
		}
		
		/// <summary>
		/// Event fired when decorator initialization is complete for this shape type.
		/// </summary>
		public static event global::System.EventHandler DecoratorsInitialized;
		
		/// <summary>
		/// List containing decorators used by this type.
		/// </summary>
		public override global::System.Collections.Generic.IList<DslDiagrams::Decorator> Decorators
		{
			get 
			{
				if(decorators == null)
				{
					decorators = CreateDecorators();
					
					// fire this event to allow the diagram to initialize decorator mappings for this shape type.
					if(DecoratorsInitialized != null)
					{
						DecoratorsInitialized(this, global::System.EventArgs.Empty);
					}
				}
				
				return decorators; 
			}
		}
		
		/// <summary>
		/// Finds a decorator associated with EntityShape.
		/// </summary>
		public static DslDiagrams::Decorator FindEntityShapeDecorator(string decoratorName)
		{	
			if(decorators == null) return null;
			return DslDiagrams::ShapeElement.FindDecorator(decorators, decoratorName);
		}
		
		
		/// <summary>
		/// Shape instance initialization.
		/// </summary>
		public override void OnInitialize()
		{
			base.OnInitialize();
			
			// Create host shapes for outer decorators.
			foreach(DslDiagrams::Decorator decorator in this.Decorators)
			{
				if(decorator.RequiresHost)
				{
					decorator.ConfigureHostShape(this);
				}
			}
			
		}
		#endregion
		#region Shape size
		
		/// <summary>
		/// Default size for this shape.
		/// </summary>
		public override DslDiagrams::SizeD DefaultSize
		{
			get
			{
				return new DslDiagrams::SizeD(1.5, 0.5);
			}
		}
		#endregion
		#region Shape styles
		/// <summary>
		/// Initializes style set resources for this shape type
		/// </summary>
		/// <param name="classStyleSet">The style set for this shape class</param>
		protected override void InitializeResources(DslDiagrams::StyleSet classStyleSet)
		{
			base.InitializeResources(classStyleSet);
			
			// Outline pen settings for this shape.
			DslDiagrams::PenSettings outlinePen = new DslDiagrams::PenSettings();
			outlinePen.Width = 0.01125F;
			classStyleSet.OverridePen(DslDiagrams::DiagramPens.ShapeOutline, outlinePen);
			// Fill brush settings for this shape.
			DslDiagrams::BrushSettings backgroundBrush = new DslDiagrams::BrushSettings();
			backgroundBrush.Color = global::System.Drawing.Color.FromKnownColor(global::System.Drawing.KnownColor.PaleGreen);
			classStyleSet.OverrideBrush(DslDiagrams::DiagramBrushes.ShapeBackground, backgroundBrush);
		
		}
		
		/// <summary>
		/// Indicates whether this shape displays a background gradient.
		/// </summary>
		public override bool HasBackgroundGradient
		{
			get
			{
				return true;
			}
		}
		
		/// <summary>
		/// Indicates the direction of the gradient.
		/// </summary>
		public override global::System.Drawing.Drawing2D.LinearGradientMode BackgroundGradientMode
		{
			get
			{
				return global::System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
			}
		}
		/// <summary>
		/// Indicates whether the shape requires connection points.
		/// </summary>
		public override bool HasConnectionPoints
		{
			get
			{
				return true;
			}
		}
		
		/// <summary>
		/// Ensures that four default connection points exist, one at the midpoint of each side of the shape.
		/// </summary>
		/// <remarks>
		/// This method is called each time a connector is created, but connection points only need to be defined once.
		/// </remarks>
		/// <param name="link">Connector which will be connected to this shape.</param>
		public override void EnsureConnectionPoints(DslDiagrams::LinkShape link)
		{
			if (this.ConnectionPoints.Count == 0)
			{
				DslDiagrams::RectangleD absoluteBoundingBox = this.AbsoluteBoundingBox;
				this.CreateConnectionPoint(new DslDiagrams::PointD(absoluteBoundingBox.Center.X, absoluteBoundingBox.Bottom));
				this.CreateConnectionPoint(new DslDiagrams::PointD(absoluteBoundingBox.Center.X, absoluteBoundingBox.Top));
				this.CreateConnectionPoint(new DslDiagrams::PointD(absoluteBoundingBox.Left, absoluteBoundingBox.Center.Y));
				this.CreateConnectionPoint(new DslDiagrams::PointD(absoluteBoundingBox.Right, absoluteBoundingBox.Center.Y));
			}
		}
		
		/// <summary>
		/// Specifies the geometry used by this shape
		/// </summary>
		public override DslDiagrams::ShapeGeometry ShapeGeometry
		{
			get
			{
				return DslDiagrams::ShapeGeometries.RoundedRectangle;
			}
		}
		#endregion
		#region Decorators
		/// <summary>
		/// Initialize the collection of shape fields associated with this shape type.
		/// </summary>
		protected override void InitializeShapeFields(global::System.Collections.Generic.IList<DslDiagrams::ShapeField> shapeFields)
		{
			base.InitializeShapeFields(shapeFields);
			DslDiagrams::TextField field1 = new DslDiagrams::TextField("Name");
			field1.DefaultText = global::Worm.Designer.DesignerDomainModel.SingletonResourceManager.GetString("EntityShapeNameDefaultText");
			field1.DefaultFocusable = true;
			field1.DefaultAutoSize = true;
			field1.AnchoringBehavior.MinimumHeightInLines = 1;
			field1.AnchoringBehavior.MinimumWidthInCharacters = 1;
			field1.DefaultAccessibleState = global::System.Windows.Forms.AccessibleStates.Invisible;
			shapeFields.Add(field1);
			
			DslDiagrams::ChevronButtonField field2 = new DslDiagrams::ChevronButtonField("ExpandCollapseDecorator1");
			field2.DefaultSelectable = false;
			field2.DefaultFocusable = false;
			shapeFields.Add(field2);
			
		}
		
		/// <summary>
		/// Initialize the collection of decorators associated with this shape type.  This method also
		/// creates shape fields for outer decorators, because these are not part of the shape fields collection
		/// associated with the shape, so they must be created here rather than in InitializeShapeFields.
		/// </summary>
		protected override void InitializeDecorators(global::System.Collections.Generic.IList<DslDiagrams::ShapeField> shapeFields, global::System.Collections.Generic.IList<DslDiagrams::Decorator> decorators)
		{
			base.InitializeDecorators(shapeFields, decorators);
			
			DslDiagrams::ShapeField field1 = DslDiagrams::ShapeElement.FindShapeField(shapeFields, "Name");
			DslDiagrams::Decorator decorator1 = new DslDiagrams::ShapeDecorator(field1, DslDiagrams::ShapeDecoratorPosition.InnerTopCenter, DslDiagrams::PointD.Empty);
			decorators.Add(decorator1);
				
			DslDiagrams::ShapeField field2 = DslDiagrams::ShapeElement.FindShapeField(shapeFields, "ExpandCollapseDecorator1");
			DslDiagrams::Decorator decorator2 = new DslDiagrams::ExpandCollapseDecorator(this.Store, (DslDiagrams::ToggleButtonField)field2, DslDiagrams::ShapeDecoratorPosition.InnerTopRight, DslDiagrams::PointD.Empty);
			decorators.Add(decorator2);
				
		}
		
		/// <summary>
		/// Ensure outer decorators are placed appropriately.  This is called during view fixup,
		/// after the shape has been associated with the model element.
		/// </summary>
		public override void OnBoundsFixup(DslDiagrams::BoundsFixupState fixupState, int iteration, bool createdDuringViewFixup)
		{
			base.OnBoundsFixup(fixupState, iteration, createdDuringViewFixup);
			
			if(iteration == 0)
			{
				foreach(DslDiagrams::Decorator decorator in this.Decorators)
				{
					if(decorator.RequiresHost)
					{
						decorator.RepositionHostShape(decorator.GetHostShape(this));
					}
				}
			}
		}
		#endregion
		#region CompartmentShape code
		/// <summary>
		/// Returns a value indicating whether compartment header should be visible if there is only one of them.
		/// </summary>
		public override bool IsSingleCompartmentHeaderVisible
		{
			get { return true; }
		}
		
		private static DslDiagrams::CompartmentDescription[] compartmentDescriptions;
		
		/// <summary>
		/// Gets an array of CompartmentDescription for all compartments shown on this shape
		/// (including compartments defined on base shapes).
		/// </summary>
		/// <returns></returns>
		public override DslDiagrams::CompartmentDescription[] GetCompartmentDescriptions()
		{
			if(compartmentDescriptions == null)
			{
				// Initialize the array of compartment descriptions if we haven't done so already. 
				// First we get any compartment descriptions in base shapes, and add on any compartments
				// that are defined on this shape. 
				DslDiagrams::CompartmentDescription[] baseCompartmentDescriptions = base.GetCompartmentDescriptions();
				
				int localCompartmentsOffset = 0;
				if(baseCompartmentDescriptions!=null)
				{
					localCompartmentsOffset = baseCompartmentDescriptions.Length;
				}
				compartmentDescriptions = new DslDiagrams::ElementListCompartmentDescription[1+localCompartmentsOffset];
				
				if(baseCompartmentDescriptions!=null)
				{
					baseCompartmentDescriptions.CopyTo(compartmentDescriptions, 0);	
				}
				{
					string title = global::Worm.Designer.DesignerDomainModel.SingletonResourceManager.GetString("EntityShapePropertiesTitle");
					DslDiagrams::ElementListCompartmentDescription descriptor = new DslDiagrams::ElementListCompartmentDescription("Properties", title, 
						global::System.Drawing.Color.FromKnownColor(global::System.Drawing.KnownColor.Honeydew), false, 
						global::System.Drawing.Color.FromKnownColor(global::System.Drawing.KnownColor.White), false,
						null, null,
						false);
					compartmentDescriptions[localCompartmentsOffset+0] = descriptor;
				}
			}
			
			return EntityShape.compartmentDescriptions;
		}
		
		private static global::System.Collections.Generic.Dictionary<global::System.Type, DslDiagrams::CompartmentMapping[]> compartmentMappings;
		
		/// <summary>
		/// Gets an array of CompartmentMappings for all compartments displayed on this shape
		/// (including compartment maps defined on base shapes). 
		/// </summary>
		/// <param name="melType">The type of the DomainClass that this shape is mapped to</param>
		/// <returns></returns>
		protected override DslDiagrams::CompartmentMapping[] GetCompartmentMappings(global::System.Type melType)
		{
			if(melType==null) throw new global::System.ArgumentNullException("melType");
			
			if(compartmentMappings==null)
			{
				// Initialize the table of compartment mappings if we haven't done so already. 
				// The table contains an array of CompartmentMapping for every Type that this
				// shape can be mapped to. 
				compartmentMappings = new global::System.Collections.Generic.Dictionary<global::System.Type, DslDiagrams::CompartmentMapping[]>();
				{
					// First we get the mappings defined for the base shape, and add on any mappings defined for this
					// shape. 
					DslDiagrams::CompartmentMapping[] baseMappings = base.GetCompartmentMappings(typeof(global::Worm.Designer.Entity));
					int localCompartmentMappingsOffset = 0;
					if(baseMappings!=null)
					{
						localCompartmentMappingsOffset = baseMappings.Length;
					}
					DslDiagrams::CompartmentMapping[] mappings = new DslDiagrams::CompartmentMapping[1+localCompartmentMappingsOffset];
					
					if(baseMappings!=null)
					{
						baseMappings.CopyTo(mappings, 0);
					}
					mappings[localCompartmentMappingsOffset+0] = new DslDiagrams::ElementListCompartmentMapping(
																				"Properties", 
																				global::Worm.Designer.Property.NameDomainPropertyId, 
																				global::Worm.Designer.Property.DomainClassId, 
																				GetElementsFromEntityForProperties,
																				null,
																				null,
																				null);
					compartmentMappings.Add(typeof(global::Worm.Designer.Entity), mappings);
				}
			}
			
			// See if we can find the mapping being requested directly in the table. 
			DslDiagrams::CompartmentMapping[] returnValue;
			if(compartmentMappings.TryGetValue(melType, out returnValue))
			{
				return returnValue;
			}
			
			// If not, loop through the types in the table, and find the 'most derived' base
			// class of melType. 
			global::System.Type selectedMappedType = null;
			foreach(global::System.Type mappedType in compartmentMappings.Keys)
			{
				if(mappedType.IsAssignableFrom(melType) && (selectedMappedType==null || selectedMappedType.IsAssignableFrom(mappedType)))
				{
					selectedMappedType = mappedType;
				}
			}
			if(selectedMappedType!=null)
			{
				return compartmentMappings[selectedMappedType];
			}
			return new DslDiagrams::CompartmentMapping[] {};
		}
		
			#region DomainPath traversal methods to get the list of elements to display in a compartment.
			internal static global::System.Collections.IList GetElementsFromEntityForProperties(DslModeling::ModelElement element)
			{
				global::Worm.Designer.Entity root = (global::Worm.Designer.Entity)element;
					// Segments 0 and 1
					DslModeling::LinkedElementCollection<global::Worm.Designer.Property> result = root.Properties;
				return result;
			}
			#endregion
		
		#endregion
		#region Constructors, domain class Id
	
		/// <summary>
		/// EntityShape domain class Id.
		/// </summary>
		public static readonly new global::System.Guid DomainClassId = new global::System.Guid(0x1ac7c68e, 0x23be, 0x47ec, 0xa6, 0xfe, 0x2e, 0xce, 0x8f, 0x63, 0xe4, 0x9c);
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="store">Store where new element is to be created.</param>
		/// <param name="propertyAssignments">List of domain property id/value pairs to set once the element is created.</param>
		public EntityShape(DslModeling::Store store, params DslModeling::PropertyAssignment[] propertyAssignments)
			: this(store != null ? store.DefaultPartition : null, propertyAssignments)
		{
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="partition">Partition where new element is to be created.</param>
		/// <param name="propertyAssignments">List of domain property id/value pairs to set once the element is created.</param>
		public EntityShape(DslModeling::Partition partition, params DslModeling::PropertyAssignment[] propertyAssignments)
			: base(partition, propertyAssignments)
		{
		}
		#endregion
	}
}

