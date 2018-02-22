using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Fresh.Global.IconHelpers
{
  /// <summary>
  /// Serializable class to represent an IconGroup structure in the IconFiles xml.
  /// </summary>
  [Serializable]
  public class IconGroup
  {
    /// <summary>
    /// The kind of group
    /// </summary>
    [XmlAttribute(AttributeName ="groupKind")]
    public string KindofGroup { get; set; }


    /// <summary>
    /// RootFolder for the list of icon filenames
    /// </summary>
    [XmlElement(ElementName="RootFolder")]
    public string RootFolder { get; set; }


    /// <summary>
    /// List of icon filenames
    /// </summary>
    [XmlArray(ElementName ="Icons")]
    [XmlArrayItem(ElementName ="Icon")]
    public List<string> Filenames { get; set; }

    // default constructor
    public IconGroup() { }

    // constructor that sets all fields
    public IconGroup(string KindofGroup, string RootFolder, List<string> Filenames)
    {
      this.KindofGroup = KindofGroup;
      this.RootFolder = RootFolder;
      this.Filenames = Filenames;
    }
  }
}
