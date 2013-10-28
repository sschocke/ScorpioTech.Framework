using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScorpioTech.Framework.DBSchema
{
    /// <summary>
    /// Default Record that needs to be recorded in the database
    /// </summary>
    public class DBDefaultRecord
    {
        /// <summary>
        /// Dictionary of the columns and values for this Default Record
        /// </summary>
        public Dictionary<string, object> Values { get; private set; }

        /// <summary>
        /// Create a new Default Record
        /// </summary>
        public DBDefaultRecord()
        {
            Values = new Dictionary<string, object>();
        }

        /// <summary>
        /// Add a new column and value to the record
        /// </summary>
        /// <param name="column"></param>
        /// <param name="value"></param>
        public void AddValue(string column, object value)
        {
            if (this.Values.ContainsKey(column))
            {
                throw new ArgumentException("Column '" + column + "' already has a value assigned", "column");
            }

            this.Values.Add(column, value);
        }

        /// <summary>
        /// Returns the string representation of this record
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if( this.Values.Count < 1 )
            {
                return "DBDefaultRecord: No Data";
            }
            int max = this.Values.Count;
            if (max > 2)
            {
                max = 2;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("DBDefaultRecord: ");
            int i = 0;
            foreach(KeyValuePair<string,object> kvp in this.Values)
            {
                sb.Append(kvp.Key + "=" + kvp.Value.ToString());
                i++;
                if (i >= max)
                {
                    break;
                }
                else
                {
                    sb.Append(",");
                }
            }

            if (this.Values.Count > 2)
            {
                sb.Append(", " + (this.Values.Count - 2).ToString() + " other...");
            }

            return sb.ToString();
        }
    }
}
