using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace Fresh.PostGIS
{
  /// <summary>
  /// Light-weight version of a DE message
  /// </summary>
  [DataContract]
  public class DELiteDTO
  {
    /// <summary>
    /// Lookup id for this DE message
    /// </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public int LookupID { get; set; }

    /// <summary>
    /// Distribution id for this DE message
    /// </summary>
    [DataMember]
    public string DistributionID { get; set; }

    /// <summary>
    /// Sender id for this DE message
    /// </summary>
    [DataMember]
    public string SenderID { get; set; }

    /// <summary>
    /// Date and time this DE message was sent
    /// </summary>
    [DataMember]
    public DateTime DateTimeSent { get; set; }
  }

  [DataContract]
  public class DEFullDTO : DELiteDTO
  {
    [DataMember]
    public string DEv1_0 { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public bool Delete { get; set; }
  }

  [DataContract]
  public class DEPositionDTO : DELiteDTO
  {
    [DataMember]
    public string Longitude { get; set; }

    [DataMember]
    public string Latitude { get; set; }

    [DataMember]
    public string Elevation { get; set; }

    [DataMember]
    public string CylinderRadius { get; set; }

    [DataMember]
    public string CylinderHalfHeight { get; set; }

    [DataMember]
    public string CylinderHeightAboveEllipsoid { get; set; }

    [DataMember]
    public DateTime? DateTimeGenerated { get; set; }

    [DataMember]
    public DateTime DateTimeStart { get; set; }

    [DataMember]
    public DateTime? DateTimeStale { get; set; }

  }

  [DataContract]
  public class DESearchDTO
  {
    /// <summary>
    /// The beginning time to search for (required and inclusive).
    /// </summary>
    [DataMember]
    public DateTime? DateTimeFrom { get; set; }

    /// <summary>
    /// The end time for the search (optional and inclusive). 
    /// </summary>
    [DataMember]
    public DateTime? DateTimeTo { get; set; }

    /// <summary>
    /// Should be Full or Lite.  Full returns the entire DE, Lite returns the lightweight version.
    /// It is optional and defaults to Lite.
    /// </summary>
    [DataMember]
    public string ReturnType { get; set; }
  }
}
