LessMsbuildTasks
================

A targets file and MsBuild tasks, that will enable compiling of .Less files

The easiest way to get it up and running is through installing it through NuGet 
     
     Install-Package LessMsbuildTasks

The Nuget package will create a new folder in the solution directory called .less.  This folder will contain a .targets file as well as some dll's that are required for the .targets to work.

It will also automatically import that .targets file into your project file.

Once the targets file is imported you will get a new Build Action "DotLess". This build action is selectable from the properties drop down.  Any files you set to use this build action will be compiled with the 
.less compiler.

In addition, the compiler is smart and will only attempt to compile the file again if it or any of the files it imports have changed since the last time it was run.

Advanced Options
----------------

There are a few Msbuild properties that you can change in order to affect the functionality of the compile

**LessOutputDirectory**: (string)  The base directory where the .css files will be output to (defaults to `$(WebProjectOutputDir)`) which is normally the project root but if being run from Tfs it is the the _PublishedWebsites directory.

**LessKeepRelativeDirectory**: (boolean) By default it will append the relative path from the project root to the Output directory path, set this to false if you want to override that behaivor.

**LessMinifyOutput**: (boolean) Output CSS will be compressed.  (defaults to True for release builds and False for debug builds)

**LessDebugMode** (boolean) Print helpful debug comments in output.

**LessDisableUrlRewriting** (boolean) Disables changing urls in imported files

**LessImportAllFilesAsLess** (boolean) Treats every import as less even if ending in .css

**LessInlineCssFiles** (boolean) Inlines CSS file imports into the output

**LessDisableVariableRedefines** (boolean) Makes variables behave more like less.js, so the last variable definition is used

**LessKeepFirstSpecialComment** (boolean) Keeps the first comment begninning /** when minified

**LessForceRun** (boolean) If this is true, it will always run and not check to see if anything has changed
