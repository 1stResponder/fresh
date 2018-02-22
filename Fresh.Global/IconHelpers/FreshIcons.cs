using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Fresh.Global.IconHelpers
{
  [Serializable]

  [XmlRoot(ElementName="FreshIcons")]
  public class FreshIcons
  {
    [XmlArray(ElementName = "IconSets")]
    [XmlArrayItem(ElementName = "IconSet")]
    public List<IconSet> Sets { get; set; }
  }
}
