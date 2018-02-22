using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Fresh.Global.IconHelpers
{
  [Serializable]
  public class IconSet
  {
    /// <summary>
    /// The kind of icon set
    /// </summary>
    [XmlAttribute(AttributeName = "setKind")]
    public string KindofSet { get; set; }

    [XmlArray(ElementName="IconGroups")]
    [XmlArrayItem(ElementName ="IconGroup")]
    public List<IconGroup> Groups { get; set; }
  }
}
