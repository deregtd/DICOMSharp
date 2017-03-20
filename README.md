Prerequisites:
* Visual Studio 2015 & VS Update 1 (with Visual C++)
* CMake 3.3.0 or higher (http://www.cmake.org/)
* dcmtk 3.6.0 (http://dicom.offis.de/dcmtk.php.en)
* CharLS 2.0.0 (https://github.com/team-charls/charls/releases)
* OpenJPEG 2.1.2 (https://github.com/uclouvain/openjpeg/releases/tag/v2.1.2)
* node.js ~5.x and npm (http://nodejs.org)
* Visual Studio Extensions:
  * Web Compiler (https://github.com/madskristensen/WebCompiler/releases)
  * Typescript 1.7 or higher (VS extensions window)

DCMTK setup for DICOMSharpJPEGCompression:
* Decompress the dcmtk-3.6.0.tar.gz into the DICOMSharp/DICOMSharpJPEGCompression dir, so you end up with a "dcmtk-3.6.0" dir alongside the openjpeg dir
* Run CMake
  * Enter the dcmtk-3.6.0 dir as both the source code dir and where to build the binaries
  * Hit configure, pick your choice of compiler (VS2015 32-bit), wait like 5 minutes while it does a bunch of checks
  * When it's done, you can press the Generate button.
  * Close CMake
* Go into the dcmtk-3.6.0 dir and open DCMTK.sln with VS
  * In the solution explorer, find the ALL_BUILD project (should be at the top), and build it once in debug and once in release
  * Assuming both built successfully, close that visual studio, you shouldn't need it ever again

CharLS setup for DICOMSharpJPEGCompression:
* Decompress the CharLS 2.0.0.zip file into the DICOMSharp/DICOMSharpJPEGCompression dir, so you end up with a "charls-2.0.0" dir alongside the openjpeg and dcmtk-3.6.0 dirs

OpenJPEG setup DICOMSharpJPEGCompression:
* Decompress the openjpeg-v2.1.2-windows-x86.zip file into the DICOMSharp/DICOMSharpJPEGCompression dir, so you end up with a "openjpeg-v2.1.2-windows-x86" dir alongside the openjpeg, dcmtk-3.6.0, and charls-2.0.0 dirs

Build:
* Run VS2015 as administrator (you'll need to do this every time)
* Open DICOMSharp.sln
* Build all and run your desired project

PSWebsite Setup:
* You must go into IIS settings and create a new application (suggested path /pswebsite) or new website that points at the PSWebsite directory, and make a new app pool for the application/website that has 32-bit enabled (App Pool -> Advanced Settings -> Enable 32-bit Applications)

WebUser.config:
* Copy WebUser.config.sample and rename it to WebUser.config
* Leave it in place to use it with SQLite, or change the parameters to use your desired MSSQL or MySQL database host

Web Compiler Setup:
* By default, Visual Studio uses a version of node.js which is too old to support Webpack. As such, you need to configure Visual Studio to use your manually-installed Node:
* Open DICOMSharp.sln
* Open Tools > Options, Projects and Solutions > External Web Tools
* Click on the entry for "$(PATH)" and click the up arrow button to move the entry above "$(DevEnvDir)\Extensions\Microsoft\Web Tools\External"
* Either manually re-run Task Runner or restart VS

Other notes:
* The DICOMSharpDocs project requires installing the Sandcastle Help File Builder (https://github.com/EWSoftware/SHFB)
* The DICOMManagerSetup project requires installing Installshield Limited Edition (http://learn.flexerasoftware.com/content/IS-EVAL-InstallShield-Limited-Edition-Visual-Studio)
