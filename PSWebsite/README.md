# Installation

If you have any issues with this, feel free to open an issue against this github, and I'll eventually take a look!

## Installation Prerequisites:
* Operating System: Windows 2000 Professional or Server, XP Professional, Vista, 7, 8, 8.1, 10, 2003 Server, or 2008 Server
  * Requires support for IIS (Internet Information Services) -- the built-in web browser available mostly on professional or server editions of windows
* Grab [the release ZIP](./PSWebsiteAlpha.zip)
* A basic working knowledge of setting up software is very helpful

## Step 1 - Prepare the web server:
* Ensure that you have the ability to run powershell scripts. Most newer versions of Windows have this out of the box, but older versions may require downloading and installing Powershell support.
* Open the [installation powershell script](./PSWebsiteInstaller.ps1) in a text editor.
* By default, the website will install into c:\inetpub\pswebsite. If you want to change this, please open the powershell script and update the $iisSitePath folder to your desired folder.
* Open a powershell console in administrator mode (right click the powershell program and hit "run as administrator").
* In theory, one should be able to copy and paste the entire contents of the open installation script into the powershell window and everything will magically work. Given differences in systems, however, it might be wise to slowly copy and paste little sections of the installation script at a time into the powershell window and watch carefully for angry red error messages.

## Step 2 - Dropping in the web site itself:
* You should now uncompress the publish folder compressed file from the download page into the destination from above (by default, c:\inetpub\pswebsite). The folder should start out with a single subfolder in it ("db") that was created by the powershell script, and will end with several subdirectories and several files (you sohuld have a web.config file at the root installation folder, for example -- it shouldn't go into a subfolder.)
* By default, PACSsoft PACS will simply use a SQLite file for storing its database, which will now reside in the "db" subfolder of your website directory.

## Step 3 - Setup:
* If all went well, you should actually simply be able to open a browser to http://localhost/ and it should open a login page. If not, then, well, it's time to start debugging. Again, feel free to contact us for help.
* You can login with the default user of "root" and password "pass" (no quotes on either), which should drop you into an empty query window. At this point, you'll want to close the search window, hit the gear icon in the top right, and open the Server Settings section.
* From here, you'll want to configure your image storage directory, maximum size for the storage directory so your hard drive doesn't fill up (max of 512GB in this alpha release), usernames/passwords for your viewing users, and remote DICOM devices.
* Start sending images in and using the system!

# Building By Hand

If you want to run the system by hand, you simply need to run `npm install` and `npm run start` in a command line while in the `PSWebsite` folder, and that should build the app.css and app.js files that the website needs to distribute.
