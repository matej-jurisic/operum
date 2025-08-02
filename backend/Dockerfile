# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY src/Operum.API/*.csproj ./Operum.API/
COPY src/Operum.Model/*.csproj ./Operum.Model/
COPY src/Operum.Service/*.csproj ./Operum.Service/

COPY src/Operum.API/ ./Operum.API/
COPY src/Operum.Model/ ./Operum.Model/
COPY src/Operum.Service/ ./Operum.Service/

RUN dotnet restore Operum.API/Operum.API.csproj

RUN dotnet publish Operum.API/Operum.API.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "Operum.API.dll"]
