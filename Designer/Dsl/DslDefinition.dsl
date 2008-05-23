<?xml version="1.0" encoding="utf-8"?>
<Dsl dslVersion="1.0.0.0" Id="d94f13a8-3ed2-4e0e-b6ad-af96864c44a6" Description="Designer" Name="Designer" DisplayName="Designer" Namespace="Worm.Designer" ProductName="Designer" CompanyName="Worm" PackageGuid="9c47e690-639c-42c0-99d0-4e28ee1822db" PackageNamespace="Worm.Designer" xmlns="http://schemas.microsoft.com/VisualStudio/2005/DslTools/DslDefinitionModel">
  <Classes>
    <DomainClass Id="2d55930a-31b5-4bd4-9a68-1bdec0d265fe" Description="The root in which all other elements are embedded. Appears as a diagram." Name="WormModel" DisplayName="Worm Model" Namespace="Worm.Designer">
      <Properties>
        <DomainProperty Id="ef57328f-fa14-4ae6-b1b5-66eefd00ff48" Description="Default namespace used in case entity has no any namespace specified" Name="DefaultNamespace" DisplayName="Default Namespace" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="bd1e5b90-b0b3-4647-a044-f4f459241872" Description="Schema Version" Name="SchemaVersion" DisplayName="Schema Version" DefaultValue="1" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="5eb9368f-2c70-452b-b572-7a5ed9640999" Description="Class name prefix" Name="ClassNamePrefix" DisplayName="Class Name Prefix" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="7c38f9f4-5d8a-4bfd-9112-fd7c9f96522c" Description="Class Name Suffix" Name="ClassNameSuffix" DisplayName="Class Name Suffix" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="42926c46-a937-4b2c-acd1-76859bca5084" Description="File Name Suffix" Name="FileNameSuffix" DisplayName="File Name Suffix" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="56a2b134-f643-45c6-9d9e-feb681a11e8c" Description="Split" Name="Split" DisplayName="Split" DefaultValue="False" Category="Code generation">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="e3e5953c-14f7-4661-9915-4d4bba04a001" Description="Entity Schema Def Class Name Suffix" Name="EntitySchemaDefClassNameSuffix" DisplayName="Entity Schema Def Class Name Suffix" DefaultValue="SchemaDef" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="a8bceba5-c8e5-4a7e-8f2b-0c4e32afde31" Description="Private Members Prefix" Name="PrivateMembersPrefix" DisplayName="Private Members Prefix" DefaultValue="m_" Category="Generator Settings">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="b07f1210-d276-4596-840a-df7742b9150f" Description="File Name Prefix" Name="FileNamePrefix" DisplayName="File Name Prefix" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="095cfba4-b4bd-43ff-969f-1fa0f989111a" Description="Generic члены производных классов требует наличия констрейтов" Name="DerivedGenericMembersRequireConstraits" DisplayName="Derived Generic Members Require Constraits" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="0252ee35-0a4d-4470-ae55-b58aacb2f05b" Description="Генерировать методы вместо параметризованых пропертей" Name="MethodsInsteadParametrizedProperties" DisplayName="Methods Instead Parametrized Properties" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="76b4b7cc-b1a7-4614-8f27-7f046c6bb846" Description="Add Options Strict" Name="AddOptionsStrict" DisplayName="Add Options Strict" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="567d6f8d-615a-4661-9903-e5fa83a7f6e4" Description="Options Strict On" Name="OptionsStrictOn" DisplayName="Options Strict On" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="769200c0-4be7-4e6a-bce9-f2ee0c9c1fce" Description="Add Options Explicit" Name="AddOptionsExplicit" DisplayName="Add Options Explicit" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="3add2ebb-e424-4dbd-9b82-a6c6ead25420" Description="Options Explicit On" Name="OptionsExplicitOn" DisplayName="Options Explicit On" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="50e58d80-288a-4a19-b8b5-108b2f5da6bc" Description="Generate CSUsing Statement" Name="GenerateCSUsingStatement" DisplayName="Generate CSUsing Statement" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="d8135d5c-383c-474f-bf05-55fba1c44b03" Description="Generate VBUsing Statement" Name="GenerateVBUsingStatement" DisplayName="Generate VBUsing Statement" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="29d1b322-32a0-4f9a-a06e-42f4bf0a22eb" Description="Безопасная распаковка переменных с кастом в энам" Name="SafeUnboxToEnum" DisplayName="Safe Unbox To Enum" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="eea1df37-9263-4dd7-97f7-b33214b2bf19" Description="Generate Cs Is Statement" Name="GenerateCsIsStatement" DisplayName="Generate Cs Is Statement" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="a1b94ed9-f47d-4dff-9279-b371fff48570" Description="Generate Vb Type Of Is Statement" Name="GenerateVbTypeOfIsStatement" DisplayName="Generate Vb Type Of Is Statement" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="dc11051e-0396-4dbc-8458-32ffe8ab5512" Description="Generate Cs As Statement" Name="GenerateCsAsStatement" DisplayName="Generate Cs As Statement" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="393a72ca-30af-4715-a4ee-1ea38e9c8514" Description="Generate Vb Try Cast Statement" Name="GenerateVbTryCastStatement" DisplayName="Generate Vb Try Cast Statement" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="8df88772-b5fe-45b5-b39e-58ef6ed32293" Description="Generate Cs Lock Statement" Name="GenerateCsLockStatement" DisplayName="Generate Cs Lock Statement" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="746cf092-46e5-4a4d-997e-84011c756c26" Description="Generate Vb Sync Lock Statement" Name="GenerateVbSyncLockStatement" DisplayName="Generate Vb Sync Lock Statement" DefaultValue="False" Category="Language Specific Hacks">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
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
            <DomainClassMoniker Name="Entity" />
          </Index>
          <LinkCreationPaths>
            <DomainPath>WormModelHasEntities.Entities</DomainPath>
          </LinkCreationPaths>
        </ElementMergeDirective>
        <ElementMergeDirective>
          <Index>
            <DomainClassMoniker Name="Table" />
          </Index>
          <LinkCreationPaths>
            <DomainPath>WormModelHasTables.Tables</DomainPath>
          </LinkCreationPaths>
        </ElementMergeDirective>
        <ElementMergeDirective>
          <Index>
            <DomainClassMoniker Name="WormType" />
          </Index>
          <LinkCreationPaths>
            <DomainPath>WormModelHasTypes.Types</DomainPath>
          </LinkCreationPaths>
        </ElementMergeDirective>
      </ElementMergeDirectives>
    </DomainClass>
    <DomainClass Id="4fd18942-120e-481b-9834-7c4d652242e1" Description="Сущность" Name="Entity" DisplayName="Entity" Namespace="Worm.Designer">
      <Properties>
        <DomainProperty Id="44616f38-82f8-43e3-bde6-d55d7e11e189" Description="Идентификатор сущности" Name="IdProperty" DisplayName="Id" DefaultValue="entity id" Kind="Calculated" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="a07738d3-b31e-4ae8-bddd-0de25ae5a420" Description="Наименование сущности" Name="Name" DisplayName="Name" DefaultValue="Entity" Category="Code generation" IsElementName="true">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="c0abea7d-0b57-4ddb-806f-1e0fdf96bb39" Description="Пространство имен для сущности" Name="Namespace" DisplayName="Namespace" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="41ea2ab0-0bd7-4116-82a4-63a0a386ace4" Description="Entity generator behaviour" Name="Behaviour" DisplayName="Behaviour" DefaultValue="Default" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/Worm.CodeGen.Core/EntityBehaviuor" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="b7e0a887-5470-48c5-8638-554df5318014" Description="Описание сущности" Name="Description" DisplayName="Description" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="fee6dfaf-50e0-4a7a-864d-90e3497b5246" Description="Определяет генерировать generic или строго типизированные методы" Name="UseGenerics" DisplayName="Use Generics" DefaultValue="False" Category="Code generation">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="e3b3d3c8-0ff2-48d5-9460-c087e81f051a" Description="Определяет генерировать интерфейс для сущности" Name="MakeInterface" DisplayName="Make Interface" DefaultValue="False" Category="Code generation">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="24a9a6f7-5964-44fd-9547-5b9ec4e25da9" Description="ИД базовой сущности" Name="BaseEntity" DisplayName="Base Entity" Category="Code generation">
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
        <DomainProperty Id="fccc40b6-91d8-4905-b4a3-7a408835c758" Description="Inherits table list from base entity." Name="InheritsBase" DisplayName="Inherits Base" DefaultValue="False" Category="Code generation">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
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
        <ElementMergeDirective>
          <Index>
            <DomainClassMoniker Name="Table" />
          </Index>
          <LinkCreationPaths>
            <DomainPath>TableReferencesEntities.Tables</DomainPath>
            <DomainPath>WormModelHasEntities.WormModel/!WormModel/WormModelHasTables.Tables</DomainPath>
          </LinkCreationPaths>
        </ElementMergeDirective>
      </ElementMergeDirectives>
    </DomainClass>
    <DomainClass Id="d8167da3-fa3f-4ed4-8a42-1e613b5f9902" Description="Tаблицa БД" Name="Table" DisplayName="Table" Namespace="Worm.Designer">
      <Properties>
        <DomainProperty Id="a17a80a3-b19c-4719-81d0-6015be74217d" Description="Имя таблицы" Name="Name" DisplayName="Name" Category="Database property" IsElementName="true">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="1be4d74c-edb8-4f30-a29a-fcd3e0f8baf7" Description="Table Id" Name="IdProperty" DisplayName="Id" Kind="Calculated" Category="Database property">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="cca3b40b-cd96-4448-a20c-30c6e8592209" Description="Schema name for table" Name="Schema" DisplayName="Schema" DefaultValue="dbo" Category="Database property">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
      </Properties>
    </DomainClass>
    <DomainClass Id="a3ab88fb-d6b4-420b-8325-029f77e93036" Description="Свойство сущности" Name="Property" DisplayName="Property" Namespace="Worm.Designer">
      <CustomTypeDescriptor>
        <DomainTypeDescriptor />
      </CustomTypeDescriptor>
      <Properties>
        <DomainProperty Id="8b3c0c4e-2990-403c-9528-4acabe75ecdd" Description="Имя свойства" Name="Name" DisplayName="Name" Category="Code generation" IsElementName="true">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="72a756c7-b007-4ade-ac88-696b306a4153" Description="Тип свойства (возможна ссылка на сущность)" Name="Type" DisplayName="Type" DefaultValue="System.String" Category="Code generation">
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
        <DomainProperty Id="3f78744d-0bae-4bc7-8f97-8bd98bba72e3" Description="Описание свойства" Name="Description" DisplayName="Description" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="4c13ac94-3d96-4ae5-ada6-c63cb97fc2c2" Description="Наименование колонки из БД" Name="FieldName" DisplayName="Field Name" Category="Mapping properties">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="8980f42e-ee24-4198-8a4e-b6965e0c0f0a" Description="Ссылка на таблицу БД" Name="Table" DisplayName="Table" Category="Mapping properties">
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
        <DomainProperty Id="ff8c776c-7228-4b15-8903-e744348ea9f9" Description="Уровень доступа к полю класса" Name="FieldAccessLevel" DisplayName="Field Access Level" DefaultValue="Private" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/Worm.CodeGen.Core/AccessLevel" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="0e08e4e5-983f-434a-b23d-c230096ed20a" Description="Property alias" Name="Alias" DisplayName="Alias" Category="Other">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="67ec9948-c2e5-406c-a7ec-21932a4f97f5" Description="Уровень доступа к полю класса" Name="AccessLevel" DisplayName="Access Level" DefaultValue="Public" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/Worm.CodeGen.Core/AccessLevel" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="32ebc3a6-9aa6-4fec-bcfa-b84adf684a9a" Description="Nullable" Name="Nullable" DisplayName="Nullable" DefaultValue="True" Category="Code generation">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="55b97295-ab7f-4ae8-b6a3-5cfee02b9be9" Description="Признак отключения проперти" Name="Disabled" DisplayName="Disabled" DefaultValue="False" Category="Code generation">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="53125034-447a-4175-b30d-a6ea4885f846" Description="Make property obsolete" Name="Obsolete" DisplayName="Obsolete" DefaultValue="None" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/Worm.CodeGen.Core.Descriptors/ObsoleteType" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="c71cec64-dbd4-4be1-9e67-c9416fbd8978" Description="Description for obsolete property" Name="ObsoleteDescription" DisplayName="Obsolete Description" Category="Code generation">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="27b55a46-03a8-4f6d-96c8-df53b4fd52ab" Description="Включает для свойства поднятие события PropertyChanged, при этом реализация идет на уровне конкретной сущности. При этом в целом для сущности отключается общий механизм этого события." Name="EnablePropertyChanged" DisplayName="Enable Property Changed" DefaultValue="False" Category="Other">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="343f5f78-64cb-4219-bcd0-cfaf0bde39fb" Description="Description for Worm.Designer.Property.PK" Name="PK" DisplayName="PK" DefaultValue="false" Category="Mapping attributes">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="d9353848-d1ec-4b07-9f57-d62f33cbc23c" Description="Factory" Name="Factory" DisplayName="Factory" DefaultValue="False" Category="Mapping attributes">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="b07345b1-8507-4e4f-bcde-e9ea2bfb97da" Description="InsertDefault" Name="InsertDefault" DisplayName="Insert Default" DefaultValue="False" Category="Mapping attributes">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="b73e60df-a00d-4fd7-a1ee-6c00670294a7" Description="Primary Key" Name="PrimaryKey" DisplayName="Primary Key" DefaultValue="False" Category="Mapping attributes">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="14b6d58c-4c31-4dc9-a08a-5e2cef3166a3" Description="Private" Name="Private" DisplayName="Private" DefaultValue="False" Category="Mapping attributes">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="283e4887-f046-4928-81c4-49acbff2bb72" Description="Read Only" Name="ReadOnly" DisplayName="Read Only" DefaultValue="False" Category="Mapping attributes">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="09b6faf5-7ee9-4af7-9b05-1b30ba7e9b4d" Description="Row Version" Name="RowVersion" DisplayName="Row Version" DefaultValue="False" Category="Mapping attributes">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="e1f9c393-dbbb-4479-a9f5-980deb4ab858" Description="RV" Name="RV" DisplayName="RV" DefaultValue="False" Category="Mapping attributes">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="7ef75517-7a6d-4435-ba28-12fbc277296d" Description="Sync Insert" Name="SyncInsert" DisplayName="Sync Insert" DefaultValue="False" Category="Mapping attributes">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="535c031e-5176-46ea-be1c-5138c176e820" Description="Sync Update" Name="SyncUpdate" DisplayName="Sync Update" DefaultValue="False" Category="Mapping attributes">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="ba8acd0d-b334-4d0b-b706-9975b8121e26" Description="Supressed" Name="Supressed" DisplayName="Supressed" DefaultValue="False" Category="Code generation">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
      </Properties>
    </DomainClass>
    <DomainClass Id="bc04f16d-d6ae-4ae2-b470-b81094434fb9" Description="Supressed Property" Name="SupressedProperty" DisplayName="Supressed Property" Namespace="Worm.Designer">
      <Properties>
        <DomainProperty Id="a3bda040-3c8c-41e5-9cd4-045fac074ca9" Description="Property name" Name="Name" DisplayName="Name" IsElementName="true">
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
        <DomainProperty Id="35a5eac0-bf5d-4f0e-b6e0-9b24c77a01d7" Description="Suppressed property type" Name="Type" DisplayName="Type" DefaultValue="System.String">
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
      </Properties>
    </DomainClass>
    <DomainClass Id="89305501-ec2a-47a4-8d17-2f7a08ce48bf" Description="Связь сущности самой с собой" Name="SelfRelation" DisplayName="Self Relation" Namespace="Worm.Designer">
      <Properties>
        <DomainProperty Id="4cc7eb74-c9f9-4cfd-9cf7-8f27608c036b" Description="Имя связанной сущности для генерации методов" Name="DirectAccessor" DisplayName="Direct Accessor">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="45766242-ccce-4e5a-8ed7-14fff5253190" Description="Kаскадное удаление" Name="DirectCascadeDelete" DisplayName="Direct Cascade Delete" DefaultValue="true">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="18d076fb-382a-4f46-ae6d-5cf468778ff1" Description="Имя поля таблицы связи" Name="DirectFieldName" DisplayName="Direct Field Name">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="00f08749-66ca-48dd-93d9-5952d87203fb" Description="Disable relation" Name="Disabled" DisplayName="Disabled" DefaultValue="false">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="763e2750-9675-41b9-8cce-3250b1032a5a" Description="Имя связанной сущности для генерации методов" Name="ReverseAccessor" DisplayName="Reverse Accessor">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="ba170593-27b7-4523-9d3a-a6551c492c26" Description="Каскадное удаление" Name="ReverseCascadeDelete" DisplayName="Reverse Cascade Delete" DefaultValue="true">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
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
        <DomainProperty Id="9c577e94-1d95-40ab-af19-34949cd8de47" Description="Тип связанной сущности для генерации методов" Name="DirectAccessedEntityType" DisplayName="Direct Accessed Entity Type">
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
        <DomainProperty Id="03fe3ca8-b3f8-4ac6-bd2c-10c8247cf2fc" Description="Тип связанной сущности для генерации методов" Name="ReverseAccessedEntityType" DisplayName="Reverse Accessed Entity Type">
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
        <DomainProperty Id="f26ad602-9dad-479d-80f7-fb49403bd323" Description="Name" Name="Name" DisplayName="Name" IsElementName="true">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
      </Properties>
    </DomainClass>
    <DomainClass Id="cfb4d93a-3cca-4d26-bff1-f0c193cd6fd5" Description="Description for Worm.Designer.WormType" Name="WormType" DisplayName="Type" Namespace="Worm.Designer">
      <Properties>
        <DomainProperty Id="1b097dfe-9806-4562-8117-516521124ece" Description="Description for Worm.Designer.WormType.Name" Name="Name" DisplayName="Name" IsElementName="true">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="2147e4ef-f4b9-4c02-8ad7-c66495885c32" Description="Description for Worm.Designer.WormType.Id Property" Name="IdProperty" DisplayName="Id Property">
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
        <DomainProperty Id="6bd17a24-e3bd-4412-a6c6-2c00b7824678" Description="Underlying entity" Name="UnderlyingEntity" DisplayName="Underlying entity" Category="Relation">
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
        <DomainProperty Id="ecd23e88-6e56-438b-9f10-c56c1aba7cbf" Description="Каскадное удаление" Name="LeftCascadeDelete" DisplayName="Cascade Delete" DefaultValue="true" Category="Left point">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="ec48a8e3-f1b3-4c88-bbd4-644451ce13b0" Description="Имя поля таблицы связи" Name="LeftFieldName" DisplayName="Field Name" Category="Left point">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="3bf5da1c-8e83-42ab-8179-77194758fbaf" Description="Имя связанной сущности для генерации методов" Name="LeftAccessorName" DisplayName="Accessor Name" Category="Left point">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="8c000c96-92a2-4eeb-871b-a09fd6a2e10d" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Disabled" Name="Disabled" DisplayName="Disabled" DefaultValue="false" Category="Relation">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="202b0d26-d30c-4a1f-96f1-0b6bdbce7db3" Description="Description for Worm.Designer.EntityReferencesTargetEntities.Table" Name="Table" DisplayName="Table" Category="Relation">
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
        <DomainProperty Id="845e0f02-ed94-402a-b087-c9f05a71b9bc" Description="Сущность" Name="LeftEntity" DisplayName="Entity" DefaultValue="" Category="Left point">
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
        <DomainProperty Id="880ca553-e244-4497-b380-e1445219bf73" Description="Тип связанной сущности для генерации методов" Name="LeftAccessedEntityType" DisplayName="Accessed Entity Type" Category="Left point">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="2c3390fd-432a-4268-96f7-4f8859d74b96" Description="Тип связанной сущности для генерации методов" Name="RightAccessedEntityType" DisplayName="Accessed Entity Type" Category="Right point">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="2e71233d-d194-4929-bbb4-27cf3ae1d389" Description="Имя поля таблицы связи" Name="RightFieldName" DisplayName="Field Name" Category="Right point">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="7c7b3972-2e7d-4b2c-bee9-49cb3c2d8742" Description="Каскадное удаление" Name="RightCascadeDelete" DisplayName="Cascade Delete" DefaultValue="true" Category="Right point">
          <Attributes>
            <ClrAttribute Name="System.ComponentModel.Editor">
              <Parameters>
                <AttributeParameter Value="typeof(Worm.Designer.BoolUIEditor), typeof(System.Drawing.Design.UITypeEditor) " />
              </Parameters>
            </ClrAttribute>
          </Attributes>
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="ef1396f8-92b2-4af5-bf2f-7df39cc03e01" Description="Имя связанной сущности для генерации методов" Name="RightAccessorName" DisplayName="Accessor Name" Category="Right point">
          <Type>
            <ExternalTypeMoniker Name="/System/String" />
          </Type>
        </DomainProperty>
        <DomainProperty Id="25351206-2764-46dc-8cb2-b7932c34da59" Description="Сущность" Name="RightEntity" DisplayName="Entity" Category="Right point">
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
    <DomainRelationship Id="f050e584-1dff-48c9-bf23-50a32237e54b" Description="Description for Worm.Designer.WormModelHasTables" Name="WormModelHasTables" DisplayName="Worm Model Has Tables" Namespace="Worm.Designer" IsEmbedding="true">
      <Source>
        <DomainRole Id="0d1c34b6-d715-40c9-897d-bc4e8277a297" Description="Description for Worm.Designer.WormModelHasTables.WormModel" Name="WormModel" DisplayName="Worm Model" PropertyName="Tables" PropertyDisplayName="Tables">
          <RolePlayer>
            <DomainClassMoniker Name="WormModel" />
          </RolePlayer>
        </DomainRole>
      </Source>
      <Target>
        <DomainRole Id="62720051-a6bb-4384-b79a-072d9206a4aa" Description="Description for Worm.Designer.WormModelHasTables.Table" Name="Table" DisplayName="Table" PropertyName="WormModel" Multiplicity="One" PropagatesDelete="true" PropagatesCopy="true" PropertyDisplayName="Worm Model">
          <RolePlayer>
            <DomainClassMoniker Name="Table" />
          </RolePlayer>
        </DomainRole>
      </Target>
    </DomainRelationship>
    <DomainRelationship Id="02763e91-46a7-468a-9cb0-6d88cdede236" Description="Список используемых таблиц" Name="TableReferencesEntities" DisplayName="Table References Entities" Namespace="Worm.Designer">
      <Source>
        <DomainRole Id="fed3349d-8eb3-4b10-b341-b618f1e9f1c6" Description="Description for Worm.Designer.TableReferencesEntities.Table" Name="Table" DisplayName="Table" PropertyName="Entities" Multiplicity="OneMany" IsPropertyBrowsable="false" PropertyDisplayName="Entities">
          <RolePlayer>
            <DomainClassMoniker Name="Table" />
          </RolePlayer>
        </DomainRole>
      </Source>
      <Target>
        <DomainRole Id="b738fc99-9cc0-4a55-bde2-535c48a091b8" Description="Description for Worm.Designer.TableReferencesEntities.Entity" Name="Entity" DisplayName="Entity" PropertyName="Tables" PropertyDisplayName="Tables">
          <RolePlayer>
            <DomainClassMoniker Name="Entity" />
          </RolePlayer>
        </DomainRole>
      </Target>
    </DomainRelationship>
    <DomainRelationship Id="ac0a301c-7199-4ffd-bd46-9737ed195836" Description="Description for Worm.Designer.WormModelHasTypes" Name="WormModelHasTypes" DisplayName="Worm Model Has Types" Namespace="Worm.Designer" IsEmbedding="true">
      <Source>
        <DomainRole Id="8229f74e-a29f-4395-b1f3-3b6b1428a4aa" Description="Description for Worm.Designer.WormModelHasTypes.WormModel" Name="WormModel" DisplayName="Worm Model" PropertyName="Types" PropertyDisplayName="Types">
          <RolePlayer>
            <DomainClassMoniker Name="WormModel" />
          </RolePlayer>
        </DomainRole>
      </Source>
      <Target>
        <DomainRole Id="7cad8dfc-2796-453c-bf45-dfe32d713002" Description="Description for Worm.Designer.WormModelHasTypes.WormType" Name="WormType" DisplayName="Type" PropertyName="WormModel" Multiplicity="One" PropagatesDelete="true" PropagatesCopy="true" PropertyDisplayName="Worm Model">
          <RolePlayer>
            <DomainClassMoniker Name="WormType" />
          </RolePlayer>
        </DomainRole>
      </Target>
    </DomainRelationship>
    <DomainRelationship Id="e2d4852f-d222-461e-acac-9703336a5ccb" Description="Description for Worm.Designer.WormTypeReferencesEntities" Name="WormTypeReferencesEntities" DisplayName="Worm Type References Entities" Namespace="Worm.Designer">
      <Source>
        <DomainRole Id="9cac9e2c-3e59-4ec8-8ca5-7ac088f9a4e6" Description="Description for Worm.Designer.WormTypeReferencesEntities.WormType" Name="WormType" DisplayName="Worm Type" PropertyName="Entities" PropertyDisplayName="Entities">
          <RolePlayer>
            <DomainClassMoniker Name="WormType" />
          </RolePlayer>
        </DomainRole>
      </Source>
      <Target>
        <DomainRole Id="4073219f-14b4-413c-9a59-2a80afdee0b1" Description="Description for Worm.Designer.WormTypeReferencesEntities.Entity" Name="Entity" DisplayName="Entity" PropertyName="WormTypes" PropertyDisplayName="Worm Types">
          <RolePlayer>
            <DomainClassMoniker Name="Entity" />
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
    <ExternalType Name="ObsoleteType" Namespace="Worm.CodeGen.Core.Descriptors" />
    <ExternalType Name="LanguageSpecificHacks" Namespace="Worm.CodeGen.Core" />
    <ExternalType Name="ExternalType1" Namespace="" />
  </Types>
  <Shapes>
    <CompartmentShape Id="1ac7c68e-23be-47ec-a6fe-2ece8f63e49c" Description="Entity" Name="EntityShape" DisplayName="Entity" Namespace="Worm.Designer" GeneratesDoubleDerived="true" FixedTooltipText="Entity Shape" FillColor="PaleGreen" InitialHeight="0.5" OutlineThickness="0.01125" HasDefaultConnectionPoints="true" Geometry="RoundedRectangle">
      <ShapeHasDecorators Position="InnerTopCenter" HorizontalOffset="0" VerticalOffset="0">
        <TextDecorator Name="Name" DisplayName="Name" DefaultText="Name" />
      </ShapeHasDecorators>
      <ShapeHasDecorators Position="InnerTopRight" HorizontalOffset="0" VerticalOffset="0">
        <ExpandCollapseDecorator Name="ExpandCollapseDecorator1" DisplayName="Expand Collapse Decorator1" />
      </ShapeHasDecorators>
      <Compartment TitleFillColor="Honeydew" Name="Properties" Title="Properties">
        <Notes>Свойства сущности</Notes>
      </Compartment>
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
          <XmlRelationshipData RoleElementName="tables">
            <DomainRelationshipMoniker Name="WormModelHasTables" />
          </XmlRelationshipData>
          <XmlRelationshipData RoleElementName="types">
            <DomainRelationshipMoniker Name="WormModelHasTypes" />
          </XmlRelationshipData>
          <XmlPropertyData XmlName="classNamePrefix">
            <DomainPropertyMoniker Name="WormModel/ClassNamePrefix" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="classNameSuffix">
            <DomainPropertyMoniker Name="WormModel/ClassNameSuffix" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="fileNameSuffix">
            <DomainPropertyMoniker Name="WormModel/FileNameSuffix" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="split">
            <DomainPropertyMoniker Name="WormModel/Split" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="entitySchemaDefClassNameSuffix">
            <DomainPropertyMoniker Name="WormModel/EntitySchemaDefClassNameSuffix" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="privateMembersPrefix">
            <DomainPropertyMoniker Name="WormModel/PrivateMembersPrefix" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="fileNamePrefix">
            <DomainPropertyMoniker Name="WormModel/FileNamePrefix" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="derivedGenericMembersRequireConstraits">
            <DomainPropertyMoniker Name="WormModel/DerivedGenericMembersRequireConstraits" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="methodsInsteadParametrizedProperties">
            <DomainPropertyMoniker Name="WormModel/MethodsInsteadParametrizedProperties" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="addOptionsStrict">
            <DomainPropertyMoniker Name="WormModel/AddOptionsStrict" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="optionsStrictOn">
            <DomainPropertyMoniker Name="WormModel/OptionsStrictOn" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="addOptionsExplicit">
            <DomainPropertyMoniker Name="WormModel/AddOptionsExplicit" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="optionsExplicitOn">
            <DomainPropertyMoniker Name="WormModel/OptionsExplicitOn" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="generateCSUsingStatement">
            <DomainPropertyMoniker Name="WormModel/GenerateCSUsingStatement" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="generateVBUsingStatement">
            <DomainPropertyMoniker Name="WormModel/GenerateVBUsingStatement" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="safeUnboxToEnum">
            <DomainPropertyMoniker Name="WormModel/SafeUnboxToEnum" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="generateCsIsStatement">
            <DomainPropertyMoniker Name="WormModel/GenerateCsIsStatement" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="generateVbTypeOfIsStatement">
            <DomainPropertyMoniker Name="WormModel/GenerateVbTypeOfIsStatement" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="generateCsAsStatement">
            <DomainPropertyMoniker Name="WormModel/GenerateCsAsStatement" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="generateVbTryCastStatement">
            <DomainPropertyMoniker Name="WormModel/GenerateVbTryCastStatement" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="generateCsLockStatement">
            <DomainPropertyMoniker Name="WormModel/GenerateCsLockStatement" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="generateVbSyncLockStatement">
            <DomainPropertyMoniker Name="WormModel/GenerateVbSyncLockStatement" />
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
          <XmlPropertyData XmlName="inheritsBase">
            <DomainPropertyMoniker Name="Entity/InheritsBase" />
          </XmlPropertyData>
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
          <XmlRelationshipData RoleElementName="entities">
            <DomainRelationshipMoniker Name="TableReferencesEntities" />
          </XmlRelationshipData>
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
          <XmlPropertyData XmlName="disabled">
            <DomainPropertyMoniker Name="Property/Disabled" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="obsolete">
            <DomainPropertyMoniker Name="Property/Obsolete" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="obsoleteDescription">
            <DomainPropertyMoniker Name="Property/ObsoleteDescription" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="enablePropertyChanged">
            <DomainPropertyMoniker Name="Property/EnablePropertyChanged" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="pK">
            <DomainPropertyMoniker Name="Property/PK" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="factory">
            <DomainPropertyMoniker Name="Property/Factory" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="insertDefault">
            <DomainPropertyMoniker Name="Property/InsertDefault" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="primaryKey">
            <DomainPropertyMoniker Name="Property/PrimaryKey" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="private">
            <DomainPropertyMoniker Name="Property/Private" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="readOnly">
            <DomainPropertyMoniker Name="Property/ReadOnly" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="rowVersion">
            <DomainPropertyMoniker Name="Property/RowVersion" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="rV">
            <DomainPropertyMoniker Name="Property/RV" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="syncInsert">
            <DomainPropertyMoniker Name="Property/SyncInsert" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="syncUpdate">
            <DomainPropertyMoniker Name="Property/SyncUpdate" />
          </XmlPropertyData>
          <XmlPropertyData XmlName="supressed">
            <DomainPropertyMoniker Name="Property/Supressed" />
          </XmlPropertyData>
        </ElementData>
      </XmlClassData>
      <XmlClassData TypeName="EntityHasProperties" MonikerAttributeName="" MonikerElementName="entityHasPropertiesMoniker" ElementName="entityHasProperties" MonikerTypeName="EntityHasPropertiesMoniker">
        <DomainRelationshipMoniker Name="EntityHasProperties" />
      </XmlClassData>
      <XmlClassData TypeName="EntityReferencesTargetEntities" MonikerAttributeName="" MonikerElementName="entityReferencesTargetEntitiesMoniker" ElementName="entityReferencesTargetEntities" MonikerTypeName="EntityReferencesTargetEntitiesMoniker">
        <DomainRelationshipMoniker Name="EntityReferencesTargetEntities" />
        <ElementData>
          <XmlPropertyData XmlName="underlyingEntity">
            <DomainPropertyMoniker Name="EntityReferencesTargetEntities/UnderlyingEntity" />
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
          <XmlPropertyData XmlName="type">
            <DomainPropertyMoniker Name="SupressedProperty/Type" />
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
      <XmlClassData TypeName="WormModelHasTables" MonikerAttributeName="" MonikerElementName="wormModelHasTablesMoniker" ElementName="wormModelHasTables" MonikerTypeName="WormModelHasTablesMoniker">
        <DomainRelationshipMoniker Name="WormModelHasTables" />
      </XmlClassData>
      <XmlClassData TypeName="TableReferencesEntities" MonikerAttributeName="" MonikerElementName="tableReferencesEntitiesMoniker" ElementName="tableReferencesEntities" MonikerTypeName="TableReferencesEntitiesMoniker">
        <DomainRelationshipMoniker Name="TableReferencesEntities" />
      </XmlClassData>
      <XmlClassData TypeName="WormType" MonikerAttributeName="" MonikerElementName="wormTypeMoniker" ElementName="wormType" MonikerTypeName="WormTypeMoniker">
        <DomainClassMoniker Name="WormType" />
        <ElementData>
          <XmlPropertyData XmlName="name">
            <DomainPropertyMoniker Name="WormType/Name" />
          </XmlPropertyData>
          <XmlRelationshipData RoleElementName="entities">
            <DomainRelationshipMoniker Name="WormTypeReferencesEntities" />
          </XmlRelationshipData>
          <XmlPropertyData XmlName="idProperty">
            <DomainPropertyMoniker Name="WormType/IdProperty" />
          </XmlPropertyData>
        </ElementData>
      </XmlClassData>
      <XmlClassData TypeName="WormModelHasTypes" MonikerAttributeName="" MonikerElementName="wormModelHasTypesMoniker" ElementName="wormModelHasTypes" MonikerTypeName="WormModelHasTypesMoniker">
        <DomainRelationshipMoniker Name="WormModelHasTypes" />
      </XmlClassData>
      <XmlClassData TypeName="WormTypeReferencesEntities" MonikerAttributeName="" MonikerElementName="wormTypeReferencesEntitiesMoniker" ElementName="wormTypeReferencesEntities" MonikerTypeName="WormTypeReferencesEntitiesMoniker">
        <DomainRelationshipMoniker Name="WormTypeReferencesEntities" />
      </XmlClassData>
    </ClassData>
  </XmlSerializationBehavior>
  <ExplorerBehavior Name="DesignerExplorer">
    <CustomNodeSettings>
      <ExplorerNodeSettings IconToDisplay="Resources\2651.bmp" ShowsDomainClass="true">
        <Class>
          <DomainClassMoniker Name="Table" />
        </Class>
        <PropertyDisplayed>
          <PropertyPath>
            <DomainPropertyMoniker Name="Table/Name" />
          </PropertyPath>
        </PropertyDisplayed>
      </ExplorerNodeSettings>
      <ExplorerNodeSettings IconToDisplay="Resources\2671.bmp" ShowsDomainClass="true">
        <Class>
          <DomainClassMoniker Name="WormModel" />
        </Class>
      </ExplorerNodeSettings>
      <ExplorerNodeSettings ShowsDomainClass="true">
        <Class>
          <DomainClassMoniker Name="Property" />
        </Class>
      </ExplorerNodeSettings>
    </CustomNodeSettings>
  </ExplorerBehavior>
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
    <ConnectionBuilder Name="TableReferencesEntitiesBuilder">
      <LinkConnectDirective>
        <DomainRelationshipMoniker Name="TableReferencesEntities" />
        <SourceDirectives>
          <RolePlayerConnectDirective>
            <AcceptingClass>
              <DomainClassMoniker Name="Table" />
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
    <ConnectionBuilder Name="WormTypeReferencesEntitiesBuilder">
      <LinkConnectDirective>
        <DomainRelationshipMoniker Name="WormTypeReferencesEntities" />
        <SourceDirectives>
          <RolePlayerConnectDirective>
            <AcceptingClass>
              <DomainClassMoniker Name="WormType" />
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
  <Diagram Id="b165f2b8-331e-42ed-8131-ed06fbced564" Description="Description for Worm.Designer.DesignerDiagram" Name="DesignerDiagram" DisplayName="Worm Diagram" Namespace="Worm.Designer">
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
      <ElementTool Name="EntityClass" ToolboxIcon="Resources\tool.bmp" Caption="Entity" Tooltip="Entity Class" HelpKeyword="EntityClass">
        <DomainClassMoniker Name="Entity" />
      </ElementTool>
      <ConnectionTool Name="Relation" ToolboxIcon="Resources\ExampleConnectorToolBitmap.bmp" Caption="Relation" Tooltip="Relation" HelpKeyword="Relation">
        <ConnectionBuilderMoniker Name="Designer/EntityReferencesTargetEntitiesBuilder" />
      </ConnectionTool>
    </ToolboxTab>
    <Validation UsesMenu="true" UsesOpen="true" UsesSave="true" UsesLoad="false" />
    <DiagramMoniker Name="DesignerDiagram" />
  </Designer>
  <Explorer ExplorerGuid="ab8c5858-38b3-41ee-a7d6-635d183fae3d" Title="Designer Explorer">
    <ExplorerBehaviorMoniker Name="Designer/DesignerExplorer" />
  </Explorer>
</Dsl>