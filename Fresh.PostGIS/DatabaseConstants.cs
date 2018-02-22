using Fresh.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// File:     DatabaseConstants.cs
/// Project:  Fresh.PostGIS
/// Purpose:  This file contains helper classes which define that names of PostGIS
///           tables and columns.
/// Created:  2016-03-10
/// Author:   Brian Wilkins - ArdentMC
/// 
/// Updates:  none
/// </summary>
namespace Fresh.PostGIS
{
  /// <summary>
  /// Class:    TableNames
  /// Project:  Fresh.PostGIS
  /// Purpose:  This static class defines the PostGIS table names.
  /// Created:  2016-03-16
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  static public class TableNames
  {
    static public string DE = "edxlcache";
    static public string Content = "contentcache";
    static public string FeedContent = "feedcontent";
    static public string Feeds = "feeds";
    static public string SourceValues = "sourcevalues";
    static public string Rules = "rules";
    public static string MessageArchive = "messagearch";
  }

  /// <summary>
  /// Class:    DEColumns
  /// Project:  Fresh.PostGIS
  /// Purpose:  This static class defines the column names of the DE table.
  /// Created:  2016-03-16
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  static public class DEColumns
  {
    static public string DELookupID = "dehash";
    static public string DistributionID = "distributionid";
    static public string SenderID = "senderid";
    static public string DateTimeSent = "datetimesent";
    static public string DEv1_0 = "edxlde";
    static public string Delete = "delete";
  }

  /// <summary>
  /// Class:    DEColumns
  /// Project:  Fresh.PostGIS
  /// Purpose:  This static class defines the column names of the Content table.
  /// Created:  2016-03-16
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  static public class ContentColumns
  {
    static public string ContentLookupID = "contenthash";
    static public string DELookupID = "dehash";
    static public string ExpiresTime = "expirestime";
    static public string ContentObject = "contentobject";
    static public string FeedLookupIDs = "feedhashes";
  }

  /// <summary>
  /// Class:    DEColumns
  /// Project:  Fresh.PostGIS
  /// Purpose:  This static class defines the column names of the FeedContent table.
  /// Created:  2016-03-16
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  static public class FeedContentColumns
  {
    static public string ContentLookupID = "contenthash";
    static public string ExpiresTime = "expirestime";
    static public string FeedGeo = "feedgeo";
    static public string Description = "description";
    static public string FriendlyName = "friendlyname";
    static public string Title = "title";
    static public string IconURL = "iconurl";
    static public string ImageURL = "imageurl";
    static public string DELookupID = "dehash";
  }

  /// <summary>
  /// Class:    FeedsColumns
  /// Project:  Fresh.PostGIS
  /// Purpose:  This static class defines the column names of the Feeds table.
  /// Created:  2016-03-16
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  static public class FeedsColumns
  {
    static public string FeedLookupID = "feedhash";
    static public string ContentLookupIDs = "contenthashes";
    static public string SourceID = "sourceid";
    static public string SourceValue = "sourcevalue";
    static public string ViewName = "viewname";
  }

  /// <summary>
  /// Class:    DEColumns
  /// Project:  Fresh.PostGIS
  /// Purpose:  This static class defines the column names of the SourceValues table.
  /// Created:  2016-03-16
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  static public class SourceValueColumns
  {
    static public string sourceID = "sourceID";
    static public string SourceID = "id";
    static public string SourceValue = "value";
    static public string FeedLookupID = "feedhash";
  }

  /// <summary>
  /// Class:    DEColumns
  /// Project:  Fresh.PostGIS
  /// Purpose:  This static class defines the column names of the Rules table.
  /// Created:  2016-03-16
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  static public class RulesColumns
  {
    static public string ElementName = "deelement";
    static public string RuleLookupID = "rulehash";
    static public string RuleID = "ruleid";
    static public string RuleValue = "rulevalue";
    static public string FeedLookupIDs = "feedhashes";
    static public string FederationURI = "federationuri";
  }

  /// <summary>
  /// Class:    DEColumns
  /// Project:  Fresh.PostGIS
  /// Purpose:  This static class defines the column names of the FeedViews table.
  /// Created:  2016-08-28
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  static public class FeedViewsColumns
  {
    static public string sourceID = "sourceID";
    static public string SourceID = "sourceid";
    static public string SourceValue = "sourcevalue";
    static public string ViewName = "viewname";
  }

  /// <summary>
  /// Defines the Columns in the Message Archive Table of the archive database
  /// </summary>
  public static class MessageArchiveColumns
  {
    public static string DELookupID = "dehash";
    public static string DistributionID = "distributionid";
    public static string SenderID = "senderid";
    public static string DateTimeSent = "datetimesent";
    public static string SenderIP = "senderip";
    public static string DateTimeLogged = "datetimelogged";
    public static string DE = "message";
  }
}
