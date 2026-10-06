FROM node:22-alpine AS frontend-build
WORKDIR /src/frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
ARG VITE_GANACHE_RPC_URL
RUN npm run build

FROM node:22-alpine AS contract-build
WORKDIR /src/blockchain
COPY blockchain/package*.json ./
RUN npm ci
COPY blockchain/ ./
RUN npx hardhat compile

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src
COPY backend/Auction.API/Auction.API.csproj backend/Auction.API/
COPY backend/Auction.BLL/Auction.BLL.csproj backend/Auction.BLL/
COPY backend/Auction.DAL/Auction.DAL.csproj backend/Auction.DAL/
RUN dotnet restore backend/Auction.API/Auction.API.csproj
COPY backend/ backend/
RUN dotnet publish backend/Auction.API/Auction.API.csproj -c Release -o /publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=backend-build /publish ./
COPY --from=frontend-build /src/frontend/dist ./wwwroot/
COPY --from=contract-build /src/blockchain/artifacts/contracts/Auction.sol/Auction.json ./contracts/Auction.json
RUN mkdir -p ./wwwroot/uploads/lots && chown -R app:app ./wwwroot/uploads
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
USER app
ENTRYPOINT ["dotnet", "Auction.API.dll"]
