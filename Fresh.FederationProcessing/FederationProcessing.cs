// ———————————————————————–
// <copyright file="FederationProcessing.cs" company="The MITRE Corporation">
//    Copyright (c) 2010 The MITRE Corporation. All rights reserved.
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
/////////////////////////////////////////////////////////////////////////////////////////////////
// FederationProcessing.cs - Service implementation that pulls DE messages from a SQS queue and writes
//                   FederationObjects to DynamoDB
// Project: EDXLSharp_AWSRouter- FederationProcessing
//
// Language:    C#, .NET 4.0
// Platform:    Windows 7, VS 2013
// Author:      Don McGarry The MITRE Corporation
/////////////////////////////////////////////////////////////////////////////////////////////////

// Copyright (c) 2010 The MITRE Corporation. All rights reserved.
//
// NOTICE
// This software was produced for the U. S. Government
// under Contract No. FA8721-09-C-0001, and is
// subject to the Rights in Noncommercial Computer Software
// and Noncommercial Computer Software Documentation Clause
// (DFARS) 252.227-7014 (JUN 1995)


using Amazon.SQS;
using Amazon.SQS.Model;
using EDXLSharp.EDXLDELib;
using Fresh.PostGIS;
using Fresh.Global;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.ServiceProcess;
using System.Threading;
using System.Globalization;
using EDXLSharp;
using System.Net;
using System.Text;
using System.IO;


// Configure log4net using the .config file

[assembly: log4net.Config.XmlConfigurator(Watch = true)]


namespace FederationProcessing
{
    /// <summary>
    /// Class for the Federation Processing Windows Service
    /// </summary>
    public partial class FederationProcessing : ServiceBase
    {
        /// <summary>
        /// Log4net logging object
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Thread to run the SQS polling on
        /// </summary>
        private Thread runThread;

        /// <summary>
        /// Amazon SQS configuration object 
        /// </summary>
        private AmazonSQSConfig amazonSQSConfig;

        /// <summary>
        /// Amazon SQS Client
        /// </summary>
        private AmazonSQSClient amazonSQSClient;
        /// <summary>
        /// Access to DAL methods
        /// </summary>
        /// 
        /// <summary>
        /// Initializes a new instance of the FederationProcessing class
        /// </summary>
        private PostGISDAL dbDal;
        

        public FederationProcessing()
        {
            InitializeComponent();
            this.ServiceName = "FederationProcessing Service";
        }

        /// <summary>
        /// Method called when service is started
        /// </summary>
        /// <param name="args">Command Arguments</param>
        protected override void OnStart(string[] args)
        {
            Log.Info("OnStart");
            this.amazonSQSConfig = new AmazonSQSConfig();
            this.amazonSQSConfig.ServiceURL = AWSConstants.FederationProcessingQueueUrl;
            this.amazonSQSConfig.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(AWSConstants.AWSRegion);
            this.amazonSQSClient = new AmazonSQSClient(this.amazonSQSConfig);
            this.dbDal = new PostGISDAL(ConfigurationManager.ConnectionStrings["FRESH.PostGIS"].ConnectionString, ConfigurationManager.AppSettings["PostGISSchema"], "deprecated");//TODO: once all needed functions written, change to IDatabaseDAL and set to appropriate DB dal
            this.runThread = new Thread(new ThreadStart(this.PollSQS));
            this.runThread.IsBackground = true;
            this.runThread.Start();
            Log.Info("OnStartComplete");
        }

        /// <summary>
        /// Method called when service is stopped
        /// </summary>
        protected override void OnStop()
        {
            //Log.Info("OnStop");
            if (this.runThread.IsAlive)
            {
                //Log.Info("KillingThread");
                this.runThread.Abort();
                //Log.Info("ThreadKilled");
            }
        }

        /// <summary>
        /// Thread worker method
        /// </summary>
        private void PollSQS()
        {
            Log.Info("InThreadSQS");
            while (true)
            {
                Log.Info("SQSLoop");
                ReceiveMessageRequest req = new ReceiveMessageRequest();
                req.QueueUrl = AWSConstants.FederationProcessingQueueUrl;
                Log.Info("Federation URL: " + req.QueueUrl);
                req.WaitTimeSeconds = AWSConstants.FederationProcessing.PollIntervalInSeconds;

               ReceiveMessageResponse resp = this.amazonSQSClient.ReceiveMessage(req);
               Log.Info("Got " + resp.Messages.Count + " messages from SQS");
                foreach (Message m in resp.Messages)
                {
                    try
                    {
                        Log.Info(m.Body);
                        string queueBody = m.Body;
                        int hash = -1;
                        JObject json = JObject.Parse(queueBody);

                        // Get the deHash from the JSON
                        hash = Int32.Parse(json.GetValue("DEHash").ToString());
                        Log.Info(hash);


                        EDXLDE de = dbDal.ReadDE(hash);
                        Log.Info(de.WriteToXML());
                        // Get the URIs from the JSON
                        string[] fedUris = json.GetValue("FederationURIs").ToObject<string[]>();
                        Log.Info(fedUris);

                        switch (de.DistributionType)
                        {
                            case TypeValue.Report:
                            case TypeValue.Update:

                                FederateDE(de, fedUris);
                                break;
                            case TypeValue.Cancel:
                                break;
                            default:
                                Log.Error("Got unexpected distributiontype");
                                break;
                        }
                        DeleteMessageRequest deleteMessageRequest = new DeleteMessageRequest();
                        deleteMessageRequest.QueueUrl = req.QueueUrl;
                        deleteMessageRequest.ReceiptHandle = m.ReceiptHandle;
                        try
                        {
                            DeleteMessageResponse sqsdelresp = this.amazonSQSClient.DeleteMessage(deleteMessageRequest);
                            if (sqsdelresp.HttpStatusCode == System.Net.HttpStatusCode.OK)
                            {
                                Log.Info("Message removed from queue");
                            }
                            else
                            {
                                Log.Error("SQS Delete error: " + sqsdelresp.HttpStatusCode.ToString());
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error("Error deleting from SQS", e);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }

            }
        }

        private void FederateDE(EDXLDE de, string[] fedUris)
        {
            //TODO:figure out how to federate all DE
            //TODO:build unique list of endpoints in case more than one match to same destination
            foreach (string uri in fedUris)
            {
                DoPost(new Uri(uri), de);
            }

        }

        /// <summary>
        /// Forwards the DE to another webserver.
        /// </summary>
        /// <param name="requesturi">The location to where the DE should be forwarded.</param>
        /// <param name="distributionElement">The DE to be forwarded</param>
        private void DoPost(Uri requesturi, EDXLDE distributionElement)
        {
            try
            {
                HttpWebRequest request;
                HttpWebResponse resp;
                string s = distributionElement.WriteToXML();
                s = s.Replace("<?xml version=\"1.0\" encoding=\"utf-16\" standalone=\"no\"?>\r\n", String.Empty);
                request = (HttpWebRequest)WebRequest.Create(requesturi);
                request.KeepAlive = true;
                request.Method = "POST";
                request.ContentType = "text/xml";
                request.AllowAutoRedirect = true;
                request.ContentLength = Encoding.UTF8.GetByteCount(s);
                this.SetBody(request, s);
                resp = null;
                resp = (HttpWebResponse)request.GetResponse();
                resp.Close();
            }catch(Exception e)
            {
                Log.Error("Error POSTing to " + requesturi+ ": " + e.Message);
            }
        }

        /// <summary>
        /// Function to perform HTTP POST
        /// </summary>
        /// <param name="request">Pending Web Request</param>
        /// <param name="requestBody">Body to POST</param>
        private void SetBody(HttpWebRequest request, string requestBody)
        {
            if (requestBody.Length > 0)
            {
                Stream requestStream = request.GetRequestStream();
                StreamWriter writer = new StreamWriter(requestStream);
                writer.AutoFlush = true;
                writer.Write(requestBody);
                writer.Flush();
                writer.Close();
                requestStream.Flush();
                requestStream.Close();
            }
        }
    }
}
