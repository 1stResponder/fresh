// <copyright file="DEUtilities.cs" company="EDXLSharp">
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>

using EMS.EDXL.DE;
using EMS.EDXL.DE.v1_0;
using EMS.NIEM.EMLC;

//using EDXLSharp.MQTTSensorThingsLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Fresh.Global.ContentHelpers;
using log4net;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using System.IO;
using System.Xml.Linq;
using System.Xml.Schema;
using Fresh.Global.Properties;
using System.Net;
using System.Net.Http;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace Fresh.Global
{
  /// <summary>
  /// A collection of utilities for working with DE Objects
  /// </summary>
  public static class DEUtilities
  {
	private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


	public enum LogLevel
	{
	  Debug,
	  Warning,
	  Error,
	  Fatal,
	  Info
	};

	public static void LogMessage(string msg, LogLevel level, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
	{
	  msg = filePath + "::" + memberName + " - " + msg;
	  switch (level)
	  {
		case LogLevel.Debug:
		  if (Log.IsDebugEnabled)
		  {
			Log.Debug(msg);
		  }
		  break;
		case LogLevel.Error:
		  if (Log.IsErrorEnabled)
		  {
			Log.Error(msg);
		  }
		  break;
		case LogLevel.Warning:
		  if (Log.IsWarnEnabled)
		  {
			Log.Warn(msg);
		  }
		  break;
		case LogLevel.Fatal:
		  if (Log.IsFatalEnabled)
		  {
			Log.Fatal(msg);
		  }
		  break;
		case LogLevel.Info:
		  if (Log.IsInfoEnabled)
		  {
			Log.Info(msg);
		  }
		  break;
		default:
		  break;
	  }
	}

	/// <summary>
	/// Log a message
	/// </summary>
	public static void LogMessage(string msg, LogLevel level, Exception ex, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
	{
	  if (ex == null)
	  {
		LogMessage(msg, level, filePath, memberName);
	  }
	  else
	  {
		msg = filePath + "::" + memberName + " - " + msg;
		switch (level)
		{
		  case LogLevel.Debug:
			if (Log.IsDebugEnabled)
			{
			  Log.Debug(msg, ex);
			}
			break;
		  case LogLevel.Error:
			if (Log.IsErrorEnabled)
			{
			  Log.Error(msg, ex);
			}
			break;
		  case LogLevel.Warning:
			if (Log.IsWarnEnabled)
			{
			  Log.Warn(msg, ex);
			}
			break;
		  case LogLevel.Fatal:
			if (Log.IsFatalEnabled)
			{
			  Log.Fatal(msg, ex);
			}
			break;
		  case LogLevel.Info:
			if (Log.IsInfoEnabled)
			{
			  Log.Info(msg, ex);
			}
			break;
		  default:
			break;
		}
	  }
	}

	/// <summary>
	/// Computes the hash value for a list of strings appended together
	/// </summary>
	/// <param name="hashStrings">List of Strings to Hash</param>
	/// <returns>Hash Value</returns>
	public static int ComputeHash(List<string> hashStrings)
	{
	  string dscString = "";
	  foreach (string hString in hashStrings)
	  {
		dscString += hString;
	  }

	  byte[] data = System.Text.Encoding.ASCII.GetBytes(dscString);
	  unchecked
	  {
		const int P = 16777619;
		int hash = (int)2166136261;

		for (int i = 0; i < data.Length; i++)
		{
		  hash = (hash ^ data[i]) * P;
		}

		hash += hash << 13;
		hash ^= hash >> 7;
		hash += hash << 3;
		hash ^= hash >> 17;
		hash += hash << 5;
		return hash;
	  }
	}

	/// <summary>
	/// Computes the hash value for a DE Object
	/// </summary>
	/// <param name="de">DE Object</param>
	/// <returns>Hash Value</returns>
	public static int ComputeDELookupID(DEv1_0 de)
	{
	  return ComputeHash(new List<string>() { de.DistributionID, de.SenderID });
	}

	/// <summary>
	/// Helper function to return the xml representation of a content object
	/// </summary>
	/// <param name="co">Content Object</param>
	/// <returns>XML String</returns>
	public static string ContentXML(ContentObject co)
	{
	  StringWriter srb = new StringWriter();
	  XmlWriterSettings xsettings = new XmlWriterSettings();
	  xsettings.OmitXmlDeclaration = true;
	  xsettings.Encoding = Encoding.UTF8;
	  XmlSerializer xs = new XmlSerializer(typeof(ContentObject));
	  xs.Serialize(srb, co);
	  return srb.ToString();
	}

	/// <summary>
	/// Computes the lookup value for a Content Object
	/// </summary>
	/// <param name="de">DE Object</param>
	/// <param name="co">Content Object</param>
	/// <returns>Lookup Value</returns>
	public static int ComputeContentLookupID(DEv1_0 de, ContentObject co)
	{
	  return ComputeHash(new List<string>() { de.DistributionID, de.SenderID, co.ContentDescription });
	}

	/// <summary>
	/// Creates a hash out of a ValueList Urn and Value
	/// </summary>
	/// <param name="valListUrn">ValueList Urn</param>
	/// <param name="valListVal">ValueList Value</param>
	/// <returns>Hash Value</returns>
	public static int CreateValueListHash(string valListUrn, string valListVal)
	{
	  return ComputeHash(new List<string>() { valListUrn, valListVal });
	}

	/// <summary>
	/// Determines the overall expiration time of a DE. This looks at the ContentObjects' ExpiresTimes.
	/// If none of them are set, 1 day from the DE time sent is used.
	/// </summary>
	/// <param name="de">The DE</param>
	/// <param name="co">Content Object</param>
	/// <returns>The expiration time.</returns>
	public static DateTime GetContentObjectExpiresTime(DEv1_0 de, ContentObject co)
	{
	  DateTime coexpires;

	  if (co.ExpiresTime.HasValue)
	  {
		coexpires = co.ExpiresTime.Value;
	  }
	  else
	  {
		coexpires = de.DateTimeSent.AddDays(1.0);
	  }

	  return coexpires.ToUniversalTime();
	}

	public static DEv1_0 DeserializeDE(string xmlString)
	{
	  StringReader sr = new StringReader(xmlString);
	  XmlSerializer xs = new XmlSerializer(typeof(DEv1_0));
	  return xs.Deserialize(sr) as DEv1_0;
	}

	/// <summary>
	/// Determines the overall expiration time of a DE. This looks at the ContentObjects' ExpiresTimes.
	/// If none of them are set, 1 day from the DE time sent is used.
	/// </summary>
	/// <param name="de">The DE</param>
	/// <returns>The expiration time.</returns>
	public static DateTime GetDEExpiresTime(DEv1_0 de)
	{
	  if (de.ExpiresDateTime.HasValue)
	  {
		return de.ExpiresDateTime.Value.ToUniversalTime();
	  }
	  else
	  {
		return de.DateTimeSent.AddDays(1).ToUniversalTime();
	  }
	}

	/// <summary>
	/// Will load the referenced schema files and use them to validate the given NIEM xml message.  Returns null
	/// if an error occurs.  
	/// </summary>
	/// <param name="xmldata">NIEM message as xml</param>
	/// <param name="errorString">The error list</param>
	/// <returns>True if the message is valid, false otherwise</returns>
	/// <exception cref="IOException">Their was a problem loading the schema files</exception>
	/// <exception cref="FormatException">The schema files could not be parsed</exception>
	public static bool ValidateNiemSchema(string xmldata, out List<string> errorString)
	{
	  /* 
		* NOTE: When flattening the schema files XMLSpy it will add an "xs:appinfo" element
		* to some of the files.  If this element contains a child element "term:" then the
		* appinfo element must be removed.  Otherwise, visual studio cannot
		* load the schema file properly.  
		*/

	  #region Initialize Readers and Error list

	  errorString = null;
	  XmlReader vr = null;
	  XmlReaderSettings xs = new XmlReaderSettings();
	  XmlSchemaSet coll = new XmlSchemaSet();
	  StringReader xsdsr = null;
	  StringReader xmlsr = new StringReader(xmldata);
	  XmlReader xsdread = null;
	  #endregion

	  try
	  {
		#region Load Schema Files
		string currentFile = ""; // For Error logging, holds schema file being added 

		try
		{
		  currentFile = "temporalObjects";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.temporalObjects);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.TEMPORALOBJECTS, xsdread);


		  currentFile = "xs";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.xs);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.XS, xsdread);


		  currentFile = "emlc";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.emlc);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.EMLC, xsdread);


		  currentFile = "fips_10_4";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.fips_10_4);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.FIPS_10_4, xsdread);


		  currentFile = "census_uscounty";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.census_uscounty);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.CENSUS_USCOUNTY, xsdread);


		  currentFile = "mo";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.mo);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.MO, xsdread);


		  currentFile = "em_base";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.em_base);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.EM_BASE, xsdread);


		  currentFile = "fema_rtlt1";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.fema_rtlt1);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.FEMA_RTLT1, xsdread);


		  currentFile = "maid";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.maid);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.MAID, xsdread);


		  currentFile = "emcl";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.emcl);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.EMCL, xsdread);


		  currentFile = "unece_rec20_misc";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.unece_rec20_misc);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.UNECE_REC20_MISC, xsdread);


		  currentFile = "geospatial";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.geospatial);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.GEOSPATIAL, xsdread);


		  currentFile = "fips_5_2";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.fips_5_2);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.FIPS_5_2, xsdread);


		  currentFile = "spatialReferencing";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.spatialReferencing);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.SPATIALREFERENCING, xsdread);


		  currentFile = "core_misc";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.core_misc);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.CORE_MISC, xsdread);


		  currentFile = "niem_core";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.niem_core);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.NIEM_CORE, xsdread);


		  currentFile = "geometry";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.geometry);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.GEOMETRY, xsdread);


		  currentFile = "ols";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.ols);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.OLS, xsdread);


		  currentFile = "structures";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.structures);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.STRUCTURES, xsdread);


		  currentFile = "basicTypes";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.basicTypes);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.BASICTYPES, xsdread);


		  currentFile = "fema_rtlt";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.fema_rtlt);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.FEMA_RTLT, xsdread);


		  currentFile = "xlinks";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.xlinks);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.XLINKS, xsdread);


		  currentFile = "referenceSystems";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.referenceSystems);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.REFERENCESYSTEMS, xsdread);


		  currentFile = "dataQuality";
		  xsdsr = new StringReader(Fresh.Global.Properties.Resources.dataQuality);
		  xmlsr = new StringReader(xmldata);
		  xsdread = XmlReader.Create(xsdsr);
		  coll.Add(ValidationConstants.DATAQUALITY, xsdread);

		  xs.Schemas.Add(coll);
		  currentFile = "";
		}
		catch (Exception Ex)
		{
		  throw new FormatException("There was an error parsing the schema files", Ex);
		  currentFile = "";
		}

		#endregion

		// Will Hold errors found
		List<string> errors = null;

		xs.ValidationType = ValidationType.Schema;
		xs.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
		xs.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
		xs.ValidationEventHandler += new ValidationEventHandler((object sender, ValidationEventArgs args) =>
		{
		  if (args.Severity == XmlSeverityType.Error)
		  {
			if (errors == null) errors = new List<string>();

			errors.Add(args.Message);
		  }
		});

		vr = XmlReader.Create(xmlsr, xs);
		while (vr.Read())
		{
		}

		// Setting the return value
		errorString = errors;
	  }
	  catch (IOException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (FormatException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);

		throw new Exception(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), Ex);
	  }
	  finally
	  {
		// Closing 
		if (vr != null) vr.Close();
		if (xmlsr != null) xmlsr.Close();
		if (xsdread != null) xsdread.Close();
		if (xsdsr != null) xsdsr.Close();
	  }

	  return errorString == null;
	}

	/// <summary>
	/// Returns the Feed Content information for a content object
	/// </summary>
	/// <param name="de">Parent DE</param>
	/// <param name="co">Content Object</param>
	/// <returns>Feed Content Interface</returns>
	public static IFeedContent FeedContent(DEv1_0 de, ContentObject co)
	{
	  IFeedContent myContent = null;
	  XElement xmlContent = co.XMLContent.EmbeddedXMLContent[0];

	  //TODO: Fix this hack caused by fix to gml pos element name
	  string contentString = xmlContent.ToString();
	  contentString = contentString.Replace("gml:Pos", "gml:pos");
	  var document = XDocument.Parse(contentString);
	  xmlContent = document.Root;
	  string deHash = DEUtilities.ComputeDELookupID(de).ToString();

	  //TODO: Update FeedContent method to support all supported standards 
	  //content is a SensorThings Observation
	  //if (xmlContent.Name.LocalName.Equals("Observation", StringComparison.InvariantCultureIgnoreCase) &&
	  //  xmlContent.Name.NamespaceName.Equals(EDXLSharp.MQTTSensorThingsLib.Constants.STApiNamespace, StringComparison.InvariantCultureIgnoreCase) )
	  //{
	  //  XmlSerializer serializer = new XmlSerializer(typeof(Observation));
	  //  Observation observation = (Observation)serializer.Deserialize(xmlContent.CreateReader());
	  //  myContent = new STContent(observation, deHash);
	  //}
	  //content could be CoT or NIEM EMLC, need to check the namespace to know for sure
	  if (xmlContent.Name.LocalName.Equals("event", StringComparison.InvariantCultureIgnoreCase))
	  {
		//content could be CoT or NIEM EMLC, need to check the namespace to know for sure
		//if (xmlContent.Name.NamespaceName.Equals(@"urn:cot:mitre:org", StringComparison.InvariantCultureIgnoreCase))
		//{
		//  string cotString = xmlContent.ToString();
		//  //remove namespace or CoTLibrary blows up
		//  //brute force method
		//  int index, end, len;
		//  index = cotString.IndexOf("xmlns=");
		//  while (index != -1)
		//  {
		//    end = cotString.IndexOf('"', index);
		//    end++;
		//    end = cotString.IndexOf('"', end);
		//    end++;
		//    len = end - index;
		//    cotString = cotString.Remove(index, len);
		//    index = cotString.IndexOf("xmlns=");
		//  }
		//  CoT_Library.CotEvent anEvent = new CoT_Library.CotEvent(cotString);

		//  CoTContent mCC = new CoTContent(anEvent, deHash);

		//  myContent = mCC;
		//}
		if (xmlContent.Name.NamespaceName.Equals(@"http://release.niem.gov/niem/domains/emergencyManagement/3.1/emevent/0.1/emlc/", StringComparison.InvariantCultureIgnoreCase))
		{
		  XmlSerializer serializer = new XmlSerializer(typeof(Event));
		  Event emlc = (Event)serializer.Deserialize(xmlContent.CreateReader());
		  myContent = new EMLCContent(emlc, deHash);
		}
	  }

	  return myContent;

	}

	/// <summary>
	/// Returns the expiration time of a content object
	/// </summary>
	/// <param name="de">Parent DE</param>
	/// <param name="co">Content Object</param>
	/// <returns>Expiration time</returns>
	public static DateTime? GetExpirationTime(DEv1_0 de, ContentObject co)
	{

	  IFeedContent myContent = FeedContent(de, co);
	  DateTime? expires = null;

	  if (myContent != null)
	  {
		expires = myContent.Expires();
	  }

	  if (!expires.HasValue)
	  {
		expires = de.DateTimeSent.AddDays(1.0);
	  }

	  return expires;
	}

	/// <summary>
	/// Reverse Geocode lookup to get address from lat and lon
	/// </summary>
	/// <param name="lat">Latitude</param>
	/// <param name="lon">Longitude</param>
	/// <returns>Street Address</returns>
	public static string ReverseGeocodeLookup(string lat, string lon)
	{
	  string retVal = "";

	  //http://nominatim.openstreetmap.org/reverse?format=xml&lat=43.272266&lon=-70.979983&zoom=18&addressdetails=1

	  string urlParameters = "?format=xml&lat=" + lat + "&lon=" + lon + "&zoom=18&addressdetails=1";

	  HttpClient req = new HttpClient();
	  req.BaseAddress = new Uri(@"http://nominatim.openstreetmap.org/reverse");

	  string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
	  string appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

	  req.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue(appName, version));

	  HttpResponseMessage resp = req.GetAsync(urlParameters).Result;
	  if (resp.IsSuccessStatusCode)
	  {
		string result = resp.Content.ReadAsStringAsync().Result;
		//TODO: need to parse out address here
		XElement xe = XElement.Parse(result);
		XElement resultXE = xe.Element("result");

		if (resultXE != null)
		{
		  retVal = resultXE.Value;
		}

	  }
	  return retVal;
	}

	/// <summary>
	/// Vaidates the DE message.
	/// </summary>
	/// <remarks>
	/// A DE is invalid if it is null or does not include a senderID, distID, and DateTime sent.
	/// </remarks>
	/// <param name="de"></param>
	/// <returns>True if the DE is valid, False otherwise</returns>
	public static bool ValidateDE(DEv1_0 de, out string validationError)
	{
	  validationError = null;
	  bool isSuccess = false;

	  try
	  {
		if (de == null)
		{
		  throw new ArgumentNullException("The DE object was null");
		}

		if (string.IsNullOrWhiteSpace(de.DistributionID))
		{
		  validationError = "DistributionID must be included and have a valid value";
		  return isSuccess;
		}

		if (string.IsNullOrWhiteSpace(de.SenderID))
		{
		  validationError = "SenderID must be included and have a valid value";
		  return isSuccess;
		}

		try
		{
		  if (de.DateTimeSent == null)
		  {
			validationError = "DateTimeSent must be included and have a valid value";
			return isSuccess;
		  }
		}
		catch (Exception e)
		{
		  validationError = "DateTimeSent must be included and have a valid value";
		  return isSuccess;
		}

		isSuccess = true;
	  }
	  catch (ArgumentNullException e)
	  {
		validationError = "The DE could not be read.";
		return isSuccess;
	  }
	  catch (Exception e)
	  {
		validationError = "The DE was invalid.";
		return isSuccess;
	  }

	  return isSuccess;
	}

  }
}