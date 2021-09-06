FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build-env
LABEL MAINTAINER="Laszlo Lueck <llueck@gmail.com>"
WORKDIR /app
COPY RegionalWeather.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet build
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "RegionalWeather.dll"]
