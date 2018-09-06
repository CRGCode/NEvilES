@echo off
	dotnet restore .\NEvilES.sln
	dotnet build .\NEvilES.sln -c Release
