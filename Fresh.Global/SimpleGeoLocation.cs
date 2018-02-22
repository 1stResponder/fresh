using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Global
{
  /// <summary>
  /// Simple class to hold lat and lon
  /// </summary>
  public class SimpleGeoLocation
  {
    /// <summary>
    /// Latitude
    /// </summary>
    public double Latitude;

    /// <summary>
    /// Longitude
    /// </summary>

    public double Longitude;

    /// <summary>
    /// default constructor
    /// </summary>

    public SimpleGeoLocation()
    {
      Latitude = 0.0;
      Longitude = 0.0;
    }

    /// <summary>
    /// Simple constructor
    /// </summary>
    /// <param name="Lat">Latitude</param>
    /// <param name="Lon">Longitude</param>
    public SimpleGeoLocation(double Lat, double Lon)
    {
      Latitude = Lat;
      Longitude = Lon;
    }
  }
}
