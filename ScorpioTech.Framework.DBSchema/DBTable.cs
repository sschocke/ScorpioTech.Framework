using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScorpioTech.Framework.DBSchema
{
    internal class DBTableComparer : IEqualityComparer<DBTable>
    {
        #region IEqualityComparer<DBTable> Members

        public bool Equals(DBTable x, DBTable y)
        {
            if (x.Name == y.Name)
            {
                return true;
            }

            return false;
        }

        public int GetHashCode(DBTable obj)
        {
            return obj.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// Class represents the structure of a Database Table
    /// </summary>
    public class DBTable
    {
        /// <summary>
        /// Represents a Database Column
        /// </summary>
        public class Column
        {
            /// <summary>
            /// Name
            /// </summary>
            public string Name { get; internal set; }
            /// <summary>
            /// Data Type
            /// </summary>
            public string DataType { get; internal set; }
            /// <summary>
            /// Any Database options like NOT NULL or Defaults
            /// </summary>
            public string Options { get; internal set; }

            /// <summary>
            /// String representation of the Database Column
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "DBTable.Column Name='" + Name + "', DataType='" + DataType + "', Options='" + Options + "'";
            }
        }
        /// <summary>
        /// Represents a Database Primary Key
        /// </summary>
        public class PKey
        {
            /// <summary>
            /// Name
            /// </summary>
            public string Name { get; internal set; }
            /// <summary>
            /// List of Columns in the Primary Key
            /// </summary>
            public List<String> KeyColumns { get; internal set; }

            /// <summary>
            /// Create a new Primary Key
            /// </summary>
            internal PKey()
            {
                KeyColumns = new List<string>();
            }

            /// <summary>
            /// Add a column to the Primary Key
            /// </summary>
            /// <param name="columnName"></param>
            internal void AddKeyColumn(string columnName)
            {
                if (KeyColumns.Contains(columnName) == true)
                {
                    throw new ArgumentException("Column already in Primary Key list!!", "columnName");
                }

                KeyColumns.Add(columnName);
            }

            /// <summary>
            /// Returns a string representation of the Primary Key
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "DBTable.PrimaryKey Name=" + Name + ", KeyColumns=" + KeyColumns.Count.ToString();
            }
        }
        /// <summary>
        /// Represents a Database Index
        /// </summary>
        public class Index
        {
            /// <summary>
            /// Name
            /// </summary>
            public string Name { get; internal set; }
            /// <summary>
            /// Is this a UNIQUE index
            /// </summary>
            public bool Unique { get; internal set; }
            /// <summary>
            /// Is this Index CLUSTERED
            /// </summary>
            public bool Clustered { get; internal set; }
            /// <summary>
            /// List of Columns in this Index
            /// </summary>
            public Dictionary<String, bool> Columns { get; internal set; }

            /// <summary>
            /// Create a new Index
            /// </summary>
            internal Index()
            {
                Columns = new Dictionary<string, bool>();
            }

            /// <summary>
            /// Add a column to the Index
            /// </summary>
            /// <param name="columnName"></param>
            /// <param name="descending"></param>
            internal void AddIndexColumn(string columnName, bool descending)
            {
                if (Columns.ContainsKey(columnName) == true)
                {
                    throw new ArgumentException("Column already in Index column list!!", "columnName");
                }

                Columns.Add(columnName, descending);
            }

            /// <summary>
            /// Returns a string represenation of the Index
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "DBTable.Index Name=" + Name + ", Columns=" + Columns.Count.ToString();
            }
        }
        /// <summary>
        /// Represents a Database Foreign Key
        /// </summary>
        public class ForeignKey
        {
            /// <summary>
            /// Name
            /// </summary>
            public string Name { get; internal set; }
            /// <summary>
            /// Column
            /// </summary>
            public string Column { get; internal set; }
            /// <summary>
            /// Primary Key Table
            /// </summary>
            public string PKeyTable { get; internal set; }
            /// <summary>
            /// Primary Key Column
            /// </summary>
            public string PKeyColumn { get; internal set; }

            /// <summary>
            /// Returns a string representation of the Foreign Key
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "DBTable.ForeignKey Name=" + Name + ", Column=" + Column + ", PKeyTable=" + PKeyTable + ", PKeyColumn=" + PKeyColumn;
            }
        }

        /// <summary>
        /// Table Name
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// List of Columns
        /// </summary>
        public List<Column> Columns { get; internal set; }
        /// <summary>
        /// Primary Key
        /// </summary>
        public PKey PrimaryKey { get; internal set; }
        /// <summary>
        /// List of Indexes
        /// </summary>
        public List<Index> Indexes { get; internal set; }
        /// <summary>
        /// List of Foreign Keys
        /// </summary>
        public List<ForeignKey> ForeignKeys { get; internal set; }
        /// <summary>
        /// List of Default Records for this table
        /// </summary>
        public List<DBDefaultRecord> DefaultRecords { get; internal set; }

        /// <summary>
        /// Create a new Database Table
        /// </summary>
        /// <param name="name"></param>
        internal DBTable(string name)
        {
            this.Name = name;
            this.Columns = new List<Column>();
            this.PrimaryKey = new PKey();
            this.Indexes = new List<Index>();
            this.ForeignKeys = new List<ForeignKey>();
            this.DefaultRecords = new List<DBDefaultRecord>();
        }

        /// <summary>
        /// Add a column to the table
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        /// <param name="options"></param>
        internal void AddColumn(string name, string dataType, string options)
        {
            Column col = new Column();
            col.Name = name;
            col.DataType = dataType;
            col.Options = options;

            this.Columns.Add(col);
        }

        /// <summary>
        /// Add an index to the table
        /// </summary>
        /// <param name="name"></param>
        /// <param name="clustered"></param>
        /// <param name="unique"></param>
        /// <returns></returns>
        internal Index AddIndex(string name, bool clustered, bool unique)
        {
            Index idx = new Index();
            idx.Name = name;
            idx.Unique = unique;
            idx.Clustered = clustered;

            this.Indexes.Add(idx);

            return idx;
        }

        /// <summary>
        /// Add a foreign key to the table
        /// </summary>
        /// <param name="name"></param>
        /// <param name="column"></param>
        /// <param name="pKeyTable"></param>
        /// <param name="pKeyColumn"></param>
        internal void AddForeignKey(string name, string column, string pKeyTable, string pKeyColumn)
        {
            ForeignKey fkey = new ForeignKey();
            fkey.Name = name;
            fkey.Column = column;
            fkey.PKeyTable = pKeyTable;
            fkey.PKeyColumn = pKeyColumn;

            this.ForeignKeys.Add(fkey);
        }

        /// <summary>
        /// Add a default record to the table
        /// </summary>
        /// <returns></returns>
        internal DBDefaultRecord AddDefaultRecord()
        {
            DBDefaultRecord rec = new DBDefaultRecord();
            this.DefaultRecords.Add(rec);

            return rec;
        }

        /// <summary>
        /// Find the Database Column with the given name
        /// </summary>
        /// <param name="columnName">Name of the column to find</param>
        /// <returns>The column if found, null otherwise</returns>
        public Column FindColumn(string columnName)
        {
            foreach (Column column in Columns)
            {
                if (column.Name == columnName)
                {
                    return column;
                }
            }

            return null;
        }

        /// <summary>
        /// Find the Database Index with the given name
        /// </summary>
        /// <param name="indexName">Name of the index to find</param>
        /// <returns>The index if found, null otherwise</returns>
        public Index FindIndex(string indexName)
        {
            foreach (Index idx in Indexes)
            {
                if (idx.Name == indexName)
                {
                    return idx;
                }
            }

            return null;
        }

        /// <summary>
        /// Find the Database Foreign Key with the given name
        /// </summary>
        /// <param name="fkeyName">Name of the foreign key to find</param>
        /// <returns>The foreign key if found, null otherwise</returns>
        public ForeignKey FindForeignKey(string fkeyName)
        {
            foreach (ForeignKey fkey in ForeignKeys)
            {
                if (fkey.Name == fkeyName)
                {
                    return fkey;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a string representation of the Database Table
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "DBTable Name=" + Name + ", Columns=" + Columns.Count.ToString() + ", " + PrimaryKey.ToString();
        }
    }
}
