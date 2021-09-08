del BtDamageResolver\src\Api\bin\Release\Faemiyah.BtDamageResolver.Api.*.nupkg
del BtDamageResolver\src\Common\bin\Release\Faemiyah.BtDamageResolver.Common.*.nupkg
dotnet build BtDamageResolver\BtDamageResolver.sln -c Release
copy BtDamageResolver\src\Api\bin\Release\*.nupkg CustomNugets
copy BtDamageResolver\src\Common\bin\Release\*.nupkg CustomNugets