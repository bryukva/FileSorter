FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["LargeTextFiles.Generate/LargeTextFiles.Generate.csproj", "LargeTextFiles.Generate/"]
RUN dotnet restore "LargeTextFiles.Generate/LargeTextFiles.Generate.csproj"
COPY . .
WORKDIR "/src/LargeTextFiles.Generate"
RUN dotnet build "LargeTextFiles.Generate.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LargeTextFiles.Generate.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LargeTextFiles.Generate.dll"]
