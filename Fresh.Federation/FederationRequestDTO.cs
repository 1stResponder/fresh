using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using EMS.EDXL.DE;
using EMS.EDXL.DE.v1_0;
using System.Xml.Linq;
using System.Xml;
using System.Text;

namespace Fresh.Federation
{
  /// <summary>
  /// Class:    FederationRequestDTO
  /// Project:  Fresh.Federation
  /// Purpose:  This is the Federation Request Data Transfer Object class, 
  ///           which represents a DE to be federated to the given URIs.
  ///           
  /// Created:  2016-07-13
  /// Author:   Marc Stogner
  /// 
  /// Updates:  none
  /// </summary>
  [DataContract]
  public class FederationRequestDTO
  {
    /// <summary>
    /// The DE XML information to send.
    /// </summary>
    [DataMember]
    public XElement DEXMLElement { get; set; }

    /// <summary>
    /// The Federation URIs to send the DE on to.
    /// </summary>
    [DataMember]
    public List<string> FedURIs { get; set; }

    /// <summary>
    /// Helper to return an XML representation of this DTO.
    /// </summary>
    /// <returns></returns>
    public string ToXMLString()
    {
      using (MemoryStream memoryStream = new MemoryStream())
      {
        XmlSerializer xs = new XmlSerializer(typeof(FederationRequestDTO));
        XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
        xs.Serialize(xmlTextWriter,this);
        string result = Encoding.UTF8.GetString(memoryStream.ToArray());
        return result;
      }        
    }
  }
}
