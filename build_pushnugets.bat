REM This batch file is only for the author's own use. You can safely ignore it.
call build_rollversion.bat
call build_producenugets.bat
dotnet nuget push "BtDamageResolver\src\Api\bin\Release\Faemiyah.BtDamageResolver.Api.*.nupkg" --source "githubWarma" --api-key %BtDamageResolverNugetKey%
dotnet nuget push "BtDamageResolver\src\Common\bin\Release\Faemiyah.BtDamageResolver.Common.*.nupkg" --source "githubWarma" --api-key %BtDamageResolverNugetKey%