using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace Worm.CodeGen.XmlGenerator
{
	public class Column
	{
		private string _schema;
		private string _table;
		private string _column;

		private bool _isNullable;
		private string _type;
		private string _constraintType;
		private string _constraintName;
        private bool _identity;

		protected Column()
		{
		}

		public Column(string schema, string table, string column,
			bool isNullable, string type, string constraintType, string constraintName, bool identity)
		{
			_schema = schema;
			_table = table;
			_column = column;

			_isNullable = isNullable;
			_type = type;
			_constraintType = constraintType;
			_constraintName = constraintName;
            _identity = identity;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as Column);
		}

		public bool Equals(Column obj)
		{
			if (obj == null)
				return false;

			return ToString() == obj.ToString();
		}

		public override string ToString()
		{
			return _schema + _table + _column;
		}

		public override int GetHashCode()
		{
			//System.Diagnostics.Debug.WriteLine(ToString());
			//System.Diagnostics.Debug.WriteLine(ToString().GetHashCode());
			return ToString().GetHashCode();
		}

		public string Schema
		{
			get { return _schema; }
		}

		public string Table
		{
			get { return _table; }
		}

		public string ColumnName
		{
			get { return _column; }
		}

        public bool IsAutoIncrement
        {
            get { return _identity; }
        }

		public bool IsNullable
		{
			get { return _isNullable; }
		}

		public string DbType
		{
			get { return _type; }
		}

		public string ConstraintType
		{
			get { return _constraintType; }
		}

		public string ConstraintName
		{
			get { return _constraintName; }
		}

		public string FullTableName
		{
			get { return _schema + "." + _table; }
		}

		public static Column Create(DbDataReader reader)
		{
			Column c = new Column();

			c._schema = reader.GetString(reader.GetOrdinal("table_schema"));
			c._table = reader.GetString(reader.GetOrdinal("table_name"));
			c._column = reader.GetString(reader.GetOrdinal("column_name"));

			string yn = reader.GetString(reader.GetOrdinal("is_nullable"));
			if (yn == "YES")
			{
				c._isNullable = true;
			}
			c._type = reader.GetString(reader.GetOrdinal("data_type"));

			int ct = reader.GetOrdinal("constraint_type");
			int cn = reader.GetOrdinal("constraint_name");

			if (!reader.IsDBNull(ct))
			{
				c._constraintType = reader.GetString(ct);
			}

			if (!reader.IsDBNull(cn))
			{
				c._constraintName = reader.GetString(cn);
			}
            
            c._identity = Convert.ToBoolean(reader.GetInt32(reader.GetOrdinal("identity")));

			return c;
		}
	}
}
