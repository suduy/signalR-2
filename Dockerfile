# 1. Use the ASP.NET runtime as the base image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# 2. Use the .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# LƯU Ý SỬA: Copy đúng cấu trúc thư mục để restore
COPY ["back-end/signalR/signalR/signalR.csproj", "back-end/signalR/signalR/"]
RUN dotnet restore "back-end/signalR/signalR/signalR.csproj"

# Copy the rest of the application code
COPY . .

# Build the application
WORKDIR /src/back-end/signalR/signalR
RUN dotnet build "signalR.csproj" -c Release -o /app/build

# 3. Publish the application
FROM build AS publish
# LƯU Ý SỬA: Bỏ dòng WORKDIR /src/back-end hoặc để nguyên thư mục hiện tại từ stage build
RUN dotnet publish "signalR.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Create the final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "signalR.dll"]