using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fresh.Global.ContentHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EDXLSharp.NIEMEMLCLib;
using EDXLSharp.NIEMEMLCLib.Incident;
using EDXLSharp.NIEMEMLCLib.Resource;
using Fresh.Global.IconHelpers;

namespace Fresh.Global.ContentHelpers.Tests
{
    [TestClass()]
    public class EMLCContentTests
    {
        // Tests if IconURL returns a URL.  
        // Fails if the return URL is an empty string
        [TestMethod()]
        public void IconURLTest()
        {
            Event newEvent = new Event();

            //set the basics
            newEvent.EventID = "ARDENTMC:TESTMESSAGE:EVENT:SIMPLE";
            newEvent.EventMessageDateTime = System.DateTime.UtcNow;
            newEvent.EventTypeDescriptor.CodeValue = EventTypeCodeList.ATOM_GRDTRK_EQT_GRDVEH_CVLVEH_EM_EMS_AMBULANCE;
            newEvent.EventTypeDescriptor.EventTypeDescriptorExtension.Add("Ambulance");
            newEvent.EventValidityDateTimeRange.StartDate = System.DateTime.UtcNow;
            newEvent.EventValidityDateTimeRange.EndDate = System.DateTime.UtcNow.AddDays(1.0);

            //set the location
            EventLocation location = new EventLocation();
            location.LocationCylinder.CodeValue = LocationCreationCodeList.HUMAN;
            location.LocationCylinder.LocationPoint.Point.Lat = 30.0;
            location.LocationCylinder.LocationPoint.Point.Lon = 30.0;
            location.LocationCylinder.LocationCylinderRadiusValue = (decimal)1.0;
            newEvent.EventLocation = location;
            newEvent.Details = new ResourceDetail();


            //set a comment
            EventComment comment = new EventComment();
            comment.CommentText = "This is a simple NIEM Event message.";
            comment.DateTime = System.DateTime.UtcNow;
            comment.OrganizationIdentification = "ArdentMC";
            comment.PersonHumanResourceIdentification = "Brian Wilkins";
            newEvent.EventComment = new List<EventComment>();
            newEvent.EventComment.Add(comment);

            // set event 
            EMLCContent cont = new EMLCContent(newEvent);

            // getting URL
            string iconLoc = cont.IconURL();

            Console.WriteLine(iconLoc);

            if(iconLoc == "") // If no url is returned
            {
                Assert.Fail();
            }

        }
    }
}