using System;

namespace Fresh.Global
{
  /// <summary>
  /// Interface representing Generic Feed Content
  /// </summary>
  public interface IFeedContent
  {
    /// <summary>
    /// Human readable description of this content
    /// Example: CA.LFD.RA15 Rescue Ambulance, Base Station A15
    /// </summary>
    /// <returns>Description</returns>
    string Description();

    /// <summary>
    /// Human readable title of this content
    /// Example: CA.LFD.RA15
    /// </summary>
    /// <returns>Title</returns>
    string Title();

    /// <summary>
    /// Friendly name of this content
    /// Example: Rescue Ambulance
    /// </summary>
    /// <returns>Friendly Name</returns>
    string FriendlyName();

    /// <summary>
    /// Location of the icon that represents this content
    /// </summary>
    /// <returns>Icon location</returns>
    string IconURL();

    /// <summary>
    /// Location of an image associated to this content
    /// </summary>
    /// <returns>Image Location</returns>
    string ImageURL();

    /// <summary>
    /// Simple WGS84 lat and lon of this content
    /// </summary>
    /// <returns>Lat/Long</returns>
    SimpleGeoLocation Location();

    /// <summary>
    /// Date and time this content is no longer valid
    /// </summary>
    /// <returns>Expiration Date + Time</returns>
    DateTime? Expires();

    /// <summary>
    /// Parent DE hash id
    /// </summary>
    /// <returns>DE Hash</returns>
    string DEHash();
  }
}
