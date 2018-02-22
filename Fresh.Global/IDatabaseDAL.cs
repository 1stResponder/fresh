// ———————————————————————–
// <copyright file="IDatabaseDAL.cs" company="EDXLSharp">
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
// ———————————————————————–

using System;
using EMS.EDXL.DE.v1_0;
using System.Collections.Generic;

namespace Fresh.Global
{
  /// <summary>
  /// The interface for a ContentCache implementation
  /// </summary>
  public interface IDatabaseDAL
  {

    #region Create Update Delete DE Methods
    /// <summary>
    /// Expires the DE in DB if it exists. Expiring sets the _ExpiresTime to now minus one second.
    /// </summary>
    /// <param name="de">The DE object to expire</param>
    bool ExpiredDE(DEv1_0 de);

    /// <summary>
    /// Adds an EDXL-DE Object to Dynamo
    /// </summary>
    /// <param name="de">The DE Object</param>
    /// <param name="body">The Serialized DE Object Sting</param>
    /// <returns>True if DB operation is successful</returns>
    bool AddedDEToCache(DEv1_0 de, string body);

    /// <summary>
    /// Adds an DE object to the database
    /// </summary>
    /// <param name="de">DE Object</param>
    /// <param name="deID">out parameter DE database ID</param>
    /// <returns>Returns success</returns>
    bool CreatedDE(DEv1_0 de, out int deID);

    /// <summary>
    /// Deletes the DE Object with the specified DE Id from DB
    /// </summary>
    /// <param name="deid">The DE ID</param>
    /// <returns>True if DB operation is successful</returns>
    bool DeletedDE(int deid);

    /// <summary>
    /// Deletes the specified DE Object from DB
    /// </summary>
    /// <param name="de">The DE</param>
    /// <returns>True if DB operation is successful</returns>
    bool DeletedDE(DEv1_0 de);

    /// <summary>
    /// Deletes all of the rule entries
    /// </summary>
    /// <param name="rowsAffected">Number of rows deleted</param>
    /// <returns>Success or failure</returns>
    bool DeletedAllRules(out int rowsAffected);

    #endregion Create Update Delete DE Methods

    #region Read Methods
    /// <summary>
    /// Reads the DE from the database
    /// </summary>
    /// <param name="deID">DE Hash ID</param>
    /// <returns>DE Object</returns>
    DEv1_0 ReadDE(int deID);

    /// <summary>
    /// Reads all XML
    /// </summary>
    /// <returns>All XML</returns>
    string ReadAllXML();

    /// <summary>
    /// Reads the currently active XML
    /// </summary>
    /// <returns>Currently active XML</returns>
    string ReadActiveXML();

    /// <summary>
    /// Reads Active XML by a role
    /// </summary>
    /// <param name="role">Role name</param>
    /// <param name="roleURI">Role URI</param>
    /// <returns>string of XML data</returns>
    string ReadActiveXMLByRole(string role, string roleURI);

    /// <summary>
    /// Reads all DELink
    /// </summary>
    /// <returns>All DELink</returns>
    string ReadAllDELink();

    /// <summary>
    /// Reads the currently active DELink
    /// </summary>
    /// <returns>Currently active DELink</returns>
    string ReadActiveDELink();

    /// <summary>
    /// Reads the expired DELink
    /// </summary>
    /// <returns>Expired DELinkL</returns>
    string ReadExpiredDELink();

    /// <summary>
    /// Reads all DELink that has been updated since the specified time
    /// </summary>
    /// <param name="since">The cutoff for updates</param>
    /// <returns>Updated KML since the specified time</returns>
    string ReadUpdatedDELink(DateTime since);

    /// <summary>
    /// Reads active DELinks
    /// </summary>
    /// <param name="role">Role value</param>
    /// <param name="roleURI">Role URI</param>
    /// <returns>string of xml data</returns>
    string ReadActiveDELinkByRole(string role, string roleURI);

    /// <summary>
    /// Reads expired DELinks by role
    /// </summary>
    /// <param name="role">Role value</param>
    /// <param name="roleURI">Role URI</param>
    /// <returns>string of xml data</returns>
    string ReadExpiredDELinkByRole(string role, string roleURI);

    /// <summary>
    /// Reads Updated DE Links since a specified time by role
    /// </summary>
    /// <param name="since">Cutoff time for updates</param>
    /// <param name="role">Role value</param>
    /// <param name="roleURI">Role URI</param>
    /// <returns>string of xml data</returns>
    string ReadUpdatedDELinkByRole(DateTime since, string role, string roleURI);

    /// <summary>
    /// Reads the XML representation of a ContentObject.
    /// </summary>
    /// <param name="contentHash">The hash of the content object to retrieve.</param>
    /// <returns>String of XML content to return to the user.</returns>
    string ReadContentObjectByContentLookupID(int contentLookupID);

    /// <summary>
    /// Reads the HTML representation of a ContentObject.
    /// </summary>
    /// <param name="contentLookupID">The hash of the content object to retrieve.</param>
    /// <returns>String of HTML content to return to the user.</returns>
    string ReadHTMLByContentLookupID(int contentLookupID);

    /// <summary>
    /// Queries the database to Read federation rule URLs for a given source id and source value
    /// </summary>
    /// <param name="id">source identifier</param>
    /// <param name="value">source value</param>
    /// <returns>List of URIs to federate to</returns>
    List<string> ReadFederationURIFromRule(string id, string value);
    #endregion Read Methods

    #region Helper Functions
    /// <summary>
    /// Returns whether not the identified DE is the latest version or not
    /// </summary>
    /// <param name="senderID">DE Sender ID</param>
    /// <param name="distributionID">DE Distribution ID</param>
    /// <param name="distributionTime">DE DateTimeSent</param>
    /// <returns>Returns if DE is latest or not</returns>
    bool IsLatestDE(string senderID, string distributionID, DateTime distributionTime);
    #endregion Helper Functions


    #region Connection Methods
    /// <summary>
    /// Opens a database connection
    /// </summary>
    /// <returns>True if connection successful</returns>
    //bool connectionOpened();

    /// <summary>
    /// Closes a database connection
    /// </summary>
    /// <returns>True if connection is closed</returns>
    //bool IsClosedConnection();
    #endregion Connection Methods
  }
}