//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.1433
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// This file was generated by Worm.CodeGen.CodeGenerator v1.0.3007.4016 application(Worm.CodeGen.Core v1.0.3007.4015).
//
//By user 'mike' at 31.03.2008 2:25:11.
//
//
namespace DAWorm
{


    [Worm.Orm.Meta.EntityAttribute(typeof(DAWorm.Phone.PhoneSchemaDef), "1", EntityName = DAWorm.Phone.Descriptor.EntityName)]
    [System.SerializableAttribute()]
    public class Phone : Worm.Orm.OrmBaseT<Phone>, Worm.Orm.Meta.IOrmEditable<Phone>
    {

        #region Private Fields
        private DAWorm.User _user_id;

        private string _phone_number;
        #endregion

        #region Constructors
        public Phone()
        {
            this._dontRaisePropertyChange = true;
        }

        public Phone(int id, Worm.Cache.OrmCacheBase cache, Worm.QueryGenerator schema) :
            base(id, cache, schema)
        {
            this._dontRaisePropertyChange = true;
        }
        #endregion

        #region Properties
        [Worm.Orm.Meta.ColumnAttribute(DAWorm.Phone.Properties.User_id)]
        public virtual DAWorm.User User_id
        {
            get
            {
                using (this.Read(DAWorm.Phone.Properties.User_id))
                {
                    return this._user_id;
                }
            }
            set
            {
                using (this.Write(DAWorm.Phone.Properties.User_id))
                {
                    this._user_id = value;
                }
            }
        }

        [Worm.Orm.Meta.ColumnAttribute(DAWorm.Phone.Properties.Phone_number)]
        public virtual string Phone_number
        {
            get
            {
                using (this.Read(DAWorm.Phone.Properties.Phone_number))
                {
                    return this._phone_number;
                }
            }
            set
            {
                using (this.Write(DAWorm.Phone.Properties.Phone_number))
                {
                    this._phone_number = value;
                }
            }
        }
        #endregion

        public virtual void CopyBody(Phone from, Phone to)
        {
            to._user_id = from._user_id;
            to._phone_number = from._phone_number;
        }

        public override void SetValue(System.Reflection.PropertyInfo pi, Worm.Orm.Meta.ColumnAttribute c, object value)
        {
            string fieldName = c.FieldName;
            if (DAWorm.Phone.Properties.User_id.Equals(fieldName))
            {
                this._user_id = ((DAWorm.User)(value));
                return;
            }
            if (DAWorm.Phone.Properties.Phone_number.Equals(fieldName))
            {
                this._phone_number = ((string)(value));
                return;
            }
            base.SetValue(pi, c, value);
        }

        public override object GetValue(string propAlias, Worm.Orm.Meta.IOrmObjectSchemaBase schema)
        {
            if (DAWorm.Phone.Properties.User_id.Equals(propAlias))
            {
                return this.User_id;
            }
            if (DAWorm.Phone.Properties.Phone_number.Equals(propAlias))
            {
                return this.Phone_number;
            }
            return base.GetValue(propAlias, schema);
        }

        public override void CreateObject(string fieldName, object value)
        {
            throw new System.InvalidOperationException("Invalid method usage.");
        }

        #region Nested Types
        /// <summary>
        ///Алиасы свойств сущностей испльзуемые в объектной модели.
        ///</summary>
        public class Properties
        {

            #region Private Fields
            public const string ID = "ID";

            public const string User_id = "User_id";

            public const string Phone_number = "Phone_number";
            #endregion

            #region Constructors
            protected Properties()
            {
            }
            #endregion
        }

        /// <summary>
        ///Описатель сущности.
        ///</summary>
        public class Descriptor
        {

            #region Private Fields
            /// <summary>
            ///Имя сущности в объектной модели.
            ///</summary>
            public const string EntityName = "Phone";
            #endregion

            #region Constructors
            protected Descriptor()
            {
            }
            #endregion
        }

        public class PhoneSchemaDef : Worm.Orm.Meta.IOrmObjectSchema, Worm.Orm.Meta.IOrmSchemaInit
        {

            #region Private Fields
            private Worm.Collections.IndexedCollection<string, Worm.Orm.Meta.MapField2Column> _idx;

            private Worm.Orm.Meta.OrmTable[] _tables;

            private object _forTablesLock = new object();

            private Worm.Orm.Meta.M2MRelation[] _m2mRelations;

            private object _forM2MRelationsLock = new object();

            private object _forIdxLock = new object();

            protected Worm.QueryGenerator _schema;

            protected System.Type _entityType;
            #endregion

            protected virtual Worm.Orm.Meta.OrmTable GetTypeMainTable(System.Type type)
            {
                Worm.Orm.Meta.OrmTable[] tables;
                tables = this._schema.GetTables(type);
                return ((Worm.Orm.Meta.OrmTable)(tables.GetValue(0)));
            }

            public virtual Worm.Orm.Meta.OrmTable[] GetTables()
            {
                if ((this._tables == null))
                {
                    lock (this._forTablesLock)
                    {
                        if ((this._tables == null))
                        {
                            this._tables = new Worm.Orm.Meta.OrmTable[] {
                new Worm.Orm.Meta.OrmTable("[dbo].[tbl_phone]")};
                        }
                    }
                }
                return this._tables;
            }

            protected virtual Worm.Orm.Meta.OrmTable GetTable(DAWorm.Phone.PhoneSchemaDef.TablesLink tbl)
            {
                return ((Worm.Orm.Meta.OrmTable)(this.GetTables().GetValue(((int)(tbl)))));
            }

            public virtual bool ChangeValueType(Worm.Orm.Meta.ColumnAttribute c, object value, ref object newvalue)
            {
                if ((((c._behavior & Worm.Orm.Meta.Field2DbRelations.InsertDefault)
                            == Worm.Orm.Meta.Field2DbRelations.InsertDefault)
                            && ((value == null)
                            || System.Activator.CreateInstance(value.GetType()).Equals(value))))
                {
                    newvalue = System.DBNull.Value;
                    return true;
                }
                newvalue = value;
                return false;
            }

            public virtual Worm.Criteria.Joins.OrmJoin GetJoins(Worm.Orm.Meta.OrmTable left, Worm.Orm.Meta.OrmTable right)
            {
                return default(Worm.Database.Criteria.Joins.OrmJoin);
            }

            public virtual Worm.Orm.Meta.ColumnAttribute[] GetSuppressedColumns()
            {
                return new Worm.Orm.Meta.ColumnAttribute[0];
            }

            public virtual Worm.Criteria.Core.IFilter GetFilter(object filter_info)
            {
                return null;
            }

            public virtual Worm.Orm.Meta.M2MRelation[] GetM2MRelations()
            {
                if ((this._m2mRelations == null))
                {
                    lock (this._forM2MRelationsLock)
                    {
                        if ((this._m2mRelations == null))
                        {
                            Worm.Orm.Meta.M2MRelation[] m2mRelations = new Worm.Orm.Meta.M2MRelation[0];
                            this._m2mRelations = m2mRelations;
                        }
                    }
                }
                return this._m2mRelations;
            }

            public virtual Worm.Collections.IndexedCollection<string, Worm.Orm.Meta.MapField2Column> GetFieldColumnMap()
            {
                if ((this._idx == null))
                {
                    lock (this._forIdxLock)
                    {
                        if ((this._idx == null))
                        {
                            Worm.Collections.IndexedCollection<string, Worm.Orm.Meta.MapField2Column> idx = new Worm.Cache.OrmObjectIndex();
                            idx.Add(new Worm.Orm.Meta.MapField2Column("ID", "phone_id", this.GetTable(DAWorm.Phone.PhoneSchemaDef.TablesLink.tbldbotbl_phone), Worm.Orm.Meta.Field2DbRelations.None));
                            idx.Add(new Worm.Orm.Meta.MapField2Column("User_id", "user_id", this.GetTable(DAWorm.Phone.PhoneSchemaDef.TablesLink.tbldbotbl_phone)));
                            idx.Add(new Worm.Orm.Meta.MapField2Column("Phone_number", "phone_number", this.GetTable(DAWorm.Phone.PhoneSchemaDef.TablesLink.tbldbotbl_phone)));
                            this._idx = idx;
                        }
                    }
                }
                return this._idx;
            }

            public virtual void GetSchema(Worm.QueryGenerator schema, System.Type t)
            {
                this._schema = schema;
                this._entityType = t;
            }

            #region Nested Types
            public enum TablesLink
            {

                tbldbotbl_phone = 0,
            }
            #endregion
        }
        #endregion
    }
}