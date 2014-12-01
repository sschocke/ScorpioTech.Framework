using System;

namespace ScorpioTech.Framework.netNUTClient
{
    /// <summary>
    /// UPS Helper class for parsing UPS definitions
    /// </summary>
    public class UPS
    {
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
