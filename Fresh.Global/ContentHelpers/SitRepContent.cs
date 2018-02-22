using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Global.ContentHelpers
{
  public class SitRepContent : IFeedContent
  {
    private string myDELookupID = "";

    public string Description()
    {
      throw new NotImplementedException();
    }

    public DateTime? Expires()
    {
      throw new NotImplementedException();
    }

    public string FriendlyName()
    {
      throw new NotImplementedException();
    }

    public string IconURL()
    {
      throw new NotImplementedException();
    }

    public string ImageURL()
    {
      throw new NotImplementedException();
    }

    public SimpleGeoLocation Location()
    {
      throw new NotImplementedException();
    }

    public string Title()
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Returns this EMLC content's parent DE Hash ID
    /// </summary>
    /// <returns>DE Hash</returns>
    public string DEHash()
    {
      if (string.IsNullOrWhiteSpace(myDELookupID))
      {
        return "0";
      }
      else
      {
        return myDELookupID;
      }
    }
  }
}
