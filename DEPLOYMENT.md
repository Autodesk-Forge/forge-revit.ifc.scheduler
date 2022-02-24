# Revit to IFC Scheduler - Deployment Guide

The Admin Dashboard tool can be deployed to a range of systems. This guide will provide instructions for setting up the tool locally, on Azure, and on AWS.

### Prerequisites

Please ensure the following are present on your computer:
* [Visual Studio](https://code.visualstudio.com/): Either Community 2019+ (Windows) or Code (Windows, MacOS).
* [dotNET 5.0](https://dotnet.microsoft.com/en-us/download/dotnet/5.0)
* [NodeJS (with NPM)](https://nodejs.org/en/download/)

### Resource List

The Revit to IFC Scheduler requires the following resources:
* Autodesk Forge App
* SQL Server Database
* Windows Server to run dotNET 5

### Basic Steps

To set up the Revit to IFC Scheduler, the following steps must be completed:
1. [Create a Forge App](#step-1-create-forge-app).
2. [Create a SQL Server database](#step-2-create-sql-server-database).
3. [Create the application server](#step-3-create-the-application-server).
4. [Configure the application server](#step-4-set-configuration-variables)

## Step 1: Create Forge App

###### Create Forge App

A Forge App is required to access Autodesk Forge APIs and services. To learn more, see [Getting Started with Forge](https://forge.autodesk.com/developer/start-now/signup).

1. Create a Forge Account at https://forge.autodesk.com/, or log in to your account.
2. Create a new Forge App at https://forge.autodesk.com/myapps/create, with the following APIs:
   1. BIM 360 API
   2. Data Management API
   3. Model Derivative API
   4. Webhooks API
3. Set the callback URL to `https://localhost:3000/api/forge/oauth/callback` for local deployments, or replace `localhost:3000` with the deployment URL for cloud deployments.
4. Add the Forge App to the desired BIM 360 account by following [these steps](https://forge.autodesk.com/en/docs/bim360/v1/tutorials/getting-started/get-access-to-account/#step-2-connect-your-app-to-a-specific-bim-360-account).

    **Note.** [Model Derivate API](https://forge.autodesk.com/en/docs/model-derivative/v2/developers_guide/overview/) incurs cost. To view the current cost of the Model Derivative service, and to purchase Cloud Credits for file conversions, please view the [Forge Pricing page](https://forge.autodesk.com/pricing#cloud-credits).

###### Add Forge App to BIM 360 Tenants

1. Navigate to https://admin.b360.autodesk.com/ (Must be an Account Admin)
2. Navigate to the Account Admin page for your target BIM 360 Tenant
3. Choose `Settings`
4. Choose `Custom Integrations` (If this option is not available to you, please follow the instructions in the [Get Access to a BIM 360 Account Tutorial](https://forge.autodesk.com/en/docs/bim360/v1/tutorials/getting-started/get-access-to-account/))
5. Press `Add Custom Integration`
    1. Select both `BIM 360 Account Administration` and `Document Management`, then press `Next`
6. Select `I'm the Developer`, then press `Next`
7. Fill out the `Add Custom Integration` popup:
    1. Check the `I have saved the Account ID` checkbox
    2. Add the Client ID from your newly created Forge App
    3. Name the app
    4. Press `Save`

## Step 2: Create SQL Server Database

An instance of SQL Server is required for this tool. It is responsible for holding session data (such as users), configuration settings, and job queuing for the indexing process. It also holds the data used for displaying project information. You may create two databases if desired -- one for the tool's data, and one for the data used in PowerBI. 

Note that this tool uses Entity Framework Core in a code-first, migration based setup. When provided with a connection string (or multiple strings),  it will automatically create or update the database and tables as needed.

### Local

To create a local instance of SQL Server, you can download a developer account from https://www.microsoft.com/en-us/sql-server/sql-server-downloads

### Azure

1. Log in to https://portal.azure.com/#create/Microsoft.SQLDatabase

2. Fill in the necessary fields to create a new database (or use an existing one). For most purposes, a 'Basic' database should have more than enough storage and compute power.

3. Save your login name, password, and Resource Group.

4. Wait for the database to be created, then navigate to the resource page.

5. Click 'Show database connection strings' from the database overview, and save the result.

6. Click `Set server firewall` at the top of the database overview.

7. Depending on where you will be hosting the application server, either turn `Allow Azure services and resources to access this server` to On, or add the server IP address, then `Save`.

8. Go to `Connection Strings`, and copy the ADO.NET connection string for later use.

### AWS

To set up an instance of SQL Server, please follow this tutorial:
https://aws.amazon.com/getting-started/hands-on/create-microsoft-sql-db/

Note that the database must be configured to allow access from the EC2 instances deployed below.

## Step 3: Create the Application Server

This application should be run using a single Windows server.

### Local

[Clone the repository](https://docs.github.com/en/repositories/creating-and-managing-repositories/cloning-a-repository) to your local machine, then continue with Step 4.

### Azure

Create an App Service instance using the following instructions. 

1. Navigate to https://portal.azure.com/#create/Microsoft.WebSite
2. Fill out the resource group and name as desired.
3. Set the following Instance Details:
   1. Publish: `Code`
   2. Runtime Stack: `.NET 5`
   3. Operating System: `Windows`
   4. Region: Should match the region used for the database and Elastic Search.
4. Create an App Service Plan with a SKU of S1 or greater.
5. Press `Create`, then `Create` again.
6. Wait for the webapp to be created, then navigate to the resource page.
7. Navigate to the `Configuration` page, and set the application settings and connnection string required by [Environment Variables](#environment-variables).


### AWS

This application should be run an EC2 instance running Windows Server with IIS enabled, hosting an ASP.NET 5 application.

For instructions on setting up the EC2 instance, please follow these instructions to deploy the application:

- [Tutorial: How to deploy a .NET sample application using Elastic Beanstalk](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/create_deploy_NET.quickstart.html)
- [Tutorial: Deploying an ASP.NET core application with Elastic Beanstalk](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/dotnet-core-tutorial.html)
- [Adding an Amazon RDS DB instance to your .NET application environment](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/create_deploy_NET.rds.html)

## Step 4: Set Configuration Variables

A number of environment variables must be set prior to launching the tool. These are set in different ways, depending on the chosen environment.

### Local

Create a copy of the [appsettings.template.json](appsettings.template.json) file and name it `appsettings.development.json`. Fill in all required fields, and delete any unused fields.

### Azure

Navigate to the `Configuration` page for each App Service, and set the application settings and connnection string required below.

### AWS

The simplest method of setting the configuration variables is by modifying the `appsettings.json` file directly. 

**NOTE.** Once modified, this file will contain sensitive data and must not be checked into source control.

## App Settings Variables

Name | Description | Example Value
--- | --- | ---
ForgeClientId | From the Forge App created during Setup | _CL35ag54e6aghsaf4cacwe_
ForgeClientSecret | From the Forge App created during Setup | _aa46asffaws_
AdminEmails | Semicolon-separated list of email addresses | _admin@mycompany.com;bimmanager@mycompany.com_
ConnectionStrings.SqlDB | A SQL connection String |  _Server=MY-SERVER;Database=revit-to-ifc-scheduler;Trusted_Connection=True;ConnectRetryCount=0_

### Optional App Settings

Name | Description | Default Value
--- | --- | ---
AppId | A name for the application, used when naming cookies and buckets | revit-to-ifc
SendGridApiKey | If email notifications are desired, an API key from SendGrid should be provided | _null_
FromEmail | The email address that SendGrid should attempt to put into the 'From' field | _null_
ToEmail | The email address that SendGrid should attempt to put into the 'To' field | _null_
LogPath | The specific path where log files should be stored | _null_
IncludeShallowCopies | Copying a file in BIM 360 does not create a new file, only a reference to the original file, and cannot be passed to the model derivative service. Setting this to true will make a true copy of the file, and pass that to the model derivative service.  | true
TwoLegScope | The Forge scopes used by two legged tokens | data:read data:create account:read
ThreeLegScope | The Forge scopes used by three legged tokens | user:read data:read
ForgeBaseUrl | The root URL used to connect to the Forge platform | https://developer.api.autodesk.com


## Step 4: Deploying Application

### Local

Open the application in VS Code, Visual Studio, or your IDE of choice. 

Open the console, and type `dotnet run --urls=https://localhost:3000`, then press enter.

To learn more about running .NET in Visual Studio Code, please see [Using .NET Core in Visual Studio Code](https://code.visualstudio.com/docs/languages/dotnet).

### Azure

To deploy code using Visual Studio Code, please see [Deploy to Azure App Service using Visual Studio Code](https://docs.microsoft.com/en-us/aspnet/core/tutorials/publish-to-azure-webapp-using-vscode?view=aspnetcore-6.0), skipping the `Create an ASP.Net Core MVC Project` section.

To deploy code using Visual Studio, please follow the instructions below:

* Open the project in Visual Studio
* Select Build > Publish `RevitToIfcScheduler` > Start
* Select `Azure App Service (Windows)` > Next
* Sign in using the same Account you used in Azure
* Search and select the appropriate resource group and App Service instance.
* Set `Configuration` to `Release`, `Target Framework` to `net5.0`, and `Deployment Mode` to `Framework-dependent`.
* Select 'Publish'

### AWS

Continue to follow the instructions available at:

- [Tutorial: How to deploy a .NET sample application using Elastic Beanstalk](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/create_deploy_NET.quickstart.html)
- [Tutorial: Deploying an ASP.NET core application with Elastic Beanstalk](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/dotnet-core-tutorial.html)
- [Adding an Amazon RDS DB instance to your .NET application environment](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/create_deploy_NET.rds.html)