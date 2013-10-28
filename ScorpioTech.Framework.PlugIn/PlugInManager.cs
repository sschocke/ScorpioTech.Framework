using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace ScorpioTech.Framework.PlugIn
{
    /// <summary>
    /// A Base definition of a PlugIn Manager
    /// </summary>
    /// <typeparam name="PlugInType">The class of the PlugIn being managed</typeparam>
    /// <typeparam name="PlugInHostType">The class of the PlugIn Host to use for the PlugIns</typeparam>
    public class PlugInManager<PlugInType, PlugInHostType> 
        where PlugInType : IPlugIn<PlugInHostType>
        where PlugInHostType : IPlugInHost
    {
        /// <summary>
        /// A list of all the PlugIns managed by this Manager
        /// </summary>
        public List<PlugInType> PlugIns { get; private set; }
        /// <summary>
        /// The registered PlugIn Host for the PlugIns
        /// </summary>
        public PlugInHostType Host { get; private set; }

        /// <summary>
        /// Create a new PlugIn Manager with the given PlugIn Host
        /// </summary>
        /// <param name="host"></param>
        public PlugInManager(PlugInHostType host)
        {
            PlugIns = new List<PlugInType>();
            Host = host;
        }

        /// <summary>
        /// Scan through a given Directory for all PlugIns, and load and Initialize them
        /// </summary>
        /// <param name="directory">Full path of directory to scan</param>
        public void Find(string directory)
        {
            Type typeInterface = typeof(PlugInType);

            try
            {
                PlugIns.Clear();
                if (Directory.Exists(directory) == false)
                {
                    return;
                }
                string[] dllFiles = Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);

                foreach (string file in dllFiles)
                {
                    Assembly pluginAssembly = Assembly.LoadFrom(file);

                    foreach (Type pluginType in pluginAssembly.GetTypes())
                    {
                        Type testType = pluginType.GetInterface(typeInterface.FullName);

                        if ((pluginType.IsSubclassOf(typeInterface) == true) || (testType != null))
                        {
                            PlugInType instance = (PlugInType)Activator.CreateInstance(pluginType);
                            instance.Initialize(Host);

                            PlugIns.Add(instance);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.Host.ThrowException(ex);
            }
        }
    }
}
