# magenta
A data API using resource locators and identifiers as a middleware for client applications. Built using C#.



## Create Function
Add the global.json to set dotnet 3.1 sdk.
```
dotnet new globaljson --sdk-version 3.1
```

Scaffold the first cs http trigger function.
```
func init LandReg 
cd LandREg
func new --language C# --name GetPrices --template "HTTP trigger" --authlevel function
```

## Add csproj packages
We need the Webjobs.Extensions.Storage and Azure.Data.Tables package added to our csproj file.  You can lookup the specific names and the versions using nuget.org and obtain the dotnet install commands.
```
dotnet add package Azure.Data.Tables
dotnet add package Microsoft.Azure.WebJobs.Extensions.Storage
```

