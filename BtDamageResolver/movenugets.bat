BuildPipeline\BuildPipeline.exe src\Api\Api.csproj src\Common\Common.csproj
dotnet build BtDamageResolver.sln -c Release
copy src\Api\bin\Release\*.nupkg ..\CustomNugets
copy src\Common\bin\Release\*.nupkg ..\CustomNugets
move src\Api\bin\Release\*.nupkg C:\temp\MSVS_CustomNugets
move src\Common\bin\Release\*.nupkg C:\temp\MSVS_CustomNugets