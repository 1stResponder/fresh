using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Global
{

  /// <summary>
  /// Custom exception for DE Federation related issues
  /// </summary>
  [Serializable]
  public class FederationException : System.Exception
  {
	#region Constructor

	/// <summary>
	/// Default Constructor
	/// </summary>
	public FederationException() : base()
	{
	}

	/// <summary>
	/// Initalize Exception with a string msg
	/// </summary>
	/// <param name="msg">Exception msg</param>
	public FederationException(string msg) : base(msg)
	{
	  
	}

	/// <summary>
	/// Intialize exception with the msg and another Exception
	/// </summary>
	/// <param name="msg">string message</param>
	/// <param name="e">Inner Exception</param>
	public FederationException(string msg, Exception e) : base(msg, e)
	{

	}

	/// <summary>
	/// Constructor used for serialization
	/// </summary>
	/// <param name="info"></param>
	/// <param name="context"></param>
	protected FederationException(SerializationInfo info, StreamingContext context) : base(info, context)
	{
	}

	#endregion

  }
}
