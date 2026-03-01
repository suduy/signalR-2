# Use the ASP.NET runtime as the base image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["back-end/BinanceRSIAPI.csproj", "back-end/"]
RUN dotnet restore "back-end/BinanceRSIAPI.csproj"

# Copy the rest of the application code
COPY . .

# Build the application
WORKDIR /src/back-end
RUN dotnet build "BinanceRSIAPI.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
WORKDIR /src/back-end
RUN dotnet publish "BinanceRSIAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Create the final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BinanceRSIAPI.dll"]
