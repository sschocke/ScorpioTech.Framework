using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScorpioTech.Framework.PlugIn
{
    /// <summary>
    /// Defines a base interface for PlugIn classes
    /// </summary>
    /// <typeparam name="PlugInHost">The type of the Host class to use as a PlugIn Host</typeparam>
    public interface IPlugIn<PlugInHost> where PlugInHost : IPlugInHost
    {
        /// <summary>
        /// Name of the PlugIn
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Author of the PlugIn
        /// </summary>
        string Author { get; }
        /// <summary>
        /// Long description about the functionality of the PlugIn
        /// </summary>
        string Description { get; }
        /// <summary>
        /// Version of the PlugIn
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// The PlugIn Host of the PlugIn
        /// </summary>
        PlugInHost Host { get; }
        /// <summary>
        /// Base Initialization function for the PlugIn. Called automatically by PlugIn Manager
        /// </summary>
        /// <param name="host">The PlugIn Host for this PlugIn</param>
        void Initialize(PlugInHost host);
    }
}
