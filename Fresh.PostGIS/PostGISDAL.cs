// <copyright file="PostGISDAL.cs" company="EDXLSharp">
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
using Fresh.Global;
using Fresh.Federation;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Web;
using System.IO;
using System.Data.Common;

namespace Fresh.PostGIS
{
  /// <summary>
  /// PostGIS Implementation of the FRESH Data Abstraction Layer
  /// </summary>
  public class PostGISDAL : IDatabaseDAL
  {

	#region Private fields
	private NpgsqlConnectionStringBuilder connectionStringBuilder;
	private string schemaName;
	private string federationServiceURL;
	#endregion

	#region Constructor

	/// <summary>
	/// Default constructor, creates DB connection object
	/// </summary>
	public PostGISDAL()
	{
	  DEUtilities.LogMessage("Default ctor called...", DEUtilities.LogLevel.Error);
	}

	/// <summary>
	/// Constructor that Reads passes connection string information
	/// </summary>
	/// <param name="connectionInfo">Connection string to database</param>
	/// <param name="schemaInfo">Database schema information</param>
	public PostGISDAL(string connectionInfo, string schemaInfo, string federationServiceURL)
	{
	  // build the connection string for later usage
	  connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionInfo);
	  connectionStringBuilder.SearchPath = schemaInfo + ",public";
	  this.schemaName = schemaInfo;
	  this.federationServiceURL = federationServiceURL;

	  DEUtilities.LogMessage("Database Connection String: " + connectionStringBuilder + " FedURL: " + federationServiceURL, DEUtilities.LogLevel.Debug);
	}

	#endregion

	#region DB Helper Methods

	/// <summary>
	/// Helper method to add a new parameter to a Npgsql Command
	/// </summary>
	/// <param name="cmd">Npgsql Command</param>
	/// <param name="dbType">Npgsql Database Type</param>
	/// <param name="paramName">Parameter Name</param>
	/// <param name="paramValue">Parameter Value</param>
	private void AddParameter(NpgsqlCommand cmd, NpgsqlDbType dbType, string paramName, object paramValue)
	{
	  cmd.Parameters.Add(paramName, dbType);
	  cmd.Parameters[paramName].Value = paramValue;
	}

	/// <summary>
	/// Gets a database connection
	/// </summary>
	/// <returns>A database connection.</returns>
	/// <exception cref="NpgsqlException">Error creating the Npgsql Connection</exception>
	public NpgsqlConnection GetDatabaseConnection()
	{
	  NpgsqlConnection connection = null;

	  try
	  {
		connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
		return connection;
	  }
	  catch (Exception ex)
	  {
		throw new NpgsqlException("Error occurred when creating the database connection", ex);
	  }
	}

	/// <summary>
	/// Closes a database connection
	/// </summary>
	/// <param name="connection">The connection to close.</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="NpgsqlException">Database connection could not be closed</exception>
	public bool CloseConnection(NpgsqlConnection connection)
	{	  
	  // Closing the connection
	  try
	  {
		// If the connection is null, throw an error
		if (connection == null)
		{
		  DEUtilities.LogMessage("Attempted to close a null database connection.", DEUtilities.LogLevel.Error);
		  throw new ArgumentNullException("Attempted to close a null database connection.");
		}

		// make sure connection is not already closed
		if (connection.State == System.Data.ConnectionState.Closed)
		{
		  DEUtilities.LogMessage("Connection is already closed", DEUtilities.LogLevel.Warning);
		  return true;
		}

		connection.Close();
		return true;
	  }
	  catch (Exception exClose)
	  {
		DEUtilities.LogMessage("Connection Close Error", DEUtilities.LogLevel.Error, exClose);
		throw new NpgsqlException("Database connection could not be closed", exClose);
	  }
	}

	/// <summary>
	/// Opens a database connection
	/// </summary>
	/// <param name="connection">The connection to open</param>
	/// <param name="caller">The calling method for error reporting</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="NpgsqlException">Error occurred when opening the connection to the Database</exception>
	private bool OpenConnection(NpgsqlConnection connection, [CallerMemberName] string caller = "")
	{  
	  try
	  {

		// If the connection is null, throw an error
		if (connection == null)
		{
		  DEUtilities.LogMessage("Attempted to open a null database connection.", DEUtilities.LogLevel.Error);
		  throw new ArgumentNullException("Attempted to open a null database connection.");
		}

		// If the connection is broken, close it first
		if (connection.State == System.Data.ConnectionState.Broken)
		{
		  CloseConnection(connection);
		}

		// Check if the connection is already open
		if (connection.State != System.Data.ConnectionState.Closed)
		{
		  DEUtilities.LogMessage("Connection was already opened.", DEUtilities.LogLevel.Warning);
		  return true;
		}

		connection.Open();
	  }
	  catch (Exception exp)
	  {
		DEUtilities.LogMessage("Open Connection Error in " + caller, DEUtilities.LogLevel.Error, exp);
		throw new NpgsqlException("Error occurred when opening the connection to the database", exp);
	  }

	  // Checking that connection is open
	  if (connection.State != System.Data.ConnectionState.Open)
	  {
		throw new NpgsqlException("Could not open the connection to the Database");
	  }

	  return true;
	}

	/// <summary>
	/// Attempts to rollback an active transaction
	/// </summary>
	/// <param name="sqlTrans">Transaction</param>
	/// <returns>Success or Failure</returns>
	public bool RollBackTransaction(NpgsqlTransaction sqlTrans)
	{
	  if (sqlTrans == null)
	  {
		DEUtilities.LogMessage("No transaction to rollback", DEUtilities.LogLevel.Warning);
		return false;
	  }

	  if (sqlTrans.IsCompleted)
	  {
		DEUtilities.LogMessage("Transaction already complete", DEUtilities.LogLevel.Warning);
		return false;
	  }

	  try
	  {
		sqlTrans.Rollback();
		return true;
	  }
	  catch (InvalidOperationException Ex)
	  {
		DEUtilities.LogMessage("The transaction was already committed or rolled back OR a connection error occurred", DEUtilities.LogLevel.Warning);
		return false;
	  }
	  catch (Exception exRollback)
	  {
		DEUtilities.LogMessage("Rollback Error", DEUtilities.LogLevel.Error, exRollback);
		//throw new NpgsqlException("Error occurred during transaction rollback", exRollback);

		return false;
	  }

	}

	/// <summary>
	/// Returns qualified table name with schema
	/// </summary>
	/// <param name="tName">Table Name</param>
	/// <returns>Schema.TableName</returns>
	public string QualifiedTableName(string tableName)
	{
	  return this.schemaName + "." + tableName;
	}
	#endregion

	#region DE Controller Methods

	#region Get

	/// <summary>
	/// Returns the list of DE which match the search parameters
	/// </summary>
	/// <param name="searchParams">The search parameters.  Returns everything if null</param>
	/// <exception cref="Exception">Error occurred when attempting to read the DE</exception>
	/// <exception cref="NpgsqlException">Database connection error.</exception>
	/// <returns>List of DeLite DTOs</returns>
	public List<DELiteDTO> ReadDE_Lite(DESearchDTO searchParams)
	{
	  NpgsqlCommand command = null;
	  List<DELiteDTO> retVal = null;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		// Getting and Opening the Database Connection.
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return retVal; // currently null
		}

		retVal = new List<DELiteDTO>();

		// Creating SQL Command
		command = currentConnection.CreateCommand();

		string sColumns = DEColumns.DELookupID + "," + DEColumns.DistributionID + "," + DEColumns.SenderID + "," + DEColumns.DateTimeSent;
		string sSQL = "SELECT " + sColumns + " FROM " + QualifiedTableName(TableNames.DE);

		// Adding Search param if there are any
		if (searchParams != null)
		{
		  // Setting <TO> search param.  Defaults to Now if not included.
		  DateTime toDateTime = (searchParams.DateTimeTo.HasValue) ? searchParams.DateTimeTo.Value : DateTime.Now;

		  // Adding search params to the command
		  sSQL = sSQL + " WHERE " + DEColumns.DateTimeSent + " >= @FromDateTime AND " + DEColumns.DateTimeSent + " <= @ToDateTime";
		  command.CommandText = sSQL;
		  AddParameter(command, NpgsqlDbType.TimestampTZ, "FromDateTime", searchParams.DateTimeFrom);
		  AddParameter(command, NpgsqlDbType.TimestampTZ, "ToDateTime", toDateTime);
		}

		command.CommandText = sSQL;

		// Reading from the Database
		NpgsqlDataReader reader = command.ExecuteReader();

		while (reader.Read())
		{
		  DELiteDTO deRow = new DELiteDTO();
		  deRow.LookupID = (int)reader[DEColumns.DELookupID];
		  deRow.DistributionID = reader[DEColumns.DistributionID] as string;
		  deRow.SenderID = reader[DEColumns.SenderID] as string;
		  deRow.DateTimeSent = (DateTime)reader[DEColumns.DateTimeSent];

		  retVal.Add(deRow);
		}

		reader.Close();

	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return retVal;
	}

	/// <summary>
	/// Returns the DE from the database
	/// </summary>
	/// <param name="deID">DE Lookup ID</param>
	/// <returns>DE Object</returns>
	/// <see cref="ReadDE(int, NpgsqlCommand)"/>
	/// <exception cref="Exception">Error occurred when reading the DE</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	/// <exception cref="ArgumentException">The DE was not found</exception>
	public DEv1_0 ReadDE(int deID)
	{
	  try
	  {
		return ReadDE(deID, null);
	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	}

	/// <summary>
	/// Returns the DE from the database
	/// </summary>
	/// <param name="deID">DE Lookup ID</param>
	/// <param name="parentCommand">Parent SQL command (optional)</param>
	/// <returns>DE Object</returns>
	/// <exception cref="Exception">Error occurred when reading the DE</exception>
	/// <exception cref="NpgsqlException">Database connection error.</exception>
	/// <exception cref="ArgumentException">The DE was not found</exception>
	private DEv1_0 ReadDE(int deID, NpgsqlCommand parentCommand = null)
	{
	  DEv1_0 retVal = null;
	  bool hasParentCommand = parentCommand != null;
	  NpgsqlCommand command = parentCommand;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		// Attempting to Connect to the Database if there is no parent command
		if (!hasParentCommand)
		{
		  currentConnection = GetDatabaseConnection();

		  // If the connection could not be open, returning null
		  if (OpenConnection(currentConnection) == false)
		  {
			return retVal; // Currently null
		  }

		  command = currentConnection.CreateCommand();
		}
		else
		{
		  command.Parameters.Clear();
		}

		// Creating SQL Command
		string sSQL = "SELECT * FROM " + QualifiedTableName(TableNames.DE) + " WHERE " + DEColumns.DELookupID + " = @DELookupID";
		command.CommandText = sSQL;
		AddParameter(command, NpgsqlDbType.Integer, "DELookupID", deID);

		// Reading from DB
		NpgsqlDataReader reader = command.ExecuteReader();

		if (reader.Read())
		{
		  string deXML = (string)reader[DEColumns.DEv1_0];

		  if (!string.IsNullOrWhiteSpace(deXML))
		  {
			retVal = DEUtilities.DeserializeDE(deXML);
		  }
		}
		else
		{
		  DEUtilities.LogMessage("The DE was not found.", DEUtilities.LogLevel.Error);
		  throw new ArgumentException("The DE was not found");
		}

		reader.Close();
	  }
	  
	  catch (NpgsqlException Ex) { throw; }
	  catch (ArgumentException Ex) { throw; }
	  catch (Exception Ex)
	  {
		throw new Exception(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), Ex);
	  }
	  finally
	  {
		if (!hasParentCommand)
		{
		  CloseConnection(currentConnection);
		}
	  }
	  return retVal;
	}

	/// <summary>
	/// Searches for DEs according to the supplied search parameters.
	/// </summary>
	/// <param name="searchParams">The params to use to find the DEs</param>
	/// <returns>A list of Full DE DTOs found.</returns>
	/// <exception cref="Exception">An error occurred when searching for the DE</exception>
	/// <exception cref="NpgsqlException">Database connection error.</exception>
	public List<DEFullDTO> SearchForDEs(DESearchDTO searchParams)
	{
	  NpgsqlCommand command = null;
	  List<DEFullDTO> retVal = null;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return retVal;
		}

		retVal = new List<DEFullDTO>();

		command = currentConnection.CreateCommand();
		string sSQL = "SELECT * FROM " + QualifiedTableName(TableNames.DE);

		if (searchParams != null)
		{
		  DateTime toDateTime = DateTime.Now;  // defaults to now

		  if (searchParams.DateTimeTo.HasValue)
		  {
			toDateTime = searchParams.DateTimeTo.Value;
		  }

		  sSQL = sSQL + " WHERE " + DEColumns.DateTimeSent + " >= @FromDateTime AND " + DEColumns.DateTimeSent + " <= @ToDateTime";
		  command.CommandText = sSQL;
		  AddParameter(command, NpgsqlDbType.TimestampTZ, "FromDateTime", searchParams.DateTimeFrom);
		  AddParameter(command, NpgsqlDbType.TimestampTZ, "ToDateTime", toDateTime);
		}
		command.CommandText = sSQL;
		NpgsqlDataReader reader = command.ExecuteReader();

		while (reader.Read())
		{
		  DEFullDTO deRow = new DEFullDTO();
		  deRow.LookupID = (int)reader[DEColumns.DELookupID];
		  deRow.DistributionID = reader[DEColumns.DistributionID] as string;
		  deRow.SenderID = reader[DEColumns.SenderID] as string;
		  deRow.DateTimeSent = (DateTime)reader[DEColumns.DateTimeSent];
		  deRow.DEv1_0 = (string)reader[DEColumns.DEv1_0];
		  retVal.Add(deRow);
		}
		reader.Close();
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }
	  return retVal;
	}

	#endregion

	#region Put/Post Message

	/// <summary>
	/// Add new DE to the database
	/// </summary>
	/// <param name="de">DE Object</param>
	/// <param name="lookupId">Out - DE lookup id</param>
	/// <returns>Success</returns>
	/// <exception cref="Exception">Error occurred when creating the DE</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	/// <exception cref="ArgumentNullException">The de cannot be null</exception>
	/// <exception cref="FederationException">Error occur when federating the DE</exception>
	/// <exception cref="InvalidOperationException">DE was not valid for this operation (i.e exists already)</exception>
	public bool CreatedDE(DEv1_0 de, out int lookupId)
	{
	  bool success = false;

	  try
	  {
		success = CreatedDE(de, out lookupId, null);
	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);

		throw;
	  }

	  return success;
	}

	/// <summary>
	/// Add new DE to the database
	/// </summary>
	/// <param name="de">DE Object</param>
	/// <param name="lookupId">Out - DE lookup id</param>
	/// <param name="parentCommand">Parent SQL command (optional)</param>
	/// <returns>Success</returns>
	/// <exception cref="Exception">Error occurred when creating the DE</exception>
	/// <exception cref="NpgsqlException">Database connection error.</exception>
	/// <exception cref="ArgumentNullException">The de cannot be null</exception>
	/// <exception cref="FederationException">Error occur when federating the DE</exception>
	/// <exception cref="InvalidOperationException">DE was not valid for this operation (i.e exists already)</exception>
	private bool CreatedDE(DEv1_0 de, out int lookupId, NpgsqlCommand parentCommand = null)
	{
	  bool wasSuccessful = false;
	  lookupId = -1;

	  bool hasParentCommand = parentCommand != null;
	  NpgsqlCommand command = parentCommand;
	  NpgsqlTransaction sqlTrans = null;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		// test to see if we need to open the connection and 
		// setup the transaction
		if (!hasParentCommand)
		{
		  // no parent connection so created a new one for this work
		  currentConnection = GetDatabaseConnection();

		  if (OpenConnection(currentConnection) == false)
		  {
			return wasSuccessful;
		  }

		  sqlTrans = currentConnection.BeginTransaction();
		  command = currentConnection.CreateCommand();
		  command.Transaction = sqlTrans;
		}
		else
		{
		  command.Parameters.Clear();
		}

		if (de == null)
		{
		  DEUtilities.LogMessage("The DE cannot be null.", DEUtilities.LogLevel.Error);
		  throw new ArgumentNullException("The DE cannot be null.");
		}

		bool alreadyExists = DEExists(de, command);

		if (alreadyExists == true)
		{
		  throw new InvalidOperationException("Error creating a new DE, the DE already exists in the database.");
		}

		if (AddedDEToCache(de, "", command))
		{
		  lookupId = DEUtilities.ComputeDELookupID(de);
		}
		else
		{
		  DEUtilities.LogMessage("Error adding a DE to the cache.", DEUtilities.LogLevel.Error);
		  throw new Exception("Error adding a DE to the cache.");
		}

		if (!hasParentCommand)
		{
		  sqlTrans.Commit();
		}

		wasSuccessful = true;
	  }
	  catch (NpgsqlException Ex) { throw; }
	  catch (ArgumentException Ex) { throw; }
	  catch (InvalidOperationException Ex) { throw; }
	  catch (FederationException Ex) { throw; }
	  catch (Exception Ex)
	  {
		throw new Exception(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), Ex);
	  }
	  finally
	  {
		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		if (!hasParentCommand)
		{
		  CloseConnection(currentConnection);
		}
	  }
	  return wasSuccessful;
	}

	/// <summary>
	/// Updates the DE object in database
	/// </summary>
	/// <param name="de">DE object</param>
	/// <returns>Success of update</returns>
	/// <exception cref="NpgsqlException">Database connection error.</exception>
	/// <exception cref="Exception">Thrown if the Put fails</exception>
	/// <exception cref="InvalidOperationException">If an update, The new DE was sent before the old DE</exception>
	/// <exception cref="ArgumentNullException">The de was null</exception>
	/// <exception cref="FederationException">Error occur when federating the DE</exception>
	public bool PutDE(DEv1_0 de)
	{
	  NpgsqlTransaction sqlTrans = null;
	  NpgsqlCommand command = null;
	  NpgsqlConnection currentConnection = null;
	  bool wasSuccessful = false;

	  // Attempting to Update or create the DE
	  try
	  {

		if (de == null)
		{
		  throw new ArgumentNullException("The DE cannot be null.");
		}

		// Attempting to Connect to Database and create transaction
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		sqlTrans = currentConnection.BeginTransaction();
		command = currentConnection.CreateCommand();
		command.Transaction = sqlTrans;

		// If the DE already exists this is an update
		if (DEExists(de, command))
		{
		  bool success = UpdateDE(de, currentConnection, command);

		  if (success)
		  {
			DEUtilities.LogMessage("Updated the DE: " + de.DistributionID, DEUtilities.LogLevel.Debug);
			sqlTrans.Commit();
		  }
		  else
		  {
			return wasSuccessful;
		  }
		}
		else // if the DE does not exist we are creating the DE
		{

		  int deID;
		  bool success = CreatedDE(de, out deID, command);

		  if (success)
		  {
			DEUtilities.LogMessage("Created a new DE: " + de.DistributionID, DEUtilities.LogLevel.Debug);
			sqlTrans.Commit();
		  }
		  else
		  {
			return wasSuccessful;
		  }
		}

		wasSuccessful = true;
	  }
	  catch (NpgsqlException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (ArgumentException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (FederationException Ex)
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

		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	/// <summary>
	/// Updates the DE object in database
	/// </summary>
	/// <param name="de">DE object</param>
	/// <returns>Success of update</returns>
	/// <exception cref="Exception">Thrown if the Update Fails</exception>
	/// <exception cref="InvalidOperationException">The new DE was sent before the old DE</exception>
	/// <exception cref="ArgumentNullException">The de cannot be null</exception>
	private bool UpdateDE(DEv1_0 de, NpgsqlConnection currentConnection, NpgsqlCommand command)
	{
	  if (de == null)
	  {
		throw new ArgumentNullException("The DE cannot be null.");
	  }

	  // The new message must be more recent then the old one
	  if (!NewerDE(de, command))
	  {
		DEUtilities.LogMessage("Unable to update existing DE, supplied DE is older. DE: " + de.ToString(), DEUtilities.LogLevel.Error);
		throw new InvalidOperationException("The update cannot of occurred before the original message");
	  }

	  // Attempting to Update DE 
	  if (UpdateEDXLCache(de, command))
	  {
		bool wasContentSuccessful = true;

		// Attempting to Update Content Objects
		foreach (ContentObject co in de.ContentObjects)
		{
		  if (!UpdateContent(de, command))
		  {
			wasContentSuccessful = false;
			break;
		  }
		}

		if (wasContentSuccessful)
		{
		  DEUtilities.LogMessage("Updated the DE: " + de.DistributionID, DEUtilities.LogLevel.Debug);
		  return true;
		}
		else
		{
		  DEUtilities.LogMessage("Failed to update the Content Objects", DEUtilities.LogLevel.Error);
		  throw new Exception("Failed to update the Content Objects");
		}
	  }
	  else
	  {
		DEUtilities.LogMessage("Failed to update the DE message", DEUtilities.LogLevel.Error);
		throw new Exception("Failed to update the DE message");
	  }
	}

	/// <summary>
	/// Updates the non-key fields in the EDXL Cache table
	/// </summary>
	/// <param name="de">DE to update</param>
	/// <param name="cmd">SQL Command</param>
	/// <returns>Success or not</returns>
	/// <exception cref="ArgumentNullException">The de cannot be null</exception>
	private bool UpdateEDXLCache(DEv1_0 de, NpgsqlCommand cmd)
	{
	  bool retVal = false;

	  if (de == null)
	  {
		throw new ArgumentNullException("The DE cannot be null.");
	  }

	  //assumes calling function will wrap this in a try catch block
	  //allows the error to be thrown to the calling function
	  int iLookupID = DEUtilities.ComputeDELookupID(de);

	  cmd.CommandText = "UPDATE " + QualifiedTableName(TableNames.DE) +
		" SET " +
		DEColumns.DateTimeSent + " = @DateTimeSent, " +
		DEColumns.DEv1_0 + " = @DEv1_0, " +
		DEColumns.Delete + " = @Delete" +
		" WHERE " +
		DEColumns.DELookupID + " = @DELookupID";

	  cmd.Parameters.Clear();
	  AddParameter(cmd, NpgsqlDbType.Integer, "DELookupID", iLookupID);
	  AddParameter(cmd, NpgsqlDbType.TimestampTZ, "DateTimeSent", de.DateTimeSent);
	  AddParameter(cmd, NpgsqlDbType.Xml, "DEv1_0", de.ToString());
	  AddParameter(cmd, NpgsqlDbType.Boolean, "Delete", false);

	  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
	  int iRows = cmd.ExecuteNonQuery();

	  retVal = (1 == iRows);

	  return retVal;
	}

	/// <summary>
	/// Updates the DE's Content Objects in the ContentCache table
	/// </summary>
	/// <param name="de">Source DE</param>
	/// <param name="cmd">Parent SQL Command</param>
	/// <returns>Success or not</returns>
	/// <exception cref="ArgumentNullException">The de was null</exception>
	private bool UpdateContent(DEv1_0 de, NpgsqlCommand cmd)
	{

	  if (de == null)
	  {
		throw new ArgumentNullException("The DE cannot be null.");
	  }

	  bool retVal = false;

	  int deLookupID = DEUtilities.ComputeDELookupID(de);

	  List<int> contentLookupIDs = ReadContentLookupIDsForDE(deLookupID, cmd);

	  int coLookupID = 0;
	  //check for updated or added content
	  foreach (ContentObject co in de.ContentObjects)
	  {
		coLookupID = DEUtilities.ComputeContentLookupID(de, co);
		int[] arrFeeds = ReadFeedsFromRules(de, co, cmd.Connection).ToArray();
		DateTime? expires = DEUtilities.GetExpirationTime(de, co);
		IFeedContent myFeedContent = DEUtilities.FeedContent(de, co);

		if (contentLookupIDs.Contains(coLookupID)) //update it
		{
		  UpdateContentObjectInContentCache(deLookupID, coLookupID, expires, cmd, co, arrFeeds);
		  UpdateContentObjectInFeedContent(deLookupID, coLookupID, expires, cmd, myFeedContent);
		}
		else //add it
		{
		  AddContentObjectToContentCache(deLookupID, coLookupID, expires, cmd, co, arrFeeds);
		  AddContentObjectToFeedContent(deLookupID, coLookupID, expires, cmd, myFeedContent);
		}
		AddContentLookupIDToFeeds(coLookupID, arrFeeds, cmd);
	  }

	  //now check for removed content
	  foreach (int existingCOLookupID in contentLookupIDs)
	  {
		bool foundLookupID = false;

		foreach (ContentObject co in de.ContentObjects)
		{
		  coLookupID = DEUtilities.ComputeContentLookupID(de, co);

		  if (existingCOLookupID == coLookupID)
		  {
			foundLookupID = true;
			break;
		  }
		}

		if (!foundLookupID) //remove from ContentCache, FeedContent, and Feeds
		{
		  DeleteContentObject(existingCOLookupID, cmd);
		}
	  }
	  retVal = true;

	  return retVal;
	}

	#region Helpers

	/// <summary>
	/// Checks the existing DE's DateTimeSent field to see if the passed in DE is newer
	/// </summary>
	/// <param name="de">Newer DE?</param>
	/// <param name="cmd">Parent Command</param>
	/// <returns>True or False</returns>
	private bool NewerDE(DEv1_0 de, NpgsqlCommand cmd)
	{
	  bool retVal = false;

	  int lookupId = DEUtilities.ComputeDELookupID(de);
	  cmd.Parameters.Clear();

	  string sSQL = "SELECT " + DEColumns.DateTimeSent + " FROM " + QualifiedTableName(TableNames.DE) + " WHERE " + DEColumns.DELookupID + " = @DELookupID";
	  cmd.CommandText = sSQL;
	  AddParameter(cmd, NpgsqlDbType.Integer, "DELookupID", lookupId);
	  NpgsqlDataReader reader = cmd.ExecuteReader();

	  if (reader.Read())
	  {
		DateTime dt = (DateTime)reader[DEColumns.DateTimeSent];

		//NOTE - Need to allow for the same DE to be re-PUT more than once
		if (dt <= de.DateTimeSent)
		{
		  retVal = true;
		}
	  }
	  reader.Close();

	  return retVal;
	}

	/// <summary>
	/// Returns a list of feed lookup ids based on the rules matched by the DE Object and Content Object
	/// </summary>
	/// <param name="de">DE Object</param>
	/// <param name="co">Content Object</param>
	/// <param name="currentConnection">The parent connection to use.</param>
	/// <returns>List of Feed lookup ids</returns>
	private List<int> ReadFeedsFromRules(DEv1_0 de, ContentObject co, NpgsqlConnection currentConnection)
	{
	  List<int> feeds = new List<int>();

	  NpgsqlCommand cmd = currentConnection.CreateCommand();
	  cmd.CommandText = "SELECT * FROM " + QualifiedTableName(TableNames.Rules);
	  NpgsqlDataReader reader = cmd.ExecuteReader();

	  while (reader.Read())
	  {
		//TODO: rewrite to use element name on look up
		string deElement = (string)reader[RulesColumns.ElementName];
		string ruleID = (string)reader[RulesColumns.RuleID];
		string ruleValue = (string)reader[RulesColumns.RuleValue];
		int[] feedLookupIDs = (int[])reader[RulesColumns.FeedLookupIDs];

		foreach (EMS.EDXL.DE.ValueList vl in de.Keyword)
		{
		  if (vl.ValueListURN.Equals(ruleID, StringComparison.InvariantCultureIgnoreCase) &&
			vl.Value.Contains(ruleValue))
		  {
			if (feedLookupIDs.Length > 0)
			{
			  feeds.AddRange(feedLookupIDs);
			}
		  }
		}
	  }
	  reader.Close();

	  // remove any duplicates
	  feeds = feeds.Distinct().ToList();

	  return feeds;
	}

	/// <summary>
	/// Updates a single Content Object in the FeedContent table
	/// </summary>
	/// <param name="deLookupID">DE Foreign Key</param>
	/// <param name="coLookupID">Content Object Primary Key</param>
	/// <param name="expires">Expires Time</param>
	/// <param name="cmd">Parent SQL Command</param>
	/// <param name="myFeedContent">Content Object (IFeedContent interface)</param>
	private void UpdateContentObjectInFeedContent(int deLookupID, int coLookupID, DateTime? expires, NpgsqlCommand cmd, IFeedContent myFeedContent)
	{
	  SimpleGeoLocation location = (myFeedContent != null && myFeedContent.Location() != null) ? myFeedContent.Location() : new SimpleGeoLocation(0.0, 0.0);  //initialize to 0,0
	  string description = myFeedContent != null ? myFeedContent.Description() : "";
	  string friendly = myFeedContent != null ? myFeedContent.FriendlyName() : "";
	  string title = myFeedContent != null ? myFeedContent.Title() : "";
	  string icon = myFeedContent != null ? myFeedContent.IconURL() : "";
	  string image = myFeedContent != null ? myFeedContent.ImageURL() : "";

	  cmd.CommandText = "UPDATE " + QualifiedTableName(TableNames.FeedContent) +
		" SET " +
		FeedContentColumns.Description + " = @Description, " +
		FeedContentColumns.ExpiresTime + " = @ExpiresTime, " +
		FeedContentColumns.FeedGeo + " = ST_SetSRID(ST_MakePoint(@Longitude, @Latitude),4326), " +
		FeedContentColumns.FriendlyName + " = @FriendlyName, " +
		FeedContentColumns.IconURL + " = @IconURL, " +
		FeedContentColumns.ImageURL + " = @ImageURL, " +
		FeedContentColumns.Title + " = @Title " +
		" WHERE " +
		FeedContentColumns.ContentLookupID + "= @ContentLookupID AND " +
		FeedContentColumns.DELookupID + "= @DELookupID";

	  //DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);

	  cmd.Parameters.Clear();
	  AddParameter(cmd, NpgsqlDbType.Integer, "ContentLookupID", coLookupID);
	  AddParameter(cmd, NpgsqlDbType.Integer, "DELookupID", deLookupID);
	  AddParameter(cmd, NpgsqlDbType.TimestampTZ, "ExpiresTime", expires);
	  AddParameter(cmd, NpgsqlDbType.Double, "Longitude", location.Longitude);
	  AddParameter(cmd, NpgsqlDbType.Double, "Latitude", location.Latitude);
	  AddParameter(cmd, NpgsqlDbType.Text, "Description", description);
	  AddParameter(cmd, NpgsqlDbType.Text, "FriendlyName", friendly);
	  AddParameter(cmd, NpgsqlDbType.Text, "Title", title);
	  AddParameter(cmd, NpgsqlDbType.Text, "IconURL", icon);
	  AddParameter(cmd, NpgsqlDbType.Text, "ImageURL", image);

	  cmd.ExecuteNonQuery();
	}

	/// <summary>
	/// Updates a single Content Object in the ContentCache table
	/// </summary>
	/// <param name="deLookupID">DE Foreign Key</param>
	/// <param name="coLookupID">ContentObject Primary Key</param>
	/// <param name="expires">Expires Time</param>
	/// <param name="cmd">Parent SQL Command</param>
	/// <param name="co">Source Content Object</param>
	/// <param name="feeds">Feed Primary Keys</param>
	private void UpdateContentObjectInContentCache(int deLookupID, int coLookupID, DateTime? expires, NpgsqlCommand cmd, ContentObject co, int[] feeds)
	{
	  cmd.CommandText = "UPDATE " + QualifiedTableName(TableNames.Content) +
		" SET " +
		ContentColumns.ExpiresTime + " = @ExpiresTime, " +
		ContentColumns.ContentObject + " = @ContentObject, " +
		ContentColumns.FeedLookupIDs + " = @FeedLookupIDs" +
		" WHERE " +
		ContentColumns.ContentLookupID + "= @ContentLookupID AND " +
		ContentColumns.DELookupID + "= @DELookupID";

	  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);

	  cmd.Parameters.Clear();
	  AddParameter(cmd, NpgsqlDbType.Integer, "ContentLookupID", coLookupID);
	  AddParameter(cmd, NpgsqlDbType.Integer, "DELookupID", deLookupID);
	  AddParameter(cmd, NpgsqlDbType.TimestampTZ, "ExpiresTime", expires);
	  AddParameter(cmd, NpgsqlDbType.Xml, "ContentObject", DEUtilities.ContentXML(co));
	  AddParameter(cmd, NpgsqlDbType.Array | NpgsqlDbType.Integer, "FeedLookupIDs", feeds);

	  cmd.ExecuteNonQuery();
	}

	/// <summary>
	/// Adds a single content object to the ContentCache Table
	/// </summary>
	/// <param name="deLookupID">Foreign Key</param>
	/// <param name="coLookupID">Primary Key</param>
	/// <param name="expires">Expires Time</param>
	/// <param name="cmd">Parent SQL Command</param>
	/// <param name="co">Content Object</param>
	/// <param name="feeds">Feed Primary Key Array</param>
	private void AddContentObjectToContentCache(int deLookupID, int coLookupID, DateTime? expires, NpgsqlCommand cmd, ContentObject co, int[] feeds)
	{
	  string sTable = QualifiedTableName(TableNames.Content);
	  string sColumns = ContentColumns.ContentLookupID + "," + ContentColumns.DELookupID + "," + ContentColumns.ExpiresTime + "," +
					  ContentColumns.ContentObject + "," + ContentColumns.FeedLookupIDs;


	  cmd.CommandText = "INSERT INTO " + sTable + " (" + sColumns + ") VALUES " +
						"(@ContentLookupID, @DELookupID, @ExpiresTime, @ContentObject, @FeedLookupIDs)";

	  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);

	  cmd.Parameters.Clear();
	  AddParameter(cmd, NpgsqlDbType.Integer, "ContentLookupID", coLookupID);
	  AddParameter(cmd, NpgsqlDbType.Integer, "DELookupID", deLookupID);
	  AddParameter(cmd, NpgsqlDbType.TimestampTZ, "ExpiresTime", expires);
	  AddParameter(cmd, NpgsqlDbType.Xml, "ContentObject", DEUtilities.ContentXML(co));
	  AddParameter(cmd, NpgsqlDbType.Array | NpgsqlDbType.Integer, "FeedLookupIDs", feeds);

	  cmd.ExecuteNonQuery();
	}

	/// <summary>
	/// Add a single content object to the FeedContent table
	/// </summary>
	/// <param name="deLookupID">Foreign Key</param>
	/// <param name="coLookupID">Primary Key</param>
	/// <param name="expires">Expires Time</param>
	/// <param name="cmd">Parent SQL command</param>
	/// <param name="myFeedContent">Content Object (IFeedContent interface)</param>
	private void AddContentObjectToFeedContent(int deLookupID, int coLookupID, DateTime? expires, NpgsqlCommand cmd, IFeedContent myFeedContent)
	{
	  SimpleGeoLocation location = (myFeedContent != null && myFeedContent.Location() != null) ? myFeedContent.Location() : new SimpleGeoLocation(0.0, 0.0);  //initialize to 0,0
	  string description = myFeedContent != null ? myFeedContent.Description() : "";
	  string friendly = myFeedContent != null ? myFeedContent.FriendlyName() : "";
	  string title = myFeedContent != null ? myFeedContent.Title() : "";
	  string icon = myFeedContent != null ? myFeedContent.IconURL() : "";
	  string image = myFeedContent != null ? myFeedContent.ImageURL() : "";

	  string sTable = QualifiedTableName(TableNames.FeedContent); //qualify with schema
	  string sColumns = FeedContentColumns.ContentLookupID + "," + FeedContentColumns.ExpiresTime + "," + FeedContentColumns.FeedGeo + "," +
					  FeedContentColumns.Description + "," + FeedContentColumns.FriendlyName + "," + FeedContentColumns.Title + "," +
					  FeedContentColumns.IconURL + "," + FeedContentColumns.ImageURL + "," + FeedContentColumns.DELookupID;

	  cmd.CommandText = "INSERT INTO " + sTable + " (" + sColumns + ") VALUES " +
						"(@ContentLookupID, @ExpiresTime, ST_SetSRID(ST_MakePoint(@Longitude, @Latitude),4326), " +
						"@Description, @FriendlyName, @Title, @IconURL, @ImageURL, @DELookupID)";

	  cmd.Parameters.Clear();
	  AddParameter(cmd, NpgsqlDbType.Integer, "ContentLookupID", coLookupID);
	  AddParameter(cmd, NpgsqlDbType.Integer, "DELookupID", deLookupID);
	  AddParameter(cmd, NpgsqlDbType.TimestampTZ, "ExpiresTime", expires);
	  AddParameter(cmd, NpgsqlDbType.Double, "Longitude", location.Longitude);
	  AddParameter(cmd, NpgsqlDbType.Double, "Latitude", location.Latitude);
	  AddParameter(cmd, NpgsqlDbType.Text, "Description", description);
	  AddParameter(cmd, NpgsqlDbType.Text, "FriendlyName", friendly);
	  AddParameter(cmd, NpgsqlDbType.Text, "Title", title);
	  AddParameter(cmd, NpgsqlDbType.Text, "IconURL", icon);
	  AddParameter(cmd, NpgsqlDbType.Text, "ImageURL", image);

	  cmd.ExecuteNonQuery();
	}

	/// <summary>
	/// Update Feeds table with Content lookup id
	/// </summary>
	/// <param name="contentLookupID">Content Object Lookup ID</param>
	/// <param name="feedLookupIDs">Feed Lookup IDs</param>
	/// <param name="cmd">Transactional Command</param>
	private void AddContentLookupIDToFeeds(int contentLookupID, int[] feedLookupIDs, NpgsqlCommand cmd)
	{
	  //for each feed, update Feeds table content array
	  foreach (int feedLookupID in feedLookupIDs)
	  {
		List<int> coLookupIDs = ReadContentObjectLookupIDsForFeed(feedLookupID, cmd);
		coLookupIDs.Add(contentLookupID);
		UpdateContentObjectLookupIDsForFeed(feedLookupID, coLookupIDs, cmd);
	  }
	}

	/// <summary>
	/// Removes the ContentObject Lookup ID from the Feeds table
	/// </summary>
	/// <param name="coLookupID">ContentObject Foreign Key</param>
	/// <param name="cmd">Parent SQL Command</param>
	private void DeleteContentObjectLookupIDFromFeeds(int coLookupID, NpgsqlCommand cmd)
	{
	  List<int> feedLookupIDs = ReadFeedLookupIDsForContentObject(coLookupID, cmd);

	  //for each feed, update the Content Object Lookup Array
	  foreach (int feedLookupID in feedLookupIDs)
	  {
		List<int> coLookupIDs = ReadContentObjectLookupIDsForFeed(feedLookupID, cmd);

		coLookupIDs.RemoveAll(x => x == coLookupID);

		UpdateContentObjectLookupIDsForFeed(feedLookupID, coLookupIDs, cmd);
	  }
	}

	/// <summary>
	/// Deletes a single ContentObject from the FeedContent table
	/// </summary>
	/// <param name="coLookupID">ContentObject Primary Key</param>
	/// <param name="cmd">Parent SQL Command</param>
	private void DeleteContentObjectFromFeedContent(int coLookupID, NpgsqlCommand cmd)
	{
	  cmd.CommandText = "DELETE FROM " + QualifiedTableName(TableNames.FeedContent) +
		" WHERE " + FeedContentColumns.ContentLookupID + " = @ContentLookupID";

	  cmd.Parameters.Clear();
	  AddParameter(cmd, NpgsqlDbType.Integer, "ContentLookupID", coLookupID);

	  cmd.ExecuteNonQuery();
	}

	/// <summary>
	/// Deletes a single ContentObject from the ContentCache table
	/// </summary>
	/// <param name="coLookupID">ContentObject Primary Key</param>
	/// <param name="cmd">Parent SQL Command</param>
	private void DeleteContentObjectFromContentCache(int coLookupID, NpgsqlCommand cmd)
	{
	  cmd.CommandText = "DELETE FROM " + QualifiedTableName(TableNames.Content) +
		" WHERE " + ContentColumns.ContentLookupID + " = @ContentLookupID";

	  cmd.Parameters.Clear();
	  AddParameter(cmd, NpgsqlDbType.Integer, "ContentLookupID", coLookupID);

	  cmd.ExecuteNonQuery();
	}

	/// <summary>
	/// Retrieves the Feed primary keys for a given ContentObject from the ContentCache table
	/// </summary>
	/// <param name="coLookupID">ContentObject Primary Key</param>
	/// <param name="cmd">Parent SQL Command</param>
	/// <returns>Feed Primary Keys</returns>
	private List<int> ReadFeedLookupIDsForContentObject(int coLookupID, NpgsqlCommand cmd)
	{
	  List<int> retVals = new List<int>();

	  cmd.CommandText = "SELECT " + ContentColumns.FeedLookupIDs +
		" FROM " + QualifiedTableName(TableNames.Content) +
		" WHERE " + ContentColumns.ContentLookupID + "= @COLookupID";

	  cmd.Parameters.Clear();
	  AddParameter(cmd, NpgsqlDbType.Integer, "COLookupID", coLookupID);

	  NpgsqlDataReader reader = cmd.ExecuteReader();

	  if (reader.Read())
	  {
		int[] feedLookupIDs = (int[])reader[ContentColumns.FeedLookupIDs];
		retVals = feedLookupIDs.ToList();
	  }
	  reader.Close();

	  return retVals;
	}

	/// <summary>
	/// Retrieves the ContentObject primary keys for a given Feed from the Feeds table
	/// </summary>
	/// <param name="feedLookupID">Feed Primary Key</param>
	/// <param name="cmd">Parent SQL Command</param>
	/// <returns>ContentObject Primary Keys</returns>
	private List<int> ReadContentObjectLookupIDsForFeed(int feedLookupID, NpgsqlCommand cmd)
	{
	  List<int> retVals = new List<int>();

	  cmd.CommandText = "SELECT " + FeedsColumns.ContentLookupIDs +
		" FROM " + QualifiedTableName(TableNames.Feeds) +
		" WHERE " + FeedsColumns.FeedLookupID + "= @FeedLookupID";

	  cmd.Parameters.Clear();
	  AddParameter(cmd, NpgsqlDbType.Integer, "FeedLookupID", feedLookupID);

	  NpgsqlDataReader reader = cmd.ExecuteReader();

	  if (reader.Read())
	  {
		int[] coLookupIDs = (int[])reader[FeedsColumns.ContentLookupIDs];
		retVals = coLookupIDs.ToList();
	  }
	  reader.Close();

	  return retVals;
	}

	/// <summary>
	/// Adds a DE message to the database
	/// </summary>
	/// <param name="de">DE message to add</param>
	/// <param name="body">String of the DE message</param>
	/// <param name="parentCommand">Parent SQL command (optional)</param>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	/// <exception cref="FederationException">Error occurred when federating the DE.</exception>
	/// <returns>Success or Failure</returns>
	private bool AddedDEToCache(DEv1_0 de, string body, NpgsqlCommand parentCommand = null)
	{
	  bool wasSuccessful = false;
	  bool hasParentCommand = parentCommand != null;
	  NpgsqlCommand command = parentCommand;
	  NpgsqlTransaction sqlTrans = null;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		// test to see if we need to open the connection and setup the transaction
		if (!hasParentCommand)
		{
		  currentConnection = GetDatabaseConnection();
		  if (OpenConnection(currentConnection) == false)
		  {
			return wasSuccessful;
		  }
		  sqlTrans = currentConnection.BeginTransaction();
		  command = currentConnection.CreateCommand();
		  command.Transaction = sqlTrans;
		}
		else
		{
		  command.Parameters.Clear();
		}

		//add DE first
		AddDEtoEDXLCache(de, command);

		//add ContentCache next
		//don't forRead to calculate the Expires time - either from DE or Content...content trumps DE, use DE Sent + 24h if no content location
		AddContentObjectsToContentTables(de, command);

		//add FeedContent next
		//don't forRead to determine geolocation - either fromDe DE or Content...content trumps DE, use DE if no content location
		//CreateFeedContent(de, command);

		if (!hasParentCommand)
		{
		  sqlTrans.Commit();
		}

		wasSuccessful = true;
	  }
	  catch (Exception Ex)
	  {
		wasSuccessful = false;
		throw;
	  }
	  finally
	  {
		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		if (!hasParentCommand)
		{
		  CloseConnection(currentConnection);
		}
	  }

	  try
	  {
		DEUtilities.LogMessage("Adding DE to Federation Queue", DEUtilities.LogLevel.Info);
		AddContentToFederationQueue(de, command);
	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage("Error occurred when adding the De to the Federation Queue", DEUtilities.LogLevel.Error, Ex);
		throw new FederationException("Error occurred when adding the De to the Federation Queue", Ex);
	  }

	  return wasSuccessful;
	}

	/// <summary>
	/// Adds the DE object to the database
	/// </summary>
	/// <param name="de">DE Object</param>
	/// <param name="cmd">NPGSQL Command</param>
	private void AddDEtoEDXLCache(DEv1_0 de, NpgsqlCommand cmd)
	{
	  //assumes calling function will wrap this in a try catch block
	  //allows the error to be thrown to the calling function
	  int deLookupID = DEUtilities.ComputeDELookupID(de);

	  string sTable = QualifiedTableName(TableNames.DE);
	  string sColumns = DEColumns.DELookupID + "," + DEColumns.DistributionID + "," + DEColumns.SenderID + "," +
						DEColumns.DateTimeSent + "," + DEColumns.DEv1_0 + "," + DEColumns.Delete;

	  cmd.CommandText = "INSERT INTO " + sTable + " (" + sColumns + ") VALUES (@DELookupID, @DistributionID, @SenderID, @DateTimeSent, @DEv1_0, 'false')";

	  cmd.Parameters.Clear();
	  AddParameter(cmd, NpgsqlDbType.Integer, "DELookupID", deLookupID);
	  AddParameter(cmd, NpgsqlDbType.Text, "DistributionID", de.DistributionID);
	  AddParameter(cmd, NpgsqlDbType.Text, "SenderID", de.SenderID);
	  AddParameter(cmd, NpgsqlDbType.TimestampTZ, "DateTimeSent", de.DateTimeSent);
	  AddParameter(cmd, NpgsqlDbType.Xml, "DEv1_0", de.ToString());

	  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
	  cmd.ExecuteNonQuery();
	}

	/// <summary>
	/// Add Content objects to ContentCache, FeedContent, and Feeds tables
	/// </summary>
	/// <param name="de">DE Object</param>
	/// <param name="cmd">Transactional Command</param>
	private void AddContentObjectsToContentTables(DEv1_0 de, NpgsqlCommand cmd)
	{
	  //assumes calling function will wrap this in a try catch block
	  //allows the error to be thrown to the calling function
	  int deLookupID = DEUtilities.ComputeDELookupID(de);

	  foreach (ContentObject co in de.ContentObjects)
	  {
		IFeedContent myFeedContent = DEUtilities.FeedContent(de, co);
		int[] arrFeeds = ReadFeedsFromRules(de, co, cmd.Connection).ToArray();
		DateTime? expires = DEUtilities.GetExpirationTime(de, co);
		int coLookupID = DEUtilities.ComputeContentLookupID(de, co);

		AddContentObjectToContentCache(deLookupID, coLookupID, expires, cmd, co, arrFeeds);
		AddContentObjectToFeedContent(deLookupID, coLookupID, expires, cmd, myFeedContent);
		AddContentLookupIDToFeeds(coLookupID, arrFeeds, cmd);
	  }
	}

	/// <summary>
	/// Add new DE to the Federation queue.
	/// </summary>
	/// <param name="de">The DE to be federated</param>
	/// <param name="parentCommand">Parent SQL command (optional)</param>
	/// <returns>Success</returns>
	/// <exception cref="InvalidOperationException">The federation queue for this DE would contain an invalid URI</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	private bool AddContentToFederationQueue(DEv1_0 de, NpgsqlCommand parentCommand)
	{
	  bool wasSuccessful = false;
	  List<string> uris = GetDEFederationURIs(de);

	  // work to do
	  if (uris != null && uris.Count > 0)
	  {
		// If URI would point back to FRESH, do not add it
		foreach (string uriString in uris)
		{
		  try
		  {
			if (CreateFederationURI(uriString) == null)
			{
			  return wasSuccessful;
			}
		  }
		  catch (InvalidOperationException Ex) { throw; }
		  catch (Exception Ex)
		  {
			throw new InvalidOperationException("The URI was not valid", Ex);
		  }

		}

		// Create a DTO
		FederationRequestDTO dto = new FederationRequestDTO();
		dto.DEXMLElement = XElement.Parse(de.ToString());
		dto.FedURIs = uris;
		// Send it to Federation

		wasSuccessful = SendDEToFederationService(dto, this.federationServiceURL);
	  }

	  return wasSuccessful;
	}

	/// <summary>
	/// Get any Federation URIs for the given DE
	/// </summary>
	/// <param name="de">The DE to check</param>
	/// <returns>The list for URIs to Federate the DE to (if any) or null if an error occurred.</returns>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	private List<string> GetDEFederationURIs(DEv1_0 de)
	{
	  // The dictionary to hold the URIs
	  Dictionary<string, string> uriDictionary = new Dictionary<string, string>();
	  List<string> resultUris = null;

	  // Checking if there are any Wild Card matches
	  List<string> urisFound = GetWildCardFederationURI();

	  // If the list is null, something went wrong
	  if (urisFound == null)
	  {
		return resultUris;
	  }

	  // Adding each URI to the Dictionary
	  uriDictionary = urisFound.Distinct().ToDictionary(x => x, x => x);

	  if (uriDictionary.Count > 0)
	  {
		DEUtilities.LogMessage(String.Format("Found {0} wild card URIs to federate this message to.", uriDictionary.Count), DEUtilities.LogLevel.Info);
	  }

	  // Now Checking the DE itself for Rule matches 
	  foreach (ContentObject co in de.ContentObjects)
	  {
		foreach (EMS.EDXL.DE.ValueList vl in co.ContentKeyword)
		{
		  foreach (string value in vl.Value)
		  {
			// Find rules based on ValueURN/Value lookup id
			int ruleLookupID = DEUtilities.ComputeHash(new List<string> { vl.ValueListURN, value });

			urisFound = GetFederationURIsForLookupID(ruleLookupID);

			// add any URIs found to the dictionary
			if (urisFound != null)
			{
			  foreach (string uri in urisFound)
			  {
				uriDictionary[uri] = uri;
			  }
			}
			else // something went wrong getting the URIs
			{
			  return resultUris;
			}
		  }  // end values loop
		} // end valueList loop 
	  } // end ContentObject loop

	  // transform to a list 
	  resultUris = new List<string>(uriDictionary.Values);
	  return resultUris;
	}

	/// <summary>
	/// Returns the list of URIs associated with a wild card rule
	/// </summary>
	/// <remarks>
	/// Wild card rules will have an id of "*" and a value of "*"
	/// </remarks>
	/// <returns>The list of URIs associated with a wild card rule.  Otherwise, returns an empty list</returns>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	private List<string> GetWildCardFederationURI()
	{
	  //-- Checking if there are any Wild Card matches
	  // A wild card rule will have an id of "*" and a value of "*"
	  int sendToAllRuleLookupID = DEUtilities.ComputeHash(new List<string> { "*", "*" });
	  return GetFederationURIsForLookupID(sendToAllRuleLookupID);
	}

	/// <summary>
	/// Checks the database to see if any rules match the supplied Rule lookup id.
	/// </summary>
	/// <param name="uriDictionary">The dictionary to add found URIS to</param>
	/// <param name="ruleLookupID">The rule lookup id to use to find the matching rules</param>
	/// <returns>Returns a list of the URIs found.  The list will be empty if there are no matches</returns>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	private List<string> GetFederationURIsForLookupID(int ruleLookupID)
	{
	  List<string> urisFound = null;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		RuleFedDTO rule = new RuleFedDTO();

		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return urisFound;
		}

		urisFound = new List<string>();
		NpgsqlDataReader reader = this.SelectThisRule(ruleLookupID, currentConnection.CreateCommand());

		if (reader.Read())
		{
		  rule = this.ReadRuleRow_Federation(reader);
		  urisFound = rule.FedURI;
		}
		else
		{
		  DEUtilities.LogMessage("No rule was found for lookupID: " + ruleLookupID, DEUtilities.LogLevel.Debug);
		}

		reader.Close();
		return urisFound;
	  }
	  catch (Exception Ex)
	  {
		throw;
	  }
	  finally
	  {
		CloseConnection(currentConnection);
	  }
	}

	/// <summary>
	/// Sends the FederationRequest to the Federation Service
	/// </summary>
	/// <param name="federationRequest">The request to send</param>
	/// <param name="federationServiceURL">The URL of the Federation Service</param>
	/// <returns>True if it was a success.</returns>
	/// <exception cref="WebException">The Post failed</exception>
	private bool SendDEToFederationService(FederationRequestDTO federationRequest, string federationServiceURL)
	{
	  string results = "";
	  bool success = true;

	  using (WebClient client = new WebClient())
	  {
		try
		{
		  client.Encoding = Encoding.UTF8;
		  client.Headers.Add("Content-Type", "text/xml");
		  string xmlString = federationRequest.ToXMLString();
		  results = client.UploadString(federationServiceURL, "POST", xmlString);
		  DEUtilities.LogMessage("Successfully sent DE to Federation Service for processing.", DEUtilities.LogLevel.Info);
		}
		catch (Exception Ex)
		{
		  DEUtilities.LogMessage("Error occurred when sending the message.", DEUtilities.LogLevel.Error, Ex);
		  throw;
		}
	  }
	  return success;
	}

	#endregion

	#endregion

	#region Put/Post Position

	/// <summary>
	/// Updates the DE object's position and DateTimeSent in the database
	/// Assumes there is only 1 content object to be updated, returns false if
	/// more than 1 is found in the DE.
	/// </summary>
	/// <exception cref="Exception">Error occurred when updating the position</exception>
	/// <exception cref="ArgumentNullException">The DEPositionDTO cannot be null</exception>
	/// <exception cref="InvalidOperationException">The positionDTO was not pointing to a valid de for this operation</exception>
	/// <exception cref="ArgumentException">The DE was not found</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	/// <param name="positionDTO">DE object position info.  This must point to a DE already in the system</param>
	/// <returns>Success of update</returns>
	public bool UpdateDEPosition(DEPositionDTO positionDTO)
	{
	  NpgsqlTransaction sqlTrans = null;
	  NpgsqlCommand command = null;
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;

	  try
	  {

		if (positionDTO == null)
		{
		  DEUtilities.LogMessage("The Position DTO cannot be null.", DEUtilities.LogLevel.Error);
		  throw new ArgumentNullException("The Position DTO cannot be null.");
		}

		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		sqlTrans = currentConnection.BeginTransaction();
		command = currentConnection.CreateCommand();
		command.Transaction = sqlTrans;

		DEv1_0 theDE = ReadDE(positionDTO.LookupID, command);

		// TODO check assumptions about required objects
		if (theDE == null)
		{
		  DEUtilities.LogMessage("Error updating the position for DE [ " + positionDTO.LookupID + " ] because" +
			  " it was not found in the database.", DEUtilities.LogLevel.Error);
		  throw new InvalidOperationException(string.Format("The De [{0}] was not found", positionDTO.LookupID));
		}
		else if (theDE.ContentObjects == null || theDE.ContentObjects.Count == 0)
		{
		  DEUtilities.LogMessage("Error updating the position for DE [ " + positionDTO.LookupID + " ] because" +
			" it DE has no ContentObject.", DEUtilities.LogLevel.Error);
		  throw new InvalidOperationException(string.Format("The De [{0}] has no ContentObject", positionDTO.LookupID));
		}
		else if (theDE.ContentObjects.Count > 1) //TODO: fix to allow for multiple content in the future
		{
		  DEUtilities.LogMessage("Error updating the position for DE [ " + positionDTO.LookupID + " ] because" +
			  " it has more than 1 ContentObjects.", DEUtilities.LogLevel.Error);
		  throw new InvalidOperationException(string.Format("The De [{0}] has more then one ContentObject", positionDTO.LookupID));
		}

		// update the date time sent.
		theDE.DateTimeSent = positionDTO.DateTimeSent;

		// need to also update the content object XML (for all the different types of Message types)
		if (!UpdateDEContentXML(theDE, positionDTO))
		{
		  DEUtilities.LogMessage("Error updating DE Content XML with new position info.", DEUtilities.LogLevel.Error);
		  throw new Exception("Error updating DE Content XML with new position info.");
		}

		// updates the database
		UpdateContentCachePosition(theDE, positionDTO, command);

		// updates the database
		UpdateDEPositionInEDXLCache(theDE, command);

		// for now delete the feed content and re-add it. 
		DeleteFeedContent(theDE, theDE.ContentObjects[0], command);
		CreateFeedContentForDE(theDE, command);

		sqlTrans.Commit();
		wasSuccessful = true;

	  }
	  catch (ArgumentException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (InvalidOperationException Ex)
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
		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	#region Helper Methods

	/// <summary>
	/// This method will handle updating the DE Content XML for the various message types
	/// that are supported (CoT and NIEM).  Assumes only 1 content object, returns false if 
	/// more than 1 is found.
	/// </summary>
	/// <param name="de"></param>
	/// <param name="positionDTO"></param>
	/// <returns>Success of the update</returns>
	/// <exception cref="ArgumentNullException">DE was null</exception>
	/// <exception cref="InvalidOperationException">The DE was invalid for this operation</exception>
	private bool UpdateDEContentXML(DEv1_0 de, DEPositionDTO positionDTO)
	{
	  // check for error conditions
	  // TODO check assumptions about required objects
	  if (de == null)
	  {
		DEUtilities.LogMessage("Error DE was null.", DEUtilities.LogLevel.Error);
		throw new ArgumentNullException("The DE cannot be null");
	  }

	  if (de.ContentObjects == null || de.ContentObjects.Count == 0)
	  {
		DEUtilities.LogMessage("Error DE has no ContentObject.", DEUtilities.LogLevel.Error);
		throw new InvalidOperationException("The DE has no content object");
	  }

	  if (de.ContentObjects.Count > 1)
	  {
		DEUtilities.LogMessage("Error DE has more than 1 ContentObject.", DEUtilities.LogLevel.Error);
		throw new InvalidOperationException("The DE has more then one content object");
	  }

	  if (de.ContentObjects[0].XMLContent == null)
	  {
		DEUtilities.LogMessage("Error DE has no XMLContent", DEUtilities.LogLevel.Error);
		throw new InvalidOperationException("The DE has no xml content");
	  }

	  if (de.ContentObjects[0].XMLContent.EmbeddedXMLContent == null || de.ContentObjects[0].XMLContent.EmbeddedXMLContent.Count == 0)
	  {
		DEUtilities.LogMessage("Error DE has no EmbeddedXMLContent", DEUtilities.LogLevel.Error);
		throw new InvalidOperationException("The DE has no EmbeddedXMLContent");
	  }
	  if (de.ContentObjects[0].XMLContent.EmbeddedXMLContent.Count > 1)
	  {
		DEUtilities.LogMessage("Error DE has more than 1 EmbeddedXMLContent", DEUtilities.LogLevel.Error);
		throw new InvalidOperationException("The DE has more then one EmbeddedXMLContent");
	  }

	  XElement xmlContent = de.ContentObjects[0].XMLContent.EmbeddedXMLContent[0];
	  string elmcnsStr = @"http://release.niem.gov/niem/domains/emergencyManagement/3.1/emevent/0.1/emlc/";
	  //content could be CoT or NIEM EMLC, need to check the namespace to know for sure
	  if (xmlContent.Name.LocalName.Equals("event", StringComparison.InvariantCultureIgnoreCase))
	  {
		XmlWriterSettings xsettings = new XmlWriterSettings();
		xsettings.Indent = true;
		xsettings.IndentChars = "\t";
		xsettings.OmitXmlDeclaration = true;

		string cotString = xmlContent.ToString();
		//content could be CoT or NIEM EMLC, need to check the namespace to know for sure
		//if (xmlContent.Name.NamespaceName.Equals(@"urn:cot:mitre:org", StringComparison.InvariantCultureIgnoreCase))
		//{
		//  //remove namespace or CoTLibrary blows up, brute force method
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

		//  // update event date times
		//  CoT_Library.CotEvent anEvent = new CoT_Library.CotEvent(cotString);

		//  // if present use it, else leave as we found it
		//  if (positionDTO.DateTimeStale.HasValue)
		//  {
		//    anEvent.Stale = positionDTO.DateTimeStale.Value;
		//  }
		//  anEvent.Start = positionDTO.DateTimeStart;

		//  // if present use it, else leave as we found it
		//  if (positionDTO.DateTimeGenerated.HasValue)
		//  {
		//    anEvent.Time = positionDTO.DateTimeGenerated.Value;
		//  }

		//  // update event position (Point)
		//  CoT_Library.CotPoint newPosition = anEvent.Point;
		//  float latitude = float.Parse(positionDTO.Latitude, System.Globalization.CultureInfo.InvariantCulture);
		//  float longitude = float.Parse(positionDTO.Longitude, System.Globalization.CultureInfo.InvariantCulture);
		//  float heightAboveEllipsoid = float.Parse(positionDTO.CylinderHeightAboveEllipsoid, System.Globalization.CultureInfo.InvariantCulture);
		//  float halfHeight = float.Parse(positionDTO.CylinderHalfHeight, System.Globalization.CultureInfo.InvariantCulture);
		//  float radius = float.Parse(positionDTO.CylinderRadius, System.Globalization.CultureInfo.InvariantCulture);

		//  newPosition.Latitude = latitude;
		//  newPosition.Longitude = longitude;
		//  newPosition.HeightAboveEllipsoid = heightAboveEllipsoid;
		//  newPosition.CircularError = radius;
		//  newPosition.LinearError = halfHeight;

		//  EDXLCoT.CoTWrapper wrapper = new CoTWrapper();
		//  wrapper.CoTEvent = anEvent;

		//  // Get the XML after the updates
		//  StringBuilder output = new StringBuilder();
		//  XmlWriter xwriter = XmlWriter.Create(output, xsettings);
		//  wrapper.WriteXML(xwriter);
		//  xwriter.Flush();
		//  xwriter.Close();
		//  string theXML = output.ToString();
		//  XElement newContent = XElement.Parse(theXML);

		//  // update the DE with the updated embedded content
		//  de.ContentObjects[0].XMLContent.EmbeddedXMLContent[0] = newContent;
		//}
		if (xmlContent.Name.NamespaceName.Equals(elmcnsStr, StringComparison.InvariantCultureIgnoreCase))
		{
		  // For now we are ignoring USNGCoordinate

		  XmlSerializer serializer = new XmlSerializer(typeof(Event));
		  Event emlc;
		  emlc = (Event)serializer.Deserialize(xmlContent.CreateReader());
		  emlc.EventMessageDateTime = positionDTO.DateTimeSent;
		  // TODO this has some serial date times as well - what to do with them?
		  emlc.EventValidityDateTimeRange.StartDate = positionDTO.DateTimeStart;

		  // if present use it, else leave as we found it
		  if (positionDTO.DateTimeStale.HasValue)
		  {
			emlc.EventValidityDateTimeRange.EndDate = positionDTO.DateTimeStale.Value;
		  }

		  emlc.EventLocation.LocationCylinder.LocationPoint.Point.Lat = double.Parse(positionDTO.Latitude, System.Globalization.CultureInfo.InvariantCulture);
		  emlc.EventLocation.LocationCylinder.LocationPoint.Point.Lon = double.Parse(positionDTO.Longitude, System.Globalization.CultureInfo.InvariantCulture);
		  emlc.EventLocation.LocationCylinder.LocationPoint.Point.Height = double.Parse(positionDTO.Elevation, System.Globalization.CultureInfo.InvariantCulture);
		  emlc.EventLocation.LocationCylinder.LocationCylinderRadiusValue = decimal.Parse(positionDTO.CylinderRadius, System.Globalization.CultureInfo.InvariantCulture);
		  emlc.EventLocation.LocationCylinder.LocationCylinderHalfHeightValue = decimal.Parse(positionDTO.CylinderHalfHeight, System.Globalization.CultureInfo.InvariantCulture);

		  // Get the XML after the updates
		  StringBuilder output = new StringBuilder();
		  XmlWriter xwriter = XmlWriter.Create(output, xsettings);
		  serializer.Serialize(xwriter, emlc);
		  xwriter.Flush();
		  xwriter.Close();
		  string theXML = output.ToString();
		  XElement newContent = XElement.Parse(theXML);

		  // update the DE with the updated embedded content
		  de.ContentObjects[0].XMLContent.EmbeddedXMLContent[0] = newContent;
		}
	  }
	  return true;
	}

	/// <summary>
	/// Update Content object in ContentCache database table with new position information.
	/// </summary>
	/// <param name="de">DE Object</param>
	/// <param name="positionDTO">DEPositionDTO Object</param>
	/// <param name="cmd">Transactional Command</param>
	private void UpdateContentCachePosition(DEv1_0 de, DEPositionDTO positionDTO, NpgsqlCommand cmd)
	{
	  //assumes calling function will wrap this in a try catch block
	  //allows the error to be thrown to the calling function
	  // assumes there is only one ContentObject to be updated
	  ContentObject co = de.ContentObjects[0];

	  string sTable = QualifiedTableName(TableNames.Content);

	  int iContentLookupID = DEUtilities.ComputeContentLookupID(de, co);
	  cmd.CommandText = "UPDATE " + sTable +
		" SET " +
		ContentColumns.ExpiresTime + " = @ExpiresTime, " +
		ContentColumns.ContentObject + " = @ContentObject" +
		" WHERE " +
		ContentColumns.DELookupID + " = @DELookupID" +
		" AND " +
		ContentColumns.ContentLookupID + " = @ContentLookupID";

	  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);

	  cmd.Parameters.Clear();
	  AddParameter(cmd, NpgsqlDbType.Integer, "ContentLookupID", iContentLookupID);
	  AddParameter(cmd, NpgsqlDbType.Integer, "DELookupID", positionDTO.LookupID);
	  if (positionDTO.DateTimeStale == null)
	  {
		AddParameter(cmd, NpgsqlDbType.TimestampTZ, "ExpiresTime", de.DateTimeSent.AddDays(1.0));
	  }
	  else
	  {
		AddParameter(cmd, NpgsqlDbType.TimestampTZ, "ExpiresTime", positionDTO.DateTimeStale);
	  }
	  AddParameter(cmd, NpgsqlDbType.Xml, "ContentObject", DEUtilities.ContentXML(co));
	  cmd.ExecuteNonQuery();
	}

	/// <summary>
	/// Updates the DE position and date time sent in the database
	/// </summary>
	/// <param name="de">DE Object</param>
	/// <param name="parentCommand">NPGSQL Command from parent to propagate the transaction.</param>
	private void UpdateDEPositionInEDXLCache(DEv1_0 de, NpgsqlCommand parentCommand)
	{
	  //assumes calling function will wrap this in a try catch block
	  //allows the error to be thrown to the calling function
	  int deLookupID = DEUtilities.ComputeDELookupID(de);

	  string sTable = QualifiedTableName(TableNames.DE);

	  // update the date time and the DEv1_0
	  string sSQL = "UPDATE " + sTable +
		" SET " +
		DEColumns.DateTimeSent + " = @DateTimeSent, " +
		DEColumns.DEv1_0 + " = @EDXLDE" +
		" WHERE " + DEColumns.DELookupID + " = @DELookupID";

	  parentCommand.CommandText = sSQL;
	  parentCommand.Parameters.Clear();
	  AddParameter(parentCommand, NpgsqlDbType.TimestampTZ, "DateTimeSent", de.DateTimeSent);
	  AddParameter(parentCommand, NpgsqlDbType.Xml, "EDXLDE", de.ToString());
	  AddParameter(parentCommand, NpgsqlDbType.Integer, "DELookupID", deLookupID);

	  DEUtilities.LogMessage(parentCommand.CommandText, DEUtilities.LogLevel.Debug);
	  parentCommand.ExecuteNonQuery();
	}

	/// <summary>
	/// Delete FeedContent object by parent ContentObject from FeedContent table
	/// </summary>
	/// <param name="de">DE Object</param>
	/// <param name="co">Content Object</param>
	/// <param name="cmd">Transactional Command</param>
	private void DeleteFeedContent(DEv1_0 de, ContentObject co, NpgsqlCommand cmd)
	{
	  //assumes calling function will wrap this in a try catch block
	  //allows the error to be thrown to the calling function
	  int iContentLookupID = DEUtilities.ComputeContentLookupID(de, co);

	  string sTable = QualifiedTableName(TableNames.FeedContent);
	  string sSQL = "DELETE FROM " + sTable + " WHERE " + FeedContentColumns.ContentLookupID + " = @ContentLookupID";

	  cmd.CommandText = sSQL;
	  AddParameter(cmd, NpgsqlDbType.Integer, "ContentLookupID", iContentLookupID);
	  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
	  cmd.ExecuteNonQuery();
	}

	/// <summary>
	/// Add content info to FeedContent table
	/// </summary>
	/// <param name="de">DE Object</param>
	/// <param name="cmd">Transaction Command</param>
	private void CreateFeedContentForDE(DEv1_0 de, NpgsqlCommand cmd)
	{
	  int deLookupID = DEUtilities.ComputeDELookupID(de);

	  foreach (ContentObject co in de.ContentObjects)
	  {
		IFeedContent myFeedContent = DEUtilities.FeedContent(de, co);
		int coLookupID = DEUtilities.ComputeContentLookupID(de, co);
		DateTime? expires = DEUtilities.GetExpirationTime(de, co);

		AddContentObjectToFeedContent(deLookupID, coLookupID, expires, cmd, myFeedContent);
	  }
	}
	#endregion

	#endregion

	#region Delete

	/// <summary>
	/// Deletes the DE object from the database
	/// </summary>
	/// <param name="deLookupId">DE Lookup ID Value</param>
	/// <returns>Success of deletion</returns>
	/// <exception cref="NpgsqlException">Database connection error</exception>
	/// <exception cref="Exception">Error occurred during delete</exception>
	public bool DeletedDE(int deLookupId)
	{
	  try
	  {
		return DeletedDE(deLookupId, null);
	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);

		throw;
	  }

	}

	/// <summary>
	/// Deletes the DE object from the database
	/// </summary>
	/// <param name="deLookupId">DE Lookup ID Value</param>
	/// <param name="parentCommand">The parent SQL command (optional)</param>
	/// <returns>Success of deletion</returns>
	/// <exception cref="NpgsqlException">Database connection error</exception>
	/// <exception cref="Exception">Error occurred during delete</exception>
	private bool DeletedDE(int deLookupId, NpgsqlCommand parentCommand = null)
	{
	  bool wasSuccessful = false;
	  bool hasParentCommand = parentCommand != null;
	  NpgsqlConnection currentConnection = null;
	  NpgsqlCommand command = parentCommand;
	  NpgsqlTransaction sqlTrans = null;

	  try
	  {
		// test to see if we need to open the connection and setup the transaction
		if (!hasParentCommand)
		{
		  currentConnection = GetDatabaseConnection();

		  if (OpenConnection(currentConnection) == false)
		  {
			return wasSuccessful;
		  }

		  sqlTrans = currentConnection.BeginTransaction();
		  command = currentConnection.CreateCommand();
		  command.Transaction = sqlTrans;
		}
		else
		{
		  command.Parameters.Clear();
		}

		List<int> contentLookupIDs = this.ReadContentLookupIDsForDE(deLookupId, command);
		DEUtilities.LogMessage("Deleting content by lookup ids in DeletedDE()", DEUtilities.LogLevel.Debug);

		foreach (int iContentLookupID in contentLookupIDs)
		{
		  //delete all of the content for the DE
		  //NOTE - this has the side affect of updating the Feeds table to remove the deleted content lookup id,
		  //which is why a simple cascading delete from the DE won't work
		  this.DeleteContentObject(iContentLookupID, command);
		}

		//now delete the DE
		command.Parameters.Clear();
		command.CommandText = "DELETE FROM " + QualifiedTableName(TableNames.DE) + " WHERE " + DEColumns.DELookupID + " = @DELookupID";
		AddParameter(command, NpgsqlDbType.Integer, "DELookupID", deLookupId);
		DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
		command.ExecuteNonQuery();

		if (!hasParentCommand)
		{
		  sqlTrans.Commit();
		}
		wasSuccessful = true;
	  }
	  catch (NpgsqlException Ex) { throw; }
	  catch (Exception Ex)
	  {
		throw new Exception(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), Ex);
	  }
	  finally
	  {
		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		if (!hasParentCommand)
		{
		  CloseConnection(currentConnection);
		}
	  }
	  return wasSuccessful;
	}

	/// <summary>
	/// Deletes of the DE messages in the database
	/// </summary>
	/// <param name="iRowsAffected">Number of DE messages deleted</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="NpgsqlException">Database connection error</exception>
	/// <exception cref="Exception">Error occurred during delete</exception>
	public bool DeleteAllDE(out int iRowsAffected)
	{
	  iRowsAffected = -1;
	  NpgsqlCommand cmd = null;
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  NpgsqlTransaction sqlTrans = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		sqlTrans = currentConnection.BeginTransaction();
		cmd = currentConnection.CreateCommand();
		cmd.Transaction = sqlTrans;

		//delete ContentObject lookupIDs in Feeds
		List<int> feedLookupIDs = ReadAllFeedLookupIDs(cmd);

		foreach (int feedLookupID in feedLookupIDs)
		{
		  UpdateContentObjectLookupIDsForFeed(feedLookupID, new List<int>(), cmd);
		}

		//delete FeedContent
		cmd.CommandText = "DELETE FROM " + TableNames.FeedContent;
		cmd.ExecuteNonQuery();

		//delete ContentCache
		cmd.CommandText = "DELETE FROM " + TableNames.Content;
		cmd.ExecuteNonQuery();

		//finally delete the DE cache
		cmd.CommandText = "DELETE FROM " + TableNames.DE;
		iRowsAffected = cmd.ExecuteNonQuery();

		sqlTrans.Commit();
		wasSuccessful = true;

	  }
	  catch (NpgsqlException Ex) { throw; }
	  catch (Exception Ex)
	  {
		throw new Exception(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), Ex);
	  }
	  finally
	  {
		if (!wasSuccessful)
		{
		  RollBackTransaction(sqlTrans);
		}

		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	#endregion

	#region Status

	/// <summary>
	/// Returns true if the DE is already in the database
	/// </summary>
	/// <param name="de">DE object</param>
	/// <param name="parentCommand">The parent SQL command (optional)</param>
	/// <returns>true if the DE exists</returns>
	///  <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	public bool DEExists(DEv1_0 de, NpgsqlCommand parentCommand = null)
	{
	  try
	  {
		int lookupId = DEUtilities.ComputeDELookupID(de);
		return Exists(lookupId, parentCommand);
	  }
	  catch (NpgsqlException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);

		throw new Exception(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), Ex);
	  }


	}

	#endregion

	#region Helpers

	/// <summary>
	/// Returns true if the DE is already in the database
	/// </summary>
	/// <param name="lookupId">DE Lookup ID</param>
	/// <param name="parentCommand">The parent SQL command (optional)</param>
	/// <returns>true if the DE exists</returns>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	private bool Exists(int lookupId, NpgsqlCommand parentCommand = null)
	{
	  bool hasParentCommand = parentCommand != null;
	  NpgsqlCommand command = parentCommand;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		// test to see if we need to open the connection and 
		// setup the transaction
		if (!hasParentCommand)
		{
		  currentConnection = GetDatabaseConnection();
		  if (OpenConnection(currentConnection) == false)
		  {
			return false;
		  }
		  command = currentConnection.CreateCommand();
		}
		else
		{
		  command.Parameters.Clear();
		}

		string sSQL = "SELECT " + DEColumns.DELookupID + " FROM " + QualifiedTableName(TableNames.DE) + " WHERE " + DEColumns.DELookupID + " = @DELookupID";
		command.CommandText = sSQL;
		AddParameter(command, NpgsqlDbType.Integer, "DELookupID", lookupId);
		NpgsqlDataReader reader = command.ExecuteReader();

		if (reader.Read())
		{
		  reader.Close();
		  return true;
		}
		else
		{
		  reader.Close();
		  return false;
		}

	  }
	  catch (Exception Ex)
	  {
		throw;
	  }
	  finally
	  {
		if (!hasParentCommand)
		{
		  CloseConnection(currentConnection);
		}
	  }

	}

	/// <summary>
	/// Retrieves the ContentObject primary keys for a given DE from the ContentCache table
	/// </summary>
	/// <param name="deLookupID">DE Foreign Key</param>
	/// <param name="cmd">Parent SQL Command</param>
	/// <returns>DE ContentObject Primary Keys</returns>
	private List<int> ReadContentLookupIDsForDE(int deLookupID, NpgsqlCommand cmd)
	{
	  List<int> retVals = new List<int>();

	  cmd.CommandText = "SELECT " + ContentColumns.ContentLookupID +
		" FROM " + QualifiedTableName(TableNames.Content) +
		" WHERE " + FeedContentColumns.DELookupID + "= @DELookupID";

	  cmd.Parameters.Clear();
	  AddParameter(cmd, NpgsqlDbType.Integer, "DELookupID", deLookupID);

	  NpgsqlDataReader reader = cmd.ExecuteReader();

	  if (reader.Read())
	  {
		int coLookupID = (int)reader[ContentColumns.ContentLookupID];
		retVals.Add(coLookupID);
	  }
	  reader.Close();

	  return retVals;
	}

	/// <summary>
	/// Deletes a Content Object by LookupID
	/// </summary>
	/// <param name="contentLookupID">Unique LookupID of the Content Object</param>
	/// <param name="parentCommand">SQL command to use.</param>     
	private void DeleteContentObject(int contentLookupID, NpgsqlCommand cmd)
	{
	  DeleteContentObjectLookupIDFromFeeds(contentLookupID, cmd);
	  DeleteContentObjectFromFeedContent(contentLookupID, cmd);
	  DeleteContentObjectFromContentCache(contentLookupID, cmd);
	}

	/// <summary>
	/// Updates the Content Object LookupID array in the Feeds table
	/// </summary>
	/// <param name="feedLookupID">Feed Primary Key</param>
	/// <param name="coLookupIDs">Content Object Foreign Key Array</param>
	/// <param name="cmd">Parent SQL Command</param>
	private void UpdateContentObjectLookupIDsForFeed(int feedLookupID, List<int> coLookupIDs, NpgsqlCommand cmd)
	{
	  cmd.CommandText = "UPDATE " + QualifiedTableName(TableNames.Feeds) +
		  " SET " +
		  FeedsColumns.ContentLookupIDs + " = @ContentLookupIDs" +
		  " WHERE " +
		  FeedsColumns.FeedLookupID + " = @FeedLookupID";

	  cmd.Parameters.Clear();
	  AddParameter(cmd, NpgsqlDbType.Integer, "FeedLookupID", feedLookupID);
	  AddParameter(cmd, NpgsqlDbType.Array | NpgsqlDbType.Integer, "ContentLookupIDs", coLookupIDs.ToArray());

	  cmd.ExecuteNonQuery();
	}

	/// <summary>
	/// Retrieves the Feed primary keys from the Feeds table
	/// </summary>
	/// <param name="cmd">Parent SQL Command</param>
	/// <returns>Feed Primary Keys</returns>
	private List<int> ReadAllFeedLookupIDs(NpgsqlCommand cmd)
	{
	  List<int> retVals = new List<int>();

	  cmd.CommandText = "SELECT " + FeedsColumns.FeedLookupID +
		" FROM " + QualifiedTableName(TableNames.Feeds);

	  cmd.Parameters.Clear();

	  NpgsqlDataReader reader = cmd.ExecuteReader();

	  while (reader.Read())
	  {
		int feedLookupID = (int)reader[FeedsColumns.FeedLookupID];
		retVals.Add(feedLookupID);
	  }
	  reader.Close();

	  return retVals;
	}

	#endregion

	#endregion

	#region Feeds + View Controller

	#region Get

	/// <summary>
	/// Gets all of the feed details in the database
	/// </summary>
	/// <returns>The list of FeedDTOs found</returns>
	/// <exception cref="Exception">An error occurred when getting the feeds</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	public List<FeedDTO> GetAllFeedDetails()
	{
	  List<FeedDTO> feedsFound = null;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		FeedDTO rule = new FeedDTO();

		// Getting Database Connection
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return feedsFound; // currently null
		}

		// Creating and executing command
		feedsFound = new List<FeedDTO>();
		NpgsqlCommand cmd = currentConnection.CreateCommand();

		cmd.CommandText = "SELECT * FROM " + TableNames.Feeds + " ORDER BY " + FeedsColumns.ViewName + " ASC";

		DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		NpgsqlDataReader reader = cmd.ExecuteReader();
		while (reader.Read())
		{
		  FeedDTO theFeed = new FeedDTO();
		  theFeed.ViewName = reader[FeedsColumns.ViewName] as string;
		  theFeed.LookupID = (int)reader[FeedsColumns.FeedLookupID];
		  theFeed.SourceID = reader[FeedsColumns.SourceID] as string;
		  theFeed.SourceValue = reader[FeedsColumns.SourceValue] as string;
		  feedsFound.Add(theFeed);
		}
		reader.Close();

	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return feedsFound;
	}

	/// <summary>
	/// Read the feed for the supplied feed lookupId
	/// </summary>
	/// <param name="lookupID">Feed lookupId</param>
	/// <param name="feed">[Out] The feedDTO containing the feed</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when getting the feed</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	public bool ReadFeed(int lookupID, out FeedDTO feed)
	{
	  bool wasSuccessful = false;

	  NpgsqlConnection currentConnection = null;
	  NpgsqlCommand command = null;
	  feed = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}
		command = currentConnection.CreateCommand();

		command.CommandText = "SELECT * FROM " + TableNames.Feeds + " WHERE " + FeedsColumns.FeedLookupID + " = @FeedLookupID";
		AddParameter(command, NpgsqlDbType.Integer, "FeedLookupID", lookupID);

		DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
		NpgsqlDataReader reader = command.ExecuteReader();

		if (reader.Read())
		{
		  feed = new FeedDTO();

		  feed.LookupID = (int)reader[FeedsColumns.FeedLookupID];
		  feed.ContentLookupIDs = new List<int>(reader[FeedsColumns.ContentLookupIDs] as int[]);//TODO: This might be an issue depending on what an empty array returns
		  feed.SourceID = (string)reader[FeedsColumns.SourceID];
		  feed.SourceValue = (string)reader[FeedsColumns.SourceValue];
		  feed.ViewName = (string)reader[FeedsColumns.ViewName];

		  wasSuccessful = true;
		}
		reader.Close();
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	/// <summary>
	/// Gets all of the feed view names in the db
	/// </summary>
	/// <returns>The list of view names</returns>
	/// <exception cref="Exception">An error occurred when getting the views</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	public List<string> GetFeedViewNames()
	{
	  List<string> viewsFound = null;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return viewsFound; // currently null
		}

		viewsFound = new List<string>();
		NpgsqlCommand cmd = currentConnection.CreateCommand();

		cmd.CommandText = "SELECT " + FeedsColumns.ViewName + " FROM " + TableNames.Feeds + " ORDER BY " + FeedsColumns.ViewName + " ASC";

		DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		NpgsqlDataReader reader = cmd.ExecuteReader();
		while (reader.Read())
		{
		  viewsFound.Add(reader[FeedViewsColumns.ViewName] as string);
		}
		reader.Close();

	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return viewsFound;
	}

	/// <summary>
	/// Gets the FeedDTO for the given view name
	/// </summary>
	/// <param name="viewName">The view name of the feed</param>
	/// <returns>The feedDTO for the relevant feed</returns>
	/// <exception cref="Exception">An error occurred when getting the feed</exception>
	/// <exception cref="ArgumentNullException">The view name was null</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	public FeedDTO GetFeedByViewName(string viewName)
	{
	  FeedDTO feed = null;
	  NpgsqlConnection currentConnection = null;

	  try
	  {

		if (viewName == null)
		{
		  throw new ArgumentNullException("The View name cannot be null");
		}

		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return feed; // currently null
		}

		NpgsqlCommand cmd = currentConnection.CreateCommand();

		cmd.CommandText = "SELECT * FROM " + TableNames.Feeds + " WHERE " + FeedsColumns.ViewName + " = @ViewName";
		AddParameter(cmd, NpgsqlDbType.Text, "ViewName", viewName);

		DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		NpgsqlDataReader reader = cmd.ExecuteReader();
		if (reader.Read())
		{
		  feed = new FeedDTO();
		  feed.ViewName = reader[FeedsColumns.ViewName] as string;
		  feed.LookupID = (int)reader[FeedsColumns.FeedLookupID];
		  feed.SourceID = reader[FeedsColumns.SourceID] as string;
		  feed.SourceValue = reader[FeedsColumns.SourceValue] as string;
		  feed.ContentLookupIDs = new List<int>(reader[FeedsColumns.ContentLookupIDs] as int[]);//TODO: This might be an issue depending on what an empty array returns
		}
		reader.Close();
	  }
	  catch (ArgumentNullException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return feed;
	}

	/// <summary>
	/// Gets the FeedContent data specified by the view.  If no view is specified
	/// it returns all the data in the FeedContent table.
	/// </summary>
	/// <param name="viewName">(Optional) The view to use</param>
	/// <returns>FeedContent data</returns>
	/// <exception cref="Exception">An error occurred when getting the feed</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	/// <exception cref="ArgumentException">The View table could not be found</exception>
	public List<Dictionary<string, string>> GetFeedContentDataByView(string viewName)
	{
	  List<Dictionary<string, string>> retVal = null;

	  NpgsqlCommand cmd = null;
	  NpgsqlConnection currentConnection = null;
	  NpgsqlTransaction sqlTrans = null;

	  // FeedContent  
	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return retVal;
		}

		retVal = new List<Dictionary<string, string>>();

		cmd = currentConnection.CreateCommand();
		cmd.AllResultTypesAreUnknown = true;
		sqlTrans = currentConnection.BeginTransaction();
		cmd.Transaction = sqlTrans;

		string sSQL = null;
		if (viewName == null)
		{
		  sSQL = "SELECT * FROM " + QualifiedTableName(TableNames.FeedContent);
		}
		else
		{
		  sSQL = "SELECT * FROM " + QualifiedTableName(viewName);
		}

		cmd.CommandText = sSQL;

		NpgsqlDataReader reader = null;

		try
		{
		  reader = cmd.ExecuteReader();
		}
		catch (NpgsqlException Ex)
		{
		  throw new ArgumentException("The view table: " + viewName + " was not found.");
		}

		while (reader.Read())
		{
		  Dictionary<string, string> rowData = new Dictionary<string, string>();
		  for (int i = 0; i < reader.FieldCount; i++)
		  {
			rowData.Add(reader.GetName(i), reader.GetValue(i).ToString());
		  }
		  retVal.Add(rowData);
		}

		reader.Close();
	  }
	  catch (ArgumentNullException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (ArgumentException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return retVal;
	}

	#endregion

	#region Post/Put

	/// <summary>
	/// Adds a feed to the Feeds table
	/// </summary>
	/// <param name="feedDTO">The FeedDTO</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when adding the feed</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	/// <exception cref="ArgumentNullException">The feedDTO was null</exception>
	public bool AddedFeed(FeedDTO feed)
	{
	  try
	  {
		return AddedFeed(feed, null);
	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);

		throw;
	  }
	}

	/// <summary>
	/// Adds a feed to the Feeds table
	/// </summary>
	/// <param name="feedDTO">The FeedDTO</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when adding the feed</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	/// <exception cref="ArgumentNullException">The feedDTO was null</exception>
	private bool AddedFeed(FeedDTO feed, NpgsqlCommand parentCommand = null)
	{
	  bool hasParentCommand = parentCommand != null;
	  NpgsqlCommand command = parentCommand;
	  NpgsqlConnection currentConnection = null;
	  bool wasSuccessful = false;

	  try
	  {
		if (feed == null)
		{
		  throw new ArgumentNullException("The FeedDTO cannot be null.");
		}


		if (!hasParentCommand)
		{
		  currentConnection = GetDatabaseConnection();
		  if (OpenConnection(currentConnection) == false)
		  {
			return wasSuccessful;
		  }
		  command = currentConnection.CreateCommand();
		}
		else
		{
		  command.Parameters.Clear();
		}

		string sTable = QualifiedTableName(TableNames.Feeds);
		string sColumns = FeedsColumns.FeedLookupID + "," + FeedsColumns.ContentLookupIDs + "," + FeedsColumns.SourceID + "," + FeedsColumns.SourceValue + "," + FeedsColumns.ViewName;

		command.CommandText = "INSERT INTO " + sTable +
						  " (" + sColumns + ") VALUES (@FeedLookupID, @ContentLookupIDs, @SourceID, @SourceValue, @ViewName)";

		AddParameter(command, NpgsqlDbType.Integer, "FeedLookupID", feed.LookupID);
		AddParameter(command, NpgsqlDbType.Array | NpgsqlDbType.Integer, "ContentLookupIDs", feed.ContentLookupIDs);
		AddParameter(command, NpgsqlDbType.Text, "SourceID", feed.SourceID);
		AddParameter(command, NpgsqlDbType.Text, "SourceValue", feed.SourceValue);
		AddParameter(command, NpgsqlDbType.Text, "ViewName", feed.ViewName);

		DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
		command.ExecuteNonQuery();
		wasSuccessful = true;
	  }
	  catch (NpgsqlException Ex) { throw; }
	  catch (ArgumentNullException Ex) { throw; }
	  catch (Exception Ex)
	  {
		throw new Exception(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), Ex);
	  }
	  finally
	  {
		if (!hasParentCommand)
		{
		  CloseConnection(currentConnection);
		}
	  }

	  return wasSuccessful;
	}

	#endregion

	#region Delete

	/// <summary>
	/// Deletes the Feed with the matching FeedLookupID
	/// </summary>
	/// <param name="lookupID">The LookupID</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when deleting the feed</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	public bool DeletedFeed(int lookupID)
	{
	  NpgsqlCommand cmd = null;
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}
		cmd = currentConnection.CreateCommand();

		cmd.CommandText = "DELETE FROM " + TableNames.Feeds + " WHERE " + FeedsColumns.FeedLookupID + " = @lookupID";
		AddParameter(cmd, NpgsqlDbType.Integer, "lookupID", lookupID);

		DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		int temp = cmd.ExecuteNonQuery();

		wasSuccessful = true;

	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	/// <summary>
	/// Deletes all feeds in the views table
	/// </summary>
	/// <param name="rowsAffected">[Out] Number of rows deleted</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when deleting the feeds</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	public bool DeletedAllFeeds(out int iRowsAffected)
	{
	  iRowsAffected = -1;
	  NpgsqlCommand cmd = null;
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}
		cmd = currentConnection.CreateCommand();
		cmd.CommandText = "DELETE FROM " + TableNames.Feeds;

		DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		iRowsAffected = cmd.ExecuteNonQuery();

		wasSuccessful = true;
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	#endregion

	#endregion

	#region List Controller

	#region Get

	/// <summary>
	/// Read this SourceValue item for the supplied SourceValue lookup id
	/// </summary>
	/// <param name="lookupID">lookupID</param>
	/// <param name="value">[Out] The found source value</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when getting the source value list</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	public bool ReadSourceValue_List(int lookupID, out SourceValueListDTO value)
	{
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  value = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		NpgsqlDataReader reader = this.SelectThisSourceList(lookupID, currentConnection);

		value = new SourceValueListDTO();
		value.Values = new List<ValueDTO>();

		while (reader.Read())
		{
		  SourceValueDTO singleValue = this.ReadSourceValueRow(reader);

		  if (singleValue != null)
		  {
			value.LookupID = singleValue.LookupID;
			value.ID = singleValue.ID;

			ValueDTO valueItem = new ValueDTO() { LookupID = singleValue.FeedLookupID, Value = singleValue.Value };

			value.Values.Add(valueItem);
			wasSuccessful = true;
		  }
		}
		reader.Close();
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	#region Helpers

	/// <summary>
	/// Reads a Database Reader with the row in the SourceValues table for the supplied SourceValue lookup id
	/// </summary>
	/// <param name="lookupID">The lookupID </param>
	/// <param name="currentConnection">The database connection to use.</param>
	/// <returns>The Database reader</returns>
	private NpgsqlDataReader SelectThisSourceList(int lookupID, NpgsqlConnection currentConnection)
	{
	  NpgsqlCommand cmd = currentConnection.CreateCommand();

	  cmd.CommandText = "SELECT * FROM " + TableNames.SourceValues + " WHERE " + SourceValueColumns.sourceID + " = @lookupID";
	  AddParameter(cmd, NpgsqlDbType.Integer, "lookupID", lookupID);

	  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
	  NpgsqlDataReader reader = cmd.ExecuteReader();

	  return reader;
	}

	/// <summary>
	/// Reads a single row entry from the SourceValues table
	/// </summary>
	/// <param name="reader">Database Reader</param>
	/// <returns>SourceValue Data Transfer Object</returns>
	public SourceValueDTO ReadSourceValueRow(NpgsqlDataReader reader)
	{
	  SourceValueDTO sourceVal = new SourceValueDTO();
	  sourceVal.FeedLookupID = (int)reader[SourceValueColumns.FeedLookupID];
	  sourceVal.Value = reader[SourceValueColumns.SourceValue] as string;
	  sourceVal.ID = reader[SourceValueColumns.SourceID] as string;
	  sourceVal.LookupID = (int)reader[SourceValueColumns.sourceID];

	  return sourceVal;
	}

	#endregion

	#endregion

	#region Post/Put

	/// <summary>
	/// Adds the Source Value List DTO
	/// </summary>
	/// <param name="valueList">The Source Value list DTO</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when adding the source value list</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	/// <exception cref="ArgumentNullException">The SourceValueListDTO cannot be null</exception>
	/// <exception cref="InvalidOperationException">The Source Value already exists</exception>
	public bool CreatedSourceValueList(SourceValueListDTO valueList)
	{
	  bool wasSuccessful = true;

	  try
	  {

		if (valueList == null)
		{
		  throw new ArgumentNullException("The Source value list DTO cannot be null.");
		}

		valueList.LookupID = DEUtilities.ComputeHash(new List<string>() { valueList.ID });

		foreach (ValueDTO valueItem in valueList.Values)
		{
		  valueItem.LookupID = DEUtilities.ComputeHash(new List<string>() { valueList.ID, valueItem.Value });

		  if (!AddedSourceValue(valueItem.LookupID, valueList.LookupID, valueList.ID, valueItem.Value))
		  {
			wasSuccessful = false;
			break;
		  }
		}
	  }
	  catch (ArgumentNullException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (InvalidOperationException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);

		throw new Exception(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), Ex);
	  }

	  return wasSuccessful;
	}

	#region Helpers

	/// <summary>
	/// Creates the source value with the given values and adds it to the db
	/// </summary>
	/// <param name="lookupID">The LookupID</param>
	/// <param name="sourceID">The SourceID</param>
	/// <param name="valueID">The ValueID</param>
	/// <param name="value">The value</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	/// <exception cref="InvalidOperationException">The Source Value already exists</exception>
	private bool AddedSourceValue(int lookupID, int sourceID, string valueID, string value)
	{
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		// Checking if this source value exists
		bool doesExist = this.doesExist_SourceValue(lookupID, currentConnection);

		if (!doesExist)
		{
		  NpgsqlCommand cmd = currentConnection.CreateCommand();
		  cmd.CommandText = "INSERT INTO " + QualifiedTableName(TableNames.SourceValues) +
							"(" + SourceValueColumns.sourceID + ", " + SourceValueColumns.SourceID +
							", " + SourceValueColumns.SourceValue + ", " + SourceValueColumns.FeedLookupID + ") VALUES " +
							"(@sourceID, @valueID, @value, @lookupID)";

		  AddParameter(cmd, NpgsqlDbType.Integer, "sourceID", sourceID);
		  AddParameter(cmd, NpgsqlDbType.Text, "valueID", valueID);
		  AddParameter(cmd, NpgsqlDbType.Text, "value", value);
		  AddParameter(cmd, NpgsqlDbType.Integer, "lookupID", lookupID);

		  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		  cmd.ExecuteNonQuery();

		  wasSuccessful = true;
		}
		else
		{
		  DEUtilities.LogMessage("Source value already exists", DEUtilities.LogLevel.Error);
		  throw new InvalidOperationException("The Source Value already exists");
		}

	  }
	  catch (NpgsqlException Ex) { throw; }
	  catch (InvalidOperationException Ex) { throw; }
	  catch (Exception Ex)
	  {
		throw new Exception(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), Ex);
	  }
	  finally
	  {
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	/// <summary>
	/// Checks if a source value exists
	/// </summary>
	/// <param name="lookupID"></param>
	/// <param name="currentConnection">The database connection to use.</param>
	/// <returns></returns>
	private bool doesExist_SourceValue(int lookupID, NpgsqlConnection currentConnection)
	{
	  NpgsqlCommand cmd = currentConnection.CreateCommand();

	  cmd.CommandText = "SELECT COUNT(*) FROM " + TableNames.SourceValues + " WHERE " + SourceValueColumns.FeedLookupID + " = @lookupID";
	  AddParameter(cmd, NpgsqlDbType.Integer, "lookupID", lookupID);

	  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
	  long count = (long)cmd.ExecuteScalar();

	  return count > 0;
	}

	#endregion

	#endregion

	#region Delete

	/// <summary>
	/// Deletes the source value list with the given lookupID
	/// </summary>
	/// <param name="lookupID">The lookupID</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when deleting the source value list</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	/// <exception cref="ArgumentException">The Source Value List was not found</exception>
	public bool DeletedSourceValueList(int lookupID)
	{
	  NpgsqlCommand cmd = null;
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		// Checking if source value list exists
		NpgsqlDataReader reader = this.SelectThisSourceList(lookupID, currentConnection);
		bool listExist = reader.Read();
		reader.Close();

		if (!listExist)
		{
		  throw new ArgumentException("Source Value list was not found");
		}

		// Deleting the source value list
		cmd = currentConnection.CreateCommand();

		cmd.CommandText = "DELETE FROM " + TableNames.SourceValues + " WHERE " + SourceValueColumns.sourceID + " = @lookupID";
		AddParameter(cmd, NpgsqlDbType.Integer, "lookupID", lookupID);

		DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		int temp = cmd.ExecuteNonQuery();

		wasSuccessful = true;
	  }
	  catch (ArgumentException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	#endregion

	#endregion

	#region Rules Controller

	#region Get

	/// <summary>
	/// Read all of the rules
	/// </summary>
	/// <param name="lstRules">[Out] The list of rules found</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when getting all of the rules</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	public bool ReadRule_All(out List<RuleDTO> lstRules)
	{
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  lstRules = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}
		NpgsqlDataReader reader = SelectAllRules(currentConnection);
		lstRules = new List<RuleDTO>();

		while (reader.Read())
		{
		  RuleDTO rule = this.ReadRuleRow(reader);
		  lstRules.Add(rule);
		}
		reader.Close();

		wasSuccessful = true;
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	/// <summary>
	/// Read the rule for the supplied rule lookupId
	/// </summary>
	/// <param name="lookupid">Rule lookupId</param>
	/// <param name="rule">[Out] Rule to return</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when reading the rule</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	public bool ReadRule(int lookupid, out RuleDTO rule)
	{
	  bool wasSuccessful = false;
	  rule = null;

	  NpgsqlConnection currentConnection = null;
	  NpgsqlCommand command = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		command = currentConnection.CreateCommand();
		NpgsqlDataReader reader = this.SelectThisRule(lookupid, command);

		if (reader.Read())
		{
		  rule = this.ReadRuleRow(reader);
		  wasSuccessful = rule != null;
		}
		reader.Close();
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	#region Helpers

	/// <summary>
	/// Reads a Database Reader with all of the rows in the Rules table
	/// </summary>
	/// <param name="connection">Database connection to use.</param>
	/// <returns>Database Reader</returns>
	public NpgsqlDataReader SelectAllRules(NpgsqlConnection connection)
	{
	  NpgsqlCommand cmd = connection.CreateCommand();

	  cmd.CommandText = "SELECT * FROM " + TableNames.Rules;

	  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
	  NpgsqlDataReader reader = cmd.ExecuteReader();

	  return reader;
	}

	/// <summary>
	/// Reads a Database Reader with the row in the Rules table for the supplied rule lookup id
	/// </summary>
	/// <param name="RuleLookupID">Rule to lookup</param>
	/// <param name="cmd">Database command to use.</param>
	/// <returns>Database Reader</returns>
	public NpgsqlDataReader SelectThisRule(int RuleLookupID, NpgsqlCommand cmd)
	{
	  cmd.Parameters.Clear();

	  cmd.CommandText = "SELECT * FROM " + TableNames.Rules + " WHERE " + RulesColumns.RuleLookupID + " = @RuleLookupID";
	  AddParameter(cmd, NpgsqlDbType.Integer, "RuleLookupID", RuleLookupID);

	  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
	  NpgsqlDataReader reader = cmd.ExecuteReader();

	  return reader;
	}

	/// <summary>
	/// Reads a single row entry from the Rules table
	/// </summary>
	/// <param name="reader">Database Reader</param>
	/// <returns>raw Rule Data Transfer Object</returns>
	public RuleDTO ReadRuleRow(NpgsqlDataReader reader)
	{
	  RuleDTO rule = new RuleDTO();
	  rule.DEElement = reader[RulesColumns.ElementName] as string;
	  rule.RuleLookupID = (int)reader[RulesColumns.RuleLookupID];
	  rule.SourceID = reader[RulesColumns.RuleID] as string;
	  rule.SourceValue = reader[RulesColumns.RuleValue] as string;
	  rule.Feeds = new List<int>(reader[RulesColumns.FeedLookupIDs] as int[]);//TODO: This might be an issue depending on what an empty array returns
	  rule.FedURI = new List<string>(reader[RulesColumns.FederationURI] as string[]); //TODO: This might be an issue depending on what an empty array returns
																					  //a NULL will probably make the constructor fail
	  return rule;
	}

	#endregion

	#endregion

	#region Delete

	/// <summary>
	/// Tries to delete the rule from the rules table
	/// with the matching RuleLookupID
	/// </summary>
	/// <param name="lookupid">Rule lookupId</param>
	/// <returns>Success or failure</returns>
	/// <exception cref="Exception">An error occurred when deleting the rule</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	/// <exception cref="ArgumentException">Rule was not found</exception>
	public bool DeletedRule(int lookupid)
	{
	  NpgsqlCommand cmd = null;
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}
		cmd = currentConnection.CreateCommand();

		// Check if Rule exists
		NpgsqlDataReader reader = this.SelectThisRule(lookupid, cmd);
		bool ruleExists = reader.Read();
		reader.Close();

		if (!ruleExists)
		{
		  throw new ArgumentException("Rule with lookupId: " + lookupid + " not found");
		}

		// Deleting the rule
		cmd.CommandText = "DELETE FROM " + TableNames.Rules + " WHERE " + RulesColumns.RuleLookupID + " = @RuleLookupID";
		AddParameter(cmd, NpgsqlDbType.Integer, "RuleLookupID", lookupid);

		DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		int temp = cmd.ExecuteNonQuery();

		wasSuccessful = true;
	  }
	  catch (ArgumentException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	/// <summary>
	/// Delete all of the rules in the Rules table
	/// Fail if there are any DE messages hanging around
	/// </summary>
	/// <param name="rowsAffected">Number of rows deleted</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when deleting all of the rules</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	public bool DeletedAllRules(out int rowsAffected)
	{
	  NpgsqlTransaction sqlTrans = null;
	  NpgsqlCommand cmd = null;
	  bool wasSuccessful = false;
	  int iRows = -1;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  rowsAffected = iRows;
		  return wasSuccessful;
		}

		sqlTrans = currentConnection.BeginTransaction();
		cmd = currentConnection.CreateCommand();
		cmd.Transaction = sqlTrans;

		cmd.CommandText = "SELECT COUNT(*) FROM " + TableNames.DE;
		long count = (long)cmd.ExecuteScalar();

		//check for existing de messages
		if (count < 1)
		{
		  cmd.CommandText = "DELETE FROM " + TableNames.Rules;

		  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		  int temp = cmd.ExecuteNonQuery();
		  sqlTrans.Commit();
		  iRows = temp;  //don't set rows affected until commit is successful
		  wasSuccessful = true;
		}
		else //found at least one de message
		{
		  iRows = 0;
		  wasSuccessful = false;
		}
	  }
	  catch (NpgsqlException Ex)
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

		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		CloseConnection(currentConnection);
	  }
	  rowsAffected = iRows;
	  return wasSuccessful;
	}

	#endregion

	#endregion

	#region Rules Fed Controller

	#region Get

	/// <summary>
	/// Read all of the rules that have at least one federation Uri associated to them
	/// </summary>
	/// <param name="lstRules">[Out] List of found rules</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when getting the federation rules</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	public bool ReadRule_AllFedUri(out List<RuleFedDTO> lstRules)
	{
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  lstRules = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		NpgsqlDataReader reader = this.SelectAllRules(currentConnection);
		lstRules = new List<RuleFedDTO>();

		while (reader.Read())
		{
		  RuleFedDTO rule = this.ReadRuleRow_Federation(reader);

		  //only return the rules with feeds associated to them
		  if (rule.FedURI.Count > 0)
		  {
			lstRules.Add(rule);
		  }
		}
		reader.Close();

		if (lstRules.Count == 0)
		{
		  DEUtilities.LogMessage("No federation rules were found", DEUtilities.LogLevel.Warning);
		}

		wasSuccessful = true;
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }
	  return wasSuccessful;
	}

	/// <summary>
	/// Read the federation rule for the supplied rule lookupId
	/// </summary>
	/// <param name="lookupid">Rule lookupId</param>
	/// <param name="rule">[Out] Rule to return</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when getting the federation rule</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	/// <exception cref="ArgumentException">The Federation rule was not found</exception>
	public bool ReadRule_FedUri(int lookupid, out RuleFedDTO rule)
	{
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  rule = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		NpgsqlDataReader reader = this.SelectThisRule(lookupid, currentConnection.CreateCommand());

		if (reader.Read())
		{
		  rule = this.ReadRuleRow_Federation(reader);
		  wasSuccessful = rule != null;
		}
		else
		{
		  throw new ArgumentException("Federation rule was not found");
		}

		reader.Close();
	  }
	  catch (ArgumentException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }
	  return wasSuccessful;
	}

	#region Helpers

	/// <summary>
	/// Reads a single row entry from the Rules table focusing
	/// on the federation URI
	/// </summary>
	/// <param name="reader">Database Reader</param>
	/// <returns>Federation Rule Data Transfer Object</returns>
	
	private RuleFedDTO ReadRuleRow_Federation(NpgsqlDataReader reader)
	{
	  RuleFedDTO rule = new RuleFedDTO();
	  rule.DEElement = reader[RulesColumns.ElementName] as string;
	  rule.RuleLookupID = (int)reader[RulesColumns.RuleLookupID];
	  rule.SourceID = reader[RulesColumns.RuleID] as string;
	  rule.SourceValue = reader[RulesColumns.RuleValue] as string;
	  rule.FedURI = new List<string>(reader[RulesColumns.FederationURI] as string[]);
	  return rule;
	}

	#endregion

	#endregion

	#region Post/Put

	/// <summary>
	/// Create a new rule using the federation methodology
	/// </summary>
	/// <param name="rule">Rule to add to database</param>
	/// <param name="ruleLookupID">[Out] unique id for rule</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when adding the federation rule</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	/// <exception cref="ArgumentNullException">The rule cannot be null</exception>
	/// <exception cref="InvalidOperationException">The DTO was missing a required value or contained an invalid value</exception>
	public bool CreatedRule_FedUri(RuleFedDTO rule, out int ruleLookupID)
	{
	  bool wasSuccessful = false;
	  int id = -1;

	  if (rule == null)
	  {
		DEUtilities.LogMessage("The RuleFedDTO cannot be null", DEUtilities.LogLevel.Error);
		throw new ArgumentNullException("The rule cannot be null.");
	  }

	  try
	  {		
		int lookupID = DEUtilities.ComputeHash(new List<string> { rule.SourceID, rule.SourceValue });

		if (AddedRule(rule.DEElement, lookupID, rule.SourceID, rule.SourceValue, rule.FedURI.ToArray()))
		{
		  wasSuccessful = true;
		  id = lookupID;
		}

		ruleLookupID = id;
	  }
	  catch (ArgumentNullException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw new InvalidOperationException(Ex.Message, Ex);
	  }
	  catch (InvalidOperationException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);

		throw new Exception(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), Ex);
	  }

	  return wasSuccessful;
	}

	/// <summary>
	/// Add federation Uri from the supplied rule
	/// </summary>
	/// <param name="lookupid">Rule lookup id Id</param>
	/// <param name="uri">Federation URI to add</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when adding the rule URI </exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	/// <exception cref="ArgumentNullException">The Uri cannot be null</exception>
	/// <exception cref="InvalidOperationException">The URI is invalid.  It's pointing to this application or is not a valid URI</exception>
	/// <exception cref="ArgumentException">The URI was an invalid URI</exception>
	public bool AddRuleFed(int lookupid, string uri)
	{
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  NpgsqlCommand command = null;
	  NpgsqlTransaction sqlTrans = null;

	  try
	  {
		
		if (CreateFederationURI(uri) == null)
		{
		  return wasSuccessful;
		}

		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		command = currentConnection.CreateCommand();
		sqlTrans = currentConnection.BeginTransaction();
		command.Transaction = sqlTrans;

		NpgsqlDataReader reader = this.SelectThisRule(lookupid, command);
		string[] feduris;
		List<string> lstFed = null;

		if (reader.Read())
		{
		  //Read the array of feed lookup ids for this rule
		  feduris = reader[RulesColumns.FederationURI] as string[];
		  lstFed = new List<string>(feduris);
		}
		reader.Close();

		if (lstFed != null && lstFed.Contains(uri))
		{
		  //This is already in the URI list, so return.
		  //TODO: Respond with a report to this?
		  wasSuccessful = true;
		  DEUtilities.LogMessage("The URI was already there", DEUtilities.LogLevel.Warning);
		}
		else
		{
		  if (lstFed == null)
		  {
			lstFed = new List<string>();
		  }

		  //success, found the feed lookupId
		  lstFed.Add(uri);

		  command.Parameters.Clear();
		  command.CommandText = "UPDATE " + TableNames.Rules + " SET " + RulesColumns.FederationURI + " = @FederationURI " +
							"WHERE " + RulesColumns.RuleLookupID + " = @RuleLookupID";
		  AddParameter(command, NpgsqlDbType.Integer, "RuleLookupID", lookupid);
		  AddParameter(command, NpgsqlDbType.Array | NpgsqlDbType.Text, "FederationURI", lstFed.ToArray());

		  DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
		  int temp = command.ExecuteNonQuery();
		  sqlTrans.Commit();
		  wasSuccessful = true;
		}

	  }
	  catch (ArgumentNullException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (InvalidOperationException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (UriFormatException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw new ArgumentException("The Uri string was not a valid URI", Ex);
	  }
	  catch (NpgsqlException Ex)
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

		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		CloseConnection(currentConnection);
	  }
	  return wasSuccessful;
	}

	/// <summary>
	/// Update the rule with the new feed lookup ids
	/// </summary>
	/// <param name="lookupid">Rule lookupId</param>
	/// <param name="rule">The Rule DTO, cannot be null</param>
	/// <returns>Success or failure</returns>
	/// <exception cref="Exception">An error occurred when adding the rule</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	/// <exception cref="ArgumentNullException">The rule DTO was null</exception>
	/// <exception cref="InvalidOperationException">The rule contains a URI that is not a valid federation URI.</exception>
	public bool UpdatedRule_FedUri(int lookupid, RuleFedDTO rule)
	{
	  NpgsqlCommand cmd = null;
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  NpgsqlTransaction sqlTrans = null;

	  try
	  {

		if (rule == null)
		{
		  DEUtilities.LogMessage("The rule cannot be null", DEUtilities.LogLevel.Error);
		  throw new ArgumentNullException("The Federation rule DTO cannot be null.");
		}

		// Check that none of the FedURIs point to self
		Uri baseURI = new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority)); // holds URI for current instance of FRESH

		foreach (string uriString in rule.FedURI.ToArray())
		{		  
		  if (CreateFederationURI(uriString) == null)
		  {
			return wasSuccessful;
		  }
		}

		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}
		sqlTrans = currentConnection.BeginTransaction();
		cmd = currentConnection.CreateCommand();
		cmd.Transaction = sqlTrans;

		cmd.CommandText = "SELECT Count(*) FROM " + TableNames.Rules + " WHERE " + RulesColumns.RuleLookupID + " = @RuleLookupID";
		AddParameter(cmd, NpgsqlDbType.Integer, "RuleLookupID", lookupid);

		DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		long count = (long)cmd.ExecuteScalar();

		if (count > 0)
		{
		  cmd.CommandText = "UPDATE " + TableNames.Rules +
			" SET " +
			RulesColumns.FederationURI + " = @FederationURI" +
			" WHERE " +
			RulesColumns.RuleLookupID + " = @RuleLookupID";

		  AddParameter(cmd, NpgsqlDbType.Array | NpgsqlDbType.Text, "FederationURI", rule.FedURI.ToArray());

		  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		  int temp = cmd.ExecuteNonQuery();
		  sqlTrans.Commit();
		  wasSuccessful = true;
		}
	  }
	  catch (InvalidOperationException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (UriFormatException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw new InvalidOperationException("The Federation Rule DTO contained an invalid URI", Ex);
	  }
	  catch (ArgumentNullException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw new InvalidOperationException("The Federation Rule DTO contained an invalid URI", Ex);
	  }
	  catch (NpgsqlException Ex)
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

		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	#region Helpers

	/// <summary>
	/// Adds a new rule to the rules table without any feeds
	/// </summary>
	/// <param name="elementName">DE Element Name</param>
	/// <param name="ruleLookupID">Rule Lookup ID</param>
	/// <param name="ruleID">Rule ID</param>
	/// <param name="ruleValue">Rule Value</param>
	/// <param name="federation">Federation string array</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	/// <exception cref="InvalidOperationException">The rule contains a URI that is not a valid federation URI.</exception>
	/// <exception cref="ArgumentNullException">Required value was missing</exception>
	private bool AddedRule(string elementName, int ruleLookupID, string ruleID, string ruleValue, string[] federation)
	{
	  bool wasSuccessful = false;
	  bool readSuccessful = false;

	  NpgsqlConnection currentConnection = null;
	  NpgsqlTransaction sqlTrans = null;
	  NpgsqlCommand command = null;

	  try
	  {

		if (elementName == null || ruleID == null || ruleValue == null || federation == null)
		{
		  throw new ArgumentNullException("Required value was null");
		}

		foreach (string uriString in federation)
		{
		  try
		  {
			if (CreateFederationURI(uriString) == null)
			{
			  return wasSuccessful;
			}
		  }
		  catch (Exception Ex)
		  {
			throw new InvalidOperationException("The Federation Rule DTO contained an invalid URI", Ex);
		  }

		}

		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		sqlTrans = currentConnection.BeginTransaction();
		command = currentConnection.CreateCommand();
		command.Transaction = sqlTrans;

		NpgsqlDataReader reader = this.SelectThisRule(ruleLookupID, command);

		readSuccessful = reader.Read();
		reader.Close();

		if (readSuccessful)
		{
		  //probably added by source values methods
		  command.Parameters.Clear();
		  command.CommandText = "UPDATE " + QualifiedTableName(TableNames.Rules) +
			" SET " +
			RulesColumns.FederationURI + " = @FederationURI" +
			" WHERE " +
			RulesColumns.RuleLookupID + " = @RuleLookupID";

		  AddParameter(command, NpgsqlDbType.Integer, "RuleLookupID", ruleLookupID);
		  AddParameter(command, NpgsqlDbType.Array | NpgsqlDbType.Integer, "FederationURI", federation);

		  DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
		  command.ExecuteNonQuery();

		  wasSuccessful = true;
		}
		else
		{
		  wasSuccessful = AddedRule(elementName, ruleLookupID, ruleID, ruleValue, new int[0], federation, command);
		}
		sqlTrans.Commit();
	  }
	  
	  catch (NpgsqlException Ex) { throw; }
	  catch (InvalidOperationException Ex) { throw; }
	  catch (ArgumentException Ex) { throw; }
	  catch (Exception Ex)
	  {
		throw new Exception(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), Ex);
	  }
	  finally
	  {
		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		CloseConnection(currentConnection);
	  }
	  return wasSuccessful;
	}

	/// <summary>
	/// Adds a rule to the Rules table
	/// </summary>
	/// <param name="ruleLookupID">Rule lookup id</param>
	/// <param name="ruleID">Rule ID</param>
	/// <param name="ruleValue">Rule Value</param>
	/// <param name="feeds">Feed lookup id array</param>
	/// <param name="federation">Federation string array</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	private bool AddedRule(string elementName, int ruleLookupID, string ruleID, string ruleValue,
	  int[] feeds, string[] federation, NpgsqlCommand parentCommand = null)
	{

	  bool hasParentCommand = parentCommand != null;
	  NpgsqlCommand command = parentCommand;
	  NpgsqlConnection currentConnection = null;
	  bool wasSuccessful = false;

	  try
	  {
		if (!hasParentCommand)
		{
		  currentConnection = GetDatabaseConnection();
		  if (OpenConnection(currentConnection) == false)
		  {
			return wasSuccessful;
		  }
		  command = currentConnection.CreateCommand();
		}
		else
		{
		  command.Parameters.Clear();
		}

		string sTable = QualifiedTableName(TableNames.Rules);
		string sColumns = RulesColumns.ElementName + "," + RulesColumns.RuleLookupID + "," + RulesColumns.RuleID + "," +
						  RulesColumns.RuleValue + "," + RulesColumns.FeedLookupIDs + "," + RulesColumns.FederationURI;

		command.CommandText = "INSERT INTO " + sTable +
						  " (" + sColumns + ") VALUES " +
						  "(@ElementName, @RuleLookupID, @RuleID, @RuleValue, @FeedLookupIDs, @FederationURI)";
		AddParameter(command, NpgsqlDbType.Text, "ElementName", elementName);
		AddParameter(command, NpgsqlDbType.Integer, "RuleLookupID", ruleLookupID);
		AddParameter(command, NpgsqlDbType.Text, "RuleID", ruleID);
		AddParameter(command, NpgsqlDbType.Text, "RuleValue", ruleValue);
		AddParameter(command, NpgsqlDbType.Array | NpgsqlDbType.Integer, "FeedLookupIDs", feeds);
		AddParameter(command, NpgsqlDbType.Array | NpgsqlDbType.Text, "FederationURI", federation);

		DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
		command.ExecuteNonQuery();
		wasSuccessful = true;
	  }
	  catch (Exception Ex)
	  {
		throw;
	  }
	  finally
	  {
		if (!hasParentCommand)
		{
		  CloseConnection(currentConnection);
		}
	  }
	  return wasSuccessful;
	}

	/// <summary>
	/// Creates the URI from the string
	/// </summary>
	/// <param name="newURI">Uri string</param>
	/// <returns>The Uri object</returns>
	/// <exception cref="ArgumentNullException">The URI string was null</exception>
	/// <exception cref="UriFormatException">The URI string does not create a valid URI</exception>
	/// <exception cref="InvalidOperationException">The URI points back to the application</exception>
	private Uri CreateFederationURI(string newURI)
	{
	  Uri fedUri = new Uri(newURI);

	  // Check that the URI does not point back to this application
	  Uri baseURI = new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority)); // holds URI for current instance of the Application

	  if (baseURI.Authority == fedUri.Authority) // if this URI would cause federation to point to FRESH
	  {
		DEUtilities.LogMessage("This URI points back to this application", DEUtilities.LogLevel.Error);
		throw new InvalidOperationException("The URI points back to this application");
	  }

	  return fedUri;
	}

	#endregion

	#endregion

	#region Delete

	/// <summary>
	/// Remove federation Uri from the supplied rule
	/// </summary>
	/// <param name="lookupid">Rule lookup id Id</param>
	/// <param name="uri">Federation URI to remove</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when removing the federation rule Uri</exception>
	
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	/// <exception cref="ArgumentNullException">The federation Uri cannot be null</exception>
	/// <exception cref="ArgumentException">This lookupID does not points to any rules</exception>
	/// <exception cref="InvalidOperationException">The Rule does not contain this URI</exception>
	public bool DeletedRuleFed(int lookupid, string uri)
	{
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  NpgsqlCommand command = null;
	  NpgsqlTransaction sqlTrans = null;

	  try
	  {
		if (uri == null)
		{
		  DEUtilities.LogMessage("The Federation URI to remove cannot be null", DEUtilities.LogLevel.Error);
		  throw new ArgumentNullException("The Federation URI cannot be null");
		}

		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		command = currentConnection.CreateCommand();
		sqlTrans = currentConnection.BeginTransaction();
		command.Transaction = sqlTrans;

		NpgsqlDataReader reader = this.SelectThisRule(lookupid, command);
		string[] feduris = null;
		List<string> lstFed = null;
		bool foundRule = false;

		if (reader.Read())
		{
		  //Read the array of feed lookup ids for this rule
		  feduris = reader[RulesColumns.FederationURI] as string[];
		  lstFed = new List<string>(feduris);
		  foundRule = true;
		}

		reader.Close();

		if (!foundRule)
		{
		  wasSuccessful = false;
		  throw new ArgumentException("This lookupID does not point to any rules");
		}
		else if (feduris == null || lstFed == null || feduris.Length == 0)
		{
		  wasSuccessful = false;
		  throw new InvalidOperationException("This Rule does not have any federation URIs");
		}
		else if (!lstFed.Contains(uri))
		{
		  wasSuccessful = false;
		  throw new InvalidOperationException("This Rule does not have the URI: " + uri);
		}
		else
		{
		  //success, found the feed lookupId
		  lstFed.Remove(uri);

		  command.Parameters.Clear();
		  command.CommandText = "UPDATE " + TableNames.Rules + " SET " + RulesColumns.FederationURI + " = @FederationURI " +
							"WHERE " + RulesColumns.RuleLookupID + " = @RuleLookupID";
		  AddParameter(command, NpgsqlDbType.Integer, "RuleLookupID", lookupid);
		  AddParameter(command, NpgsqlDbType.Array | NpgsqlDbType.Text, "FederationURI", lstFed.ToArray());

		  DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
		  int temp = command.ExecuteNonQuery();
		  sqlTrans.Commit();

		  wasSuccessful = true;
		}
	  }
	  catch (ArgumentException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (InvalidOperationException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
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
		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	/// <summary>
	/// Removes all federation Uris from the supplied rule
	/// </summary>
	/// <param name="lookupid">Rule lookup id Id</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when deleting all of Uri from the federation rule</exception>
	
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	/// <exception cref="ArgumentException">This lookupID does not points to any rules</exception>
	/// <exception cref="InvalidOperationException">The Rule does not have any URIs</exception>
	public bool DeletedAllRuleFed(int lookupid)
	{
	  bool wasSuccessful = false;

	  NpgsqlConnection currentConnection = null;
	  NpgsqlCommand command = null;
	  NpgsqlTransaction sqlTrans = null;

	  try
	  {
		// Opening DB Connection
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}
		command = currentConnection.CreateCommand();
		sqlTrans = currentConnection.BeginTransaction();
		command.Transaction = sqlTrans;

		// Reading the Rule
		NpgsqlDataReader reader = this.SelectThisRule(lookupid, command);
		string[] feduris = null;
		List<string> lstFed = null;
		bool RuleFound = false;

		if (reader.Read())
		{
		  //Read the array of feed lookup ids for this rule
		  feduris = reader[RulesColumns.FederationURI] as string[];
		  lstFed = new List<string>(feduris);
		  RuleFound = true;
		}

		reader.Close();

		// Checking
		if (!RuleFound)
		{
		  wasSuccessful = false;
		  throw new ArgumentException("No rule was found for lookupID " + lookupid);
		}
		else if (feduris == null || feduris.Length == 0 || lstFed == null)
		{
		  wasSuccessful = false;
		  throw new InvalidOperationException("The rule has no URIs to remove");
		}
		else
		{
		  //success, found the feed lookupId
		  lstFed.Clear();

		  command.Parameters.Clear();
		  command.CommandText = "UPDATE " + TableNames.Rules + " SET " + RulesColumns.FederationURI + " = @FederationURI " +
							"WHERE " + RulesColumns.RuleLookupID + " = @RuleLookupID";
		  AddParameter(command, NpgsqlDbType.Integer, "RuleLookupID", lookupid);
		  AddParameter(command, NpgsqlDbType.Array | NpgsqlDbType.Text, "FederationURI", lstFed.ToArray());

		  DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
		  int temp = command.ExecuteNonQuery();
		  sqlTrans.Commit();
		  wasSuccessful = true;
		}

	  }
	  catch (ArgumentException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (InvalidOperationException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
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
		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		CloseConnection(currentConnection);
	  }
	  return wasSuccessful;
	}
	#endregion

	#endregion

	#region Rules SV + Value Controller

	#region Get

	/// <summary>
	/// Read all of the rules that have at least one feed associated to them
	/// </summary>
	/// <param name="lstRules">[Out] List of rules found</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when reading all of the rules</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	public bool ReadRule_AllFeeds(out List<RuleSourceValueDTO> lstRules)
	{
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  lstRules = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		NpgsqlDataReader reader = SelectAllRules(currentConnection);
		lstRules = new List<RuleSourceValueDTO>();

		while (reader.Read())
		{
		  RuleDTO rawRule = this.ReadRuleRow_SourceValue(reader);
		  RuleSourceValueDTO rule = (rawRule != null) ? (RawRowSourceValueTransform(rawRule)) : null;

		  if (rule == null)
		  {
			return wasSuccessful;
		  }

		  lstRules.Add(rule);
		}

		reader.Close();

		wasSuccessful = true;
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	/// <summary>
	/// Read the feed rule for the supplied rule lookupId
	/// </summary>
	/// <param name="lookupid">Rule lookupId</param>
	/// <param name="rule">[Out] Rule to return</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when reading the rule</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	/// <exception cref="ArgumentException">The lookupID does not point to a rule</exception>
	public bool ReadRule_Feed(int lookupid, out RuleSourceValueDTO rule)
	{
	  bool wasSuccessful = false;
	  RuleDTO rawRule = null;
	  NpgsqlConnection currentConnection = null;
	  NpgsqlCommand command = null;
	  rule = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		command = currentConnection.CreateCommand();

		NpgsqlDataReader reader = this.SelectThisRule(lookupid, command);
		bool ruleRead = false;

		if (reader.Read())
		{
		  rawRule = this.ReadRuleRow_SourceValue(reader);
		  ruleRead = true;
		}

		reader.Close();

		if (!ruleRead || rawRule == null)
		{
		  wasSuccessful = false;
		  throw new ArgumentException("No Source Value Rules was found for lookupID: " + lookupid);
		}

		if (rawRule != null)
		{
		  //transform into RuleSourceValueDTO
		  rule = this.RawRowSourceValueTransform(rawRule);
		  wasSuccessful = rule != null;
		}
	  }
	  catch (ArgumentException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	/// <summary>
	/// Read this SourceValue item for the supplied SourceValue lookup id
	/// </summary>
	/// <param name="lookupID">lookup ID for the source value</param>
	/// <param name="value">[Out] The source value found</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when reading the source value</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	/// <exception cref="ArgumentException">The lookupID does not point to a Source Value</exception>
	public bool ReadSourceValue_Item(int lookupID, out SourceValueDTO value)
	{
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  value = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		NpgsqlDataReader reader = this.SelectThisSourceValue(lookupID, currentConnection);
		bool svRead = false;

		if (reader.Read())
		{
		  value = this.ReadSourceValueRow(reader);
		  wasSuccessful = (value != null);
		  svRead = true;
		}

		reader.Close();

		if (!svRead || value == null)
		{
		  wasSuccessful = false;
		  throw new ArgumentException("There are no Source Value with lookupID: " + lookupID);
		}

		wasSuccessful = true;
	  }
	  catch (ArgumentException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	#region Helpers

	//TODO:FIX Summary
	/// <summary>
	/// Reads a single row entry from the Rules table focusing
	/// on the source value pairs
	/// </summary>
	/// <param name="source"></param>
	/// <returns>Source Value Rule Data Transfer Object</returns>
	private RuleSourceValueDTO RawRowSourceValueTransform(RuleDTO source)
	{
	  RuleSourceValueDTO rule = new RuleSourceValueDTO();
	  rule.DEElement = source.DEElement;
	  rule.RuleLookupID = source.RuleLookupID;
	  rule.SourceID = source.SourceID;
	  rule.SourceValue = source.SourceValue;
	  rule.Feeds = new List<SourceValueDTO>();

	  //only return the rules with feeds associated to them
	  foreach (int feed in source.Feeds)
	  {
		SourceValueDTO sourceVal = new SourceValueDTO();

		if (ReadSourceValueFromFeedLookupID(feed, out sourceVal))
		{
		  rule.Feeds.Add(sourceVal);
		}
		else //something is cross wired
		{
		  rule = null;
		  break;
		}
	  }

	  return rule;
	}

	/// <summary>
	///  Reads a Database Reader with the row in the SourceValues table for the supplied SourceValue lookup id
	/// </summary>
	/// <param name="lookupID"></param>
	/// <param name="currentConnection">The database connection to use.</param>
	/// <returns></returns>
	private NpgsqlDataReader SelectThisSourceValue(int lookupID, NpgsqlConnection currentConnection)
	{
	  NpgsqlCommand cmd = currentConnection.CreateCommand();

	  cmd.CommandText = "SELECT * FROM " + TableNames.SourceValues + " WHERE " + SourceValueColumns.FeedLookupID + " = @lookupID";
	  AddParameter(cmd, NpgsqlDbType.Integer, "lookupID", lookupID);

	  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
	  NpgsqlDataReader reader = cmd.ExecuteReader();

	  return reader;
	}

	/// <summary>
	/// Read a source value entry from the SourceValues table
	/// </summary>
	/// <param name="FeedLookupID">FeedLookupID to lookup</param>
	/// <param name="sourceVal">[Out] SourceValue to return</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	private bool ReadSourceValueFromFeedLookupID(int FeedLookupID, out SourceValueDTO sourceVal)
	{
	  NpgsqlCommand cmd = null;
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  sourceVal = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		cmd = currentConnection.CreateCommand();

		cmd.CommandText = "SELECT * FROM " + TableNames.SourceValues + " WHERE " + SourceValueColumns.FeedLookupID + " = @lookupID";
		AddParameter(cmd, NpgsqlDbType.Integer, "lookupID", FeedLookupID);

		DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		NpgsqlDataReader reader = cmd.ExecuteReader();

		if (reader.Read())
		{
		  sourceVal = this.ReadSourceValueRow(reader);
		  wasSuccessful = true;
		}

		reader.Close();
	  }
	  catch (Exception Ex)
	  {
		throw;
	  }
	  finally
	  {
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	/// <summary>
	/// Reads a single row entry from the Rules table focusing
	/// on the source value pairs
	/// </summary>
	/// <param name="reader">Database Reader</param>
	/// <returns>Source Value Rule Data Transfer Object</returns>
	private RuleDTO ReadRuleRow_SourceValue(NpgsqlDataReader reader)
	{
	  RuleDTO rule = new RuleDTO();
	  rule.DEElement = reader[RulesColumns.ElementName] as string;
	  rule.RuleLookupID = (int)reader[RulesColumns.RuleLookupID];
	  rule.SourceID = reader[RulesColumns.RuleID] as string;
	  rule.SourceValue = reader[RulesColumns.RuleValue] as string;
	  int[] feeds = reader[RulesColumns.FeedLookupIDs] as int[];
	  rule.Feeds = new List<int>(feeds);

	  //only return the rules with feeds associated to them
	  //if (feeds.Length > 0)
	  //{
	  //  foreach (int feed in feeds)
	  //  {
	  //    SourceValueDTO sourceVal = new SourceValueDTO();

	  //    if (ReadSourceValueFromFeedLookupID(feed, sourceVal))
	  //    {
	  //      rule.Feeds.Add(sourceVal);
	  //    }
	  //    else //something is cross wired
	  //    {
	  //      rule = null;
	  //      break;
	  //    }
	  //  }
	  //}
	  return rule;
	}

	#endregion

	#endregion

	#region Post/Put

	/// <summary>
	/// Method:       CreateRule_Feed
	/// Project:      Fresh.PostGIS
	/// Purpose:      Create a new rule using the source id/source value methodology
	/// Created:      2016-03-10
	/// Author:       Brian Wilkins - ArdentMC
	/// Side Effects: The rule lookup id is an outbound parameter, set in the method. 
	/// 
	/// Updates:
	/// </summary>
	/// <param name="rule">Rule to add to database</param>
	/// <param name="ruleLookupID">[Out] unique id for rule</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when adding the rule</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	/// <exception cref="ArgumentNullException">The rule cannot be null</exception>
	public bool CreatedRule_Feed(RuleSourceValueDTO rule, out int ruleLookupID)
	{
	  bool wasSuccessful = false;
	  int id = -1;
	  List<int> feedLookupIDs = new List<int>();

	  try
	  {

		if (rule == null)
		{
		  throw new ArgumentNullException("The Rule SourveValue DTO cannot be null.");
		}

		foreach (SourceValueDTO sv in rule.Feeds)
		{
		  feedLookupIDs.Add(DEUtilities.ComputeHash(new List<string> { sv.ID, sv.Value }));
		}

		int lookupID = DEUtilities.ComputeHash(new List<string> { rule.SourceID, rule.SourceValue });

		if (AddedRule(rule.DEElement, lookupID, rule.SourceID, rule.SourceValue, feedLookupIDs.ToArray()))
		{
		  wasSuccessful = true;
		  id = lookupID;
		}

	  }
	  catch (ArgumentNullException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);

		throw new Exception(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), Ex);
	  }

	  ruleLookupID = id;
	  return wasSuccessful;
	}

	/// <summary>
	/// Update the rule with the new feed lookup ids
	/// </summary>
	/// <param name="lookupid">Rule lookupId</param>
	/// <param name="rule"></param>
	/// <returns>Success or failure</returns>
	/// <exception cref="Exception">An error occurred when updating the rule</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	/// <exception cref="ArgumentNullException">The rule cannot be null</exception>
	public bool UpdatedRule_Feed(int lookupid, RuleSourceValueDTO rule)
	{
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  NpgsqlCommand command = null;
	  NpgsqlTransaction sqlTrans = null;

	  try
	  {

		if (rule == null)
		{
		  throw new ArgumentNullException("The Rule SourveValue DTO cannot be null.");
		}

		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}
		command = currentConnection.CreateCommand();
		sqlTrans = currentConnection.BeginTransaction();
		command.Transaction = sqlTrans;

		command.CommandText = "SELECT Count(*) FROM " + TableNames.Rules + " WHERE " + RulesColumns.RuleLookupID + " = @RuleLookupID";
		AddParameter(command, NpgsqlDbType.Integer, "RuleLookupID", lookupid);

		DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);

		long count = (long)command.ExecuteScalar();

		if (count > 0)
		{
		  command.CommandText = "UPDATE " + TableNames.Rules +
			" SET " +
			RulesColumns.FeedLookupIDs + " = @FeedLookupIDes" +
			" WHERE " +
			RulesColumns.RuleLookupID + " = @RuleLookupID";

		  List<int> FeedLookupIDes = new List<int>();

		  foreach (SourceValueDTO sv in rule.Feeds)
		  {
			FeedLookupIDes.Add(DEUtilities.ComputeHash(new List<string> { sv.ID, sv.Value }));
		  }

		  AddParameter(command, NpgsqlDbType.Array | NpgsqlDbType.Integer, "FeedLookupIDes", FeedLookupIDes.ToArray());

		  DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
		  int temp = command.ExecuteNonQuery();
		  sqlTrans.Commit();
		  wasSuccessful = true;
		}
	  }
	  catch (ArgumentNullException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
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
		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		CloseConnection(currentConnection);
	  }
	  return wasSuccessful;
	}

	/// <summary>
	/// Adds a new feed to the rule
	/// </summary>
	/// <param name="lookupid">Rule Lookup ID</param>
	/// <param name="feedid">Feed ID</param>
	/// <param name="feedvalue">Feed Value</param>
	/// <returns>Success or failure</returns>
	/// <exception cref="Exception">An error occurred when updating the rule</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	/// <exception cref="ArgumentNullException">The feedID and feedValue cannot be null</exception>
	/// <exception cref="ArgumentException">The lookupID does not point to a rule in the feed table</exception>
	public bool UpdateRuleFeed(int lookupid, string feedid, string feedvalue)
	{
	  NpgsqlCommand cmd = null;
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
  
	  try
	  {
		if (feedid == null || feedvalue == null)
		{
		  throw new ArgumentNullException("The feedID cannot be null");
		}

		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		cmd = currentConnection.CreateCommand();

		cmd.CommandText = "SELECT " + RulesColumns.FeedLookupIDs + " FROM " + TableNames.Rules + " WHERE " + RulesColumns.RuleLookupID + " = @RuleLookupID";
		AddParameter(cmd, NpgsqlDbType.Integer, "RuleLookupID", lookupid);

		DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);

		NpgsqlDataReader reader = cmd.ExecuteReader();
		bool readFeed = false;

		if (reader.Read())
		{
		  int[] FeedLookupIDesArray = (int[])reader[RulesColumns.FeedLookupIDs];
		  List<int> FeedLookupIDes = new List<int>(FeedLookupIDesArray);
		  FeedLookupIDes.Add(DEUtilities.ComputeHash(new List<string> { feedid, feedvalue }));

		  cmd.CommandText = "UPDATE " + TableNames.Rules + " SET " + RulesColumns.FeedLookupIDs + " = @FeedLookupIDes " +
							"WHERE " + RulesColumns.RuleLookupID + " = @RuleLookupID";
		  AddParameter(cmd, NpgsqlDbType.Array | NpgsqlDbType.Integer, "FeedLookupIDes", FeedLookupIDes.ToArray());

		  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		  reader.Close();
		  int temp = cmd.ExecuteNonQuery();

		  wasSuccessful = true;
		  readFeed = true;
		}

		reader.Close();

		if (!readFeed)
		{
		  throw new ArgumentException("Could not find rule with lookupID " + lookupid + " in the feed table");
		}

	  }
	  catch (ArgumentException Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }
	  return wasSuccessful;
	}

	/// <summary>
	/// Adds the source value
	/// </summary>
	/// <param name="value">The source value DTO</param>
	/// <param name="lookupID">[Out] The lookupID</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when adding the source value</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	/// <exception cref="ArgumentNullException">The source value DTO cannot be null</exception>
	/// <exception cref="InvalidOperationException">The Source Value already exists</exception>
	public bool CreatedSourceValue(SourceValueDTO value, out int? lookupID)
	{
	  bool wasSuccessful = false;
	  int LocalLookupID = -1;
	  int sourceID = -1;

	  try
	  {

		if (value == null)
		{
		  throw new ArgumentNullException("The SourceValue DTO cannot be null.");
		}

		LocalLookupID = DEUtilities.ComputeHash(new List<string> { value.ID, value.Value });
		sourceID = DEUtilities.ComputeHash(new List<string> { value.ID });

		if (AddedSourceValue(LocalLookupID, sourceID, value.ID, value.Value))
		{
		  wasSuccessful = true;
		  lookupID = LocalLookupID;
		}
		else
		{
		  lookupID = null;
		}

	  }
	  catch (ArgumentNullException Ex)
	  {
		lookupID = null;
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (InvalidOperationException Ex)
	  {
		lookupID = null;
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (NpgsqlException Ex)
	  {
		lookupID = null;
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		throw;
	  }
	  catch (Exception Ex)
	  {
		lookupID = null;
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);

		throw new Exception(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), Ex);
	  }

	  return wasSuccessful;
	}

	#region Helpers

	/// <summary>
	/// Adds a new rule to the rules table without any federation Uri
	/// </summary>
	/// <param name="elementName">DE Element Name</param>
	/// <param name="ruleLookupID">Rule Lookup ID</param>
	/// <param name="ruleID">Rule ID</param>
	/// <param name="ruleValue">Rule Value</param>
	/// <param name="feeds">Feed lookup id array</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	private bool AddedRule(string elementName, int ruleLookupID, string ruleID, string ruleValue, int[] feeds)
	{
	  bool wasSuccessful = false;
	  bool readSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  NpgsqlCommand command = null;
	  NpgsqlTransaction sqlTrans = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		command = currentConnection.CreateCommand();
		sqlTrans = currentConnection.BeginTransaction();
		command.Transaction = sqlTrans;

		NpgsqlDataReader reader = this.SelectThisRule(ruleLookupID, command);
		readSuccessful = reader.Read();
		reader.Close();

		// If the rule was found update it, otherwise add it
		if (readSuccessful)
		{
		  command.Parameters.Clear();
		  command.Transaction = sqlTrans;
		  command.CommandText = "UPDATE " + QualifiedTableName(TableNames.Rules) +
			" SET " +
			RulesColumns.FeedLookupIDs + " = @FeedLookupIDs" +
			" WHERE " +
			RulesColumns.RuleLookupID + " = @RuleLookupID";

		  AddParameter(command, NpgsqlDbType.Integer, "RuleLookupID", ruleLookupID);
		  AddParameter(command, NpgsqlDbType.Array | NpgsqlDbType.Integer, "FeedLookupIDs", feeds);

		  DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
		  command.ExecuteNonQuery();

		  wasSuccessful = true;
		}
		else 
		{
		  wasSuccessful = AddedRule(elementName, ruleLookupID, ruleID, ruleValue, feeds, new string[0], command);
		}

		sqlTrans.Commit();
	  }
	  catch (Exception Ex)
	  {
		RollBackTransaction(sqlTrans);
		throw;
	  }
	  finally
	  {
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	#endregion

	#endregion

	#region Delete

	/// <summary>
	/// Removes Feed lookup ids from a rule
	/// </summary>
	/// <param name="lookupid">Rule lookup id Id</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when deleting the rule feed</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	public bool DeletedRuleFeed(int lookupid)
	{
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  NpgsqlTransaction sqlTrans = null;
	  NpgsqlCommand cmd = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		sqlTrans = currentConnection.BeginTransaction();
		cmd = currentConnection.CreateCommand();
		cmd.Transaction = sqlTrans;

		NpgsqlDataReader reader = this.SelectThisRule(lookupid, cmd);
		int[] FeedLookupIDes;
		List<int> lstFeed = null;

		if (reader.Read())
		{
		  //Read the array of feed lookup ids for this rule
		  FeedLookupIDes = reader[RulesColumns.FeedLookupIDs] as int[];
		  lstFeed = new List<int>(FeedLookupIDes);
		}
		reader.Close();

		if (lstFeed == null)
		{
		  //Nothing to delete, the list of feed lookup ids is empty
		  wasSuccessful = true;
		}
		else if (lstFeed != null)
		{
		  //success, found the feed lookup ids
		  NpgsqlCommand updateCmd = currentConnection.CreateCommand();
		  updateCmd.Transaction = sqlTrans;

		  updateCmd.CommandText = "UPDATE " + TableNames.Rules +
			" SET " +
			RulesColumns.FeedLookupIDs + " = @FeedLookupIDes" +
			" WHERE " +
			RulesColumns.RuleLookupID + " = @RuleLookupID";

		  AddParameter(updateCmd, NpgsqlDbType.Integer, "RuleLookupID", lookupid);
		  AddParameter(updateCmd, NpgsqlDbType.Array | NpgsqlDbType.Integer, "FeedLookupIDes", (new List<int>(0)).ToArray());

		  DEUtilities.LogMessage(updateCmd.CommandText, DEUtilities.LogLevel.Debug);
		  int temp = updateCmd.ExecuteNonQuery();

		  // Delete the FeedLookupIDes from the table as well.
		  foreach (int FeedLookupID in lstFeed)
		  {
			NpgsqlCommand findCommand = currentConnection.CreateCommand();
			findCommand.Transaction = sqlTrans;
			findCommand.CommandText = "SELECT * FROM " + TableNames.Feeds + " WHERE " + FeedsColumns.FeedLookupID + " =@FeedLookupID";
			AddParameter(findCommand, NpgsqlDbType.Integer, "FeedLookupID", FeedLookupID);
			DEUtilities.LogMessage(findCommand.CommandText, DEUtilities.LogLevel.Debug);
			NpgsqlDataReader read = findCommand.ExecuteReader();

			if (read.Read())
			{
			  read.Close();
			  NpgsqlCommand deleteCommand = currentConnection.CreateCommand();
			  deleteCommand.Transaction = sqlTrans;
			  deleteCommand.CommandText = "DELETE FROM " + TableNames.Feeds + " WHERE " + FeedsColumns.FeedLookupID + " =@FeedLookupID";
			  AddParameter(cmd, NpgsqlDbType.Integer, "FeedLookupID", FeedLookupID);
			  int result = deleteCommand.ExecuteNonQuery();
			}
			else
			{
			  read.Close();
			  DEUtilities.LogMessage(FeedLookupID + " is not a valid value for a FeedLookupID in the Feeds table.", DEUtilities.LogLevel.Debug);
			}
		  }
		  wasSuccessful = true;
		}
		sqlTrans.Commit();
	  }
	  catch (NpgsqlException Ex)
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
		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		CloseConnection(currentConnection);
	  }
	  return wasSuccessful;
	}

	/// <summary>
	/// Tries to delete the SourceValue from the SourceValue table with the matching sourceID
	/// </summary>
	/// <param name="lookupID">The lookupID of the source value to remove</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when deleting the source value</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	public bool DeletedSourceValue(int lookupID)
	{
	  NpgsqlCommand cmd = null;
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}
		cmd = currentConnection.CreateCommand();

		cmd.CommandText = "DELETE FROM " + TableNames.SourceValues + " WHERE " + SourceValueColumns.FeedLookupID + " = @lookupID";
		AddParameter(cmd, NpgsqlDbType.Integer, "lookupID", lookupID);

		DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		int temp = cmd.ExecuteNonQuery();

		wasSuccessful = true;

	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	#endregion

	#endregion

	#region Values Controller

	#region Get

	/// <summary>
	/// Gets all of the Source Values
	/// </summary>
	/// <param name="lstRules">[Out] List of source values found</param>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when </exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	public bool ReadSourceValue_AllItems(out List<SourceValueDTO> lstValues)
	{
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;
	  lstValues = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		NpgsqlDataReader reader = this.SelectAllSourceValues(currentConnection);
		bool failedSourceValueRead = false;

		lstValues = new List<SourceValueDTO>();

		while (reader.Read())
		{
		  SourceValueDTO rule = this.ReadSourceValueRow(reader);

		  if (rule == null)
		  {
			failedSourceValueRead = true;
			break;
		  }

		  lstValues.Add(rule);
		}

		wasSuccessful = !failedSourceValueRead;
		reader.Close();
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	#region Helpers

	/// <summary>
	/// Select all of the Source Values
	/// </summary>
	/// <param name="currentConnection">The SQL connection</param>
	/// <returns>The DataReader</returns>
	private NpgsqlDataReader SelectAllSourceValues(NpgsqlConnection currentConnection)
	{
	  NpgsqlCommand cmd = currentConnection.CreateCommand();

	  cmd.CommandText = "SELECT * FROM " + TableNames.SourceValues;

	  DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
	  NpgsqlDataReader reader = cmd.ExecuteReader();

	  return reader;
	}

	#endregion

	#endregion

	#region Delete

	/// <summary>
	/// Deletes all of the Source Values
	/// </summary>
	/// <returns>Success or Failure</returns>
	/// <exception cref="Exception">An error occurred when deleting all of the source values</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	public bool DeletedAllSourceValues()
	{
	  NpgsqlCommand cmd = null;
	  bool wasSuccessful = false;
	  NpgsqlConnection currentConnection = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}

		cmd = currentConnection.CreateCommand();
		cmd.CommandText = "DELETE FROM " + TableNames.SourceValues;

		DEUtilities.LogMessage(cmd.CommandText, DEUtilities.LogLevel.Debug);
		int temp = cmd.ExecuteNonQuery();

		wasSuccessful = true;
	  }
	  catch (NpgsqlException Ex)
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
		CloseConnection(currentConnection);
	  }

	  return wasSuccessful;
	}

	#endregion

	#endregion

	#region Deletion Service

	/// <summary>
	/// Will expire all the stale DE's.  Uses Content's ExpiresTime as DE expiration.
	/// </summary>
	/// <returns>Number of Stale DE messages Expired</returns>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	
	public int ExpireStaleDEs()
	{
	  List<DEv1_0> staleDEs = new List<DEv1_0>();
	  NpgsqlConnection currentConnection = null;
	  NpgsqlCommand command = null;
	  NpgsqlTransaction sqlTrans = null;

	  try
	  {
		// test to see if we need to open the connection and setup the transaction
		currentConnection = GetDatabaseConnection();

		if (OpenConnection(currentConnection) == false)
		{
		  return 0;
		}

		sqlTrans = currentConnection.BeginTransaction();
		command = currentConnection.CreateCommand();
		command.Transaction = sqlTrans;

		// Getting stale DE messages       
	    staleDEs = getDEsByExpiration(DateTime.UtcNow, command);

		// Expiring stale DE messages
		if (staleDEs.Count == 0)
		{
		  DEUtilities.LogMessage("No Stale Messages to expire", DEUtilities.LogLevel.Info);
		  return 0;
		}
		



	  try
	  {
		foreach (DEv1_0 message in staleDEs)
		{
		  if (!ExpiredDE(message, command))
		  {
			DEUtilities.LogMessage("Error expiring stale DE messages.", DEUtilities.LogLevel.Error);
			this.RollBackTransaction(sqlTrans);
			return 0;
		  }
		}

	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage("Error in ExpireStaleDes", DEUtilities.LogLevel.Error, Ex);
		this.RollBackTransaction(sqlTrans);
		return 0;
	  }
		

		sqlTrans.Commit();
		return staleDEs.Count;
	  }
	  catch (Exception ex)
	  {
		DEUtilities.LogMessage("Error in ExpireStaleDes", DEUtilities.LogLevel.Error, ex);
		this.RollBackTransaction(sqlTrans);
		return 0;
	  }
	  finally
	  {
		CloseConnection(currentConnection);
	  }
	}

	/// <summary>
	/// Removes all DE messages flagged for deletion
	/// </summary>
	/// <returns>Number of DE messages removed from database</returns>
	public int DeleteExpiredDEs()
	{
	  NpgsqlCommand cmd = null;
	  List<DEv1_0> retVal = new List<DEv1_0>();
	  NpgsqlConnection currentConnection = null;
	  NpgsqlTransaction sqlTrans = null;

	  // Getting DEs marked for deletion  
	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return 0;
		}

		// Find the DEs to delete
		try
		{
		  cmd = currentConnection.CreateCommand();
		  sqlTrans = currentConnection.BeginTransaction();
		  cmd.Transaction = sqlTrans;

		  string sSQL = "SELECT " + QualifiedTableName(TableNames.DE) + "." + DEColumns.DEv1_0
		  + " FROM " + QualifiedTableName(TableNames.DE) + " WHERE " + DEColumns.Delete + "= @toBeDeleted";

		  cmd.CommandText = sSQL;
		  AddParameter(cmd, NpgsqlDbType.Boolean, "toBeDeleted", true);

		  NpgsqlDataReader reader = cmd.ExecuteReader();

		  while (reader.Read())
		  {
			string deXML = (string)reader[DEColumns.DEv1_0];
			retVal.Add(DEUtilities.DeserializeDE(deXML));
		  }
		  reader.Close();

		}
		catch (Exception Ex)
		{
		  DEUtilities.LogMessage("Error when getting marked DEs", DEUtilities.LogLevel.Error, Ex);
		  return 0;
		}

		// Removing DEs
		if (retVal.Count == 0)
		{
		  DEUtilities.LogMessage("No DE messages marked for deletion", DEUtilities.LogLevel.Info);
		  return 0;
		}
		else
		{
		  try
		  {
			foreach (DEv1_0 message in retVal)
			{
			  if (!DeletedDE(message))
			  {
				RollBackTransaction(sqlTrans);
				DEUtilities.LogMessage("Error removing DE", DEUtilities.LogLevel.Error);
				return 0;
			  }
			}
		  }
		  catch (Exception Ex)
		  {
			RollBackTransaction(sqlTrans);
			DEUtilities.LogMessage("Error removing all DEs", DEUtilities.LogLevel.Error, Ex);
			return 0;
		  }
		  sqlTrans.Commit();
		}
		return retVal.Count;
	  }
	  finally
	  {
		CloseConnection(currentConnection);
	  }
	}

	#region Helper Methods

	/// <summary>  
	/// Gets DE messages with expiration dates before the given DateTime.  Uses Content's ExpiresTime as DE expiration.
	/// </summary>
	/// <param name="cutOffTime">Cut off dateTime</param>
	/// <param name="parentCommand">SQL command to use.</param>
	/// <returns>A list of DEv1_0</returns>
	private List<DEv1_0> getDEsByExpiration(DateTime cutOffTime, NpgsqlCommand parentCommand)
	{
	  List<DEv1_0> retVal = new List<DEv1_0>();

	  parentCommand.Parameters.Clear();

	  string sqlContentExpireTime = QualifiedTableName(TableNames.Content) + "." + ContentColumns.ExpiresTime;

	  string sSQL = "SELECT " + QualifiedTableName(TableNames.DE)
			  + "." + DEColumns.DEv1_0 + " FROM " + QualifiedTableName(TableNames.DE) + ", " + QualifiedTableName(TableNames.Content)
				  + " WHERE " + QualifiedTableName(TableNames.Content) + "." + ContentColumns.DELookupID
					  + " = " + QualifiedTableName(TableNames.DE) + "." + DEColumns.DELookupID;

	  sSQL = sSQL + " AND " + sqlContentExpireTime + " <= @CurrentDateTime";

	  parentCommand.CommandText = sSQL;
	  AddParameter(parentCommand, NpgsqlDbType.TimestampTZ, "CurrentDateTime", cutOffTime);

	  NpgsqlDataReader reader = parentCommand.ExecuteReader();

	  while (reader.Read())
	  {
		string deXML = (string)reader[DEColumns.DEv1_0];
		retVal.Add(DEUtilities.DeserializeDE(deXML));
	  }
	  reader.Close();
	  return retVal;
	}

	/// <summary>
	/// Expires the DE in DB if it exists. Expiring sets the _ExpiresTime to now minus one second.
	/// </summary>
	/// <param name="de">The DE object to expire</param>
	/// <param name="parentCommand">The parent SQL command (optional)</param>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	private bool ExpiredDE(DEv1_0 de, NpgsqlCommand parentCommand = null)
	{
	  int deLookupID = DEUtilities.ComputeDELookupID(de);

	  NpgsqlTransaction sqlTrans = null;
	  NpgsqlConnection currentConnection = null;
	  bool wasSuccessful = false;
	  bool hasParentCommand = parentCommand != null;
	  NpgsqlCommand command = parentCommand;

	  try
	  {
		if (!hasParentCommand)
		{
		  currentConnection = GetDatabaseConnection();
		  if (OpenConnection(currentConnection) == false)
		  {
			return wasSuccessful;
		  }

		  sqlTrans = currentConnection.BeginTransaction();
		  command = currentConnection.CreateCommand();
		  command.Transaction = sqlTrans;
		}
		else
		{
		  command.Parameters.Clear();
		}

		string sSQL = "";
		string sTable = "";
		string sColumn = "";
		string sCondition = "";
		List<int> contentLookupIDs = ReadContentLookupIDsForDE(deLookupID, command);

		foreach (int iContentLookupID in contentLookupIDs)
		{
		  sTable = QualifiedTableName(TableNames.FeedContent);
		  sColumn = FeedContentColumns.ExpiresTime;
		  sCondition = FeedContentColumns.ContentLookupID + " = @ContentLookupID";

		  //expire the feed content first
		  sSQL = "UPDATE " + sTable + " SET " + sColumn + " = @TimeParam WHERE " + sCondition;
		  command.CommandText = sSQL;
		  AddParameter(command, NpgsqlDbType.TimestampTZ, "TimeParam", DateTime.UtcNow.AddSeconds(-1.0));
		  AddParameter(command, NpgsqlDbType.Integer, "ContentLookupID", iContentLookupID);

		  DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
		  //expire the content
		  command.ExecuteNonQuery();

		  //then expire the content
		  sTable = QualifiedTableName(TableNames.Content);
		  sColumn = ContentColumns.ExpiresTime;
		  sCondition = ContentColumns.ContentLookupID + " = @ContentLookupID";
		  sSQL = "UPDATE " + sTable + " SET " + sColumn + " = @TimeParam WHERE " + sCondition;
		  command.CommandText = sSQL;
		  //don't worry about the column, condition, or parameters, they are the same as above
		  DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
		  command.ExecuteNonQuery();
		}

		//finally expire the DE
		sTable = QualifiedTableName(TableNames.DE);
		sColumn = DEColumns.Delete;
		sCondition = DEColumns.DELookupID + " = @DELookupID";
		sSQL = "UPDATE " + sTable + " SET " + sColumn + " = 'true' WHERE " + sCondition;

		command.CommandText = sSQL;
		command.Parameters.Clear();
		AddParameter(command, NpgsqlDbType.Integer, "DELookupID", deLookupID);

		DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
		command.ExecuteNonQuery();

		if (!hasParentCommand)
		{
		  sqlTrans.Commit();
		}
		wasSuccessful = true;

	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage("Error occurred when expiring the DE", DEUtilities.LogLevel.Error, Ex);
		this.RollBackTransaction(sqlTrans);
		throw;
	  }
	  finally
	  {
		if (!hasParentCommand)
		{
		  CloseConnection(currentConnection);
		}
	  }

	  return wasSuccessful;
	}

	#endregion
	#endregion

	#region Unused Methods

	#region Unused IDbDal Methods

	/// <summary>
	/// Adds a DE message to the database
	/// </summary>
	/// <param name="de">DE message to add</param>
	/// <param name="body">String of the DE message</param>
	/// <exception cref="FederationException">Error occur when federating the DE</exception>
	
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	public bool AddedDEToCache(DEv1_0 de, string body)
	{
	  return AddedDEToCache(de, body, null);
	}

	/// <summary>
	/// Deletes the DE object from the database
	/// </summary>
	/// <param name="de">DE object</param>
	/// <returns>Success of deletion</returns>
	public bool DeletedDE(DEv1_0 de)
	{
	  return DeletedDE(DEUtilities.ComputeDELookupID(de), null);
	}
	#endregion

	/// <summary>
	/// Cancels the DE in DB if it exists
	/// </summary>
	/// <param name="de">The DE object to expire</param>
	/// <param name="parentCommand">The parent SQL command (optional)</param>
	/// <returns>Success or failure</returns>
	private bool CancelDE(DEv1_0 de, NpgsqlCommand parentCommand = null)
	{
	  bool wasSuccessful = false;

	  if (ExpiredDE(de, parentCommand))
	  {
		DEUtilities.LogMessage("Adding DE to Federation Queue", DEUtilities.LogLevel.Info);


		try
		{

		  if (!AddContentToFederationQueue(de, parentCommand))
		  {
			DEUtilities.LogMessage("Problem adding DE to Federation Queue", DEUtilities.LogLevel.Error);
		  }

		  wasSuccessful = true;
		}
		catch (Exception Ex)
		{
		  DEUtilities.LogMessage("Error occurred when adding the De to the Federation Queue", DEUtilities.LogLevel.Error, Ex);
		  throw new FederationException("Error occurred when adding the De to the Federation Queue", Ex);
		}

	  }

	  return wasSuccessful;
	}

	/// Cancels the DE in DB if it exists
	/// </summary>
	/// <param name="de">The DE object to expire</param>
	/// <returns>Success or failure</returns>
	public bool CancelDE(DEv1_0 de)
	{
	  return CancelDE(de, null);
	}

	/// <summary>
	/// Expires the DE in DB if it exists. Expiring sets the _ExpiresTime to now minus one second.
	/// </summary>
	/// <param name="de">The DE object to expire</param>
	public bool ExpiredDE(DEv1_0 de)
	{
	  return ExpiredDE(de, null);
	}

	/// <summary>
	/// Deletes the DE object from the database
	/// </summary>
	/// <param name="de">DE object</param>
	/// <param name="parentCommand">The parent SQL command (optional)</param>
	/// <returns>Success of deletion</returns>
	private bool DeletedDE(DEv1_0 de, NpgsqlCommand parentCommand = null)
	{
	  return DeletedDE(DEUtilities.ComputeDELookupID(de), parentCommand);
	}

	/// <summary>
	/// Check if the Feed Exists
	/// </summary>
	/// <param name="FeedLookupID">The feed lookupID</param>
	/// <returns>True if the feed exists, false otherwise</returns>
	private Boolean FeedExists(int FeedLookupID)
	{

	  //TODO: Not sure what this does - it always returns false
	  //NpgsqlCommand cmd = myConnection.CreateCommand();
	  //cmd.CommandText = "SELECT * FROM " + TableNames.Feeds + " WHERE " + FeedsColumns.FeedLookupID + "=@FeedLookupID";
	  //AddParameter(cmd, NpgsqlDbType.Integer, "FeedLookupID", FeedLookupID);
	  //int temp = cmd.ExecuteNonQuery();
	  return false;
	}

	/// <summary>
	/// Delete the supplied feed lookup id (id + val) from the rule
	/// </summary>
	/// <param name="lookupid">Rule lookupId</param>
	/// <param name="id">Feed Id</param>
	/// <param name="val">Feed Value</param>
	/// <returns>Success or failure</returns>
	public bool DeletedRuleFeed(int lookupid, string id, string val)
	{
	  bool wasSuccessful = false;

	  //make the feed lookup id from the feed id and feed value
	  int FeedLookupID = DEUtilities.ComputeHash(new List<string>() { id, val });

	  NpgsqlConnection currentConnection = null;
	  NpgsqlCommand command = null;
	  NpgsqlTransaction sqlTrans = null;

	  try
	  {
		currentConnection = GetDatabaseConnection();
		if (OpenConnection(currentConnection) == false)
		{
		  return wasSuccessful;
		}
		command = currentConnection.CreateCommand();
		//TODO: Not sure this needs a transaction per se
		sqlTrans = currentConnection.BeginTransaction();
		command.Transaction = sqlTrans;

		NpgsqlDataReader reader = this.SelectThisRule(lookupid, command);
		bool readSuccessful = reader.Read();

		if (readSuccessful)
		{
		  //Read the array of feed lookup ids for this rule
		  int[] feedsArray = (int[])reader[RulesColumns.FeedLookupIDs];

		  List<int> feeds = new List<int>(feedsArray);

		  reader.Close();

		  if (feeds != null && !feeds.Contains(FeedLookupID))
		  {
			//didn't find the feed lookupId
			wasSuccessful = false;
			sqlTrans.Commit();
		  }
		  else if (feeds != null)
		  {
			//success, found the feed lookupId
			feeds.Remove(FeedLookupID);

			command.Parameters.Clear();
			command.CommandText = "UPDATE " + TableNames.Rules + " SET " + RulesColumns.FeedLookupIDs + " = @FeedLookupIDes " +
							  "WHERE " + RulesColumns.RuleLookupID + " = @RuleLookupID";
			AddParameter(command, NpgsqlDbType.Integer, "RuleLookupID", lookupid);
			AddParameter(command, NpgsqlDbType.Array | NpgsqlDbType.Integer, "FeedLookupIDes", feeds.ToArray());

			DEUtilities.LogMessage(command.CommandText, DEUtilities.LogLevel.Debug);
			command.ExecuteNonQuery();
			sqlTrans.Commit();
			wasSuccessful = true;
		  }
		}
	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage("General Error", DEUtilities.LogLevel.Error, Ex);
	  }
	  finally
	  {

		if (!wasSuccessful)
		{
		  this.RollBackTransaction(sqlTrans);
		}

		CloseConnection(currentConnection);
	  }
	  return wasSuccessful;
	}

	/// <summary>
	/// Reads the list of federation Uri for a rule
	/// </summary>
	/// <param name="id">Rule id to lookup</param>
	/// <param name="value">Rule value to lookup</param>
	/// <returns>List of Federation strings</returns>
	/// <exception cref="ArgumentException">The Federation rule was not found</exception>
	public List<string> ReadFederationURIFromRule(string id, string value)
	{
	  int lookupid = DEUtilities.ComputeHash(new List<string>() { id, value });
	  RuleFedDTO rule = null;
	  List<string> retVal = new List<string>();

	  if (this.ReadRule_FedUri(lookupid, out rule))
	  {
		retVal = rule.FedURI;
	  }

	  return retVal;
	}

	/// <summary>
	/// Creates a new rule in the Rules table 
	/// </summary>
	/// <param name="elementName">DE Element Name to match</param>
	/// <param name="ruleKey">Rule Key to match</param>
	/// <param name="ruleValue">Rule Value to match</param>
	/// <param name="feeds">Feeds associated to the rule</param>
	/// <param name="federation">Federation endpoints associated to the rule</param>
	/// <param name="ruleID">Outbound rule ID</param>
	/// <returns></returns>
	public bool CreatedRule(string elementName, string ruleKey, string ruleValue, int[] feeds, string[] federation, out int ruleID)
	{
	  bool wasSuccessful = false;
	  int id = -1;
	  int tempID = DEUtilities.ComputeHash(new List<string> { ruleKey, ruleValue });
	  if (AddedRule(elementName, tempID, ruleKey, ruleValue, feeds, federation, null))
	  {
		wasSuccessful = true;
		id = tempID;
	  }

	  ruleID = id;
	  return wasSuccessful;
	}

	#endregion

	#region Not Implemented Methods

	/// <summary>
	/// Reads all DELink that has been updated since the specified time
	/// </summary>
	/// <param name="since">The cutoff for updates</param>
	/// <returns>Updated KML since the specified time</returns>
	public string ReadActiveDELink()
	{
	  throw new NotImplementedException();
	}

	/// <summary>
	/// Reads active DELinks
	/// </summary>
	/// <param name="role">Role value</param>
	/// <param name="roleURI">Role URI</param>
	/// <returns>string of xml data</returns>
	public string ReadActiveDELinkByRole(string role, string roleURI)
	{
	  throw new NotImplementedException();
	}

	/// <summary>
	/// Reads the currently active XML
	/// </summary>
	/// <returns>Currently active XML</returns>
	public string ReadActiveXML()
	{
	  throw new NotImplementedException();
	}

	/// <summary>
	/// Reads Active XML by a role
	/// </summary>
	/// <param name="role">Role name</param>
	/// <param name="roleURI">Role URI</param>
	/// <returns>string of XML data</returns>
	public string ReadActiveXMLByRole(string role, string roleURI)
	{
	  throw new NotImplementedException();
	}

	/// <summary>
	/// Reads all DELink
	/// </summary>
	/// <returns>All DELink</returns>
	public string ReadAllDELink()
	{
	  throw new NotImplementedException();
	}

	/// <summary>
	/// Reads all XML
	/// </summary>
	/// <returns>All XML</returns>
	public string ReadAllXML()
	{
	  throw new NotImplementedException();
	}

	/// <summary>
	/// Reads the XML representation of a ContentObject.
	/// </summary>
	/// <param name="ContentLookupID">The lookup id of the content object to retrieve.</param>
	/// <returns>String of XML content to return to the user.</returns>
	public string ReadContentObjectByContentLookupID(int ContentLookupID)
	{
	  throw new NotImplementedException();
	}

	/// <summary>
	/// Reads all DELink that has been updated since the specified time
	/// </summary>
	/// <param name="since">The cutoff for updates</param>
	/// <returns>Updated KML since the specified time</returns>
	public string ReadExpiredDELink()
	{
	  throw new NotImplementedException();
	}

	/// <summary>
	/// Reads active DELinks
	/// </summary>
	/// <param name="role">Role value</param>
	/// <param name="roleURI">Role URI</param>
	/// <returns>string of xml data</returns>
	public string ReadExpiredDELinkByRole(string role, string roleURI)
	{
	  throw new NotImplementedException();
	}

	/// <summary>
	/// Reads the HTML representation of a ContentObject.
	/// </summary>
	/// <param name="ContentLookupID">The lookup id of the content object to retrieve.</param>
	/// <returns>String of HTML content to return to the user.</returns>
	public string ReadHTMLByContentLookupID(int ContentLookupID)
	{
	  throw new NotImplementedException();
	}

	/// <summary>
	/// Reads all DELink that has been updated since the specified time
	/// </summary>
	/// <param name="since">The cutoff for updates</param>
	/// <returns>Updated KML since the specified time</returns>
	public string ReadUpdatedDELink(DateTime since)
	{
	  throw new NotImplementedException();
	}

	/// <summary>
	/// Reads active DELinks
	/// </summary>
	/// <param name="role">Role value</param>
	/// <param name="roleURI">Role URI</param>
	/// <returns>string of xml data</returns>
	public string ReadUpdatedDELinkByRole(DateTime since, string role, string roleURI)
	{
	  throw new NotImplementedException();
	}

	/// <summary>
	/// Queries the database to see if the specified message is the latest message
	/// </summary>
	/// <param name="senderID">SenderID of the DE Message</param>
	/// <param name="distributionID">DistributionID of the DE Message</param>
	/// <param name="distributionTime">DistributionTime of the DE Message</param>
	/// <returns>True if the message is the latest (e.g. ok to delete other messages)</returns>
	public bool IsLatestDE(string senderID, string distributionID, DateTime distributionTime)
	{
	  throw new NotImplementedException();
	}



	#endregion


























  }
}
