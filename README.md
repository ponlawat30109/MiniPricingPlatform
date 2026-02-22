# Mini Pricing Platform

Technical Assignment: Microservice-based Pricing Platform using .NET 9.

## Architecture Overvew

The system consists of two microservices:
1. **Rule Service**: Manages pricing rules (CRUD).
2. **Pricing Service**: Calculates quotes based on active rules.

### Design Principles
- **Microservices**: Decoupled services communicating via HTTP.
- **Modern C#**: Utilizes **C# 12/13** features such as Primary Constructors, Collection Expressions (`[]`), and Required/Init properties for safety and conciseness.
- **Background Worker**: Bulk jobs are processed asynchronously using `System.Threading.Channels` and `BackgroundService`.
- **In-Memory Storage**: Rules and Jobs are stored in-memory, initialized from `rules.json` (Rule Service).
- **SOLID**: Implementation follows clean code principles with Dependency Injection and Service layer separation.

## Setup Guide

### Prerequisites
- Docker & Docker Compose
- .NET 9 SDK (for local development)

### Running with Docker
```bash
docker-compose up --build
```
- **Pricing API**: [http://localhost:8080/swagger](http://localhost:8080/swagger)
- **Rule API**: [http://localhost:5000/swagger](http://localhost:5000/swagger)

## Sample Requests

### 1. Calculate Single Quote
**POST** `http://localhost:8080/quotes/price`
```json
{
  "weight": 15,
  "area": "Islands & Mountains"
}
```

### 2. Submit Bulk Job (JSON)
**POST** `http://localhost:8080/quotes/bulk`
```json
{
  "quotes": [
    { "weight": 5, "area": "City" },
    { "weight": 25, "area": "Islands & Mountains" }
  ]
}
```

### 3. Submit Bulk Job (CSV)
**POST** `http://localhost:8080/quotes/bulk`
- Header: `Content-Type: multipart/form-data`
- Body: `file=@data/bulk_quotes.csv`

```bash
curl -X POST http://localhost:8080/quotes/bulk -F "file=@data/bulk_quotes.csv"
```

### 4. Check Job Status
**GET** `http://localhost:8080/jobs/{job_id}`

## Testing
```bash
dotnet test
```
