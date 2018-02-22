//using EDXLSharp.EDXLHAVE_2_0Lib;
//using GeoOASISWhereLib;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Fresh.Global.ContentHelpers
//{
//  public class HAVEContent : IFeedContent
//  {
//    private EDXLHAVE myHAVE = null;

//    public HAVEContent(EDXLHAVE hav)
//    {
//      myHAVE = hav;
//    }

//    public string Description()
//    {
//      string retVal = "";
//      if (myHAVE != null)
//      {
//        retVal = "Status:" + myHAVE.Hospital[0].HospitalFacilityStatus.FacilityStatus.ToString();
//      }
//      return retVal;
//    }

//    public DateTime? Expires()
//    {
//      DateTime? retVal = null;

//      if (myHAVE != null)
//      {
//        retVal = myHAVE.Hospital[0].LastUpdateTime.AddDays(1.0).ToUniversalTime();
//      }

//      return retVal;
//    }

//    public string FriendlyName()
//    {
//      return "Health Facility Status";
//    }

//    public string IconURL()
//    {
//      throw new NotImplementedException();
//    }

//    public string ImageURL()
//    {
//      throw new NotImplementedException();
//    }

//    public FreshLocation Location()
//    {
//      FreshLocation mLoc = null;

//      if (myHAVE != null && myHAVE.Hospital.Count > 0)
//      {
//        GMLPoint point = myHAVE.Hospital[0].Organization.OrganizationGeoLocation.Location as GMLPoint;
//        mLoc = new FreshLocation(point.Pos.Pos[0], point.Pos.Pos[1]);
//      }

//      return mLoc;
//    }

//    public string Title()
//    {
//      string sTitle = "HAVE Message";
//      if (myHAVE != null && myHAVE.Hospital.Count > 0)
//      {
//        sTitle = myHAVE.Hospital[0].Organization.OrganizationInformation.OrgName.NameElement;
//      }
//      return sTitle;
//    }
//  }
//}
