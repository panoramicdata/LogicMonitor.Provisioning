# LogicMonitor.Provisioning

Important: recent breaking change: you may need to update you configuration to update (case-sensitively):
- all instances of the word "device" to the word "resource"
- all instances of the word "Device" to the word "Resource"
If you are new to the tool, you can ignore this!

## Introduction

This is a command line application for provisioning
a customer or site in a LogicMonitor system.

## Regular Users

Most users will find it easiest to just install the tool and run it.  To do so:
1.	Download the MSI [here](https://github.com/panoramicdata/LogicMonitor.Provisioning/blob/main/Installer/LogicMonitor.Provisioning.Setup.msi).
2.  Run the installer
3.  Press [Windows] and type "LogicMonitor P"
4.  Select "LogicMonitor Provisioning Folder"
5.  Follow the configuration instructions below
6.  Press [Windows] and type "LogicMonitor P"
7.  Select "LogicMonitor Provisioning"

## Developers

To run in debug:
1.  Install Visual Studio 2022 (Community is fine)
2.  Clone the project from Github to your local machine
3.  Open the .sln file
4.  Follow the configuration instructions below
5.  Run in debug mode

To create the installer:
1.  Ensure that the "Microsoft Visual Studio Installer Projects" extension is installed
2.  Build the LogicMonitor.Provisioning.Setup project in the "Release" configuration
3.  This will create an MSI file in the LogicMonitor.Provisioning.Setup/Release folder

## Google Drive

Sure, here are the instructions formatted in Markdown:

# Instructions to Create a Google Cloud Client ID for Desktop (OAuth 2.0)

Follow these steps to create a Google Cloud Client ID for a desktop application called "LogicMonitor Provisioner" with full read access to Google Drive.

1. **Go to the Google Cloud Console:**
   - Open your web browser and go to [Google Cloud Console](https://console.cloud.google.com/).

2. **Create a New Project:**
   - In the top-left corner, click on the project dropdown and select "New Project".
   - Enter a project name (e.g., "LogicMonitor Provisioner Project") and select your organization if applicable.
   - Click "Create".

3. **Enable APIs and Services:**
   - In the left-hand navigation menu, click on "APIs & Services" > "Library".
   - In the search bar, type "Google Drive API" and select it from the list.
   - Click "Enable" to enable the API for your project.

4. **Create OAuth Consent Screen:**
   - In the left-hand menu, click on "OAuth consent screen".
   - Select "External" and click "Create".
   - Fill in the required fields such as "App name" (e.g., "LogicMonitor Provisioner"), "User support email", and "Developer contact information".
   - Click "Save and Continue".
   - Add scopes by clicking on "Add or Remove Scopes" and select the following scope:
     - `../auth/drive.readonly` - Full read access to the user's Google Drive.
   - Click "Save and Continue".
   - Add test users (your email address or any other test user email addresses).
   - Click "Save and Continue", and then "Back to Dashboard".

5. **Create OAuth 2.0 Client ID:**
   - In the left-hand menu, click on "Credentials".
   - Click "Create Credentials" and select "OAuth 2.0 Client ID".
   - Choose "Desktop app" as the application type.
   - Enter a name for your client ID (e.g., "LogicMonitor Provisioner").
   - Click "Create".
   - A dialog will appear with your Client ID and Client Secret. Note these down securely.

6. **Download JSON File:**
   - In the Credentials page, find your newly created OAuth 2.0 Client ID.
   - Click on the download icon (downward arrow) to download the JSON file.
   - Provide this JSON file to the application that requires it for OAuth 2.0 authentication.

Your client has now created a Google Cloud Client ID for a desktop application, which can be used to gain read access to their Google Drive files using OAuth 2.0.

## Configuration

1.  Copy the appsettings.example.json to appsettings.json in the same folder
2.  Either
    - Copy the data.example.xlsx to data.xlsx in the same folder and use the Xlsx repetition type OR
    - Copy the data.example.xlsx to Google drive, ensuring that the Application is authorized to access the file
3.  Ensure that all tables in the XLSX file are formatted as tables.
4.  Edit the files as follows:

### appsettings.json

#### LogicMonitor credentials

To complete this, you will need to [create a LogicMonitor API Token](https://www.logicmonitor.com/support/settings/users-and-roles/api-tokens).

Configure your logicMonitor credentials as follows:
-  Account: this is the first part of your LogicMonitor URL. For example, 'acme' in https://acme.logicmonitor.com/.
-  AccessId: Add 
-  AccessKey: Add (You won't be able to view in LogicMonitor once the API key is saved)

#### Variables
These are tokens that can be repeated later on in your configuration.  Optional.

#### Repetition
The entire configuration can be applied multiple times.
-  Type 'None': No repetition. 
-  Type 'Xlsx' or 'GoogleDriveXlsx': Repeat the config for each row in the sheet specified in Config.
-  Config:
   -  Xlsx repetition type, set this to &lt;import file path&gt;|&lt;sheet name&gt;  The config will be applied to all rows in the single table on that sheet.
   -  GoogleDriveXlsx repetition type, set this to &lt;import file id&gt;|&lt;sheet name&gt;  The config will be applied to all rows in the single table on that sheet.

#### Collectors
The specification of the Collector Group to create/delete.

#### Dashboards
The specification of the Dashboard Group to create/delete.

#### Resources
The specification of the Resource Group to create/delete.

#### Mappings
The specification of the Mapping Group to create/delete.

#### Netscans
The specification of the Netscan Group to create/delete.

#### Reports
The specification of the Report Group to create/delete.

#### Roles
The specification of the Role Group to create/delete.

#### Users
The specification of the User Group to create/delete.

#### Websites
The specification of the Website Group to create/delete.

#### RoleConfigurations
The specification of the Role Configurations to create/delete.

### data.xlsx

This file should contain multiple sheets:
*  One for repetition (if repetition type Xlsx is used)
	*  a sheet with a single table with two rows: one for customer 1, one for customer 2
*  One per list that you wish to manage, for example:
  *  one sheet with a single table containing all subnets for customer 1
  *  another sheet with a single table containing all subnets for customer 2

## Running

When running, by default the system will look for the appsettings.json file.
If you want to force a mode, you can do this in the appsettings.json file, though we recommend that you start in "Menu" mode, as configured in appsettings.example.json.
From menu mode, you can choose to "C"reate or "D"elete the configuration specified in your appsettings.json file.
Press Ctrl+C to exit the application.
