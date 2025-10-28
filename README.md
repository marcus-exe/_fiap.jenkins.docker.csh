# Microservice Architecture with .NET

A microservice architecture demonstration project using C# and .NET, containerized with Docker and orchestrated via Docker Compose. This project includes a CI/CD pipeline configured for Jenkins.

[🇧🇷 Leia em Português](README.pt.md)

## 🏗️ Architecture

This project consists of two microservices:

- **Products Service** (`service-products`): C# .NET service for managing products
- **Orders Service** (`service-orders`): C# .NET service for managing orders that communicates with the products service

### Service Communication

- Services communicate internally using service names (e.g., `http://products:8080`)
- External access is mapped to different ports to avoid conflicts
- Products Service: `http://localhost:8082`
- Orders Service: `http://localhost:8083`

## 📋 Prerequisites

- Docker Desktop (or Docker Engine + Docker Compose)
- .NET SDK 8.0 (for local development, optional)
- Jenkins (configured for CI/CD, optional)

## 🚀 Getting Started

### Local Development

1. Clone the repository:
```bash
git clone https://github.com/marcus-exe/_fiap.jenkins.docker.csh.git
cd micro-service
```

2. Build and run the services:
```bash
docker compose up --build
```

3. Access the services:
- Products API: http://localhost:8082
- Orders API: http://localhost:8083
- Health check: http://localhost:8082/health
- Health check: http://localhost:8083/health

### Docker Compose Commands

- Start services: `docker compose up -d`
- Stop services: `docker compose down`
- View logs: `docker compose logs -f`
- Rebuild and restart: `docker compose up -d --build`

## 🔧 Jenkins Integration

### Jenkins Configuration

This repository is configured to work with Jenkins via SCM (Source Code Management). Your Jenkins instance should be running on port 8080 (as configured in your setup).

### Jenkins Setup

For Jenkins to work with Docker, it needs Docker installed inside the container. Here's how to set it up:

#### Initial Setup (Fresh Installation)

```bash
# Create Jenkins container
docker run -d \
  --name jenkins \
  -p 8080:8080 \
  -p 50000:50000 \
  -v jenkins_home:/var/jenkins_home \
  -v /var/run/docker.sock:/var/run/docker.sock \
  jenkins/jenkins:lts

# Install Docker inside Jenkins container (one-time setup)
docker exec -u root jenkins bash -c "apt-get update && apt-get install -y docker.io docker-compose"

# Fix Docker socket permissions
docker exec -u root jenkins chmod 666 /var/run/docker.sock

# Restart Jenkins
docker restart jenkins
```

#### Verify Docker Access

```bash
docker exec jenkins docker --version
docker exec jenkins docker compose version
```

### Creating a Jenkins Job

1. Create a new Pipeline job in Jenkins
2. Configure "Pipeline script from SCM"
3. Select Git as SCM
4. Add your repository URL
5. Set Branch Specifier to `*/main` (or your default branch)
6. Set Script Path to `Jenkinsfile`
7. Save and run the build

### Pipeline Stages

The Jenkins pipeline includes the following stages:

1. **Checkout**: Clones the repository via SCM
2. **Check Docker Access**: Verifies Docker and Docker Compose are available
3. **Build Images**: Builds Docker images for both services
4. **Deploy**: Starts the services in detached mode
5. **Post-Deploy and Security Tests**: Runs integration tests and inter-service communication
6. **Cleanup** (Post-action): Automatically cleans up containers after build

Note: The cleanup happens automatically via Jenkins post actions, so containers are removed after each pipeline run.

## 🗂️ Project Structure

```
micro-service/
├── docker-compose.yml       # Docker Compose orchestration
├── Jenkinsfile              # Jenkins CI/CD pipeline
├── service-products/        # Products microservice
│   ├── Dockerfile
│   ├── Products.Api.csproj
│   └── Program.cs
├── service-orders/          # Orders microservice
│   ├── Dockerfile
│   ├── Orders.Api.csproj
│   └── Program.cs
└── captures/                # Network capture files (created at runtime)
```

## 🌐 API Endpoints

### Products Service (Port 8082)

- `GET /api/products` - List all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create a new product
- `GET /health` - Health check endpoint

### Orders Service (Port 8083)

- `GET /api/orders` - List all orders
- `GET /api/orders/{id}` - Get order by ID
- `POST /api/orders` - Create a new order
- `GET /health` - Health check endpoint

## 🔒 Security Notes

- The project includes TShark for network traffic analysis
- Services currently communicate over HTTP (insecure)
- For production, consider implementing HTTPS and service mesh solutions
- Security captures are saved in the `captures/` directory

## 🐛 Troubleshooting

### Port Conflicts

If you encounter port conflicts:

```bash
# Check what's using the ports
lsof -i :8082
lsof -i :8083

# Or check with docker
docker ps
```

### Clean Docker Environment

```bash
# Remove all containers, networks, and volumes
docker compose down -v

# Remove all stopped containers
docker system prune -a
```

## 🧪 Testing

### Quick Test

```bash
# Health checks
curl http://localhost:8082/health
curl http://localhost:8083/health

# Get products
curl http://localhost:8082/api/products

# Get orders
curl http://localhost:8083/api/orders

# Create a new order (tests inter-service communication)
curl -X POST http://localhost:8083/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerName":"Test User","productId":1,"quantity":1}'
```

### Swagger UI

- Products API Docs: http://localhost:8082/swagger
- Orders API Docs: http://localhost:8083/swagger

## 📝 Environment Variables

### Orders Service

- `PRODUCTS_URL`: Internal URL of the products service (default: `http://products:8080`)
- `ASPNETCORE_ENVIRONMENT`: Environment setting (Docker, Development, Production)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📄 License

This project is provided as-is for educational and demonstration purposes.

## 👨‍💻 Author

Marcus Sena

