using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScorpioTech.Framework.DBSchema
{
    /// <summary>
    /// Dictionary of Default Records per Database Table
    /// </summary>
    public class DBDefaultRecords : Dictionary<string, List<DBDefaultRecord>>
    {
        /// <summary>
        /// Create a new Dictionary
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public DBDefaultRecord AddRecord(string table)
        {
            if (this.ContainsKey(table) == false)
            {
                this.Add(table, new List<DBDefaultRecord>());
            }

            DBDefaultRecord rec = new DBDefaultRecord();
            this[table].Add(rec);

            return rec;
        }
    }
}
