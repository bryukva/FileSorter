FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["LargeTextFiles.Sorter/LargeTextFiles.Sorter.csproj", "LargeTextFiles.Sorter/"]
RUN dotnet restore "LargeTextFiles.Sorter/LargeTextFiles.Sorter.csproj"
COPY . .
WORKDIR "/src/LargeTextFiles.Sorter"
RUN dotnet build "LargeTextFiles.Sorter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LargeTextFiles.Sorter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LargeTextFiles.Sorter.dll"]
