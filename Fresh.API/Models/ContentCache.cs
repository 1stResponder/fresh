using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;

namespace Fresh.API.Models
{
  [Table("ContentCache", Schema = "em_data")]
  public class ContentCache
  {
    [Key, Column("ContentHash")]
    public int ContentHash { get; set; }
    [ForeignKey("DEHash"), Column("DEHash")]
    public int DEHash { get; set; }
    [Column("ExpiresTime")]
    public DateTime Expires { get; set; }
    [Column("ContentObject", TypeName = "xml")]
    public string Content { get; set; }
    [NotMapped]
    public XElement Content_XML
    {
      get { return XElement.Parse(Content); }
      set { Content = value.ToString(); }
    }
    [Column("FeedHashes")]
    public int[] FeedHashes { get; set; }
  }
}