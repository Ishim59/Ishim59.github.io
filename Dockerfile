FROM node:24-alpine AS frontend
WORKDIR /source
COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts
COPY . .
RUN npm run build:server

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /source
COPY Directory.Packages.props ./
COPY backend/Gymnasium.Api/Gymnasium.Api.csproj backend/Gymnasium.Api/
RUN dotnet restore backend/Gymnasium.Api/Gymnasium.Api.csproj
COPY backend/Gymnasium.Api/ backend/Gymnasium.Api/
RUN dotnet publish backend/Gymnasium.Api/Gymnasium.Api.csproj --no-restore -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend /publish/ ./
COPY --from=frontend /source/dist/ ./wwwroot/
COPY content/ ./content/
ENV ASPNETCORE_HTTP_PORTS=8080 Site__ContentRoot=/app/content Site__StaticRoot=/app/wwwroot
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Gymnasium.Api.dll"]

