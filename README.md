# BestAuction

Full-stack auction application built with React, ASP.NET Core, Entity Framework Core and PostgreSQL.

## Backend features

- JWT registration and login with PBKDF2 password hashing.
- Public lot catalogue with pagination, search and category/status filters.
- Owner-protected lot creation, editing, deletion and completion.
- Transactional bidding with minimum steps, balance reservation and refunds.
- User profile, created-lot history and simulated balance top-ups.
- Category administration and lot image management.
- Central exception handling, CORS, Swagger JWT support and a health endpoint.
- Hybrid settlement schema prepared for future MetaMask/smart-contract auctions.

## Requirements

- .NET 10 SDK
- PostgreSQL
- Node.js and npm for the frontend

## Backend setup

1. Copy `.env.example` to `.env` in the repository root.
2. Set a PostgreSQL connection string and a random JWT secret of at least 32 bytes.
3. Restore and start the API:

```powershell
dotnet restore backend/Auction.slnx
dotnet run --project backend/Auction.API/Auction.API.csproj
```

Database migrations are applied automatically when the API starts. Development seed data is inserted only in the Development environment. Set `DevelopmentSeed__Password` in your local `.env` to create sample users and lots; without it, only sample categories are created.

Swagger is available at `/swagger` and the API health endpoint is available at `GET /api/health`.

### Development accounts

When `DevelopmentSeed__Password` is configured, both seeded accounts use that password:

- `user1@example.com` — regular user
- `admin@example.com` — administrator

These accounts are development-only and must not be used in production. Existing seeded accounts retain their old password hash; changing the environment variable does not reset them.

## Main API endpoints

Endpoints return JSON DTOs directly. Model-validation failures return `400`
with a problem-details body. Business-rule rejections (for example, a bid
below the minimum step) return `400` with a plain message string. Server
errors include a trace ID.

### Authentication

- `POST /api/auth/register`
- `POST /api/auth/login`

### Users

- `GET /api/users/me`
- `PUT /api/users/me`
- `POST /api/users/me/balance`

Balance top-up body:

```json
{
  "amount": 100
}
```

### Lots

- `GET /api/lots?page=1&pageSize=20&search=&categoryId=&status=`
- `GET /api/lots/{id}`
- `POST /api/lots`
- `PUT /api/lots/{id}`
- `DELETE /api/lots/{id}`
- `POST /api/lots/{id}/close`

### Bids

- `GET /api/lots/{lotId}/bids`
- `POST /api/bids`

### Images

- `POST /api/lots/{lotId}/images`
- `DELETE /api/lots/images/{imageId}`
- `PATCH /api/lots/{lotId}/images/{imageId}/set-main`

### Categories

- `GET /api/categories`
- `GET /api/categories/{id}`
- `POST /api/categories` — admin
- `PUT /api/categories/{id}` — admin
- `DELETE /api/categories/{id}` — admin

## Tests

```powershell
dotnet test backend/Auction.Tests/Auction.Tests.csproj
```

## Frontend setup

```powershell
cd frontend
npm install
npm run dev
```

- [![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=Project-Organization-2026_Auction-react-aspnet&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=Project-Organization-2026_Auction-react-aspnet)

- [![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=Project-Organization-2026_Auction-react-aspnet&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=Project-Organization-2026_Auction-react-aspnet)

- [![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=Project-Organization-2026_Auction-react-aspnet&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=Project-Organization-2026_Auction-react-aspnet)
[![Reliability issues](https://sonarcloud.io/api/project_badges/measure?project=Project-Organization-2026_Auction-react-aspnet&metric=software_quality_reliability_issues)](https://sonarcloud.io/summary/new_code?id=Project-Organization-2026_Auction-react-aspnet)

- [![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=Project-Organization-2026_Auction-react-aspnet&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=Project-Organization-2026_Auction-react-aspnet)
[![Security issues](https://sonarcloud.io/api/project_badges/measure?project=Project-Organization-2026_Auction-react-aspnet&metric=software_quality_security_issues)](https://sonarcloud.io/summary/new_code?id=Project-Organization-2026_Auction-react-aspnet)

- [![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=Project-Organization-2026_Auction-react-aspnet&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=Project-Organization-2026_Auction-react-aspnet)
[![Maintainability issues](https://sonarcloud.io/api/project_badges/measure?project=Project-Organization-2026_Auction-react-aspnet&metric=software_quality_maintainability_issues)](https://sonarcloud.io/summary/new_code?id=Project-Organization-2026_Auction-react-aspnet)
