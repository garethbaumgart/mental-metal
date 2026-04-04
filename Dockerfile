# Stage 1: Build Angular frontend
FROM node:22-alpine AS frontend-build
WORKDIR /app
COPY src/MentalMetal.Web/ClientApp/package*.json ./
RUN npm ci
COPY src/MentalMetal.Web/ClientApp/ ./
RUN npx ng build --configuration production

# Stage 2: Restore .NET dependencies
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY src/MentalMetal.slnx ./
COPY src/MentalMetal.Domain/MentalMetal.Domain.csproj MentalMetal.Domain/
COPY src/MentalMetal.Application/MentalMetal.Application.csproj MentalMetal.Application/
COPY src/MentalMetal.Infrastructure/MentalMetal.Infrastructure.csproj MentalMetal.Infrastructure/
COPY src/MentalMetal.Web/MentalMetal.Web.csproj MentalMetal.Web/
RUN dotnet restore MentalMetal.slnx

# Stage 3: Build and publish .NET app
FROM restore AS build
COPY src/ .
COPY --from=frontend-build /wwwroot/browser/ MentalMetal.Web/wwwroot/
RUN dotnet publish MentalMetal.Web/MentalMetal.Web.csproj -c Release -o /app/publish --no-restore

# Stage 4: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN adduser --disabled-password --no-create-home appuser
COPY --from=build /app/publish .
USER appuser
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "MentalMetal.Web.dll"]
