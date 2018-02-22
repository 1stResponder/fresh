using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Configuration;
using System.Threading;
using System.Net.Cache;
using EMS.EDXL.DE.v1_0;
using System.Xml.Serialization;
using Fresh.Global;
using System.Web;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace Fresh.Federation.Controllers
{
    /// <summary>
    /// Class:    FederationController.cs
    /// Project:  Fresh.Federation
    /// Purpose:  The Federation Controller class, which is used by client to 
    ///           request federation of DE information.
    ///           
    /// Created:  2016-07-13
    /// Author:   Marc Stogner
    /// 
    /// Updates:  none
    /// </summary>
    [RoutePrefix("api")]
    public class FederationController : ApiController
    {

        #region Private Data Members

        /// <summary>
        /// Log4net logging object
        /// </summary>
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int federationConnectionRetryAttempts = Int16.Parse(ConfigurationManager.AppSettings["FederationConnectionRetryAttempts"]);
        private static Dictionary<string, DateTime> unreachableURI;

        #endregion
        #region Constructor
        /// <summary>
        /// Default constructor for Federation Controller
        /// </summary>
        public FederationController()
        {
            if (unreachableURI == null)
            {
                unreachableURI = new Dictionary<string, DateTime>();
            }
        }
        #endregion

        #region Endpoints
        //TODO Remove after testing.
        // POST: api/TimeMeOut
        /// <summary>
        /// A method to test federation request timeouts
        /// </summary>
        /// <param name="federationRequest">DTO containing the DE message and URIs to send it to.</param>
        /// <returns>Federation success or failure message.</returns>
        [Route("TimeMeOut")]
        [HttpPost]
        public IHttpActionResult TimeMeOut([FromBody] FederationRequestDTO federationRequest)
        {
            Thread.Sleep(10000);
            return this.StatusCode(HttpStatusCode.Accepted);
        }

        // POST: api/FederationRequest
        /// <summary>
        /// Makes a request to Federate the DE message to the specified URIs
        /// </summary>
        /// <param name="federationRequest">DTO containing the DE message and URIs to send it to.</param>
        /// <returns>Federation success or failure message.</returns>
        [Route("FederationRequest")]
        [HttpPost]
        public IHttpActionResult FederationRequest([FromBody] FederationRequestDTO federationRequest)
        {
            // If the request is null
            if (federationRequest == null)
            {
                logger.Warn("Empty federation request");
                return (Content(HttpStatusCode.BadRequest, "Empty federation request"));
            }

            logger.Debug(string.Format("Received Federation Request: {0}", federationRequest.ToXMLString()));
            DEv1_0 de = DEUtilities.DeserializeDE(federationRequest.DEXMLElement.ToString());

            FederateDE(de, federationRequest.FedURIs);
            return this.StatusCode(HttpStatusCode.Accepted);
        }

        #endregion
        #region Private Methods

        private void FederateDE(DEv1_0 de, List<string> fedUris)
        {
            //TODO:figure out how to federate all DE
            foreach (string uri in fedUris)
            {
                // Federating the URL only if its valid
                if(IsValid(uri))
                {
                    Task.Run(() => DoPost(new Uri(uri), de));
                }
            }
        }

        /// <summary>
        /// Forwards the DE to another webserver.
        /// </summary>
        /// <param name="requesturi">The location to where the DE should be forwarded.</param>
        /// <param name="distributionElement">The DE to be forwarded</param>
        private void DoPost(Uri requesturi, DEv1_0 distributionElement)
        {
            int numRetries = 0;
            bool deliveryFailed = false;

            do
            {
                logger.Debug(string.Format("Attempting to federate the message to {0}.  This is the attempt #{1}", requesturi, numRetries + 1));

                if (deliveryFailed)
                {
                    Thread.Sleep(1000 * 5);
                    deliveryFailed = false;
                }

                try
                {
                    HttpWebRequest request;

                    string s = distributionElement.ToString();
                    s = s.Replace("<?xml version=\"1.0\" encoding=\"utf-16\" standalone=\"no\"?>\r\n", String.Empty);
                    request = (HttpWebRequest)WebRequest.Create(requesturi);
                    //request.Timeout = 7000;
                    request.KeepAlive = true;
                    request.Method = "POST";
                    request.ContentType = "text/xml";
                    request.AllowAutoRedirect = true;
                    request.ContentLength = Encoding.UTF8.GetByteCount(s);
                    logger.Debug("The Post requesturi is: " + requesturi.ToString());
                    // HACK HACKITY HACK HACK HACK
                    if (requesturi.ToString() == "https://c2crouter.nics.ll.mit.edu/api/de")
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        X509Certificate2 clientCert = new X509Certificate2("C:\\public\\ArdentAWS.pfx", "ArdentAWS");
                        request.ClientCertificates.Add(clientCert);
                        request.PreAuthenticate = true;
                        request.Credentials = CredentialCache.DefaultCredentials;
                        request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                    }
                    this.SetBody(request, s);

                    HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
                    resp.Close();
                }
                catch (WebException e)
                {
                    deliveryFailed = true;

                    logger.Info("TLS Info: " + System.Net.ServicePointManager.SecurityProtocol.ToString());
                    logger.Error("Error POSTing to " + requesturi + ": " + e.Message);
                    logger.Error("Stacktrace: " + e.StackTrace);
                    logger.Error("Err: " + e.ToString());
                    if (e.InnerException != null)
                    {
                        logger.Error("Inner: " + e.InnerException.Message);
                    }

                    logger.Error("NumRetries: " + numRetries);
                }
            }
            while (deliveryFailed && (numRetries++ < federationConnectionRetryAttempts));

            // If the message was never able to be delivered successfully, add it to the unreachable URI map
            if(deliveryFailed)
            {
                //TODO: Change this to add hour 1 after testing
                unreachableURI.Add(requesturi.ToString(), DateTime.Now.AddHours(1));
                logger.Error("Failed to federate message to " + requesturi.ToString());
            } else
            {
                logger.Debug("Message was federated successfully to " + requesturi.ToString());
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

        /// <summary>
        /// Checks if the string is a valid URL for the Federation Service
        /// </summary>
        /// <remarks>
        /// A valid URL string is one which is a properly formatted URL, does not point to this webapp, 
        /// and has not been marked unreachable.
        /// A URL is marked unreadable if a federation request failed to deliver to it in the past hour.
        /// </remarks>
        /// <param name="urlString"></param>
        /// <returns></returns>
        private bool IsValid(string urlString)
        {
            bool isValidURL = true;

            try
            {
                // Checking that the string is not null or empty
                if(String.IsNullOrWhiteSpace(urlString))
                {
                    logger.Error("urlString is null or whitespace!");
                    isValidURL = false;
                    return isValidURL;
                }

                // Checking if the URL points to this WebApp
                Uri baseURI = new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority)); // holds URI for current instance of Federation

                if (baseURI.Authority == new Uri(urlString).Authority)
                {
                    logger.Error("The URL string is pointing to this application.  This URL string will be ignored.");
                    isValidURL = false;
                    return isValidURL;
                }

                // Checking if the URL has been marked unreachable
                if(unreachableURI.ContainsKey(urlString))
                {
                    DateTime expiration = unreachableURI[urlString];
                    
                    if(DateTime.Compare(expiration, DateTime.Now) <= 0) // If the expiration date time has been reached
                    {
                        // Remove from unreachable dictionary and reattempt federate
                        logger.Debug(string.Format("Unreachable status has expired for {0}.  Will attempt to federate.", urlString));
                        unreachableURI.Remove(urlString);
                    }
                    else // If the expiration is later
                    {
                        logger.Error(urlString + " is marked as unreachable");
                        isValidURL = false;
                    }
                }

            } catch (UriFormatException e)
            {
                // Wrong format for URL
                logger.Error(string.Format("The URL string: {0} is in an invalid format. This URL string will be ignored. {1}", urlString, e.Message));
                isValidURL = false;

            } catch (Exception e)
            {
                // Other errors
                logger.Error(string.Format("An error occurred when checking the URL string: {0}. This URL string will be ignored. {1}", urlString, e.Message));
                isValidURL = false;
            }

            return isValidURL;     
        }

        #endregion
    }
}
