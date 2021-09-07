#!/bin/sh

rm BtDamageResolver/src/Api/bin/Release/Faemiyah.BtDamageResolver.Api.*.nupkg
rm BtDamageResolver/src/Common/bin/Release/Faemiyah.BtDamageResolver.Common.*.nupkg
dotnet build BtDamageResolver/BtDamageResolver.sln -c Release
cp BtDamageResolver/src/Api/bin/Release/*.nupkg CustomNugets
cp BtDamageResolver/src/Common/bin/Release/*.nupkg CustomNugets