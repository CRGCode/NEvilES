@echo off
call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\Tools\VsDevCmd.bat"
IF %ERRORLEVEL% EQU 1 exit
	dotnet restore .\src\NEvilES.sln
	dotnet build .\src\NEvilES.sln
