using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

/// <summary>
/// File:     SourceValueListDTO.cs
/// Project:  Fresh.PostGIS
/// Purpose:  This file contains the Source Value List Data Transfer Object class, which represents a set of entries
///           in the Source Values table.
/// Created:  2016-04-20
/// Author:   Brian Wilkins - ArdentMC
/// 
/// Updates:  none
/// </summary>
namespace Fresh.PostGIS
{
  /// <summary>
  /// Class:    SourceValueListDTO
  /// Project:  Fresh.PostGIS
  /// Purpose:  This class is a data transfer class for entries in the Source Values table.
  /// Created:  2016-04-20
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  /// </summary>
  [DataContract]
  public class SourceValueListDTO
  {
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public int LookupID { get; set; }

    [DataMember]
    public string ID { get; set; }

    [DataMember]
    public List<ValueDTO> Values { get; set; }

    public override string ToString()
    {
      var writer = new StringWriter();
      (new XmlSerializer(typeof(SourceValueListDTO))).Serialize(writer, this);
      return writer.ToString();
    }
  }

  [CollectionDataContract(Name ="ValueItem")]
  public class ValueDTO
  {
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public int LookupID { get; set; }

    [DataMember]
    public string Value { get; set; }
  }
}
