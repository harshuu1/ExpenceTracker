# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/PaisaFlow.Core/PaisaFlow.Core.csproj", "src/PaisaFlow.Core/"]
COPY ["src/PaisaFlow.Infrastructure/PaisaFlow.Infrastructure.csproj", "src/PaisaFlow.Infrastructure/"]
COPY ["src/PaisaFlow.Web/PaisaFlow.Web.csproj", "src/PaisaFlow.Web/"]

RUN dotnet restore "src/PaisaFlow.Web/PaisaFlow.Web.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/src/PaisaFlow.Web"
RUN dotnet publish "PaisaFlow.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:EnableSourceControlManagerQueries=false /p:EnableSourceLink=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PaisaFlow.Web.dll"]
