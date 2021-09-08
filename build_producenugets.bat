del BtDamageResolver\src\Api\bin\Release\Faemiyah.BtDamageResolver.Api.*.nupkg
del BtDamageResolver\src\Common\bin\Release\Faemiyah.BtDamageResolver.Common.*.nupkg
del CompressionLzma\src\CompressionLzma\bin\Release\SevenZip.Compression.LZMA.*.nupgk
dotnet build BtDamageResolver\BtDamageResolver.sln -c Release
dotnet build CompressionLzma\CompressionLzma.sln -c Release
copy BtDamageResolver\src\Api\bin\Release\*.nupkg CustomNugets
copy BtDamageResolver\src\Common\bin\Release\*.nupkg CustomNugets
copy CompressionLzma\src\CompressionLzma\bin\Release\*.nupkg CustomNugets