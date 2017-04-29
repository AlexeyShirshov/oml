## Object schema
Ok. Lets have a look at how database entities (they might be represented not only by tables) turn into objects. Every object must have EntityAttribute that connects object schema to that class. Object schema is a class that implements certain interfaces. IOrmObjectSchema is the main interface. It provides two main pieces of information - which table map to a class, and which columns map to properties. 
Here is an example of how information is represented in Album schema
##### VB.NET
{{
Public Overridable Function GetTables() As Worm.Orm.OrmTable() Implements Worm.Orm.IOrmObjectSchema.GetTables
  'multithreaded staff skipped
  '...
      If (Me._tables Is Nothing) Then
        Me._tables = New Worm.Orm.OrmTable() {New Worm.Orm.OrmTable("[dbo](dbo).[Albums](Albums)")}
      End If
  'multithreaded staff skipped
  '...
  Return Me._tables
End Function
}}
##### C#
{{
public virtual Worm.Orm.OrmTable[]() GetTables()
{
  //multithreaded staff skipped
  //...
        this._tables = new Worm.Orm.OrmTable[]() {
            new Worm.Orm.OrmTable("[dbo](dbo).[Albums](Albums)")};
      }
  //multithreaded staff skipped
  //...
  return this._tables;
}
}}
We see that the function returns an array of OrmTable objects each describing fully-qualified database table name.
So it's pretty clear. Things get worse when we look at the table columns. The general approach is to use reflection to map rowset data to object properties. But in high performance applications it is not acceptable. It's really slow. Taking this into account, all properties related to database columns are marked with ColumnAttribute attribute.
##### VB.NET
{{
<Worm.Orm.ColumnAttribute("ID", Worm.Orm.Field2DbRelations.PK)> _
Public Overridable Property Id() As Integer
}}
##### C#
{{
[Worm.Orm.ColumnAttribute("ID", Worm.Orm.Field2DbRelations.PK)](Worm.Orm.ColumnAttribute(_ID_,-Worm.Orm.Field2DbRelations.PK))
public virtual int Id
}}
Every property has a name. It's a logical title for a database column.
Those names are resolved in method GetFieldColumnMap of object schema
##### VB.NET
{{
Public Overridable Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column) _
Implements Worm.Orm.IOrmObjectSchema.GetFieldColumnMap
  'multithreaded staff skipped
  '...
      If (Me._idx Is Nothing) Then
        Dim idx As Worm.Orm.OrmObjectIndex = New Worm.Orm.OrmObjectIndex
        idx.Add(New Worm.Orm.MapField2Column("ID", "id", Me.GetTable(test.Albums.AlbumsSchemaDef.TablesLink.tbldboAlbums)))
        idx.Add(New Worm.Orm.MapField2Column("Name", "name", Me.GetTable(test.Albums.AlbumsSchemaDef.TablesLink.tbldboAlbums)))
        idx.Add(New Worm.Orm.MapField2Column("Release", "release_dt", Me.GetTable(test.Albums.AlbumsSchemaDef.TablesLink.tbldboAlbums)))
        Me._idx = idx
      End If
  'multithreaded staff skipped
  '...
  Return Me._idx
End Function
}}
##### C#
{{
public virtual Worm.Orm.Collections.IndexedCollection<string, Worm.Orm.MapField2Column> GetFieldColumnMap()
{
  //multithreaded staff skipped
  //...
      if ((this._idx == null))
      {
        Worm.Orm.OrmObjectIndex idx = new Worm.Orm.OrmObjectIndex();
        idx.Add(new Worm.Orm.MapField2Column("ID", "id", this.GetTable(test.Albums.AlbumsSchemaDef.TablesLink.tbldboAlbums)));
        idx.Add(new Worm.Orm.MapField2Column("Name", "name", this.GetTable(test.Albums.AlbumsSchemaDef.TablesLink.tbldboAlbums)));
        idx.Add(new Worm.Orm.MapField2Column("Release", "release_dt", this.GetTable(test.Albums.AlbumsSchemaDef.TablesLink.tbldboAlbums)));
        this._idx = idx;
      }
  //multithreaded staff skipped
  //...
  return this._idx;
}
}}
Now about algorithms. On startup Worm collects the whole object schema for a particular class and stores it in internal dictionary. When you are going to save or load an object, Worm gets this information, creates and executes corresponding SQL statements. Then the data from rowset is bounded to the object.
So, the first requirement is
* [Object must have a schema. The schema must implement at least 2 methods: GetFieldColumnMap and GetTables](requirement1)
Next requirements are related to [The Class](The-Class).
See also class [diagram](Object schema_schema.png). It is described in [advanced section](Schema-interfaces).