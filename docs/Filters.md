## Filters
There are no queries in terms of SQL in Worm. You can use Criteria to filter data. Lets have a look at examples.
### Get album with identifier equals to 20
{{
Using mgr As OrmDBManager = GetDBManager()
  Dim sort As Sort = Nothing
  Dim album_type As Type = GetType(test.Album)

  Dim albums As ICollection(Of test.Album) = mgr.Find(Of test.Album)( _
    New Criteria(album_type).Field("ID").Eq(20), sort, True)
End Using
}}
### Get albums with a title starting with 'love'
{{
Using mgr As OrmDBManager = GetDBManager()
  Dim sort As Sort = Nothing
  Dim album_type As Type = GetType(test.Album)

  Dim albums As ICollection(Of test.Album) = mgr.Find(Of test.Album)( _
    New Criteria(album_type).Field("Name").Like("love%"), sort, True)
End Using
}}
### Get albums released in a year specified order by desc
{{
Using mgr As OrmDBManager = GetDBManager()
  Dim sort As Sort = Nothing
  Dim album_type As Type = GetType(test.Album)

  Dim albums As ICollection(Of test.Album) = mgr.Find(Of test.Album)( _
    New Criteria(album_type).Field("Release").GreaterThan(New Date(Now.Year, 1, 1)), _
    Sorting.Field("Release").Desc, True)
End Using
}}
Here we use Find method of OrmDBManager to get the data. The prototype
{{
Public Function Find(Of T As {New, Worm.Orm.OrmBase})( _
  ByVal criteria As Worm.Orm.CriteriaLink, _ ' criteria
  ByVal sort As Worm.Orm.Sort, _ 'sorting
  ByVal withLoad As Boolean _ 'whether to query all data or only id
) As System.Collections.Generic.ICollection(Of T)
}}
## Caching
All resultsets are cached in memory, so the next time you will call the method there is no round trip to database server. Resultsets are cached by the criteria key. If you change the criteria, new resultset will be fetched from the DB and cached in memory.