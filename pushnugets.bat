REM This batch file is only for the author's own use. Ignore it.

BtDamageResolver\BuildPipeline\BuildPipeline.exe BtDamageResolver\src\Api\Api.csproj BtDamageResolver\src\Common\Common.csproj
call producenugets.bat
dotnet nuget push "BtDamageResolver\src\Api\bin\Release\Faemiyah.BtDamageResolver.Api.*.nupkg" --source "githubWarma" --api-key %BtDamageResolverNugetKey%
dotnet nuget push "BtDamageResolver\src\Common\bin\Release\Faemiyah.BtDamageResolver.Common.*.nupkg" --source "githubWarma" --api-key %BtDamageResolverNugetKey%
