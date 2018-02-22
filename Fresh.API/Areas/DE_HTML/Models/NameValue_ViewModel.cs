using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Fresh.API.Areas.DE_HTML.Models
{
  
  /// <summary>
  /// Intended to be used as a basic vm for razor pages with tables that display name/value columns
  /// </summary>
  public class NameValue_ViewModel
  {
    public string Name { get; set; }
    public string Value { get; set; }

    public NameValue_ViewModel()
    { }

    public NameValue_ViewModel(string name, string value)
    {
      Name = name;
      Value = value;
    }
  }
}