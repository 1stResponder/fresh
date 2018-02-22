//using EDXLSharp.MQTTSensorThingsLib;
//using Fresh.Global.IconHelpers;
//using System;
//using Newtonsoft.Json.Linq;

//namespace Fresh.Global.ContentHelpers
//{
//  /// <summary>
//  /// Content parser for SensorThings content
//  /// </summary>
//  public class STContent: IFeedContent
//  {
//    private Observation myObservation;
//    private string myDeHash;

//    /// <summary>
//    /// Simple constructor
//    /// </summary>
//    /// <param name="observation">SensorThings Observation</param>
//    public STContent(Observation observation, string deHash)
//    {
//      myObservation = observation;
//      myDeHash = deHash;
//    }

//    /// <summary>
//    /// Returns this STO content's parent DE Hash ID
//    /// </summary>
//    /// <returns>DE Hash</returns>
//    public string DEHash()
//    {
//      if (string.IsNullOrWhiteSpace(myDeHash))
//      {
//        return "0";
//      }
//      else
//      {
//        return myDeHash;
//      }
//    }

//    /// <summary>
//    /// Return a description from this Observation
//    /// </summary>
//    /// <returns>Description</returns>
//    public string Description()
//    {
//      string retVal = "SensorThings Observation";

//      if (myObservation != null)
//      {
//        retVal = myObservation.Result;
//      }

//      return retVal;
//    }

//    /// <summary>
//    /// Return the expires time for this Observation.
//    /// Its value is the ValidEndTime of the Observation(if any).
//    /// </summary>
//    /// <returns>Expires Time</returns>
//    public DateTime? Expires()
//    {
//      DateTime? retVal = null;

//      if (myObservation != null && myObservation.ValidTimeEnd.HasValue)
//      { 
//        retVal = myObservation.ValidTimeEnd;
//      }
//      return retVal;
//    }

//    /// <summary>
//    /// Return the friendly name for this Observation
//    /// </summary>
//    /// <returns>Friendly Name</returns>
//    public string FriendlyName()
//    {
//      string retVal = "SensorThings Observation";

//      if (myObservation != null)
//      {
//        retVal = retVal + "# " + myObservation.Id;
//      }
//      return retVal;
//    }

//    /// <summary>
//    /// Return the url that points to the icon for this Observation
//    /// </summary>
//    /// <returns>Icon Path</returns>
//    public string IconURL()
//    {
//      string retVal = "";      

//      if (myObservation != null)
//      {
//        // TODO: revisit this once we have more than 1 type of icon
//        retVal = GetIconFilename("OGC");
//      }

//      return retVal;
//    }

//    /// <summary>
//    /// Parses through the Icon File looking for a match on type and icon filename
//    /// </summary>
//    /// <param name="groupName">Group of icons to look for</param>
//    /// <returns>Icon Fully Qualified Path</returns>
//    private string GetIconFilename(string groupName)
//    {
//      string iconFilename = "";
//      IconSet sensorIcons = null;
//      IconGroup group = null;
      
//      if (IconConfig.Icons != null)  // This should never happen..but it did at least once.
//      {
//        foreach (IconSet iset in IconConfig.Icons.Sets)
//        {
//          if (iset.KindofSet.Equals("SENSOR", StringComparison.InvariantCultureIgnoreCase))
//          {
//            sensorIcons = iset;
//            break;
//          }
//        }

//        if (sensorIcons != null)
//        {
//          foreach (IconGroup iGroup in sensorIcons.Groups)
//          {
//            if (iGroup.KindofGroup.Equals(groupName, StringComparison.InvariantCultureIgnoreCase))
//            {
//              group = iGroup;
//              break;
//            }
//          }

//          // TODO: revisit this once we have more than 1 type of icon
//          if (group != null)
//          {
//            if (group.Filenames != null && group.Filenames.Count >= 1)
//            {
//              iconFilename = group.RootFolder + @"/" + group.Filenames[0];
//            }
//          }
//        }
//      }
//      return iconFilename;

//    }

//    /// <summary>
//    /// URL of any images associated to this Observation
//    /// </summary>
//    /// <returns>Fully Qualified Path</returns>
//    public string ImageURL()
//    {
//      string retVal = "";

//      if (myObservation != null)
//      {
//        //TODO: add image code here
//      }

//      return retVal;
//    }

//    /// <summary>
//    /// Returns the lat and lon of this Observation
//    /// </summary>
//    /// <returns>Lat and Lon</returns>
//    public SimpleGeoLocation Location()
//    {
//      SimpleGeoLocation retVal = null;

//      if (myObservation != null && myObservation.FeatureOfInterest != null 
//        && myObservation.FeatureOfInterest.EncodingType != null 
//        && myObservation.FeatureOfInterest.FeatureString != null )
//      {
//        string featureString = myObservation.FeatureOfInterest.FeatureString;
//        if( myObservation.FeatureOfInterest.EncodingType.Equals("application/vnd.geo+json"))
//        {
//          try
//          {
//            JObject result = JObject.Parse(featureString);
//            string type = result.Value<string>("type");
//            if ("Point".Equals(type, StringComparison.InvariantCultureIgnoreCase))
//            {
//              JArray coordinates = (JArray)result["coordinates"];
//              double lat = System.Convert.ToDouble(coordinates[1]);
//              double lon = System.Convert.ToDouble(coordinates[0]);
//              retVal = new SimpleGeoLocation(lat, lon);
//            }
//          }
//          catch ( Exception ex)
//          {
//            // JSON parsing issue, just return null.
//          }

//        }
//      }

//      return retVal;
//    }

//    /// <summary>
//    /// Returns a title for this Observation
//    /// </summary>
//    /// <returns>Title</returns>
//    public string Title()
//    {
//      string retVal = "SensorThings Observation";

//      if (myObservation != null)
//      {
//        retVal = myObservation.Id.ToString();
//      }

//      return retVal;
//    }
//  }
//}
