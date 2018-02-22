using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;

namespace Fresh.API.Models
{
  [Table("EDXLCache", Schema = "em_data")]
  public class EDXLCache
  {
    [Key]
    [Column("DEHash")]
    public int DEHash { get; set; }
    [Column("DistributionID")]
    public string DistributionID { get; set; }
    [Column("SenderID")]
    public string SenderID { get; set; }
    [Column("DateTimeSent")]
    public DateTime DateTimeSent { get; set; }
    [Column("EDXLDE", TypeName = "xml")]
    public string EDXLDE { get; set; }
    [NotMapped]
    public XElement EDXLDE_XML
    {
      get { return XElement.Parse(EDXLDE); }
      set { EDXLDE = value.ToString(); }
    }
  }
}