using System;
using System.IO;
using System.Xml.Serialization;


namespace Fresh.Global
{
    /// <summary>
    /// Configuration for the AWS Feed Constants
    /// </summary>
    /// 
    /// <summary>
    /// Contains the constants used in various pats of IC.NET
    /// </summary>
    public class AWSConstants
    {
        /// <summary>
        /// Log4net logging object
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The base icnet global namespace for WCF services
        /// </summary>

        /// <summary>
        /// Initializes static members of the ICNETSOAConstants class. 
        /// </summary>
        static AWSConstants()
        {
        }

        public static int KMLIconScale
        {
          get { return 2050; }
        }
  }
}
