using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScorpioTech.Framework.PlugIn
{
    /// <summary>
    /// Defines a base interface for PlugIn Host classes
    /// </summary>
    public interface IPlugInHost
    {
        /// <summary>
        /// Method for returning an error to the PlugIn Host
        /// </summary>
        /// <param name="error">Error message</param>
        void PlugInError(string error);
        /// <summary>
        /// Used to pass an exception back to the PlugIn Host from a PlugIn
        /// </summary>
        /// <param name="ex"></param>
        void ThrowException(Exception ex);
    }
}
