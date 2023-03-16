FROM mcr.microsoft.com/dotnet/sdk:7.0
WORKDIR /build
COPY [".", "."]
RUN mkdir customnugets
RUN mv *.nupkg ./customnugets/
