using EMS.EDXL.DE.v1_0;
using Fresh.Global;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Runtime.CompilerServices;
using log4net;

namespace Fresh.PostGIS
{
  /// <summary>
  /// Data Access Layer for the Archive DB
  /// </summary>
  public class ArchiveDAL
  {
    private NpgsqlConnectionStringBuilder connectionStringBuilder;
    private string schemaName;
    private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


    public ArchiveDAL()
    {
      Log.Debug("Archive DAL default ctor");
    }

    public ArchiveDAL(string _connStr, string _schema)
    {
      this.schemaName = _schema;
      connectionStringBuilder = new NpgsqlConnectionStringBuilder(_connStr);
      connectionStringBuilder.SearchPath = this.schemaName + ",public";
      Log.Debug("Archive DB Connection String: " + connectionStringBuilder);
    }

    /// <summary>
    /// Gets a database connection
    /// </summary>
    /// <returns>A database connection.</returns>
    public NpgsqlConnection GetDatabaseConnection()
    {
      NpgsqlConnection connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
      return connection;
    }

    /// <summary>
    /// Closes a database connection
    /// </summary>
    /// <param name="connection">The connection to close.</param>
    /// <returns>Success or Failure</returns>
    public bool CloseConnection(NpgsqlConnection connection)
    {
      if (connection != null)
      {
        try
        {
          connection.Close();
          Log.Debug("Archive DB connection closed");
          return true;
        }
        catch (Exception exClose)
        {
          Log.Error("Connection Close Error in archive db", exClose);
          return false;
        }
      }
      else
      {
        Log.Error("Attempted to Close a null archive database connection.");
        return false;
      }
    }

    /// <summary>
    /// Opens a database connection
    /// </summary>
    /// <param name="connection">The connection to open</param>
    /// <param name="caller">The calling method for error reporting</param>
    /// <returns>Success or Failure</returns>
    private bool OpenConnection(NpgsqlConnection connection, [CallerMemberName] string caller = "")
    {
      try
      {
        connection.Open();
        Log.Debug("Archive DB connection opened");
        return true;
      }
      catch (Exception exp)
      {
        Log.Error("Open archive db Connection Error in " + caller, exp);
        return false;
      }
    }

    /// <summary>
    /// Archives a DE Message
    /// </summary>
    /// <param name="de">DE message to archive</param>
    /// <param name="sourceip">Source IP Address of the message</param>
    public bool ArchiveDE(DEv1_0 de, string sourceip)
    {
      bool wasSuccessful = false;
      NpgsqlCommand command = null;
      NpgsqlTransaction sqlTrans = null;
      NpgsqlConnection currentConnection = null;

      try
      {
        currentConnection = GetDatabaseConnection();
        if (OpenConnection(currentConnection) == false)
        {
          return wasSuccessful;
        }
        sqlTrans = currentConnection.BeginTransaction();
        command = currentConnection.CreateCommand();
        command.Transaction = sqlTrans;

        //add DE first
        //assumes calling function will wrap this in a try catch block
        //allows the error to be thrown to the calling function
        int iHash = DEUtilities.ComputeDELookupID(de);

        string sTable = QualifiedTableName(TableNames.MessageArchive);
        string sColumns = MessageArchiveColumns.DELookupID + "," + MessageArchiveColumns.DistributionID + "," + MessageArchiveColumns.SenderID + "," +
              MessageArchiveColumns.DateTimeSent + "," + MessageArchiveColumns.SenderIP + "," + MessageArchiveColumns.DateTimeLogged + "," + MessageArchiveColumns.DE;
        command.CommandText = "INSERT INTO " + sTable + " (" + sColumns + ") VALUES (@DEHash, @DistributionID, @SenderID, @DateTimeSent, @SourceIP, @DateTimeLogged, @DEv1_0)";
        command.Parameters.Clear();
        AddParameter(command, NpgsqlDbType.Integer, "DEHash", iHash);
        AddParameter(command, NpgsqlDbType.Text, "DistributionID", de.DistributionID);
        AddParameter(command, NpgsqlDbType.Text, "SenderID", de.SenderID);
        AddParameter(command, NpgsqlDbType.TimestampTZ, "DateTimeSent", de.DateTimeSent);
        AddParameter(command, NpgsqlDbType.Text, "SourceIP", sourceip);
        AddParameter(command, NpgsqlDbType.TimestampTZ, "DateTimeLogged", DateTime.UtcNow);
        AddParameter(command, NpgsqlDbType.Xml, "DEv1_0", de.ToString());
        Log.Debug(command.CommandText);
        command.ExecuteNonQuery();
        sqlTrans.Commit();
        wasSuccessful = true;
      }
      catch (Exception Ex)
      {
        Log.Error("General Error in AddedDEToCache()", Ex);
        this.WasRolledBackTransaction(sqlTrans);
      }
      finally
      {
        CloseConnection(currentConnection);
      }

      return wasSuccessful;
    }

    /// <summary>
    /// Attempts to rollback an active transaction
    /// </summary>
    /// <param name="sqlTrans">Transaction</param>
    /// <returns>Success or Failure</returns>
    public bool WasRolledBackTransaction(NpgsqlTransaction sqlTrans)
    {
      bool wasSuccessful = false;

      try
      {
        //rollback if transaction started
        if (sqlTrans != null)
        {
          sqlTrans.Rollback();
          wasSuccessful = true;
        }
      }
      catch (Exception exRollback)
      {
        Log.Error("Archive Transaction Rollback Error", exRollback);
      }
      return wasSuccessful;
    }

    /// <summary>
    /// Returns qualified table name with schema
    /// </summary>
    /// <param name="tableName">Table Name</param>
    /// <returns>Schema.TableName</returns>
    public string QualifiedTableName(string tableName)
    {
      return this.schemaName + "." + tableName;
    }
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
  }
}