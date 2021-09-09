#!/bin/sh
rm BtDamageResolver/src/Api/bin/Release/Faemiyah.BtDamageResolver.Api.*.nupkg
rm BtDamageResolver/src/Common/bin/Release/Faemiyah.BtDamageResolver.Common.*.nupkg
rm CompressionLzma/src/CompressionLzma/bin/Release/SevenZip.Compression.LZMA.*.nupgk
dotnet build BtDamageResolver/BtDamageResolver.sln -c Release
dotnet build CompressionLzma/CompressionLzma.sln -c Release
cp BtDamageResolver/src/Api/bin/Release/*.nupkg CustomNugets
cp BtDamageResolver/src/Common/bin/Release/*.nupkg CustomNugets
cp CompressionLzma/src/CompressionLzma/bin/Release/*.nupkg CustomNugets