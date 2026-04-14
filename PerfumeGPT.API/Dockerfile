FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5141
EXPOSE 7011

ENV ASPNETCORE_URLS=http://+:5141;https://+:7011
ENV ASPNETCORE_ENVIRONMENT=Development

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["PerfumeGPT.API/PerfumeGPT.API.csproj", "PerfumeGPT.API/"]
RUN dotnet restore "PerfumeGPT.API/PerfumeGPT.API.csproj"
COPY . .
WORKDIR "/src/PerfumeGPT.API"
RUN dotnet build "PerfumeGPT.API.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "PerfumeGPT.API.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PerfumeGPT.API.dll"]
