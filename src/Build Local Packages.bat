set version=4.0.2
dotnet pack -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:PackageVersion=%version% .\NEvilES.Abstractions -o ..\Packages 
dotnet pack -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:PackageVersion=%version% .\NEvilES -o ..\Packages 
dotnet pack -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:PackageVersion=%version% .\NEvilES.Testing -o ..\Packages 
dotnet pack -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:PackageVersion=%version% .\NEvilES.DataStore.SQL -o ..\Packages 
dotnet pack -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:PackageVersion=%version% .\NEvilES.DataStore.Marten -o ..\Packages 
dotnet pack -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:PackageVersion=%version% .\src\Outbox\Outbox.Abstractions\Outbox.Abstractions -c Release -o ..\Packages

-- nuget api key oy2pcbm6xrelkajki5tahqvpwcc7oboe7ahlwqsgjhvbh4
