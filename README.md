

## **First Responder Extensible Sensor Hub (FRESH) Router**

This repository contains the code for the FRESH API and message deletion service.

**Requirements**

You will need:

 - A Windows Server (2012 or higher)
 - A FRESH Database (instructions can be found in the fresh-geodb repository)
 - Visual Studio (2015 or higher)


**Instructions for FRESH API/Federation API**
 -   Clone the repository
 -   Open an elevated command prompt and run fresh-setup.bat (TODO: add FRESH build scripts to this repo)
 -   This will install / setup all of the IIS and configuration for FRESH
 -   Install Windows Updates as needed
 -  Locally build the FRESH deploy package in Visual Studio (instructions for building deploy packages can be found here: https://msdn.microsoft.com/en-us/library/dd465337(v=vs.110).aspx)
 -  RDP into the server and copy/unzip the zipped deployment package to an easily accessible location (e.g. C:\Deployments).
 -   Open a command prompt tab running as Administrator and navigate to the unzipped build folder.
 - Run the following commands:

    `FreshAPI.deploy.cmd /T`

    `FreshFederation.deploy.cmd /T`

This step simulates a deployment and will create a report of what will actually happen when you deploy FRESH.

 - If no errors are reported during the previous step, you can finally deploy FRESH with the following commands:

   `FreshAPI.deploy.cmd /Y`

   `FreshFederation.deploy.cmd /Y`

**Message Deletion Service**

 - Update the database connection string:
·         Open IIS Manager and click on FRESH in the list of sites.
·         Click 'Connection Strings' and edit the database server connection string with the RDS endpoint for the database created above.
·         Restart the site
 - Confirm that FRESH has successfully been deployed by visiting http://localhost on the instance. The FRESH landing page should appear.

**Message Deletion Service**
-  In the DeDeletionService, edit the database server connection string in App.config with the correct database server created above:

 `<connectionStrings>`
`<add name="Fresh.PostGIS" connectionString="Server=database url/endpoint goes here;Port=5432;Database=freshgeo;User Id=freshdbuser;Password=mypass;" />`
`</connectionStrings>`

-  Build FRESH in Visual Studio.
-   Copy the contents of DeDeletionService\bin\Debug into C:\Deployments\DeletionService on the Fresh server.
- Install the Deletion Service using InstallUtil. Navigate to the InstallUtil directory and run the following command:
`InstallUtil.exe C:\Deployments\DeletionService\DeDeletionService.exe`
- Start the service by running the following command:

  `sc start FreshDeletionService`

**Setting Up a Map Server**

After you've stood up a FRESH database and router, you can use Geoserver to start enabling the data to be used in WatchTower. Add the FRESH database as a data store through Geoserver's admin console and create layers from the views ('active_feeds' and 'feeds_by_datetimesent' are recommended). Examples of how to retrieve the map server data are available in the WatchTower applications (use the example map server URLs in the application settings).

## **DISCLAIMER OF LIABILITY NOTICE**:

                      

> The United States Government shall not be liable or responsible for
> any maintenance, updating or for correction of any errors in the
> software. 
>
> THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT ANY WARRANTY OF ANY KIND,
> EITHER EXPRESSED, IMPLIED, OR STATUTORY, INCLUDING, BUT NOT LIMITED
> TO, ANY WARRANTY THAT THE SOFTWARE WILL CONFORM TO SPECIFICATIONS, ANY
> IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
> PURPOSE, OR FREEDOM FROM INFRINGEMENT, ANY WARRANTY THAT THE SOFTWARE
> WILL BE ERROR FREE, OR ANY WARRANTY THAT THE DOCUMENTATION, IF
> PROVIDED, WILL CONFORM TO THE SOFTWARE.  IN NO EVENT SHALL THE UNITED
> STATES GOVERNMENT OR ITS CONTRACTORS OR SUBCONTRACTORS BE LIABLE FOR
> ANY DAMAGES, INCLUDING, BUT NOT LIMITED TO, DIRECT, INDIRECT, SPECIAL
> OR CONSEQUENTIAL DAMAGES, ARISING OUT OF, RESULTING FROM, OR IN ANY
> WAY CONNECTED WITH THE SOFTWARE OR ANY OTHER PROVIDED DOCUMENTATION,
> WHETHER OR NOT BASED UPON WARRANTY, CONTRACT, TORT, OR OTHERWISE,
> WHETHER OR NOT INJURY WAS SUSTAINED BY PERSONS OR PROPERTY OR
> OTHERWISE, AND WHETHER OR NOT LOSS WAS SUSTAINED FROM, OR AROSE OUT OF
> THE RESULTS OF, OR USE OF, THE NICS SOFTWARE OR ANY PROVIDED
> DOCUMENTATION. THE UNITED STATES GOVERNMENT DISCLAIMS ALL WARRANTIES
> AND LIABILITIES REGARDING THIRD PARTY SOFTWARE, IF PRESENT IN THE
> SOFTWARE, AND DISTRIBUTES IT "AS IS."
>
>            
>
> LICENSEE AGREES TO WAIVE ANY AND ALL CLAIMS AGAINST THE U.S.
> GOVERNMENT AND THE UNITED STATES GOVERNMENT'S CONTRACTORS AND
> SUBCONTRACTORS, AND SHALL INDEMNIFY AND HOLD HARMLESS THE U.S.
> GOVERNMENT AND THE UNITED STATES GOVERNMENT'S CONTRACTORS AND
> SUBCONTRACTORS FOR ANY LIABILITIES, DEMANDS, DAMAGES, EXPENSES, OR
> LOSSES THAT MAY ARISE FROM RECIPIENT'S USE OF THE SOFTWARE OR PROVIDED
> DOCUMENTATION, INCLUDING ANY LIABILITIES OR DAMAGES FROM PRODUCTS
> BASED ON, OR RESULTING FROM, THE USE THEREOF.
>
> **[ACKNOWLEDGEMENT NOTICE]**:
>
> *This software was developed with funds from the Department of
> Homeland Security's Science and Technology Directorate.* 
>
> **[PROHIBITION ON USE OF DHS IDENTITIES NOTICE]**:
>
> A.  No user shall use the DHS or its component name, seal or other
> identity, or any variation or adaptation thereof, for an enhancement,
> improvement, modification or derivative work utilizing the software.
>
> B.  No user shall use the DHS or its component name, seal or other
> identity, or any variation or adaptation thereof for advertising its
> products or services, with the exception of using a factual statement
> such as included in the ACKNOWLEDGEMENT NOTICE indicating DHS funding
> of development of the software.           
>
> C.  No user shall make any trademark claim to the DHS or its component
> name, seal or other identity, or any other confusing similar identity,
> and no user shall seek registration of these identities at the U.S.
> Patent and Trademark Office.

