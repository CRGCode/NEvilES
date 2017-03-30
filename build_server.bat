@echo off
	dotnet restore .\src\NEvilES.sln
	dotnet build .\src\NEvilES.sln -c Release
