@echo off
rem ****************************************************************
rem BenC (2009-11-02)
rem ****************************************************************
rem Runs against ScriptDb.EXE built from source code found at http://scriptdb.codeplex.com,
rem Change Set 35929.
rem
rem This batch files is used to call ScriptDb.EXE which scripts SQL database objects,
rem but not any data.
rem
rem This gives us a way of version controlling a database as the scripts/data files
rem can be placed in an SVN Repository and used to check for changes.
rem ****************************************************************


rem "setlocal" means that any environment changes we make in this batch file are ditched when it ends.
setlocal

echo.


rem Stick parameters in nicely named variables.
set sqlServer=%1
set databaseName=%2
set outputDir=%3



if "%sqlServer%" == "" goto Syntax
if "%databaseName%" == "" goto Syntax
if %outputDir% == "" goto Syntax


title Scripting Sentori Account database "%databaseName%" on Server "%sqlServer%"...


rem If the outputDir parameter is relative (begins with ".", e.g. ".\folder1\folder2"),
rem remove the "." at the beginning and tack on the current working directory.
if "%outputDir:~0,1%" == "." set outputDir="%CD%%outputDir:~1%"


echo SQLServer
echo ----------
echo    %sqlServer%
echo.
echo DatabaseName
echo ----------
echo    %databaseName%
echo.
echo OutputDirectory
echo ---------------
echo    %outputDir%
echo.


IF EXIST %outputDir% goto DirectoryExists
goto ScriptDatabase



:DirectoryExists
rem ****************************************************************
set choice =
set /p  choice=The OutputDirectory already exists, are you sure you want to continue? [y/n] 
if "%choice%" == "y" goto ScriptDatabase
goto Done
rem ****************************************************************



:ScriptDatabase
rem ****************************************************************
rem Log that scriptDb's running.
echo -------------------------------------------------->> scriptDb.log
echo scriptDb-noData.bat run on %date% at %time%>> scriptDb.log
echo SQLServer:	%sqlServer%, DatabaseName: %databaseName%, OutputDirectory: %outputDir%>> scriptDb.log


echo.
echo ==============================
echo Scripting Database "%databaseName%" (excluding data)...
echo ==============================
echo.

rem Explanation of the parameters we're using with ScriptDb.EXE:
rem		-con:					= connection string to the database
rem		-outDir:				= where the output scripts are placed
rem		-d						= uses SQL's BCP.EXE to export all data in the database
rem		-v						= shows what it's doing (verbose)
rem		-p						= scripts extended properties for each object
rem		-TableOneFile		= individual tables and their keys/indexes are scripted as single files
rem		-ScriptAsCreate	= stored procedures are scripted as CREATE, rather than ALTER
rem		-Permissions		= script the permissions for each object
rem		-CreateOnly		= don't generate DROP statements

ScriptDb.exe -con:server=%sqlServer%;database=%databaseName%;trusted_connection=yes -outDir:%outputDir% -v -p -TableOneFile -ScriptAsCreate -CreateOnly -Permissions

if %ERRORLEVEL% GTR 0 goto Error



echo Scripting complete.>> scriptDb.log

echo.
echo ==============================
echo Scripting complete.
echo ==============================
echo.
goto Done
rem ****************************************************************



:Syntax
rem ****************************************************************
echo Scripts a database using ScriptDb.EXE
echo.
echo USAGE:	SCRIPTDB.BAT SQLServer DatabaseName OutputDirectory
echo.
echo Examples:
echo.
echo   SCRIPTDB.BAT localhost SM_PROTOACCOUNT C:\temp
echo   SCRIPTDB.BAT servername\instancename SA_Blank D:\DatabaseScripts
echo.
echo.
echo Parameters:
echo.
echo   SQLServer
echo		The SQL Server where the Sentori Account database you want to
echo		script is stored.
echo.
echo		e.g.
echo			serverName
echo			serverName\instanceName
echo.
echo	DatabaseName
echo		The name of the Sentori Account database you want to script.
echo.
echo		e.g.
echo			SA_NewOne
echo			SA_Customer
echo.
echo   OutputDirectory
echo		The directory where the scripted files should be placed
echo		(may not already exist).
echo.
echo		If the directory path contains spaces, surround it with double quotes
echo		and leave off the trailing "\".
echo.
echo		e.g.
echo			C:\database
echo			"D:\Sentori\Database Files"
goto Done
rem ****************************************************************



:Error
rem ****************************************************************
echo.
echo ** An error occurred whilst scripting the database. **
echo.
goto Done
rem ****************************************************************



:Done
rem ****************************************************************
echo.
rem ****************************************************************