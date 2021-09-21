set version=2.0.0
dotnet pack -p:PackageVersion=%version% .\NEvilES.Abstractions -o ..\Packages 
dotnet pack -p:PackageVersion=%version% .\NEvilES -o ..\Packages 
dotnet pack -p:PackageVersion=%version% .\NEvilES.Testing -o ..\Packages 
dotnet pack -p:PackageVersion=%version% .\NEvilES.DataStore.SQL -o ..\Packages 
dotnet pack -p:PackageVersion=%version% .\NEvilES.DataStore.Marten -o ..\Packages 

