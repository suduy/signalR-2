# 1. Sử dụng ASP.NET 10.0 runtime cho image cuối
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# Lưu ý: Từ .NET 8+, port mặc định là 8080 và 8081 (https) thay vì 80 và 443
EXPOSE 8080
EXPOSE 8081

# 2. Sử dụng .NET 10.0 SDK để build app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file và restore
COPY ["back-end/signalR/signalR/signalR.csproj", "back-end/signalR/signalR/"]
RUN dotnet restore "back-end/signalR/signalR/signalR.csproj"

# Copy toàn bộ mã nguồn còn lại
COPY . .

# Build project
WORKDIR /src/back-end/signalR/signalR
RUN dotnet build "signalR.csproj" -c Release -o /app/build

# 3. Publish project
FROM build AS publish
RUN dotnet publish "signalR.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Tạo image cuối cùng
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "signalR.dll"]