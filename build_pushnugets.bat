REM This batch file is only for the author's own use. You can safely ignore it.
dotnet nuget push "BtDamageResolver\src\Api\bin\Release\Faemiyah.BtDamageResolver.Api.*.nupkg" --source "githubWarma" --api-key %BtDamageResolverNugetKey%
dotnet nuget push "BtDamageResolver\src\Common\bin\Release\Faemiyah.BtDamageResolver.Common.*.nupkg" --source "githubWarma" --api-key %BtDamageResolverNugetKey%
dotnet nuget push "CompressionLzma\src\CompressionLzma\bin\Release\SevenZip.Compression.LZMA.*.nupkg" --source "githubWarma" --api-key %BtDamageResolverNugetKey%