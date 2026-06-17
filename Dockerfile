FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

# 1. Cài đặt công cụ dotnet-ef
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# 2. Tạo Migration Bundle thành một file thực thi tên là 'efbundle'
RUN dotnet ef migrations bundle \
    --project src/Infrastructure/Infrastructure.csproj \
    --startup-project src/WebAPI/WebAPI.csproj \
    --force \
    -o /app/publish/efbundle

# 3. Restore và Publish code như cũ
RUN dotnet restore "src/WebAPI/WebAPI.csproj"
RUN dotnet publish "src/WebAPI/WebAPI.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

# Cấp quyền thực thi cho file bundle
RUN chmod +x ./efbundle

ENTRYPOINT ["dotnet", "WebAPI.dll"]
