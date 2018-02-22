using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

/// <summary>
/// File:     RuleSourceValueDTO.cs
/// Project:  Fresh.PostGIS
/// Purpose:  This file contains the Rule Source Value Data Transfer Object class, which represents an entry
///           in the Rules table.
/// Created:  2016-03-10
/// Author:   Brian Wilkins - ArdentMC
/// 
/// Updates:  none
/// </summary>
namespace Fresh.PostGIS
{
  /// <summary>
  /// Class:    RuleSourceValueDTO
  /// Project:  Fresh.PostGIS
  /// Purpose:  This class is a data transfer class for entries in the Rules table.
  ///           It ignores any federation uri and only supports Source Value DTOs,
  ///           which are really just a name/value pair.
  /// Created:  2016-03-16
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  [DataContract]
  [KnownType(typeof(SourceValueDTO))]
  public class RuleSourceValueDTO
  {
    [DataMember]
    public string DEElement { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public int RuleLookupID { get; set; }

    [DataMember]
    public string SourceID { get; set; }

    [DataMember]
    public string SourceValue { get; set; }

    [DataMember]
    public List<SourceValueDTO> Feeds { get; set; }

    public override string ToString()
    {
      var writer = new StringWriter();
      (new XmlSerializer(typeof(RuleSourceValueDTO))).Serialize(writer, this);
      return writer.ToString();
    }
  }
}
