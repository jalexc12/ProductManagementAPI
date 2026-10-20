using Microsoft.AspNetCore.Mvc.Testing;

[assembly: WebApplicationFactoryContentRoot(
    "ProductManagementApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
    "ProductManagementApi",
    "ProductManagementApi.csproj",
    "1")]
[assembly: WebApplicationFactoryContentRoot(
    "ProductManagementApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
    "../ProductManagementApi",
    "ProductManagementApi.csproj",
    "2")]
[assembly: WebApplicationFactoryContentRoot(
    "ProductManagementApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
    "../../../../ProductManagementApi",
    "ProductManagementApi.csproj",
    "3")]
