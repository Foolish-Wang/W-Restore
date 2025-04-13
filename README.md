# Restore - E-commerce Platform

A modern e-commerce platform built with ASP.NET Core and React, featuring product browsing, shopping cart functionality, user authentication, and payment processing.

## Technologies Used

- **Backend**: ASP.NET Core 7.0 API with Entity Framework Core
- **Frontend**: React 18 with TypeScript and Vite
- **Database**: PostgreSQL (containerized)
- **Authentication**: JWT-based authentication
- **Payment Processing**: Stripe integration
- **Styling**: Material-UI components

## Getting Started

Follow these steps to set up and run the project locally:

### Prerequisites

- [.NET SDK 7.0](https://dotnet.microsoft.com/download) or later
- [Node.js](https://nodejs.org/) (v16 or later)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/)

### 1. Clone the Repository

```bash
git clone https://github.com/Foolish-Wang/Restore.git
cd Restore
```

### 2. Set Up PostgreSQL with Docker

The project uses PostgreSQL in a Docker container:

```bash
# Start the PostgreSQL container
docker-compose up -d
```

Verify the container is running:

```bash
docker-compose ps
```

You should see the container `restore-db` running with status `Up`.

### 3. Configure Secret Keys

The project uses `dotnet user-secrets` to manage sensitive configuration:

```bash
# Initialize user secrets
cd API
dotnet user-secrets init

# Set required secrets
dotnet user-secrets set "JWTSettings:TokenKey" "your_secure_key_at_least_32_chars_long"
dotnet user-secrets set "StripeSettings:SecretKey" "your_stripe_secret_key"
dotnet user-secrets set "StripeSettings:WhSecret" "your_stripe_webhook_secret"

# Verify secrets
dotnet user-secrets list
```

### 4. Apply Database Migrations

Create the database schema using EF Core migrations:

```bash
# Ensure EF Core tools are installed
dotnet tool install --global dotnet-ef
# Or update if already installed
dotnet tool update --global dotnet-ef

# Apply migrations
cd API
dotnet ef database update
```

### 5. Run the API

Start the backend API:

```bash
cd API
dotnet run
```

The API will be available at http://localhost:5000.

### 6. Run the React Frontend

Open a new terminal window and start the frontend:

```bash
cd client
npm install
npm run dev
```

The frontend will be available at http://localhost:3000.

## Features

- Product catalog with filtering and sorting
- User registration and authentication
- Shopping cart functionality
- Order processing and history
- Admin panel for product management
- Payment processing with Stripe

## Development Notes

- Default Users:

  - Regular User: bob@test.com (Password: Pa$$w0rd)
  - Admin User: admin@test.com (Password: Pa$$w0rd)

- Configuration files:
  - Use `appsettings.Development.template.json` as a template
  - Actual sensitive values are stored in user secrets
