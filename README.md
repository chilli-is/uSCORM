# uSCORM 
<a id="top" style="display:none"></a>

This repository creates a plugin for the Umbraco CMS that allows an Umbraco backoffice user to upload a SCORM 1.2 zip file package into their Umbraco application.

The plugin will unzip the package to the application's root folder, into a folder called "uSCORM-Assets".  It will then search for a selection of default launch files, such as index.html or index.htm.  If it finds such a file, it will set this as the default file for launching the package.

From the backoffice, the administrative user can add new e-Learning Assets but uploading SCORM packages.  This will give them a url which they can then share with other users or include as a link inside a web page.
