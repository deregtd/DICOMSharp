# Helpful notes:
# http://peter.hahndorf.eu/blog/WindowsFeatureViaCmd

# This section from http://stackoverflow.com/questions/28582116/how-to-install-iis-8-through-code with more added/filtered

Function InstallIISFeature([string]$name)
{
    & Enable-WindowsOptionalFeature -Online -FeatureName $name
}

InstallIISFeature "IIS-WebServerRole"

# this installs:
#IIS-ApplicationDevelopment
#IIS-CommonHttpFeatures
#IIS-DefaultDocument
#IIS-DirectoryBrowsing
#IIS-HealthAndDiagnostics
#IIS-HttpCompressionStatic
#IIS-HttpErrors
#IIS-HttpLogging
#IIS-ManagementConsole
#IIS-Performance
#IIS-RequestFiltering
#IIS-RequestMonitor
#IIS-Security
#IIS-StaticContent
#IIS-WebServer
#IIS-WebServerManagementTools
#IIS-WebServerRole


# AspNetPrerequisites()
InstallIISFeature "NetFx3"
InstallIISFeature "IIS-ISAPIFilter"
InstallIISFeature "IIS-ISAPIExtensions"  

# ASP.NET
InstallIISFeature "NetFx4Extended-ASPNET45"
InstallIISFeature "IIS-NetFxExtensibility45"
InstallIISFeature "IIS-ASPNET45"
InstallIISFeature "IIS-NetFxExtensibility"
InstallIISFeature "IIS-ASPNET"

# more optional features
InstallIISFeature "IIS-ManagementScriptingTools"
InstallIISFeature "IIS-HttpCompressionDynamic"
InstallIISFeature "IIS-IISCertificateMappingAuthentication"
InstallIISFeature "IIS-HttpRedirect"
InstallIISFeature "IIS-WindowsAuthentication"
InstallIISFeature "IIS-IPSecurity"
InstallIISFeature "IIS-WebSockets"
InstallIISFeature "IIS-ManagementService"
InstallIISFeature "IIS-ServerSideIncludes"
InstallIISFeature "IIS-ApplicationInit"
InstallIISFeature "IIS-StaticContent"
InstallIISFeature "IIS-HttpCompressionDynamic"
InstallIISFeature "IIS-HttpCompressionStatic"

# Create app pool and configure it -- Help from http://geekswithblogs.net/QuandaryPhase/archive/2013/02/24/create-iis-app-pool-and-site-with-windows-powershell.aspx and others

Import-Module WebAdministration
$iisAppPoolName = "PACSsoftPACS"
$iisAppPoolDotNetVersion = "v4.0"

#navigate to the app pools root
cd IIS:\AppPools\

#check if the app pool exists
if (!(Test-Path $iisAppPoolName -pathType container))
{
    #create the app pool
    $appPool = New-Item $iisAppPoolName
    $appPool | Set-ItemProperty -Name "managedRuntimeVersion" -Value $iisAppPoolDotNetVersion
    $appPool | Set-ItemProperty -Name "enable32BitAppOnWin64" -Value "true"
    $appPool | Set-ItemProperty -Name "autoStart" -Value "true"
    $appPool | Set-ItemProperty -Name "startMode" -Value "AlwaysRunning"
    $appPool | Set-ItemProperty -Name processModel.idleTimeout -value ( [TimeSpan]::FromMinutes(0))
}
else
{
	$appPool = Get-Item $iisAppPoolName
}

# Change the default website's app pool to the right one

cd IIS:\Sites\

$iisSitePath = "C:\inetpub\pswebsite"

# See if we can get a useful path out of the default web site before deleting it (if it exists)
if (Test-Path "Default Web Site" -pathType container)
{
	# Get the path from the website, and place pswebsite alongside it
	$temp = Get-WebFilePath
	$iisSitePath = $temp.parent.FullName + "\pswebsite"

	# Remove the default web site
	Remove-Website "Default Web Site"
}

$iisWebsiteName = "PACSsoftPACSsite"

# Create a database subdir

$iisSitePathDbDir = $iisSitePath + "\db"

if (!(Test-Path $iisSitePathDbDir -pathType container))
{
	New-Item -ItemType directory -Path $iisSitePathDbDir
}

# Give the directory write permission by the app pool so it can write out the sqlite file

$identifier = New-Object System.Security.Principal.SecurityIdentifier $appPool.applicationPoolSid
$user = $identifier.Translate([System.Security.Principal.NTAccount])
$acl = Get-Acl $iisSitePathDbDir

$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($user,@("CreateFiles", "AppendData", "Modify", "DeleteSubdirectoriesAndFiles"," ReadAndExecute", "Synchronize"),'ContainerInherit, ObjectInherit','None',"Allow")
#$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($user,"FullControl",'ContainerInherit, ObjectInherit','InheritOnly',"Allow")
$acl.AddAccessRule($accessRule)

Set-Acl $iisSitePathDbDir $acl

# Create directory for the pacssoft source if needed

if (!(Test-Path $iisSitePath -pathType container))
{
	New-Item -ItemType directory -Path $iisSitePath
}

# Add a new application as needed

if (!(Test-Path $iisWebsiteName -pathType container))
{
	$website = New-Website -Name $iisWebsiteName -ApplicationPool $iisAppPoolName -PhysicalPath $iisSitePath
	$website | Set-ItemProperty -Name applicationDefaults.preloadEnabled -value True
}
