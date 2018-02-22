using Fresh.Global.ContentHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Fresh.API.Areas.DE_HTML.Models
{
  /// <summary>
  /// View model to be used to assist with passing DE details data to views
  /// </summary>
  public class DE_Details_ViewModel
  {

    public EMLCContent EventHelper { get; private set; }
    public string Address { get; private set; }

    public DateTime DateTimeSent { get; private set; }

    public DE_Details_ViewModel(EMLCContent eventHelper, string address, DateTime dateTimeSent)
    {
      EventHelper = eventHelper;
      Address = address;
      DateTimeSent = dateTimeSent;
    }
  }
}