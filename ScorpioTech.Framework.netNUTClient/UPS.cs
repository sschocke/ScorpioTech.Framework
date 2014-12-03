using System;

namespace ScorpioTech.Framework.netNUTClient
{
    /// <summary>
    /// UPS Helper class for parsing UPS definitions
    /// </summary>
    public class UPS
    {
        /// <summary>
        /// UPS Variable Helper class for holding the properties of a UPS Variable
        /// </summary>
        public class VariableDescription
        {
            /// <summary>
            /// Name of the variable
            /// </summary>
            public string Name { get; private set; }
            /// <summary>
            /// Description of the variable
            /// </summary>
            public string Description { get; private set; }
            /// <summary>
            /// Data Type of the variable
            /// </summary>
            public string Type { get; private set; }
            /// <summary>
            /// Current Value of the variable
            /// </summary>
            public string Value { get; private set; }

            /// <summary>
            /// Construct a new UPS Variable
            /// </summary>
            /// <param name="name">Name of the variable</param>
            /// <param name="desc">Description of the variable</param>
            /// <param name="type">Data Type of the variable</param>
            /// <param name="val">Current Value of the variable</param>
            public VariableDescription(string name, string desc, string type, string val)
            {
                this.Name = name;
                this.Description = desc;
                this.Type = type;
                this.Value = val;
            }

            /// <summary>
            /// String Representation of the variable
            /// </summary>
            /// <returns>String containing UPS Variable Name + Current Value</returns>
            public override string ToString()
            {
                return this.Name + ": " + this.Value;
            }
        }
        /// <summary>
        /// Name of the UPS
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Description of the UPS
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// UPSD Host Address
        /// </summary>
        public string Host { get; private set; }

        /// <summary>
        /// Parse a UPS definition string
        /// </summary>
        /// <param name="upsDefinition">UPS definition string in format &lt;upsname&gt;[@&lt;hostname&gt;[:&lt;port&gt;]]</param>
        public UPS(string upsDefinition)
        {
            if( upsDefinition.Contains("@"))
            {
                this.Host = upsDefinition.Substring(upsDefinition.IndexOf('@') + 1);
                this.Name = upsDefinition.Substring(0, upsDefinition.IndexOf('@'));
            }
            else
            {
                this.Host = "localhost";
                this.Name = upsDefinition;
            }
        }

        /// <summary>
        /// Convert parts to string
        /// </summary>
        /// <returns>UPS definition string</returns>
        public override string ToString()
        {
            return this.Name + "@" + this.Host;
        }
    }
}
