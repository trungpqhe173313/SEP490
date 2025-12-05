# ================================
# Stage 1: Build
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution
COPY NutriBarn.sln ./

# Copy all csproj files (ĐÚNG TÊN FILE THỰC TẾ)
COPY NB.Model/NB.Model.csproj NB.Model/
COPY NB.Repository/NB.Repository.csproj NB.Repository/
COPY NB.Services/NB.Service.csproj NB.Services/
COPY NB.API/NB.API.csproj NB.API/
COPY NB.Tests/NB.Tests.csproj NB.Tests/

# Restore dependencies
RUN dotnet restore NutriBarn.sln

# Copy all source code
COPY NB.Model NB.Model/
COPY NB.Repository NB.Repository/
COPY NB.Services NB.Services/
COPY NB.API NB.API/
COPY NB.Tests NB.Tests/

# Build
WORKDIR /src/NB.API
RUN dotnet build -c Release -o /app/build

# ================================
# Stage 2: Publish
# ================================
FROM build AS publish
WORKDIR /src/NB.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ================================
# Stage 3: Runtime
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5224

# Copy published files
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "NB.API.dll"]