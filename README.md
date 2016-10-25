
This app is designed as a windows service, but it can also run interactivly from Visual Studio

To install as a windows Service


--install windows service for local environment  first instance
From CMD prompt 'run as administrator'
cmd>sc create  OktaImport binPath= "C:\OktaServices\OktaImport\OktaImport.exe"  DisplayName= "OktaImport"
cmd>sc description OktaImport "OktaImport for First Instance"
--uninstall
cmd>sc delete OktaImport 

Running multiple instances
edit app.config and choose a different csv file
cmd>sc create  OktaImport2 binPath= "C:\OktaServices\OktaImport2\OktaImport.exe"  DisplayName= "OktaImport2"
cmd>sc description OktaImport2 "OktaImport for Second Instance"
--uninstall
cmd>sc delete OktaImport2

Note: each instance needs a sperate working directory with all the executables added
	the ony difference in the instances is the app config file
	
Note: App.config has csv filename and path and log filename and path which should both be updated to local enviroment