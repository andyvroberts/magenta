# Magenta
A data API using resource locators and identifiers as a middleware for client applications. Built using C#.  

## Landreg Prices
Create a Function App that is triggered by an HTTP request for:  
1. return prices by exact postcode
2. return prices by using a partial postcode scan

  

## Technical Design & Implementation Details
  
### Create Function
Add the global.json to set dotnet 3.1 sdk.  
```
dotnet new globaljson --sdk-version 3.1
```

Scaffold the first cs http trigger function.  
```
func init LandReg 
cd LandReg
func new --language C# --name GetPrices --template "HTTP trigger" --authlevel function
```

### Add csproj packages
We need the Webjobs.Extensions.Storage and Azure.Data.Tables package added to our csproj file.  You can lookup the specific names and the versions using nuget.org and obtain the dotnet install commands.  
```
dotnet add package Microsoft.Azure.WebJobs.Extensions.Tables --prerelease
dotnet add package Microsoft.Azure.WebJobs.Extensions.Storage
```

you should no longer need the following package, the tables extension used above already incudes it.  
```
dotnet add package Azure.Data.Tables
```

### Create the Function App resources
Add the magenta resource group and storage account.  
```
export faresgrp=Magenta001  
export fastoreacc=magentadata001

az group create --name $faresgrp --location uksouth

az storage account create \
  --name $fastoreacc \
  --resource-group $faresgrp \
  --location uksouth \
  --kind StorageV2 \
  --sku Standard_LRS
```

get the storage account connection string and put in local.settings.json in the AzureWebJobsStorage key.  
```
export funcconnstring=$(az storage account show-connection-string -n $fastoreacc -g $faresgrp -o tsv)
```

### Azure Table Service
When making OData query request to the Azure table service, you can only return a maximum of 1,000 entities and the response must return within a maximum of 5 seconds.  See the docs.  
https://learn.microsoft.com/en-us/rest/api/storageservices/Query-Entities#remarks

In order to return more records, use pagination functionality to accumulate data before sending the response.  

