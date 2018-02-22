using Fresh.PostGIS;
using System;
using System.Configuration;
using System.ServiceProcess;
using System.Threading;

// Configure log4net using the .config file
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace StaleDeDeletionService
{
  partial class DeDeletionService : ServiceBase
    {
        /// <summary>
        /// Log4net logging object
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        /// Access to DAL methods
        /// </summary>
        private PostGISDAL dbDal;
        /// <summary>
        /// Timer used to schedule the clean up
        /// </summary>
        private Timer deletionTimer;

        public DeDeletionService()
        {     
            InitializeComponent();
            this.CanStop = true;           
        }

        protected override void OnStart(string[] args)
        {
            string currentUTCTime = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            Log.Info(currentUTCTime + " >>> OnStart");

            try
            {
                this.dbDal = new PostGISDAL(ConfigurationManager.ConnectionStrings["FRESH.PostGIS"].ConnectionString, ConfigurationManager.AppSettings["PostGISSchema"], ConfigurationManager.AppSettings["FederationEndpointURL"]);
                Log.Info(">>> Creating service task timer");
                deletionTimer = new Timer(this.TimerTick, null, Convert.ToInt64(ConfigurationManager.AppSettings["IncidentInterval"]),Timeout.Infinite); 
                Log.Info(" >>> OnStart Complete");
            }
            catch (Exception e)
            {
                Log.Error("Error in OnStart: " + e);
            }
           
        }

        /// <summary>
        /// Method called when service is started
        /// </summary>
        public void TimerTick(Object obj)
        {
            DateTime currentUTCTime = DateTime.Now;
            Log.Info("=================== TIMER TICK @ " + currentUTCTime + " ===================");
            Log.Info(currentUTCTime + " >>> Getting stale DE messages");

            bool wasSuccessful = false;
     
            // Expiring stale DEs
            try
            {

                int messageCount = 0;
                messageCount = dbDal.ExpireStaleDEs();

                if(messageCount > 0) 
                {
                    wasSuccessful = true;
                    Log.Info(">>> "+ messageCount + " Stale De messages have been expired");

                } else 
                {
                    Log.Info(">>> No DEs to be expired");
                }    
                                                
            }
            catch (Exception e)
            {
                Log.Error("Error expiring DEs in TimerTick: " + e);
            }           
           

            // Removing DEs marked for deletion

            wasSuccessful = false;
            try
            {
                int messageCount = 0;
                messageCount = dbDal.DeleteExpiredDEs();

                if(messageCount > 0)
                {
                    wasSuccessful = true;
                    Log.Info(">>> "+ messageCount + " Stale De messages have been deleted");
                }
                else 
                {
                    Log.Info(">>> No DEs need to be removed");
                }  
                    
            }
            catch (Exception e)
            {
                Log.Error("Error removing expired DEs in TimerTick: " + e);
            }

            

            if (wasSuccessful)
            {
                Log.Info(">>> Stale De messages have been removed from the db");
            }

            currentUTCTime = DateTime.Now.ToUniversalTime();
            Log.Info("=================== WORK COMPLETE @ " + currentUTCTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + " ===================");

            // Restarting timer
            try
            {
                deletionTimer.Change(Convert.ToInt64(ConfigurationManager.AppSettings["IncidentInterval"]), Timeout.Infinite);

            } catch(Exception e)
            {
                Log.Error("Error restarting TimerTick: " + e);
            } 
                
        }

        /// <summary>
        /// Method called when service is stopped
        /// </summary>
        protected override void OnStop()
        {
            string currentUTCTime = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            Log.Info("=================== SERVICE STOPPED @ " + currentUTCTime + " ===================");
        }          
        
    }
}
