# LogicMonitor.Provisioning

## Introduction

This is a command line application for provisioning
a customer or site in a LogicMonitor system.

## Regular Users

Most users will find it easiest to just install the tool and run it.  To do so:
1. Download the MSI [here](https://github.com/panoramicdata/LogicMonitor.Provisioning/blob/main/Installer/LogicMonitor.Provisioning.Setup.msi).
2. Run the installer
3. Press [Windows] and type "LogicMonitor P"
4. Select "LogicMonitor Provisioning Folder"
5. Follow the configuration instructions below
3. Press [Windows] and type "LogicMonitor P"
4. Select "LogicMonitor Provisioning"

## Developers

From Visual Studio:
1. Clone the project from Github to your local machine
2. Open the .sln file in Visual Studio Community 2022 onwards
3. Follow the configuration instructions below
4. Run in debug mode

## Configuration

1. Copy the appsettings.example.json to appsettings.json in the same folder
2. Copy the data.example.xlsx to data.xlsx in the same folder
3. Edit the files as follows:

### appsettings.json

#### LogicMonitor credentials

To complete this, you will need to [create a LogicMonitor API Token](https://www.logicmonitor.com/support/settings/users-and-roles/api-tokens).

Configure your logicMonitor credentials as follows:
* Account: this is the first part of your LogicMonitor URL. For example, 'acme' in https://acme.logicmonitor.com/.
* AccessId: Add 
* AccessKey: this is the first part of your LogicMonitor URL. For example, 'acme' in https://acme.logicmonitor.com/.

#### Variables
These are tokens that can be repeated later on in your configuration.  Optional.

#### Repetition
The entire configuration can be applied multiple times.
* Type 'None': No repetition. 
* Type 'Xlsx': Repeat the config for each row in the sheet specified in Config.
* Config: for Xlsx repetition type, set this to &lt;import file path&gt;|&lt;sheet name&gt;  The config will be applied to all rows in the single talbe on that sheet.

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
* One for repetition (if repetition type Xlsx is used)
	* a sheet with a single table with two rows: one for customer 1, one for customer 2
* One per list that you wish to manage, for example:
  * one sheet with a single table containing all subnets for customer 1
  * another sheet with a single table containing all subnets for customer 2

## Running

When running, by default the system will look for the appsettings.json file.
If you want to force a mode, you can do this in the appsettings.json file, though we recommend that you start in "Menu" mode, as configured in appsettings.example.json.
From menu mode, you can choose to "C"reate or "D"elete the configuration specified in your appsettings.json file.
Press Ctrl+C to exit the application.
