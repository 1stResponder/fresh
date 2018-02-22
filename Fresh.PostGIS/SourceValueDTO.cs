using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

/// <summary>
/// File:     SourceValueDTO.cs
/// Project:  Fresh.PostGIS
/// Purpose:  This file contains the Source Value Data Transfer Object class, which represents an entry
///           in the Source Values table.
/// Created:  2016-03-10
/// Author:   Brian Wilkins - ArdentMC
/// 
/// Updates:  none
/// </summary>
namespace Fresh.PostGIS
{
  /// <summary>
  /// Class:    SourceValueDTO
  /// Project:  Fresh.PostGIS
  /// Purpose:  This class is a data transfer class for entries in the Source Values table.
  /// Created:  2016-03-16
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  /// </summary>
  [DataContract]
  public class SourceValueDTO
  {
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public int LookupID { get; set; }

    [DataMember]
    public string ID { get; set; }

    [DataMember]
    public string Value { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public int FeedLookupID { get; set; }

    public override string ToString()
    {
      var writer = new StringWriter();
      (new XmlSerializer(typeof(SourceValueDTO))).Serialize(writer, this);
      return writer.ToString();
    }
  }
}
