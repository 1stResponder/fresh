using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Fresh.PostGIS
{
  /// <summary>
  /// Class:    FeedDTO
  /// Project:  Fresh.PostGIS
  /// Purpose:  This class is a data transfer class for entries in the Feeds table.
  ///           It represents the raw values in table columns.
  /// Created:  2016-09-08
  /// Author:   Marc Stogner
  /// 
  /// Updates:  none
  [DataContract]
  public class FeedDTO
  {
    /// <summary>
    /// Lookup id for this Feed
    /// </summary>
    [DataMember]
    public int LookupID { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public List<int> ContentLookupIDs { get; set; }

    [DataMember(IsRequired = false)]
    public string SourceID { get; set; }

    [DataMember(IsRequired = false)]
    public string SourceValue { get; set; }

    /// <summary>
    /// Name of the database view
    /// </summary>
    [DataMember(IsRequired = false)]
    public string ViewName { get; set; }

  }
}
