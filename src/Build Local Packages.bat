set version=1.7.5
dotnet pack -p:PackageVersion=%version% .\NEvilES.Abstractions -o ..\Packages 
dotnet pack -p:PackageVersion=%version% .\NEvilES -o ..\Packages 
dotnet pack -p:PackageVersion=%version% .\NEvilES.DataStore.SQL -o ..\Packages 
dotnet pack -p:PackageVersion=%version% .\NEvilES.DataStore.Marten -o ..\Packages 

