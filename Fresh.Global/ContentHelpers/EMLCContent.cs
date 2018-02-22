using EMS.NIEM.EMLC;
using EMS.NIEM.Incident;
using EMS.NIEM.MutualAid;
using EMS.NIEM.NIEMCommon;
using EMS.NIEM.Resource;
using EMS.NIEM.Sensor;
using Fresh.Global.IconHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fresh.Global.ContentHelpers
{
  /// <summary>
  /// Content parser for NIEM EM_LC content
  /// </summary>
  public class EMLCContent : IFeedContent
  {
    private Event myEvent;
    private XElement xe;
    private string myDeHash;

    private List<ResourceDetail> _resourceDetails;
    private List<IncidentDetail> _incidentDetails;
    private List<SensorDetail> _sensorDetails;
    private List<MutualAidDetail> maidDetails;

    /// <summary>
    /// Simple constructor
    /// </summary>
    /// <param name="evt">NIEM EM_LC Event</param>
    public EMLCContent(Event evt, string deHash)
    {
      myEvent = evt;
      myDeHash = deHash;
      ProcessEventDetails();

      try
      {
        xe = XElement.Parse(Properties.Resources.EventTypeCodeList);
      }
      catch (Exception e)
      {
        DEUtilities.LogMessage("Error in creating EMLCContent Content Helper", DEUtilities.LogLevel.Error, e);
        xe = null;
      }
    }

    /// <summary>
    /// Return a description from this Event
    /// </summary>
    /// <returns>Description</returns>
    public string Description()
    {
      string retVal = "Emergency Management Event";
   
      if (myEvent != null)
      {
        List<AltStatus> secondaryStatus = null;

        if(myEvent.Details != null // we have details
          && myEvent.Details.Count == 1 // there is only one details object
          && myEvent.Details[0].GetType() == typeof(ResourceDetail)) // and it is a Resource Detail
        {
          ResourceDetail resourceDetail = (ResourceDetail)myEvent.Details[0];

          if(resourceDetail.Status.SecondaryStatus != null // secondary status isn't null
            && resourceDetail.Status.SecondaryStatus.Count > 0) // and secondary status acually has content
          {
            secondaryStatus = resourceDetail.Status.SecondaryStatus;
          }
        }

        if (secondaryStatus != null) /* the only case this is not null is if
                                        a) There is a single ResourceDetail object present in the Event's Details list, and
                                        b) That ResourceDetail object has a non-null SecondaryStatus with at least one object.

                                        If those conditions are met, we want to use the secondary status for the description.
                                        Otherwise, just use EventTypeDescriptor as before.*/
        {
          StringBuilder sb = new StringBuilder();

          secondaryStatus.ForEach(ss => sb.Append(ss.GetSecondaryStatusText() + ", "));

          if (sb.Length > 0)
          {
            // remove last ", "
            sb.Length -= 2;
          }

          retVal = sb.ToString();
        }
        else if (myEvent.EventTypeDescriptor != null)
        {
          if (myEvent.EventTypeDescriptor.EventTypeDescriptorExtension != null && myEvent.EventTypeDescriptor.EventTypeDescriptorExtension.Count > 0)
          {
            retVal = "";
            foreach (string ext in myEvent.EventTypeDescriptor.EventTypeDescriptorExtension)
            {
              retVal = retVal + ext + " ";
            }
          }
          else
          {
            retVal = myEvent.EventTypeDescriptor.EventTypeCode;
          }
        }
      }

      return retVal;
    }

    /// <summary>
    /// Return the expires time for this Event
    /// </summary>
    /// <returns>Expires Time</returns>
    public DateTime? Expires()
    {
      DateTime? retVal = null;

      if (myEvent != null && myEvent.EventValidityDateTimeRange != null)
      {
        retVal = myEvent.EventValidityDateTimeRange.EndDate;
      }

      return retVal;
    }


    /// <summary>
    /// Gets the Friendly Name for the Event Type code
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private string GetNIEMTypeAsString(string type)
    {
      IEnumerable<string> friendlyname = from x in xe.Descendants(@"EventTypeCode")
                                         where (x.Attribute(@"Value").Value == type)
                                         select (string)x.Value;
      if (friendlyname.Count() == 1)
      {

        return friendlyname.First();
      }
      else
      {

        return type;
      }
    }

    /// <summary>
    /// Return the friendly name for this Event
    /// </summary>
    /// <returns>Friendly Name</returns>
    public string FriendlyName()
    {
      string retVal = "Emergency Management Event";

      if (myEvent != null && myEvent.EventTypeDescriptor != null)
      {

        if (myEvent.EventTypeDescriptor.EventTypeDescriptorExtension != null && myEvent.EventTypeDescriptor.EventTypeDescriptorExtension.Count > 0)
        {
          retVal = myEvent.EventTypeDescriptor.EventTypeDescriptorExtension[0];
        }
        else if (xe != null)
        {
          retVal = GetNIEMTypeAsString(myEvent.EventTypeDescriptor.CodeValue.ToString().Replace("_", "."));
        }
        else
        {
          retVal = myEvent.EventTypeDescriptor.CodeValue.ToString();
        }
      }

      return retVal;
    }

    //TODO: Update this function/icons to use the TypeName to future proof this method
    //TODO: as more subschemas are added with new icons, we probably don't want to have to update this everytime

    /// <summary>
    /// Return the url that points to the icon for this Event
    /// </summary>
    /// <returns>Icon Path</returns>
    public string IconURL()
    {
      string retVal = "";

      if (myEvent != null && myEvent.Details != null)
      {
        //TODO: DO something other than searching by the first type.
        // What if there were no details?!?
        if (myEvent.Details[0].GetType() == typeof(ResourceDetail))
        {
          retVal = GetIconFilename("Resource");
        }
        else if (myEvent.Details[0].GetType() == typeof(IncidentDetail))
        {
          retVal = GetIconFilename("Incident");
        }
        else if (myEvent.Details[0].GetType() == typeof(SensorDetail))
        {
          retVal = GetIconFilename("Sensor");
        }
      }

      return retVal;
    }

    /// <summary>
    /// Parses through the Icon File looking for a match on type and icon filename
    /// </summary>
    /// <param name="groupName">Group of icons to look for</param>
    /// <returns>Icon Fully Qualified Path</returns>
    private string GetIconFilename(string groupName)
    {
      string iconFilename = "";
      IconSet niemIcons = null;
      IconGroup group = null;

      //sanity check
      if (IconConfig.Icons != null)
      {
        foreach (IconSet iset in IconConfig.Icons.Sets)
        {
          if (iset.KindofSet.Equals("NIEM", StringComparison.InvariantCultureIgnoreCase))
          {
            niemIcons = iset;
            break;
          }
        }

        if (niemIcons != null)
        {
          foreach (IconGroup iGroup in niemIcons.Groups)
          {
            if (iGroup.KindofGroup.Equals(groupName, StringComparison.InvariantCultureIgnoreCase))
            {
              group = iGroup;
              break;
            }
          }

          if (group != null)
          {
            string resourceType = myEvent.EventTypeDescriptor.CodeValue.ToString();
            string[] splitType = resourceType.Split('_');

            string temp = "";
            //TODO:assumes file extension is .png, need to fix later

            //this loop strips off the right most subtype when looking for a match
            //on file name
            //For Exmample: ALS Ambulance type is
            //ATOM_GRDTRK_EQT_GRDVEH_CVLVEH_EM_EMS_AMBULANCE_ALS
            //each pass removes the right most subtype until either a match is found
            //or beginning of the string is hit (which isn't good)
            //ATOM_GRDTRK_EQT_GRDVEH_CVLVEH_EM_EMS_AMBULANCE_ALS
            //ATOM_GRDTRK_EQT_GRDVEH_CVLVEH_EM_EMS_AMBULANCE
            //ATOM_GRDTRK_EQT_GRDVEH_CVLVEH_EM_EMS
            //ATOM_GRDTRK_EQT_GRDVEH_CVLVEH_EM
            //...
            for (int i = splitType.Length; i >= 0; i--)
            {
              temp = String.Join("_", splitType, 0, i) + ".png";

              DEUtilities.LogMessage("Searching for icon filename: " + temp + " in NIEM group: " + groupName, DEUtilities.LogLevel.Debug);

              if (group.Filenames.Contains(temp))
              {
                iconFilename = group.RootFolder + @"/" + temp;
                break;
              }
            }

            if (string.IsNullOrWhiteSpace(iconFilename))
            {
              DEUtilities.LogMessage("Icon filename not found for: " + resourceType, DEUtilities.LogLevel.Error);
            }
          }
        }
      }
      return iconFilename;
    }

    /// <summary>
    /// URL of any images associated to this Event
    /// </summary>
    /// <returns>Fully Qualified Path</returns>
    public string ImageURL()
    {
      string retVal = "";

      if (myEvent != null)
      {
        //TODO: add image code here
      }

      return retVal;
    }

    /// <summary>
    /// Returns the lat and lon of this Event
    /// </summary>
    /// <returns>Lat and Lon</returns>
    public SimpleGeoLocation Location()
    {
      SimpleGeoLocation retVal = null;

      if (myEvent != null && myEvent.EventLocation != null
        && myEvent.EventLocation.LocationCylinder != null &&
        myEvent.EventLocation.LocationCylinder.LocationPoint.Point != null)
      {
        double lat = System.Convert.ToDouble(myEvent.EventLocation.LocationCylinder.LocationPoint.Point.Lat);
        double lon = System.Convert.ToDouble(myEvent.EventLocation.LocationCylinder.LocationPoint.Point.Lon);

        retVal = new SimpleGeoLocation(lat, lon);
      }

      return retVal;
    }

    /// <summary>
    /// Returns a title for this Event
    /// </summary>
    /// <returns>Title</returns>
    public string Title()
    {
      string retVal = "Emergency Management Event";

      if (myEvent != null)
      {
        retVal = myEvent.EventID;
      }

      return retVal;
    }

    /// <summary>
    /// Returns this EMLC content's parent DE Hash ID
    /// </summary>
    /// <returns>DE Hash</returns>
    public string DEHash()
    {
      if (string.IsNullOrWhiteSpace(myDeHash))
      {
        return "0";
      }
      else
      {
        return myDeHash;
      }
    }

    /// <summary>
    /// Initializations the internal detail lists and adds content to them
    /// </summary>
    private void ProcessEventDetails()
    {
      _resourceDetails = new List<ResourceDetail>();
      _incidentDetails = new List<IncidentDetail>();
      _sensorDetails = new List<SensorDetail>();

      // One pass through the list -> split into appropriate detail lists
      foreach (EventDetails detail in myEvent.Details)
      {
        Type detailType = detail.GetType();

        if (detailType == typeof(ResourceDetail))
        {
          _resourceDetails.Add(detail as ResourceDetail);
        }
        else if (detailType == typeof(IncidentDetail))
        {
          _incidentDetails.Add(detail as IncidentDetail);
        }
        else if (detailType == typeof(SensorDetail))
        {
          _sensorDetails.Add(detail as SensorDetail);
        }
        else if (detailType == typeof(MutualAidDetail))
        {
          if (maidDetails == null) //create list if null
          {
            maidDetails = new List<MutualAidDetail>();
          }
          maidDetails.Add(detail as MutualAidDetail);
        }
      }

      // reset to null if they're empty
      if (_resourceDetails.Count == 0)
      {
        _resourceDetails = null;
      }
      if (_incidentDetails.Count == 0)
      {
        _incidentDetails = null;
      }
      if (_sensorDetails.Count == 0)
      {
        _sensorDetails = null;
      }
    }

    /// <summary>
    /// Returns the list of resource details for this EMLC Event
    /// </summary>
    public List<ResourceDetail> ResourceDetails
    {
      get { return _resourceDetails; }
    }

    /// <summary>
    /// Returns the list of incident details for this EMLC Event
    /// </summary>
    public List<IncidentDetail> IncidentDetails
    {
      get { return _incidentDetails; }
    }

    /// <summary>
    /// Returns the list of sensor details for this EMLC Event
    /// </summary>
    public List<SensorDetail> SensorDetails
    {
      get { return _sensorDetails; }
    }

    /// <summary>
    /// Returns the list of mutual aid (MAID) details for this EMLC Event
    /// </summary>
    public List<MutualAidDetail> MaidDetails
    {
      get { return maidDetails; }
    }
  }
}
