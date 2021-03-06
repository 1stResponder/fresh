﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

/// <summary>
/// File:     RuleFedDTO.cs
/// Project:  Fresh.PostGIS
/// Purpose:  This file contains the Rule Federation Data Transfer Object class, which represents an entry
///           in the Rules table.
/// Created:  2016-03-10
/// Author:   Brian Wilkins - ArdentMC
/// 
/// Updates:  none
/// </summary>
namespace Fresh.PostGIS
{
  /// <summary>
  /// Class:    RuleFedDTO
  /// Project:  Fresh.PostGIS
  /// Purpose:  This class is a data transfer class for entries in the Rules table.
  ///           It ignores any Feedhashes and only supports Federation URI.
  /// Created:  2016-03-16
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  [DataContract]
  public class RuleFedDTO
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
    public List<string> FedURI { get; set; }

    public override string ToString()
    {
      var writer = new StringWriter();
      (new XmlSerializer(typeof(RuleFedDTO))).Serialize(writer, this);
      return writer.ToString();
    }
  }
}
