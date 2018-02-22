using System;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.IO;
using System.Net.Http;
using System.Xml;
using Fresh.Global.ContentHelpers;
using Fresh.Global;
using EMS.EDXL.DE.v1_0;
using EMS.EDXL.DE;
using EMS.NIEM.EMLC;
using EMS.NIEM.Resource;
using EMS.NIEM.Incident;
using EMS.NIEM.Sensor;
using System.Web;
using System.Diagnostics;

namespace Fresh.API.Formatters
{
  /// <summary>
  /// Custom media type formatter for HTML
  /// </summary>
  public class HTMLFormatter : BufferedMediaTypeFormatter
  {

    /// <summary>
    /// Constructor
    /// </summary>
    public HTMLFormatter()
    {
      SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
    }

    /// <summary>
    /// Base method, always returns false
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public override bool CanReadType(Type type)
    {
      return false;
    }

    /// <summary>
    /// Base method, specifies what can be written
    /// </summary>
    /// <param name="type">Type to write</param>
    /// <returns>True/False</returns>
    public override bool CanWriteType(Type type)
    {
      return (type == typeof(DEv1_0));
    }

    /// <summary>
    /// Base method, writes to stream
    /// </summary>
    /// <param name="type">type of object to write</param>
    /// <param name="value">object to write</param>
    /// <param name="writeStream">stream to write to</param>
    /// <param name="content">stuff</param>
    public override void WriteToStream(Type type, object value, Stream writeStream, HttpContent content)
    {
      DEv1_0 de = value as DEv1_0;
      Event evt = null;

	  try
	  {

		if (de != null)
		{
		  ContentObject co = de.ContentObjects[0];

		  EMLCContent evtHelper = (EMLCContent)DEUtilities.FeedContent(de, co);

		  var settings = new XmlWriterSettings();
		  settings.Indent = false;
		  settings.OmitXmlDeclaration = false;

		  XmlWriter writer = XmlWriter.Create(writeStream, settings);
		  writer.WriteStartElement("html"); //html
		  writer.WriteStartElement("head"); //head
		  writer.WriteStartElement("meta");
		  writer.WriteAttributeString("charset", "UTF-8");

		  // We're returning some DE-related HTML, so set up the page to refresh every 5 seconds
		  // This is not a good way to do it long term.
		  //writer.WriteAttributeString("http-equiv", "refresh");
		  //string contentValue = $"5; URL={HttpContext.Current.Request.Url}";
		  //writer.WriteAttributeString("content", contentValue);

		  writer.WriteEndElement(); //meta
		  writer.WriteElementString("title", "TEST");
		  writer.WriteEndElement(); //head

		  writer.WriteStartElement("body"); //start body

		  writer.WriteStartElement("div"); //start div 1
		  writer.WriteAttributeString("style", "background-color:black;color:white;padding:5px;");

		  #region table
		  writer.WriteStartElement("table"); //start table
		  writer.WriteAttributeString("style", "color:white;");
		  #region row 1
		  writer.WriteStartElement("tr"); //start row

		  writer.WriteStartElement("td"); //start cell
		  writer.WriteAttributeString("style", "padding:15px;");
		  writer.WriteStartElement("img"); //start image
		  writer.WriteAttributeString("src", evtHelper.IconURL());
		  writer.WriteAttributeString("alt", evtHelper.FriendlyName());
		  writer.WriteEndElement(); //end image
		  writer.WriteEndElement(); //end cell

		  writer.WriteStartElement("td"); //start cell
		  writer.WriteElementString("h1", evtHelper.Title());
		  writer.WriteEndElement(); //end cell
		  writer.WriteEndElement(); //end row
		  #endregion row 1

		  #region row 2
		  writer.WriteStartElement("tr"); //start row
		  writer.WriteStartElement("td"); //start cell

		  writer.WriteAttributeString("colspan", "2");
		  writer.WriteValue(evtHelper.FriendlyName());

		  writer.WriteEndElement(); //end cell
		  writer.WriteEndElement(); //end row
		  #endregion row 2
		  writer.WriteEndElement(); //end table
		  #endregion table


		  writer.WriteEndElement(); //end div 1

		  // Loop through each kind of details and append HTML for each one
		  if (evtHelper.ResourceDetails != null)
		  {
			foreach (ResourceDetail rd in evtHelper.ResourceDetails)
			{
			  string sColor = "color:black";

			  writer.WriteStartElement("p"); //start paragraph
			  writer.WriteAttributeString("style", sColor);

			  if (rd.Status.PrimaryStatus == ResourcePrimaryStatusCodeList.Available)
			  {
				sColor = "color:green";
			  }
			  else if (rd.Status.PrimaryStatus == ResourcePrimaryStatusCodeList.ConditionallyAvailable)
			  {
				sColor = "color:yellow";
			  }
			  else if (rd.Status.PrimaryStatus == ResourcePrimaryStatusCodeList.NotAvailable)
			  {
				sColor = "color:red";
			  }

			  writer.WriteElementString("p", "Latitude/Longitude: " + evtHelper.Location().Latitude.ToString() + ", " + evtHelper.Location().Longitude.ToString());
			  //writer.WriteElementString("p", "Lon: " + evtHelper.Location().Latitude.ToString());
			  string addr = DEUtilities.ReverseGeocodeLookup(evtHelper.Location().Latitude.ToString(), evtHelper.Location().Longitude.ToString());

			  if (string.IsNullOrWhiteSpace(addr))
			  {
				addr = "Not Found";
			  }

			  writer.WriteElementString("p", "Address: " + addr); //+ some reverse lookup;

			  writer.WriteRaw("Primary Status: <span style=\"font-weight:bold;" + sColor + "\"> " + rd.Status.PrimaryStatus.ToString() + "</ span>" + " ");
			  writer.WriteEndElement(); //end paragraph
			}
		  }

		  if (evtHelper.IncidentDetails != null)
		  {
			foreach (IncidentDetail id in evtHelper.IncidentDetails)
			{
			  string sColor = "color:black";

			  writer.WriteStartElement("p"); //start paragraph
			  writer.WriteAttributeString("style", sColor);
			  if (id.Status.PrimaryStatus == IncidentPrimaryStatusCodeList.Active)
			  {
				sColor = "color:green";
			  }
			  else if (id.Status.PrimaryStatus == IncidentPrimaryStatusCodeList.Pending)
			  {
				sColor = "color:orange";
			  }

			  string addr = "Not Found";
			  writer.WriteElementString("p", "Latitude/Longitude: " + evtHelper.Location().Latitude.ToString() + ", " + evtHelper.Location().Longitude.ToString());

			  if (id.LocationExtension != null && id.LocationExtension.Address != null)
			  {
				addr = id.LocationExtension.Address.ToString();
			  }
			  else
			  {

				//writer.WriteElementString("p", "Lon: " + evtHelper.Location().Latitude.ToString());
				addr = DEUtilities.ReverseGeocodeLookup(evtHelper.Location().Latitude.ToString(), evtHelper.Location().Longitude.ToString());

				if (string.IsNullOrWhiteSpace(addr))
				{
				  addr = "Not Found";
				}
			  }
			  writer.WriteElementString("p", "Address: " + addr); //+ some reverse lookup;

			  writer.WriteRaw("Primary Status: <span style=\"font-weight:bold;" + sColor + "\"> " + id.Status.PrimaryStatus.ToString() + "</ span>" + " ");
			  writer.WriteEndElement(); //end paragraph
			}
		  }

		  if (evtHelper.SensorDetails != null)
		  {
			foreach (SensorDetail sensor in evtHelper.SensorDetails)
			{
			  string sColor = "color:black;";

			  writer.WriteStartElement("p"); //start paragraph
			  writer.WriteAttributeString("style", sColor);
			  writer.WriteRaw("Sensor ID: " + sensor.ID + " ");
			  writer.WriteEndElement(); //end paragraph

			  writer.WriteStartElement("p"); //start paragraph
			  writer.WriteAttributeString("style", sColor);

			  if (sensor.Status == SensorStatusCodeList.Normal)
			  {
				sColor = "color:green";
			  }
			  else if (sensor.Status == SensorStatusCodeList.LowPower)
			  {
				sColor = "color:orange";
			  }
			  else if (sensor.Status == SensorStatusCodeList.Error)
			  {
				sColor = "color:red";
			  }
			  else if (sensor.Status == SensorStatusCodeList.Sleeping)
			  {
				sColor = "color:gray";
			  }
			  writer.WriteRaw("Primary Status: <span style=\"font-weight:bold;" + sColor + "\"> " + sensor.Status.ToString() + "</ span>" + " ");
			  writer.WriteEndElement(); //end paragraph

			  //assumes at least one item of device details is present
			  if (sensor.DeviceDetails != null)
			  {
				CreateSensorDeviceInfo(writer, sensor.DeviceDetails);
			  }

			  if (sensor.PowerDetails != null)
			  {
				CreateSensorPowerInfo(writer, sensor.PowerDetails);
			  }

			  if (sensor.PhysiologicalDetails != null)
			  {
				CreateSensorPhysiologicalInfo(writer, sensor.PhysiologicalDetails);
			  }

			  if (sensor.EnvironmentalDetails != null)
			  {
				CreateSensorEnvironmentalInfo(writer, sensor.EnvironmentalDetails);
			  }

			  if (sensor.LocationDetails != null)
			  {
				CreateSensorLocationInfo(writer, sensor.LocationDetails);
			  }

			}
		  }
		  writer.WriteEndElement(); //end body

		  writer.WriteEndElement(); //end html

		  writer.Flush();

		  writer.Close();
		}
	  }
	  catch (Exception e)
	  {
		DEUtilities.LogMessage("An error occurred when trying to parse the DE", DEUtilities.LogLevel.Error);
		CreateErrorInfo(writeStream);
	  }
    }

    private void CreateSensorDeviceInfo(XmlWriter writer, DeviceDetails device)
    {
      writer.WriteStartElement("p"); //start paragraph
      #region table
      writer.WriteStartElement("table"); //start table
      writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black; width: 25%;");
      writer.WriteStartElement("tr"); //start row
      writer.WriteStartElement("th"); //start header
      writer.WriteAttributeString("colspan", "2");
      writer.WriteAttributeString("style", "text-align: left;");
      writer.WriteValue("Device Details");
      writer.WriteEndElement(); //end header
      writer.WriteEndElement(); //end row
      #region row
      if (device.ShouldSerializeManufacturerName())
      {
        writer.WriteStartElement("tr"); //start row
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black; width: 50%;");

        writer.WriteValue("Manufacturer:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");

        writer.WriteValue(device.ManufacturerName);
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      #region row
      if (device.ShouldSerializeModelNumber())
      {
        writer.WriteStartElement("tr"); //start row

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");

        writer.WriteValue("Model Number:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");

        writer.WriteValue(device.ModelNumber);
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      #region row
      if (device.ShouldSerializeSerialNumber())
      {
        writer.WriteStartElement("tr"); //start row

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");

        writer.WriteValue("Serial Number:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");

        writer.WriteValue(device.SerialNumber);
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      #region row
      if (device.ShouldSerializeHardwareRevision())
      {
        writer.WriteStartElement("tr"); //start row

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");

        writer.WriteValue("Hardware Version:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");

        writer.WriteValue(device.HardwareRevision);
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      #region row
      if (device.ShouldSerializeFirmwareRevision())
      {
        writer.WriteStartElement("tr"); //start row

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");

        writer.WriteValue("Firmware Version:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");

        writer.WriteValue(device.FirmwareRevision);
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      #region row
      if (device.ShouldSerializeSoftwareRevision())
      {
        writer.WriteStartElement("tr"); //start row

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");

        writer.WriteValue("Software Version:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");

        writer.WriteValue(device.SoftwareRevision);
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      writer.WriteEndElement(); //end table
      #endregion table
      writer.WriteEndElement(); //end paragraph
    }

    private void CreateSensorPowerInfo(XmlWriter writer, PowerDetails power)
    {
      writer.WriteStartElement("p"); //start paragraph
      #region table
      writer.WriteStartElement("table"); //start table
      writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black; width: 25%;");
      writer.WriteStartElement("tr"); //start row
      writer.WriteStartElement("th"); //start header
      writer.WriteAttributeString("style", "text-align: left;");
      writer.WriteAttributeString("colspan", "2");
      writer.WriteValue("Power Details");
      writer.WriteEndElement(); //end header
      writer.WriteEndElement(); //end row

      #region row
      if (power.ShouldSerializeBatteryLevel())
      {
        writer.WriteStartElement("tr"); //start row
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;  width: 50%;");
        writer.WriteValue("Battery Level:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        float temp = (float)power.BatteryLevel / 10;

        writer.WriteValue(temp.ToString("####0.0") + " %");

        //writer.WriteValue(power.BatteryLevel.ToString());
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      writer.WriteEndElement(); //end table
      #endregion table
      writer.WriteEndElement(); //end paragraph
    }

    private void CreateSensorPhysiologicalInfo(XmlWriter writer, PhysiologicalSensorDetails physio)
    {
      writer.WriteStartElement("p"); //start paragraph
      #region table
      writer.WriteStartElement("table"); //start table
      writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black; width: 25%;");
      writer.WriteStartElement("tr"); //start row
      writer.WriteStartElement("th"); //start header
      writer.WriteAttributeString("style", "text-align: left;");
      writer.WriteAttributeString("colspan", "2");
      writer.WriteValue("Physiological Details");
      writer.WriteEndElement(); //end header
      writer.WriteEndElement(); //end row

      #region row
      if (physio.ShouldSerializeHeartRate())
      {
        writer.WriteStartElement("tr"); //start row
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;  width: 50%;");
        writer.WriteValue("Heart Rate:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteValue(physio.HeartRate.ToString());
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      #region row
      if (physio.ShouldSerializeSkinTemperature())
      {
        writer.WriteStartElement("tr"); //start row
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteValue("Skin Temperature:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        float temp = (float)physio.SkinTemperature / 10;

        writer.WriteValue(temp.ToString("##0.0") + " °F");
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      #region row
      if (physio.ShouldSerializeRespirationRate())
      {
        writer.WriteStartElement("tr"); //start row
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteValue("Respiration Rate:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteValue(physio.RespirationRate.ToString());
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      #region row
      if (physio.ShouldSerializeSPO2())
      {
        writer.WriteStartElement("tr"); //start row
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteValue("SPO2:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        float temp = (float)physio.SPO2 / 10;

        writer.WriteValue(temp.ToString("####0.0") + " %");

        //writer.WriteValue(physio.SPO2.ToString() + " %");
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      #region row
      if (physio.ShouldSerializePSI())
      {
        writer.WriteStartElement("tr"); //start row
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteValue("Physical Stress Index:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        float temp = (float)physio.PSI / 10;

        writer.WriteValue(temp.ToString("####0.0"));
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      writer.WriteEndElement(); //end table
      #endregion table
      writer.WriteEndElement(); //end paragraph
    }

    private void CreateSensorEnvironmentalInfo(XmlWriter writer, EnvironmentalSensorDetails environ)
    {
      writer.WriteStartElement("p"); //start paragraph
      #region table
      writer.WriteStartElement("table"); //start table
      writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black; width: 25%;");
      writer.WriteStartElement("tr"); //start row
      writer.WriteStartElement("th"); //start header
      writer.WriteAttributeString("style", "text-align: left;");
      writer.WriteAttributeString("colspan", "2");
      writer.WriteValue("Environmental Details");
      writer.WriteEndElement(); //end header
      writer.WriteEndElement(); //end row

      #region row
      if (environ.ShouldSerializeTemperature())
      {
        writer.WriteStartElement("tr"); //start row
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;  width: 50%;");
        writer.WriteValue("Temperature:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        float temp = (float)environ.Temperature / 10;

        writer.WriteValue(temp.ToString("##0.0") + " °F");
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      #region row
      if (environ.ShouldSerializeHumidity())
      {
        writer.WriteStartElement("tr"); //start row
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteValue("Humidity:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        float temp = environ.Humidity / 10;

        writer.WriteValue(temp.ToString("####0.0") + " %");
        //writer.WriteValue(environ.Humidity.ToString() + " %");
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      #region row
      if (environ.ShouldSerializePressure())
      {
        writer.WriteStartElement("tr"); //start row
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteValue("Pressure:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        float temp = (float)environ.Pressure / 10;

        writer.WriteValue(temp.ToString("####0.0"));
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      writer.WriteEndElement(); //end table
      #endregion table
      writer.WriteEndElement(); //end paragraph
    }

	/// <summary>
	/// Creates the page that is shown when an error occurs
	/// </summary>
	/// <param name="writeStream">The write stream</param>
	private void CreateErrorInfo(Stream writeStream)
	{
	  var settings = new XmlWriterSettings();
	  settings.Indent = false;
	  settings.OmitXmlDeclaration = false;

	  XmlWriter writer = XmlWriter.Create(writeStream, settings);

	  writer.WriteStartElement("h1"); //start header
	  writer.WriteAttributeString("style", "text-align: left;");
	  writer.WriteValue("An error occurred when getting the DE");
	  writer.WriteEndElement(); //end header

	  writer.Flush();
	  writer.Close();
	}

	private void CreateSensorLocationInfo(XmlWriter writer, XYZLocationSensorDetails loc)
    {
      writer.WriteStartElement("p"); //start paragraph
      #region table
      writer.WriteStartElement("table"); //start table
      writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black; width: 25%;");
      writer.WriteStartElement("tr"); //start row
      writer.WriteStartElement("th"); //start header
      writer.WriteAttributeString("style", "text-align: left;");
      writer.WriteAttributeString("colspan", "2");
      writer.WriteValue("XYZ Location Details");
      writer.WriteEndElement(); //end header
      writer.WriteEndElement(); //end row

      #region row
      if (loc.ShouldSerializeXAxisAcceleration())
      {
        writer.WriteStartElement("tr"); //start row
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;  width: 50%;");
        writer.WriteValue("X Axis:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteValue(loc.XAxisAcceleration.ToString("####0.0") + " g");
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      #region row
      if (loc.ShouldSerializeYAxisAcceleration())
      {
        writer.WriteStartElement("tr"); //start row
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteValue("Y Axis:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteValue(loc.YAxisAcceleration.ToString("####0.0") + " g");
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      #region row
      if (loc.ShouldSerializeZAxisAcceleration())
      {
        writer.WriteStartElement("tr"); //start row
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteValue("Z Axis:");
        writer.WriteEndElement(); //end cell

        writer.WriteStartElement("td"); //start cell
        writer.WriteAttributeString("style", "color:black; border-collapse: collapse; border: 1px solid black;");
        writer.WriteValue(loc.ZAxisAcceleration.ToString("####0.0") + " g");
        writer.WriteEndElement(); //end cell
        writer.WriteEndElement(); //end row
      }
      #endregion row

      writer.WriteEndElement(); //end table
      #endregion table
      writer.WriteEndElement(); //end paragraph
    }
  }
}