del src\Api\bin\Release\Faemiyah.BtDamageResolver.Api.*.nupkg
del src\Common\bin\Release\Faemiyah.BtDamageResolver.Common.*.nupkg
BuildPipeline\BuildPipeline.exe src\Api\Api.csproj src\Common\Common.csproj
dotnet build BtDamageResolver.sln -c Release
copy src\Api\bin\Release\*.nupkg ..\CustomNugets
copy src\Common\bin\Release\*.nupkg ..\CustomNugets
dotnet nuget push "src\Api\bin\Release\Faemiyah.BtDamageResolver.Api.*.nupkg" --source "githubWarma" --api-key %BtDamageResolverNugetKey%
dotnet nuget push "src\Common\bin\Release\Faemiyah.BtDamageResolver.Common.*.nupkg" --source "githubWarma" --api-key %BtDamageResolverNugetKey%
move src\Api\bin\Release\*.nupkg C:\temp\MSVS_CustomNugets
move src\Common\bin\Release\*.nupkg C:\temp\MSVS_CustomNugets