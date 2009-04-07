using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using Worm.CodeGen.Core;
using Worm.CodeGen.Core.Descriptors;
using System.IO;
using System.Text.RegularExpressions;

namespace Worm.CodeGen.XmlGenerator
{
	public class Pair<T>
	{
		internal T _first;
		internal T _second;

		public Pair()
		{
		}

		public Pair(T first, T second)
		{
			this._first = first;
			this._second = second;
		}

		public T First
		{
			get { return _first; }
			set { _first = value; }
		}

		public T Second
		{
			get { return _second; }
			set { _second = value; }
		}
	}
	
	public class Generator
	{
		private string _server;
		private string _m;
		private string _db;
		private bool _i;
		private string _user;
		private string _psw;
        private bool _transform;
        private Dictionary<string, object> _ents = new Dictionary<string, object>();

		public Generator(string server, string m, string db, bool i, string user, string psw, bool transformPropertyName)
		{
			_server = server;
			_m = m;
			_db = db;
			_i = i;
			_user = user;
			_psw = psw;
            _transform = transformPropertyName;
		}

		public void MakeWork(string schemas, string namelike, string file, string merge, bool dr, string name_space, bool unify)
		{
			Dictionary<Column, Column> columns = new Dictionary<Column, Column>();
			List<Pair<string>> proh = new List<Pair<string>>();

			using (DbConnection conn = GetDBConn(_server, _m, _db, _i, _user, _psw))
			{
				using (DbCommand cmd = conn.CreateCommand())
				{
					cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"select t.table_schema,t.table_name,c.column_name,c.is_nullable,c.data_type,cc.constraint_type,cc.constraint_name, " + AppendIdentity() + @",(select count(*) from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc 
                        join INFORMATION_SCHEMA.constraint_column_usage cc on 
                        tc.table_name = cc.table_name and tc.table_schema = cc.table_schema and cc.constraint_name = tc.constraint_name
                        where t.table_name = tc.table_name and t.table_schema = tc.table_schema
                        and tc.constraint_type = 'PRIMARY KEY'
                        ) pk_cnt from INFORMATION_SCHEMA.tables t
						join INFORMATION_SCHEMA.columns c on t.table_name = c.table_name and t.table_schema = c.table_schema
                        left join (
	                        select cc.table_name,cc.table_schema,cc.column_name,tc.constraint_type,cc.constraint_name from INFORMATION_SCHEMA.constraint_column_usage cc 
	                        join INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc on tc.table_name = cc.table_name and tc.table_schema = cc.table_schema and cc.constraint_name = tc.constraint_name --and tc.constraint_type is not null
                        ) cc on t.table_name = cc.table_name and t.table_schema = cc.table_schema and c.column_name = cc.column_name
						where table_type = 'base table'
						and (
						((select count(*) from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc 
						join INFORMATION_SCHEMA.constraint_column_usage cc on 
						tc.table_name = cc.table_name and tc.table_schema = cc.table_schema and cc.constraint_name = tc.constraint_name
						where t.table_name = tc.table_name and t.table_schema = tc.table_schema
						and tc.constraint_type = 'PRIMARY KEY'
						) > 0) or 
						((select count(*) from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc 
						join INFORMATION_SCHEMA.constraint_column_usage cc on 
						tc.table_name = cc.table_name and tc.table_schema = cc.table_schema and cc.constraint_name = tc.constraint_name
						where t.table_name = tc.table_name and t.table_schema = tc.table_schema
						and tc.constraint_type = 'UNIQUE'
						) > 0))
						--and (select count(*) from INFORMATION_SCHEMA.constraint_column_usage ccu 
						--	where ccu.table_name = t.table_name and ccu.table_schema = t.table_schema and ccu.constraint_name = cc.constraint_name) < 2
						--and (tc.constraint_type <> 'CHECK' or tc.constraint_type is null)
						YYYYY
						and (t.table_schema+t.table_name XXXXX like @tn or @tn is null)
						order by t.table_schema,t.table_name,c.ordinal_position";

					string slist = string.Empty;
					if (schemas != null)
					{
						slist = "and t.table_schema in ('" + schemas + "')";
					}
					cmd.CommandText = cmd.CommandText.Replace("YYYYY", slist);

					DbParameter tn = cmd.CreateParameter();
					tn.ParameterName = "tn";
					string r = string.Empty;

					if (namelike != null)
					{
						if (namelike.StartsWith("!"))
						{
							r = "not";
							namelike = namelike.Substring(1);
						}
						tn.Value = namelike;
					}
					else
						tn.Value = DBNull.Value;

					cmd.CommandText = cmd.CommandText.Replace("XXXXX", r);
					tn.Direction = ParameterDirection.Input;
					cmd.Parameters.Add(tn);

					conn.Open();					

					using (DbDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							Column c = Column.Create(reader);
							if (proh.Find(delegate(Pair<string> kv)
							{
								return kv.First == c.FullTableName;
							}) != null)
							{
								//just skip
							}
							else
							{
								try
								{
									columns.Add(c, c);
								}
								catch (ArgumentException ex)
								{
									string[] attr;
									if (GetAttributes(c, out attr) && columns[c].ConstraintType == "FOREIGN KEY")
									{
										if (unify)
										{
											proh.Add(new Pair<string>(c.FullTableName, columns[c].ConstraintName));
											foreach (Column clm in new List<Column>(columns.Keys))
											{
												if (clm.FullTableName == c.FullTableName)
													columns.Remove(clm);
											}
										}
										else
										{
											foreach (Column clm in new List<Column>(columns.Keys))
											{
												if (clm.ConstraintType == "FOREIGN KEY" && 
													clm.ColumnName == c.ColumnName &&
													clm.FullTableName == c.FullTableName)
													columns.Remove(clm);
											}
											columns.Add(c,c);
										}
									}
									else if (GetAttributes(columns[c], out attr) && c.ConstraintType == "FOREIGN KEY")
									{
										if (unify)
										{
											proh.Add(new Pair<string>(c.FullTableName, c.ConstraintName));
											foreach (Column clm in new List<Column>(columns.Keys))
											{
												if (clm.FullTableName == c.FullTableName)
													columns.Remove(clm);
											}
										}
									}
									else if(c.ConstraintType != "CHECK")
										throw new InvalidOperationException(string.Format("Column {0} already in collection. Constraint {1}.",c.ToString(),c.ConstraintType), ex);
								}
							}
						}
					}
				}
			}

            if (string.IsNullOrEmpty(name_space))
                name_space = _db;
			ProcessColumns(columns, file, merge, dr, name_space, proh);
		}

		#region Helpers
		protected string AppendIdentity()
        {
            if (_m == "msft")
            {
                return "columnproperty(object_id(c.table_schema + '.' + c.table_name),c.column_name,'isIdentity') [identity]";
            }
            else
            {
                throw new NotSupportedException(_m + " is not supported");
            }
        }

		protected void ProcessColumns(Dictionary<Column, Column> columns, string file, string merge,
			bool dropColumns, string name_space, List<Pair<string>> prohibited)
		{
			OrmObjectsDef odef = null;

			if (File.Exists(file))
			{
				if (merge == "error")
					throw new InvalidOperationException("The file " + file + " is already exists.");

				odef = OrmObjectsDef.LoadFromXml(new System.Xml.XmlTextReader(file));
			}
			else
			{
				odef = new OrmObjectsDef();
				odef.Namespace = name_space;
				odef.SchemaVersion="1";
                if (!Path.IsPathRooted(file))
                    file = Path.Combine(Directory.GetCurrentDirectory(), file);
                //File.Create(file);
			}

			foreach (Column c in columns.Keys)
			{
                bool ent, col;
                EntityDescription e = GetEntity(odef, c.Schema, c.Table, out ent);
				PropertyDescription pd = AppendColumn(columns, c, e, out col);
                if (ent)
                {
                    Console.WriteLine("Create class {0} ({1})", e.Name, e.Identifier);
                    _ents.Add(e.Identifier, null);
                }
                else if(col)
                {
                    if (!_ents.ContainsKey(e.Identifier))
                    {
                        Console.WriteLine("Alter class {0} ({1})", e.Name, e.Identifier);
                        _ents.Add(e.Identifier, null);
                    }
                    Console.WriteLine("\tAdd property: " + pd.Name);
                }
			}

			ProcessProhibited(columns, prohibited, odef);

			ProcessM2M(columns, odef);

			if (dropColumns)
			{
				foreach(EntityDescription ed in odef.Entities)
				{
					List<PropertyDescription> col2remove = new List<PropertyDescription>();
					foreach (PropertyDescription pd in ed.Properties)
					{
						string[] ss = ed.SourceFragments[0].Name.Split('.');
						Column c = new Column(ss[0].Trim(new char[] { '[', ']' }), ss[1].Trim(new char[] { '[', ']' }),
                            pd.FieldName.Trim(new char[] { '[', ']' }), false, null, null, null, false, 1);
						if (!columns.ContainsKey(c))
						{
							col2remove.Add(pd);
						}
					}
					foreach (PropertyDescription pd in col2remove)
					{
						ed.Properties.Remove(pd);
                        Console.WriteLine("Remove: {0}.{1}", ed.Name, pd.Name);
					}
				}
			}

			using (System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(file, System.Text.Encoding.UTF8))
			{
				writer.Formatting = System.Xml.Formatting.Indented;
				odef.GetXmlDocument().Save(writer);
			}
		}

		protected void ProcessM2M(Dictionary<Column, Column> columns, OrmObjectsDef odef)
		{
			List<Pair<string>> tables = new List<Pair<string>>();

			using (DbConnection conn = GetDBConn(_server, _m, _db, _i, _user, _psw))
			{
				using (DbCommand cmd = conn.CreateCommand())
				{
					cmd.CommandType = CommandType.Text;
					cmd.CommandText = @"select table_schema,table_name from INFORMATION_SCHEMA.TABLE_CONSTRAINTS
						where constraint_type = 'FOREIGN KEY'
						group by table_schema,table_name
						having count(*) = 2";
					conn.Open();

					using (DbDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							tables.Add(new Pair<string>(reader.GetString(reader.GetOrdinal("table_schema")),
								reader.GetString(reader.GetOrdinal("table_name"))));
						}
					}
				}
			}

			foreach (Pair<string> p in tables)
			{
				string underlying = GetEntityName(p.First,p.Second);
				EntityDescription ued = odef.GetEntity(underlying);				
				using (DbConnection conn = GetDBConn(_server, _m, _db, _i, _user, _psw))
				{
					using (DbCommand cmd = conn.CreateCommand())
					{
						cmd.CommandType = CommandType.Text;
						cmd.CommandText = @"select cc.table_schema,cc.table_name,cc2.column_name,rc.delete_rule
						from INFORMATION_SCHEMA.constraint_column_usage cc
						join INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc on rc.unique_constraint_name = cc.constraint_name
						join INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc on tc.constraint_name = rc.constraint_name
						join INFORMATION_SCHEMA.constraint_column_usage cc2 on cc2.constraint_name = tc.constraint_name and cc2.table_schema = tc.table_schema and cc2.table_name = tc.table_name
						where tc.table_name = @tbl and tc.table_schema = @schema
						and tc.constraint_type = 'FOREIGN KEY'";

						DbParameter tbl = cmd.CreateParameter();
						tbl.ParameterName = "tbl";
						tbl.Value = p.Second;
						cmd.Parameters.Add(tbl);

						DbParameter schema = cmd.CreateParameter();
						schema.ParameterName = "schema";
						schema.Value = p.First;
						cmd.Parameters.Add(schema);

						conn.Open();

						List<LinkTarget> targets = new List<LinkTarget>();
						using (DbDataReader reader = cmd.ExecuteReader())
						{
							while (reader.Read())
							{
								//string ename = reader.GetString(reader.GetOrdinal("table_schema")) + "." +
								//    reader.GetString(reader.GetOrdinal("table_name"));
								bool deleteCascade = false;
								switch (reader.GetString(reader.GetOrdinal("delete_rule")))
								{
									case "NO ACTION":
										break;
									case "CASCADE":
										deleteCascade = true;
										break;
									default:
										throw new NotSupportedException("Cascade " + reader.GetString(reader.GetOrdinal("delete_rule")) + " is not supported");
								}
                                bool c;
								LinkTarget lt = new LinkTarget(
									GetEntity(odef,
										reader.GetString(reader.GetOrdinal("table_schema")),
										reader.GetString(reader.GetOrdinal("table_name")),out c),
										reader.GetString(reader.GetOrdinal("column_name")),deleteCascade);
								targets.Add(lt);
							}
						}
                        if (targets[0].Entity.Name == targets[1].Entity.Name)
                        {
                            LinkTarget t = targets[0];
                            SelfRelationDescription newRel = new SelfRelationDescription(t.Entity, targets[0], targets[1], GetSourceFragment(odef, p.First, p.Second), ued);
                            if (odef.GetSimilarRelation(newRel) == null)
                            {
                                odef.Relations.Add(newRel);
                            }
                        }
                        else
                        {
                            RelationDescription newRel = new RelationDescription(targets[0], targets[1], GetSourceFragment(odef, p.First, p.Second), ued);
                            if (odef.GetSimilarRelation(newRel) == null)
                            {
                                odef.Relations.Add(newRel);
                            }
                        }						
					}
				}
			}
		}

		protected PropertyDescription AppendColumn(Dictionary<Column, Column> columns, Column c, EntityDescription e, out bool created)
		{
			OrmObjectsDef odef = e.OrmObjectsDef;
            created = false;
			PropertyDescription pe = e.Properties.Find(delegate(PropertyDescription pd)
			{
                if (pd.FieldName == c.ColumnName || pd.FieldName.TrimEnd(']').TrimStart('[') == c.ColumnName)
					return true;
				else
					return false;
			});

			if (pe == null)
			{
				string[] attrs = null;
				bool pk = GetAttributes(c, out attrs);
				string name = Trim(Capitalize(c.ColumnName));
				if (pk && c.PKCount == 1)
					name = "ID";

				pe = new PropertyDescription(name,
					 null, attrs, null, GetType(c, columns, odef), c.ColumnName,
                     e.SourceFragments[0], AccessLevel.Private, AccessLevel.Public);
				e.Properties.Add(pe);
                created = true;
			}
			else
			{
				string[] attrs = null;
				if (GetAttributes(c, out attrs))
					pe.Name = "ID";
				pe.Attributes = Merge(pe.Attributes,attrs);
                if (!pe.PropertyType.IsUserType)
				    pe.PropertyType = GetType(c, columns, odef);
			}
			return pe;
		}

        private static string[] Merge(string[] oldstrings, string[] newstrings)
        {
            List<string> l = new List<string>(oldstrings);
            foreach (string s in newstrings)
            {
                if (s == "PK")
                    l.Remove("PrimaryKey");
                if (s == "PrimaryKey")
                    l.Remove("PK");

                if (!l.Contains(s))
                {
                    l.Add(s);
                }

            }
            return l.ToArray();
        }

		private string Trim(string columnName)
		{
            if (_transform)
            {
                if (columnName.EndsWith("_id"))
                    columnName = columnName.Substring(0, columnName.Length - 3);
                else if (columnName.EndsWith("_dt"))
                    columnName = columnName.Substring(0, columnName.Length - 3);

                Regex re = new Regex(@"_(\w)");
                columnName = re.Replace(columnName,new MatchEvaluator(delegate(Match m){
                    return m.Groups[1].Value.ToUpper();
                }));

                re = new Regex(@"(\w)-(\w)");
                columnName = re.Replace(columnName,new MatchEvaluator(delegate(Match m){
                    return m.Groups[1] + m.Groups[2].Value.ToUpper();
                }));

                return columnName;
            }
            else
                return columnName;
		}

		protected void ProcessProhibited(Dictionary<Column, Column> columns, List<Pair<string>> prohibited, OrmObjectsDef odef)
		{
			foreach(Pair<string> p in prohibited)
			{
				TypeDescription td = GetRelatedType(p.Second,columns,odef);
				if (td.Entity != null)
				{
					EntityDescription ed = td.Entity;
					string[] ss = p.First.Split('.');
					AppendColumns(columns, ed, ss[0], ss[1], p.Second);
					var t = GetSourceFragment(odef, ss[0], ss[1]);
					if (!ed.SourceFragments.Contains(t))
						ed.SourceFragments.Add(t);
                    //SourceFragmentDescription t = GetSourceFragment(odef, ss[0], ss[1]);
                    //if (!ed.SourceFragments.Contains(t))
                    //    ed.SourceFragments.Add(t);
				}
			}
		}

		private static SourceFragmentDescription GetSourceFragment(OrmObjectsDef odef, string schema, string table)
		{
			string id = "tbl" + schema + table;
			var t = odef.GetSourceFragment(id);
			if (t == null)
			{
                t = new SourceFragmentDescription(id, table, schema);
				odef.SourceFragments.Add(t);
			}
			return t;
		}

		protected void AppendColumns(Dictionary<Column, Column> columns, EntityDescription ed, 
			string schema, string table, string constraint)
		{
			using (DbConnection conn = GetDBConn(_server, _m, _db, _i, _user, _psw))
			{
				using (DbCommand cmd = conn.CreateCommand())
				{
					cmd.CommandType = CommandType.Text;
					cmd.CommandText = @"select c.table_schema,@rtbl table_name,c.column_name,is_nullable,data_type,tc.constraint_type,cc.constraint_name from INFORMATION_SCHEMA.columns c
						left join INFORMATION_SCHEMA.constraint_column_usage cc on c.table_name = cc.table_name and c.table_schema = cc.table_schema and c.column_name = cc.column_name and cc.constraint_name != @cns
						left join INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc on c.table_name = cc.table_name and c.table_schema = cc.table_schema and cc.constraint_name = tc.constraint_name
						where c.table_name = @tbl and c.table_schema = @schema
						and (tc.constraint_type != 'PRIMARY KEY' or tc.constraint_type is null)";
					DbParameter tbl = cmd.CreateParameter();
					tbl.ParameterName = "tbl";
					tbl.Value = table;
					cmd.Parameters.Add(tbl);

					DbParameter s = cmd.CreateParameter();
					s.ParameterName = "schema";
					s.Value = schema;
					cmd.Parameters.Add(s);

					DbParameter rt = cmd.CreateParameter();
					rt.ParameterName = "rtbl";
					rt.Value = ed.SourceFragments[0].Name.Split('.')[1].Trim(new char[] { '[', ']' });
					cmd.Parameters.Add(rt);

					DbParameter cns = cmd.CreateParameter();
					cns.ParameterName = "cns";
					cns.Value = constraint;
					cmd.Parameters.Add(cns); 

					conn.Open();

					using (DbDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							Column c = Column.Create(reader);
							if (!columns.ContainsKey(c))
							{
								columns.Add(c, c);
                                bool cr;
								PropertyDescription pd = AppendColumn(columns, c, ed, out cr);
								if (String.IsNullOrEmpty(pd.Description))
								{
									pd.Description = "Autogenerated from table " + schema + "." + table;
								}
							}
						}
					}
				}
			}
		}

		protected TypeDescription GetType(Column c, IDictionary<Column, Column> columns, OrmObjectsDef odef)
		{
			TypeDescription t = null;

			if (c.ConstraintType == "FOREIGN KEY")
			{
				t = GetRelatedType(c, columns, odef);
			}
			else
			{
				t = GetClrType(c.DbType, c.IsNullable, odef);
			}
			return t;
		}

		protected TypeDescription GetRelatedType(Column col, IDictionary<Column, Column> columns, OrmObjectsDef odef)
		{
			using (DbConnection conn = GetDBConn(_server, _m, _db, _i, _user, _psw))
			{
				using (DbCommand cmd = conn.CreateCommand())
				{
					cmd.CommandType = CommandType.Text;
					cmd.CommandText = @"select tc.table_schema,tc.table_name,cc.column_name from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
						join INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc on tc.constraint_name = rc.unique_constraint_name
						join INFORMATION_SCHEMA.constraint_column_usage cc on tc.table_name = cc.table_name and tc.table_schema = cc.table_schema and tc.constraint_name = cc.constraint_name
						where rc.constraint_name = @cn";
					DbParameter cn = cmd.CreateParameter();
					cn.ParameterName = "cn";
					cn.Value = col.ConstraintName;
					cmd.Parameters.Add(cn);

					conn.Open();

					using (DbDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							Column c = new Column(reader.GetString(reader.GetOrdinal("table_schema")),
								reader.GetString(reader.GetOrdinal("table_name")),
								reader.GetString(reader.GetOrdinal("column_name")), false, null, null, null, false, 1);
							if (columns.ContainsKey(c))
							{
								string id = "t" + Capitalize(c.Table);
								TypeDescription t = odef.GetType(id,false);
								if (t == null)
								{
                                    bool cr;
                                    EntityDescription e = GetEntity(odef, c.Schema, c.Table, out cr);
									t = new TypeDescription(id, e);
									odef.Types.Add(t);
                                    if (cr)
                                    {
                                        Console.WriteLine("\tCreate class {0} ({1})", e.Name, e.Identifier);
                                        //_ents.Add(e.Identifier, null);
                                    }
								}
								return t;
							}
							else
							{
								return GetClrType(col.DbType, col.IsNullable, odef);
							}
						}
					}
				}
			}
			return null;
		}

		protected TypeDescription GetRelatedType(string constraint, IDictionary<Column, Column> columns, OrmObjectsDef odef)
		{
			using (DbConnection conn = GetDBConn(_server, _m, _db, _i, _user, _psw))
			{
				using (DbCommand cmd = conn.CreateCommand())
				{
					cmd.CommandType = CommandType.Text;
					cmd.CommandText = @"select tc.table_schema,tc.table_name,cc.column_name from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
						join INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc on tc.constraint_name = rc.unique_constraint_name
						join INFORMATION_SCHEMA.constraint_column_usage cc on tc.table_name = cc.table_name and tc.table_schema = cc.table_schema and tc.constraint_name = cc.constraint_name
						where rc.constraint_name = @cn";
					DbParameter cn = cmd.CreateParameter();
					cn.ParameterName = "cn";
					cn.Value = constraint;
					cmd.Parameters.Add(cn);

					conn.Open();

					using (DbDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							Column c = new Column(reader.GetString(reader.GetOrdinal("table_schema")),
								reader.GetString(reader.GetOrdinal("table_name")),
								reader.GetString(reader.GetOrdinal("column_name")), false, null, null, null, false, 1);
							if (columns.ContainsKey(c))
							{
								string id = "t" + Capitalize(c.Table);
								TypeDescription t = odef.GetType(id, true);
								if (t == null)
								{
                                    bool cr;
									t = new TypeDescription(id, GetEntity(odef, c.Schema, c.Table, out cr));
									odef.Types.Add(t);
								}
								return t;
							}							
						}
					}
				}
			}
			return null;
		}

		#endregion

		#region Static helpers
		private static DbConnection GetDBConn(string server, string m, string db, bool i, string user, string psw)
		{
			if (m == "msft")
			{
				System.Data.SqlClient.SqlConnectionStringBuilder cb = new System.Data.SqlClient.SqlConnectionStringBuilder();
				cb.DataSource = server;
				cb.InitialCatalog = db;
				if (i)
				{
					cb.IntegratedSecurity = true;
				}
				else
				{
					cb.UserID = user;
					cb.Password = psw;
				}
				return new System.Data.SqlClient.SqlConnection(cb.ConnectionString);
			}
			else
			{
				throw new NotSupportedException(m + " is not supported");
			}
		}

		private static EntityDescription GetEntity(OrmObjectsDef odef, string schema, string tableName, out bool created)
		{
            created = false;
			string ename = GetEntityName(schema,tableName);
			EntityDescription e = odef.GetEntity(ename);
			if (e == null)
			{
				e = new EntityDescription(ename, Capitalize(tableName),"", null, odef);
				var t = GetSourceFragment(odef, schema, tableName);
				e.SourceFragments.Add(t);
				odef.Entities.Add(e);
                created = true;
			}
			return e;
		}

		protected static string GetEntityName(string schema, string table)
		{
			return "e_" + schema + "_" + table;
		}

		private static string Capitalize(string s)
		{
			return s.Substring(0, 1).ToUpper() + s.Substring(1);
		}

		private static bool GetAttributes(Column c, out string[] attrs)
		{
			attrs = new string[] { };
            if (c.ConstraintType == "PRIMARY KEY")
            {
                if (!c.IsAutoIncrement)
                    attrs = new string[] { "PK" };
                else
                    attrs = new string[] { "PrimaryKey" };
                return true;
            }
			return false;
		}
		private static TypeDescription GetClrType(string dbType, bool nullable, OrmObjectsDef odef)
		{
			TypeDescription t = null;
			string id = null;
			string type = null;

			switch (dbType)
			{
				case "rowversion":
				case "timestamp":
					id = "tBytes";
					type = "System.Byte[]";
					break;
				case "varchar":
				case "nvarchar":
				case "char":
				case "nchar":
				case "text":
				case "ntext":
					id = "tString";
					type = "System.String";
					break;
				case "int":
					id = "tInt32";
					type = "System.Int32";
					break;
				case "smallint":
					id = "tInt16";
					type = "System.Int16";
					break;
				case "bigint":
					id = "tInt64";
					type = "System.Int64";
					break;
				case "tinyint":
					id = "tByte";
					type = "System.Byte";
					break;
				case "datetime":
				case "smalldatetime":
					id = "tDateTime";
					type = "System.DateTime";
					break;
				case "money":
				case "numeric":
				case "decimal":
					id = "tDecimal";
					type = "System.Decimal";
					break;
				case "float":
					id = "tDouble";
					type = "System.Double";
					break;
				case "real":
					id = "tSingle";
					type = "System.Single";
					break;
				case "varbinary":
				case "binary":
					id = "tBytes";
					type = "System.Byte[]";
					break;
				case "bit":
					id = "tBoolean";
					type = "System.Boolean";
					break;
				case "xml":
					id = "tXML";
					type = "System.Xml.XmlDocument";
					break;
				case "uniqueidentifier":
					id = "tGUID";
					type = "System.Guid";
					break;
				case "image":
					id = "tBytes";
					type = "System.Byte[]";
					break;
				default:
					throw new ArgumentException("Unknown database type " + dbType);
			}

			if (nullable)
				id += "nullable";

			t = odef.GetType(id, false);
			if (t == null)
			{
				Type tp = GetTypeByName(type);
				if (nullable && tp.IsValueType)
					type = String.Format("System.Nullable`1[{0}]", type);

				t = new TypeDescription(id, type);
				odef.Types.Add(t);
			}
			return t;
		}

		private static Type GetTypeByName(string type)
		{
			foreach (System.Reflection.Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type tp = a.GetType(type, false, true);
				if (tp != null)
					return tp;
			}
			throw new TypeLoadException("Cannot load type " + type);
		}

		private static string GetTableName(string schema, string table)
		{
			return "[" + schema + "].[" + table + "]";
		}

#endregion
	}
}
