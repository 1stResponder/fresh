//using CoT_Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Global.ContentHelpers
{
  //public class CoTContent : IFeedContent
  //{
  //  private CotEvent myCoT;
  //  private string myDeHash;

  //  public CoTContent(CotEvent evt, string deHash)
  //  {
  //    myCoT = evt;
  //    myDeHash = deHash;
  //  }

  //  /// <summary>
  //  /// Returns this CoT content's parent DE Hash ID
  //  /// </summary>
  //  /// <returns>DE Hash</returns>
  //  public string DEHash()
  //  {
  //    if (string.IsNullOrWhiteSpace(myDeHash))
  //    {
  //      return "0";
  //    }
  //    else
  //    {
  //      return myDeHash;
  //    }
  //  }

  //  public string Description()
  //  {
  //    return myCoT != null ? myCoT.Type : "";
  //  }

  //  public DateTime? Expires()
  //  {
  //    if (myCoT != null)
  //    {
  //      return myCoT.Stale;
  //    }
  //    else
  //    {
  //      return null;
  //    }
  //  }

  //  public string FriendlyName()
  //  {
  //    return myCoT != null ? myCoT.FriendlyType : "";
  //  }

  //  public string IconURL()
  //  {
  //    return "";
  //  }

  //  public string ImageURL()
  //  {
  //    return "";
  //  }

  //  public SimpleGeoLocation Location()
  //  {
  //    return myCoT != null ? new SimpleGeoLocation(myCoT.Point.Latitude, myCoT.Point.Longitude) : null;
  //  }

  //  public string Title()
  //  {
  //    return "CoT Point";
  //  }
  //}
}
