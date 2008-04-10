<?xml version="1.0" encoding="utf-8"?>
<Dsl dslVersion="1.0.0.0" Id="d94f13a8-3ed2-4e0e-b6ad-af96864c44a6" Description="Description for Worm.Designer.Designer" Name="Designer" DisplayName="Designer" Namespace="Worm.Designer" ProductName="Designer" CompanyName="Worm" PackageGuid="9c47e690-639c-42c0-99d0-4e28ee1822db" PackageNamespace="Worm.Designer" xmlns="http://schemas.microsoft.com/VisualStudio/2005/DslTools/DslDefinitionModel">
  <Classes>
    <DomainClass Id="2d55930a-31b5-4bd4-9a68-1bdec0d265fe" Description="The root in which all other elements are embedded. Appears as a diagram." Name="WormModel" DisplayName="Worm Model" Namespace="Worm.Designer">
      <Properties>
        <DomainProperty Id="ef57328f-fa14-4ae6-b1b5-66eefd00ff48" Description="Default namespace used in case entity has no any namespace specified" Name="DefaultNamespace" DisplayName="Default Namespace">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="bd1e5b90-b0b3-4647-a044-f4f459241872" Description="Schema Version" Name="SchemaVersion" DisplayName="Schema Version" DefaultValue="1">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
      </Properties>
      <ElementMergeDirectives>
        <ElementMergeDirective>
          <Index>
            <DomainClassMoniker Name="Entity" />
          </Index>
          <LinkCreationPaths>
            <DomainPath>WormModelHasEntities.Entities</DomainPath>
          </LinkCreationPaths>
        </ElementMergeDirective>
      </ElementMergeDirectives>
    </DomainClass>
    <DomainClass Id="4fd18942-120e-481b-9834-7c4d652242e1" Description="Сущность" Name="Entity" DisplayName="Entity" Namespace="Worm.Designer">
      <Properties>
        <DomainProperty Id="44616f38-82f8-43e3-bde6-d55d7e11e189" Description="Идентификатор сущности" Name="IdProperty" DisplayName="Id" DefaultValue="entity id" Kind="Calculated">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="a07738d3-b31e-4ae8-bddd-0de25ae5a420" Description="Наименование сущности" Name="Name" DisplayName="Name" DefaultValue="Entity" IsElementName="true">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="c0abea7d-0b57-4ddb-806f-1e0fdf96bb39" Description="Пространство имен для сущности" Name="Namespace" DisplayName="Namespace">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="41ea2ab0-0bd7-4116-82a4-63a0a386ace4" Description="Entity generator behaviour" Name="Behaviour" DisplayName="Behaviour">
          <Type>
            <ExternalTypeMoniker Name="/Worm.CodeGen.Core/EntityBehaviuor" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="b7e0a887-5470-48c5-8638-554df5318014" Description="Описание сущности" Name="Description" DisplayName="Description">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="fee6dfaf-50e0-4a7a-864d-90e3497b5246" Description="Определяет генерировать generic или строго типизированные методы" Name="UseGenerics" DisplayName="Use Generics">
          <Type>
            <ExternalTypeMoniker Name="/System/Boolean" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="e3b3d3c8-0ff2-48d5-9460-c087e81f051a" Description="Определяет генерировать интерфейс для сущности" Name="MakeInterface" DisplayName="Make Interface">
          <Type>
            <ExternalTypeMoniker Name="/System/Boolean" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="24a9a6f7-5964-44fd-9547-5b9ec4e25da9" Description="ИД базовой сущности" Name="BaseEntity" DisplayName="Base Entity">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BaseEntityUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
      </Properties>
      <ElementMergeDirectives>
        <ElementMergeDirective>
          <Index>
            <DomainClassMoniker Name="Table" />
          </Index>
          <LinkCreationPaths>
            <DomainPath>EntityHasTables.Tables</DomainPath>
          </LinkCreationPaths>
        </ElementMergeDirective>
        <ElementMergeDirective>
          <Index>
            <DomainClassMoniker Name="Property" />
          </Index>
          <LinkCreationPaths>
            <DomainPath>EntityHasProperties.Properties</DomainPath>
          </LinkCreationPaths>
        </ElementMergeDirective>
        <ElementMergeDirective>
          <Index>
            <DomainClassMoniker Name="SupressedProperty" />
          </Index>
          <LinkCreationPaths>
            <DomainPath>EntityHasSupressedProperties.SupressedProperties</DomainPath>
          </LinkCreationPaths>
        </ElementMergeDirective>
        <ElementMergeDirective>
          <Index>
            <DomainClassMoniker Name="SelfRelation" />
          </Index>
          <LinkCreationPaths>
            <DomainPath>EntityHasSelfRelations.SelfRelations</DomainPath>
          </LinkCreationPaths>
        </ElementMergeDirective>
      </ElementMergeDirectives>
    </DomainClass>
    <DomainClass Id="d8167da3-fa3f-4ed4-8a42-1e613b5f9902" Description="Description for Worm.Designer.Table" Name="Table" DisplayName="Table" Namespace="Worm.Designer">
      <Properties>
        <DomainProperty Id="a17a80a3-b19c-4719-81d0-6015be74217d" Description="Table name" Name="Name" DisplayName="Name" IsElementName="true">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="1be4d74c-edb8-4f30-a29a-fcd3e0f8baf7" Description="Description for Worm.Designer.Table.Id" Name="IdProperty" DisplayName="Id" Kind="Calculated">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="cca3b40b-cd96-4448-a20c-30c6e8592209" Description="Schema name for table" Name="Schema" DisplayName="Schema" DefaultValue="dbo">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
      </Properties>
    </DomainClass>
    <DomainClass Id="a3ab88fb-d6b4-420b-8325-029f77e93036" Description="Description for Worm.Designer.Property" Name="Property" DisplayName="Property" Namespace="Worm.Designer">
      <Properties>
        <DomainProperty Id="8b3c0c4e-2990-403c-9528-4acabe75ecdd" Description="Property name" Name="Name" DisplayName="Name" IsElementName="true">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="72a756c7-b007-4ade-ac88-696b306a4153" Description="Property type" Name="Type" DisplayName="Type" DefaultValue="varchar">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.TypeUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="3f78744d-0bae-4bc7-8f97-8bd98bba72e3" Description="Property description" Name="Description" DisplayName="Description">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="4c13ac94-3d96-4ae5-ada6-c63cb97fc2c2" Description="Field name" Name="FieldName" DisplayName="Field Name">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="8980f42e-ee24-4198-8a4e-b6965e0c0f0a" Description="Property table" Name="Table" DisplayName="Table">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.TableUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="ff8c776c-7228-4b15-8903-e744348ea9f9" Description="Field access level" Name="FieldAccessLevel" DisplayName="Field Access Level">
          <Type>
            <ExternalTypeMoniker Name="/Worm.CodeGen.Core/AccessLevel" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="0e08e4e5-983f-434a-b23d-c230096ed20a" Description="Property alias" Name="Alias" DisplayName="Alias">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="67ec9948-c2e5-406c-a7ec-21932a4f97f5" Description="Access Level" Name="AccessLevel" DisplayName="Access Level">
          <Type>
            <ExternalTypeMoniker Name="/Worm.CodeGen.Core/AccessLevel" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="32ebc3a6-9aa6-4fec-bcfa-b84adf684a9a" Description="Nullable" Name="Nullable" DisplayName="Nullable" DefaultValue="false">
          <Type>
            <ExternalTypeMoniker Name="/System/Boolean" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="2a909caa-923a-4796-bb3c-c16f23664b16" Description="Description for Worm.Designer.Property.Attributes" Name="Attributes" DisplayName="Attributes">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.FlagEnumUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
      </Properties>
    </DomainClass>
    <DomainClass Id="bc04f16d-d6ae-4ae2-b470-b81094434fb9" Description="SupressedProperty" Name="SupressedProperty" DisplayName="Supressed Property" Namespace="Worm.Designer">
      <Properties>
        <DomainProperty Id="a3bda040-3c8c-41e5-9cd4-045fac074ca9" Description="Description for Worm.Designer.SupressedProperty.Name" Name="Name" DisplayName="Name" IsElementName="true">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.SupressedPropertyUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
      </Properties>
    </DomainClass>
    <DomainClass Id="89305501-ec2a-47a4-8d17-2f7a08ce48bf" Description="Связь сущности самой с собой" Name="SelfRelation" DisplayName="Self Relation" Namespace="Worm.Designer">
      <Properties>
        <DomainProperty Id="4cc7eb74-c9f9-4cfd-9cf7-8f27608c036b" Description="Имя связанной сущности для генерации методов" Name="DirectAccessor" DisplayName="Direct Accessor">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="45766242-ccce-4e5a-8ed7-14fff5253190" Description="аскадное удаление" Name="DirectCascadeDelete" DisplayName="Direct Cascade Delete">
          <Type>
            <ExternalTypeMoniker Name="/System/Boolean" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="18d076fb-382a-4f46-ae6d-5cf468778ff1" Description="Имя поля таблицы связи" Name="DirectFieldName" DisplayName="Direct Field Name">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="00f08749-66ca-48dd-93d9-5952d87203fb" Description="Disable relation" Name="Disabled" DisplayName="Disabled">
          <Type>
            <ExternalTypeMoniker Name="/System/Boolean" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="763e2750-9675-41b9-8cce-3250b1032a5a" Description="Имя связанной сущности для генерации методов" Name="ReverseAccessor" DisplayName="Reverse Accessor">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="ba170593-27b7-4523-9d3a-a6551c492c26" Description="аскадное удаление" Name="ReverseCascadeDelete" DisplayName="Reverse Cascade Delete">
          <Type>
            <ExternalTypeMoniker Name="/System/Boolean" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="d4f2dffb-91fb-419b-b5ca-aca2fa83ef3b" Description="Имя поля таблицы связи" Name="ReverseFieldName" DisplayName="Reverse Field Name">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="e0558ce9-8a97-43a2-b33c-ab6c6e1bcde8" Description="Имя таблицы связи" Name="Table" DisplayName="Table">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.TableUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="e4d62605-d81b-46e7-a5d6-6266e1ae9a1d" Description="Сущность реализующая связь" Name="UnderlyingEntity" DisplayName="Underlying Entity">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="9c577e94-1d95-40ab-af19-34949cd8de47" Description="&gt;Тип связанной сущности для генерации методов" Name="DirectAccessedEntityType" DisplayName="Direct Accessed Entity Type">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="03fe3ca8-b3f8-4ac6-bd2c-10c8247cf2fc" Description="&gt;Тип связанной сущности для генерации методов" Name="ReverseAccessedEntityType" DisplayName="Reverse Accessed Entity Type">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="f26ad602-9dad-479d-80f7-fb49403bd323" Description="Name" Name="Name" DisplayName="Name" IsElementName="true">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
      </Properties>
    </DomainClass>
  </Classes>
  <Relationships>
    <DomainRelationship Id="3f154ba4-2f25-4df8-8b40-0675dbdcc663" Description="Description for Worm.Designer.WormModelHasEntities" Name="WormModelHasEntities" DisplayName="Worm Model Has Entities" Namespace="Worm.Designer" IsEmbedding="true">
      <Source>
        <DomainRole Id="8a646f6a-d1e4-43e9-87fc-5ef11b03e213" Description="Description for Worm.Designer.WormModelHasEntities.WormModel" Name="WormModel" DisplayName="Worm Model" PropertyName="Entities" PropertyDisplayName="Entities">
          <RolePlayer>
            <DomainClassMoniker Name="WormModel" />
          </RolePlayer>
        </DomainRole>
      </Source>
      <Target>
        <DomainRole Id="8de3e6d9-2399-4f8c-be54-9905b94a7d2c" Description="Description for Worm.Designer.WormModelHasEntities.Entity" Name="Entity" DisplayName="Entity" PropertyName="WormModel" Multiplicity="One" PropagatesDelete="true" PropagatesCopy="true" PropertyDisplayName="Worm Model">
          <RolePlayer>
            <DomainClassMoniker Name="Entity" />
          </RolePlayer>
        </DomainRole>
      </Target>
    </DomainRelationship>
    <DomainRelationship Id="f86803b8-bf84-45dc-8ee2-7443df861052" Description="Description for Worm.Designer.EntityHasTables" Name="EntityHasTables" DisplayName="Entity Has Tables" Namespace="Worm.Designer" IsEmbedding="true">
      <Source>
        <DomainRole Id="a188ebf0-1dce-4405-b355-2de97a9603da" Description="Description for Worm.Designer.EntityHasTables.Entity" Name="Entity" DisplayName="Entity" PropertyName="Tables" PropertyDisplayName="Tables">
          <RolePlayer>
            <DomainClassMoniker Name="Entity" />
          </RolePlayer>
        </DomainRole>
      </Source>
      <Target>
        <DomainRole Id="db03c26a-9456-4f67-8f06-5384377d2d98" Description="Description for Worm.Designer.EntityHasTables.Table" Name="Table" DisplayName="Table" PropertyName="Entity" Multiplicity="ZeroOne" PropagatesDelete="true" PropagatesCopy="true" PropertyDisplayName="Entity">
          <RolePlayer>
            <DomainClassMoniker Name="Table" />
          </RolePlayer>
        </DomainRole>
      </Target>
    </DomainRelationship>
    <DomainRelationship Id="372f6686-3fea-4da3-aedd-2a56af8723a4" Description="Description for Worm.Designer.EntityHasProperties" Name="EntityHasProperties" DisplayName="Entity Has Properties" Namespace="Worm.Designer" IsEmbedding="true">
      <Source>
        <DomainRole Id="04d141e8-2ed8-431a-afba-e26de5c2e34b" Description="Description for Worm.Designer.EntityHasProperties.Entity" Name="Entity" DisplayName="Entity" PropertyName="Properties" PropertyDisplayName="Properties">
          <RolePlayer>
            <DomainClassMoniker Name="Entity" />
          </RolePlayer>
        </DomainRole>
      </Source>
      <Target>
        <DomainRole Id="08875147-896c-4b6d-960e-1c6caac52a0b" Description="Description for Worm.Designer.EntityHasProperties.Property" Name="Property" DisplayName="Property" PropertyName="Entity" Multiplicity="ZeroOne" PropagatesDelete="true" PropagatesCopy="true" PropertyDisplayName="Entity">
          <RolePlayer>
            <DomainClassMoniker Name="Property" />
          </RolePlayer>
        </DomainRole>
      </Target>
    </DomainRelationship>
    <DomainRelationship Id="c0c83bed-1d6c-46fc-97ed-b723d83d36fc" Description="Description for Worm.Designer.EntityReferencesTargetEntities" Name="EntityReferencesTargetEntities" DisplayName="Entity References Target Entities" Namespace="Worm.Designer">
      <Properties>
        <DomainProperty Id="6bd17a24-e3bd-4412-a6c6-2c00b7824678" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Undelying entity" Name="UndelyingEntity" DisplayName="Undelying entity">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="ecd23e88-6e56-438b-9f10-c56c1aba7cbf" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Left Cascade Delete" Name="LeftCascadeDelete" DisplayName="Left Cascade Delete">
          <Type>
            <ExternalTypeMoniker Name="/System/Boolean" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="ec48a8e3-f1b3-4c88-bbd4-644451ce13b0" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Left Field Name" Name="LeftFieldName" DisplayName="Left Field Name">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="3bf5da1c-8e83-42ab-8179-77194758fbaf" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Left Accessor Name" Name="LeftAccessorName" DisplayName="Left Accessor Name">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="8c000c96-92a2-4eeb-871b-a09fd6a2e10d" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Disabled" Name="Disabled" DisplayName="Disabled">
          <Type>
            <ExternalTypeMoniker Name="/System/Boolean" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="202b0d26-d30c-4a1f-96f1-0b6bdbce7db3" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Table" Name="Table" DisplayName="Table">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="845e0f02-ed94-402a-b087-c9f05a71b9bc" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Left Entity" Name="LeftEntity" DisplayName="Left Entity">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="880ca553-e244-4497-b380-e1445219bf73" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Left Accessed Entity Type" Name="LeftAccessedEntityType" DisplayName="Left Accessed Entity Type">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="2c3390fd-432a-4268-96f7-4f8859d74b96" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Right Accessed Entity Type" Name="RightAccessedEntityType" DisplayName="Right Accessed Entity Type">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="2e71233d-d194-4929-bbb4-27cf3ae1d389" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Right Field Name" Name="RightFieldName" DisplayName="Right Field Name">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="7c7b3972-2e7d-4b2c-bee9-49cb3c2d8742" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Right Cascade Delete" Name="RightCascadeDelete" DisplayName="Right Cascade Delete">
          <Type>
            <ExternalTypeMoniker Name="/System/Boolean" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="ef1396f8-92b2-4af5-bf2f-7df39cc03e01" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Right Accessor Name" Name="RightAccessorName" DisplayName="Right Accessor Name">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="25351206-2764-46dc-8cb2-b7932c34da59" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Right Entity" Name="RightEntity" DisplayName="Right Entity">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
      </Properties>
      <Source>
        <DomainRole Id="88046d41-3c77-4ee6-8b96-77bf9fdd4644" Description="Description for Worm.Designer.EntityReferencesTargetEntities.SourceEntity" Name="SourceEntity" DisplayName="Source Entity" PropertyName="TargetEntities" PropertyDisplayName="Target Entities">
          <RolePlayer>
            <DomainClassMoniker Name="Entity" />
          </RolePlayer>
        </DomainRole>
      </Source>
      <Target>
        <DomainRole Id="484c3fcf-49d8-43a2-9e75-572aa4758296" Description="Description for Worm.Designer.EntityReferencesTargetEntities.TargetEntity" Name="TargetEntity" DisplayName="Target Entity" PropertyName="SourceEntities" PropertyDisplayName="Source Entities">
          <RolePlayer>
            <DomainClassMoniker Name="Entity" />
          </RolePlayer>
        </DomainRole>
      </Target>
    </DomainRelationship>
    <DomainRelationship Id="6a934d4c-a2a5-447c-92d5-08ee15072542" Description="Description for Worm.Designer.EntityHasSupressedProperties" Name="EntityHasSupressedProperties" DisplayName="Entity Has Supressed Properties" Namespace="Worm.Designer" IsEmbedding="true">
      <Source>
        <DomainRole Id="179f911a-4814-40bc-b3bd-60eb629a814f" Description="Description for Worm.Designer.EntityHasSupressedProperties.Entity" Name="Entity" DisplayName="Entity" PropertyName="SupressedProperties" PropertyDisplayName="Supressed Properties">
          <RolePlayer>
            <DomainClassMoniker Name="Entity" />
          </RolePlayer>
        </DomainRole>
      </Source>
      <Target>
        <DomainRole Id="fa36bfe0-f9fe-48d7-ab78-69ae22774bea" Description="Description for Worm.Designer.EntityHasSupressedProperties.SupressedProperty" Name="SupressedProperty" DisplayName="Supressed Property" PropertyName="Entity" Multiplicity="One" PropagatesDelete="true" PropagatesCopy="true" PropertyDisplayName="Entity">
          <RolePlayer>
            <DomainClassMoniker Name="SupressedProperty" />
          </RolePlayer>
        </DomainRole>
      </Target>
    </DomainRelationship>
    <DomainRelationship Id="074a5c18-b357-40b1-851f-8a5ba4062473" Description="Description for Worm.Designer.EntityHasSelfRelations" Name="EntityHasSelfRelations" DisplayName="Entity Has Self Relations" Namespace="Worm.Designer" IsEmbedding="true">
      <Source>
        <DomainRole Id="a792cb12-7597-42ff-ac48-ed7cf0f2a855" Description="Description for Worm.Designer.EntityHasSelfRelations.Entity" Name="Entity" DisplayName="Entity" PropertyName="SelfRelations" PropertyDisplayName="Self Relations">
          <RolePlayer>
            <DomainClassMoniker Name="Entity" />
          </RolePlayer>
        </DomainRole>
      </Source>
      <Target>
        <DomainRole Id="748e66b7-2b0f-4b00-a370-8f75a503bec0" Description="Description for Worm.Designer.EntityHasSelfRelations.SelfRelation" Name="SelfRelation" DisplayName="Self Relation" PropertyName="Entity" Multiplicity="One" PropagatesDelete="true" PropagatesCopy="true" PropertyDisplayName="Entity">
          <RolePlayer>
            <DomainClassMoniker Name="SelfRelation" />
          </RolePlayer>
        </DomainRole>
      </Target>
    </DomainRelationship>
  </Relationships>
  <Types>
    <ExternalType Name="DateTime" Namespace="System" />
    <ExternalType Name="String" Namespace="System" />
    <ExternalType Name="Int16" Namespace="System" />
    <ExternalType Name="Int32" Namespace="System" />
    <ExternalType Name="Int64" Namespace="System" />
    <ExternalType Name="UInt16" Namespace="System" />
    <ExternalType Name="UInt32" Namespace="System" />
    <ExternalType Name="UInt64" Namespace="System" />
    <ExternalType Name="SByte" Namespace="System" />
    <ExternalType Name="Byte" Namespace="System" />
    <ExternalType Name="Double" Namespace="System" />
    <ExternalType Name="Single" Namespace="System" />
    <ExternalType Name="Guid" Namespace="System" />
    <ExternalType Name="Boolean" Namespace="System" />
    <ExternalType Name="Char" Namespace="System" />
    <ExternalType Name="AccessLevel" Namespace="Worm.CodeGen.Core" />
    <ExternalType Name="EntityBehaviuor" Namespace="Worm.CodeGen.Core" />
    <DomainEnumeration Name="PropertyAttribute" Namespace="Worm.Designer" Description="Description for Worm.Designer.PropertyAttribute">
      <Literals>
        <EnumerationLiteral Description="Description for Worm.Designer.PropertyAttribute.PK" Name="PK" Value="" />
        <EnumerationLiteral Description="Description for Worm.Designer.PropertyAttribute.ReadOnly" Name="ReadOnly" Value="" />
        <EnumerationLiteral Description="none" Name="None" Value="" />
        <EnumerationLiteral Description="SyncInsert" Name="SyncInsert" Value="" />
        <EnumerationLiteral Description="SyncUpdate" Name="SyncUpdate" Value="" />
        <EnumerationLiteral Description="InsertDefault" Name="InsertDefault" Value="" />
        <EnumerationLiteral Description="RV" Name="RV" Value="" />
        <EnumerationLiteral Description="RowVersion" Name="RowVersion" Value="" />
        <EnumerationLiteral Description="PrimaryKey" Name="PrimaryKey" Value="" />
        <EnumerationLiteral Description="Private" Name="Private" Value="" />
        <EnumerationLiteral Description="Factory" Name="Factory" Value="" />
      </Literals>
    </DomainEnumeration>
  </Types>
  <Shapes>
    <CompartmentShape Id="1ac7c68e-23be-47ec-a6fe-2ece8f63e49c" Description="Entity" Name="EntityShape" DisplayName="Entity" Namespace="Worm.Designer" FixedTooltipText="Entity Shape" FillColor="PaleGreen" InitialHeight="0.5" OutlineThickness="0.01125" HasDefaultConnectionPoints="true" Geometry="RoundedRectangle">
      <ShapeHasDecorators Position="InnerTopCenter" HorizontalOffset="0" VerticalOffset="0">
        <TextDecorator Name="Name" DisplayName="Name" DefaultText="Name" />
      </ShapeHasDecorators>
      <ShapeHasDecorators Position="InnerTopRight" HorizontalOffset="0" VerticalOffset="0">
        <ExpandCollapseDecorator Name="ExpandCollapseDecorator1" DisplayName="Expand Collapse Decorator1" />
      </ShapeHasDecorators>
      <Compartment TitleFillColor="Honeydew" Name="Tables" Title="Tables">
        <Notes>Список используемых таблиц</Notes>
      </Compartment>
      <Compartment TitleFillColor="Honeydew" Name="Properties" Title="Properties" />
      <Compartment TitleFillColor="Honeydew" Name="SupressedProperties" Title="SupressedProperties" />
      <Compartment FillColor="Window" TitleFillColor="Honeydew" Name="SelfRelation" Title="Self Relation" />
    </CompartmentShape>
  </Shapes>
  <Connectors>
    <Connector Id="225befbc-d957-4e89-b5c2-443a14fd2215" Description="Connector between entities. Represents relationships on the Diagram." Name="EntityConnector" DisplayName="Entity Connector" Namespace="Worm.Designer" FixedTooltipText="Entity connector" Color="255, 192, 128" SourceEndStyle="FilledArrow" TargetEndStyle="FilledArrow" Thickness="0.01" />
  </Connectors>
  <XmlSerializationBehavior Name="DesignerSerializationBehavior" Namespace="Worm.Designer">
    <ClassData>
      <XmlClassData TypeName="WormModel" MonikerAttributeName="" SerializeId="true" MonikerElementName="wormModelMoniker" ElementName="wormModel" MonikerTypeName="WormModelMoniker">
        <DomainClassMoniker Name="WormModel" />
        <ElementData>
          <XmlRelationshipData RoleElementName="entities">
            <DomainRelationshipMoniker Name="WormModelHasEntities" />
          </XmlRelationshipData>
          <XmlPropertyData XmlName="defaultNamespace">
            <DomainPropertyMoniker Name="WormModel/DefaultNamespace" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="schemaVersion">
            <DomainPropertyMoniker Name="WormModel/SchemaVersion" />
          </XmlPropertyData>
        </ElementData>
      </XmlClassData>
      <XmlClassData TypeName="EntityConnector" MonikerAttributeName="" MonikerElementName="entityConnectorMoniker" ElementName="entityConnector" MonikerTypeName="EntityConnectorMoniker">
        <ConnectorMoniker Name="EntityConnector" />
      </XmlClassData>
      <XmlClassData TypeName="DesignerDiagram" MonikerAttributeName="" MonikerElementName="minimalLanguageDiagramMoniker" ElementName="minimalLanguageDiagram" MonikerTypeName="DesignerDiagramMoniker">
        <DiagramMoniker Name="DesignerDiagram" />
      </XmlClassData>
      <XmlClassData TypeName="Entity" MonikerAttributeName="" SerializeId="true" MonikerElementName="entityMoniker" ElementName="entity" MonikerTypeName="EntityMoniker">
        <DomainClassMoniker Name="Entity" />
        <ElementData>
          <XmlPropertyData XmlName="idProperty" Representation="Ignore">
            <DomainPropertyMoniker Name="Entity/IdProperty" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="name">
            <DomainPropertyMoniker Name="Entity/Name" />
          </XmlPropertyData>
          <XmlRelationshipData RoleElementName="tables">
            <DomainRelationshipMoniker Name="EntityHasTables" />
          </XmlRelationshipData>
          <XmlRelationshipData RoleElementName="properties">
            <DomainRelationshipMoniker Name="EntityHasProperties" />
          </XmlRelationshipData>
          <XmlPropertyData XmlName="namespace">
            <DomainPropertyMoniker Name="Entity/Namespace" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="behaviour">
            <DomainPropertyMoniker Name="Entity/Behaviour" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="description">
            <DomainPropertyMoniker Name="Entity/Description" />
          </XmlPropertyData>
          <XmlRelationshipData UseFullForm="true" RoleElementName="targetEntities">
            <DomainRelationshipMoniker Name="EntityReferencesTargetEntities" />
          </XmlRelationshipData>
          <XmlRelationshipData RoleElementName="supressedProperties">
            <DomainRelationshipMoniker Name="EntityHasSupressedProperties" />
          </XmlRelationshipData>
          <XmlPropertyData XmlName="useGenerics">
            <DomainPropertyMoniker Name="Entity/UseGenerics" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="makeInterface">
            <DomainPropertyMoniker Name="Entity/MakeInterface" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="baseEntity">
            <DomainPropertyMoniker Name="Entity/BaseEntity" />
          </XmlPropertyData>
          <XmlRelationshipData RoleElementName="selfRelations">
            <DomainRelationshipMoniker Name="EntityHasSelfRelations" />
          </XmlRelationshipData>
        </ElementData>
      </XmlClassData>
      <XmlClassData TypeName="WormModelHasEntities" MonikerAttributeName="" MonikerElementName="wormModelHasEntitiesMoniker" ElementName="wormModelHasEntities" MonikerTypeName="WormModelHasEntitiesMoniker">
        <DomainRelationshipMoniker Name="WormModelHasEntities" />
      </XmlClassData>
      <XmlClassData TypeName="EntityShape" MonikerAttributeName="" MonikerElementName="entityShapeMoniker" ElementName="entityShape" MonikerTypeName="EntityShapeMoniker">
        <CompartmentShapeMoniker Name="EntityShape" />
      </XmlClassData>
      <XmlClassData TypeName="Table" MonikerAttributeName="" MonikerElementName="tableMoniker" ElementName="table" MonikerTypeName="TableMoniker">
        <DomainClassMoniker Name="Table" />
        <ElementData>
          <XmlPropertyData XmlName="name">
            <DomainPropertyMoniker Name="Table/Name" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="idProperty" Representation="Ignore">
            <DomainPropertyMoniker Name="Table/IdProperty" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="schema">
            <DomainPropertyMoniker Name="Table/Schema" />
          </XmlPropertyData>
        </ElementData>
      </XmlClassData>
      <XmlClassData TypeName="Property" MonikerAttributeName="" MonikerElementName="propertyMoniker" ElementName="property" MonikerTypeName="PropertyMoniker">
        <DomainClassMoniker Name="Property" />
        <ElementData>
          <XmlPropertyData XmlName="name">
            <DomainPropertyMoniker Name="Property/Name" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="type">
            <DomainPropertyMoniker Name="Property/Type" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="description">
            <DomainPropertyMoniker Name="Property/Description" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="fieldName">
            <DomainPropertyMoniker Name="Property/FieldName" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="table">
            <DomainPropertyMoniker Name="Property/Table" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="fieldAccessLevel">
            <DomainPropertyMoniker Name="Property/FieldAccessLevel" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="alias">
            <DomainPropertyMoniker Name="Property/Alias" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="accessLevel">
            <DomainPropertyMoniker Name="Property/AccessLevel" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="nullable">
            <DomainPropertyMoniker Name="Property/Nullable" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="attributes">
            <DomainPropertyMoniker Name="Property/Attributes" />
          </XmlPropertyData>
        </ElementData>
      </XmlClassData>
      <XmlClassData TypeName="EntityHasTables" MonikerAttributeName="" MonikerElementName="entityHasTablesMoniker" ElementName="entityHasTables" MonikerTypeName="EntityHasTablesMoniker">
        <DomainRelationshipMoniker Name="EntityHasTables" />
      </XmlClassData>
      <XmlClassData TypeName="EntityHasProperties" MonikerAttributeName="" MonikerElementName="entityHasPropertiesMoniker" ElementName="entityHasProperties" MonikerTypeName="EntityHasPropertiesMoniker">
        <DomainRelationshipMoniker Name="EntityHasProperties" />
      </XmlClassData>
      <XmlClassData TypeName="EntityReferencesTargetEntities" MonikerAttributeName="" MonikerElementName="entityReferencesTargetEntitiesMoniker" ElementName="entityReferencesTargetEntities" MonikerTypeName="EntityReferencesTargetEntitiesMoniker">
        <DomainRelationshipMoniker Name="EntityReferencesTargetEntities" />
        <ElementData>
          <XmlPropertyData XmlName="undelyingEntity">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/UndelyingEntity" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="leftCascadeDelete">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/LeftCascadeDelete" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="leftFieldName">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/LeftFieldName" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="leftAccessorName">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/LeftAccessorName" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="disabled">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/Disabled" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="table">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/Table" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="leftEntity">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/LeftEntity" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="leftAccessedEntityType">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/LeftAccessedEntityType" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="rightAccessedEntityType">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/RightAccessedEntityType" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="rightFieldName">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/RightFieldName" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="rightCascadeDelete">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/RightCascadeDelete" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="rightAccessorName">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/RightAccessorName" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="rightEntity">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/RightEntity" />
          </XmlPropertyData>
        </ElementData>
      </XmlClassData>
      <XmlClassData TypeName="SupressedProperty" MonikerAttributeName="" MonikerElementName="supressedPropertyMoniker" ElementName="supressedProperty" MonikerTypeName="SupressedPropertyMoniker">
        <DomainClassMoniker Name="SupressedProperty" />
        <ElementData>
          <XmlPropertyData XmlName="name">
            <DomainPropertyMoniker Name="SupressedProperty/Name" />
          </XmlPropertyData>
        </ElementData>
      </XmlClassData>
      <XmlClassData TypeName="EntityHasSupressedProperties" MonikerAttributeName="" MonikerElementName="entityHasSupressedPropertiesMoniker" ElementName="entityHasSupressedProperties" MonikerTypeName="EntityHasSupressedPropertiesMoniker">
        <DomainRelationshipMoniker Name="EntityHasSupressedProperties" />
      </XmlClassData>
      <XmlClassData TypeName="SelfRelation" MonikerAttributeName="" MonikerElementName="selfRelationMoniker" ElementName="selfRelation" MonikerTypeName="SelfRelationMoniker">
        <DomainClassMoniker Name="SelfRelation" />
        <ElementData>
          <XmlPropertyData XmlName="directAccessor">
            <DomainPropertyMoniker Name="SelfRelation/DirectAccessor" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="directCascadeDelete">
            <DomainPropertyMoniker Name="SelfRelation/DirectCascadeDelete" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="directFieldName">
            <DomainPropertyMoniker Name="SelfRelation/DirectFieldName" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="disabled">
            <DomainPropertyMoniker Name="SelfRelation/Disabled" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="reverseAccessor">
            <DomainPropertyMoniker Name="SelfRelation/ReverseAccessor" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="reverseCascadeDelete">
            <DomainPropertyMoniker Name="SelfRelation/ReverseCascadeDelete" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="reverseFieldName">
            <DomainPropertyMoniker Name="SelfRelation/ReverseFieldName" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="table">
            <DomainPropertyMoniker Name="SelfRelation/Table" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="underlyingEntity">
            <DomainPropertyMoniker Name="SelfRelation/UnderlyingEntity" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="directAccessedEntityType">
            <DomainPropertyMoniker Name="SelfRelation/DirectAccessedEntityType" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="reverseAccessedEntityType">
            <DomainPropertyMoniker Name="SelfRelation/ReverseAccessedEntityType" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="name">
            <DomainPropertyMoniker Name="SelfRelation/Name" />
          </XmlPropertyData>
        </ElementData>
      </XmlClassData>
      <XmlClassData TypeName="EntityHasSelfRelations" MonikerAttributeName="" MonikerElementName="entityHasSelfRelationsMoniker" ElementName="entityHasSelfRelations" MonikerTypeName="EntityHasSelfRelationsMoniker">
        <DomainRelationshipMoniker Name="EntityHasSelfRelations" />
      </XmlClassData>
    </ClassData>
  </XmlSerializationBehavior>
  <ExplorerBehavior Name="DesignerExplorer" />
  <ConnectionBuilders>
    <ConnectionBuilder Name="EntityReferencesTargetEntitiesBuilder">
      <LinkConnectDirective>
        <DomainRelationshipMoniker Name="EntityReferencesTargetEntities" />
        <SourceDirectives>
          <RolePlayerConnectDirective>
            <AcceptingClass>
              <DomainClassMoniker Name="Entity" />
            </AcceptingClass>
          </RolePlayerConnectDirective>
        </SourceDirectives>
        <TargetDirectives>
          <RolePlayerConnectDirective>
            <AcceptingClass>
              <DomainClassMoniker Name="Entity" />
            </AcceptingClass>
          </RolePlayerConnectDirective>
        </TargetDirectives>
      </LinkConnectDirective>
    </ConnectionBuilder>
  </ConnectionBuilders>
  <Diagram Id="b165f2b8-331e-42ed-8131-ed06fbced564" Description="Description for Worm.Designer.DesignerDiagram" Name="DesignerDiagram" DisplayName="Minimal Language Diagram" Namespace="Worm.Designer">
    <Class>
      <DomainClassMoniker Name="WormModel" />
    </Class>
    <ShapeMaps>
      <CompartmentShapeMap>
        <DomainClassMoniker Name="Entity" />
        <ParentElementPath>
          <DomainPath>WormModelHasEntities.WormModel/!WormModel</DomainPath>
        </ParentElementPath>
        <DecoratorMap>
          <TextDecoratorMoniker Name="EntityShape/Name" />
          <PropertyDisplayed>
            <PropertyPath>
              <DomainPropertyMoniker Name="Entity/Name" />
            </PropertyPath>
          </PropertyDisplayed>
        </DecoratorMap>
        <CompartmentShapeMoniker Name="EntityShape" />
        <CompartmentMap>
          <CompartmentMoniker Name="EntityShape/Tables" />
          <ElementsDisplayed>
            <DomainPath>EntityHasTables.Tables/!Table</DomainPath>
          </ElementsDisplayed>
          <PropertyDisplayed>
            <PropertyPath>
              <DomainPropertyMoniker Name="Table/Name" />
            </PropertyPath>
          </PropertyDisplayed>
        </CompartmentMap>
        <CompartmentMap>
          <CompartmentMoniker Name="EntityShape/Properties" />
          <ElementsDisplayed>
            <DomainPath>EntityHasProperties.Properties/!Property</DomainPath>
          </ElementsDisplayed>
          <PropertyDisplayed>
            <PropertyPath>
              <DomainPropertyMoniker Name="Property/Name" />
            </PropertyPath>
          </PropertyDisplayed>
        </CompartmentMap>
        <CompartmentMap>
          <CompartmentMoniker Name="EntityShape/SupressedProperties" />
          <ElementsDisplayed>
            <DomainPath>EntityHasSupressedProperties.SupressedProperties/!SupressedProperty</DomainPath>
          </ElementsDisplayed>
          <PropertyDisplayed>
            <PropertyPath>
              <DomainPropertyMoniker Name="SupressedProperty/Name" />
            </PropertyPath>
          </PropertyDisplayed>
        </CompartmentMap>
        <CompartmentMap>
          <CompartmentMoniker Name="EntityShape/SelfRelation" />
          <ElementsDisplayed>
            <DomainPath>EntityHasSelfRelations.SelfRelations/!SelfRelation</DomainPath>
          </ElementsDisplayed>
          <PropertyDisplayed>
            <PropertyPath>
              <DomainPropertyMoniker Name="SelfRelation/Name" />
            </PropertyPath>
          </PropertyDisplayed>
        </CompartmentMap>
      </CompartmentShapeMap>
    </ShapeMaps>
    <ConnectorMaps>
      <ConnectorMap>
        <ConnectorMoniker Name="EntityConnector" />
        <DomainRelationshipMoniker Name="EntityReferencesTargetEntities" />
      </ConnectorMap>
    </ConnectorMaps>
  </Diagram>
  <Designer FileExtension="wxml" EditorGuid="ceba9934-0f38-44b5-b00c-32182d5912a8">
    <RootClass>
      <DomainClassMoniker Name="WormModel" />
    </RootClass>
    <XmlSerializationDefinition CustomPostLoad="false">
      <XmlSerializationBehaviorMoniker Name="DesignerSerializationBehavior" />
    </XmlSerializationDefinition>
    <ToolboxTab TabText="Worm Designer">
      <ElementTool Name="EntityClass" ToolboxIcon="Resources\InterfaceTool.bmp" Caption="Entity" Tooltip="Entity Class" HelpKeyword="EntityClass">
        <DomainClassMoniker Name="Entity" />
      </ElementTool>
      <ConnectionTool Name="Relation" ToolboxIcon="Resources\ExampleConnectorToolBitmap.bmp" Caption="Relation" Tooltip="Relation" HelpKeyword="Relation">
        <ConnectionBuilderMoniker Name="Designer/EntityReferencesTargetEntitiesBuilder" />
      </ConnectionTool>
    </ToolboxTab>
    <Validation UsesMenu="false" UsesOpen="false" UsesSave="false" UsesLoad="false" />
    <DiagramMoniker Name="DesignerDiagram" />
  </Designer>
  <Explorer ExplorerGuid="ab8c5858-38b3-41ee-a7d6-635d183fae3d" Title="Designer Explorer">
    <ExplorerBehaviorMoniker Name="Designer/DesignerExplorer" />
  </Explorer>
</Dsl>