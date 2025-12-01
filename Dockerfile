# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Presentation/WMS.Api/WMS.Api.csproj", "src/Presentation/WMS.Api/"]
COPY ["src/Core/WMS.Application/WMS.Application.csproj", "src/Core/WMS.Application/"]
COPY ["src/Core/WMS.Domain/WMS.Domain.csproj", "src/Core/WMS.Domain/"]
COPY ["src/Infrastructure/WMS.Infrastructure/WMS.Infrastructure.csproj", "src/Infrastructure/WMS.Infrastructure/"]

RUN dotnet restore "src/Presentation/WMS.Api/WMS.Api.csproj"

# Copy the rest of the source code
COPY . .

# Build and Publish
WORKDIR "/src/src/Presentation/WMS.Api"
RUN dotnet build "WMS.Api.csproj" -c Release -o /app/build
RUN dotnet publish "WMS.Api.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WMS.Api.dll"]
