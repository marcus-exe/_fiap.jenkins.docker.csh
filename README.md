# Microservice Architecture with .NET

A microservice architecture demonstration project using C# and .NET, containerized with Docker and orchestrated via Docker Compose. This project includes a CI/CD pipeline configured for Jenkins.

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
git clone <repository-url>
cd micro-service
```

2. Build and run the services:
```bash
docker-compose up --build
```

3. Access the services:
- Products API: http://localhost:8082
- Orders API: http://localhost:8083
- Health check: http://localhost:8082/health
- Health check: http://localhost:8083/health

### Docker Compose Commands

- Start services: `docker-compose up -d`
- Stop services: `docker-compose down`
- View logs: `docker-compose logs -f`
- Rebuild and restart: `docker-compose up -d --build`

## 🔧 Jenkins Integration

### Jenkins Configuration

This repository is configured to work with Jenkins via SCM (Source Code Management). Your Jenkins instance should be running on port 8080 (as configured in your setup).

### Jenkins Setup

If you haven't already set up Jenkins, you can run it with:

```bash
docker run -d \
  --name jenkins \
  -p 8080:8080 \
  -p 50000:50000 \
  -u root \
  -v jenkins_home:/var/jenkins_home \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v /usr/local/bin/docker:/usr/local/bin/docker \
  jenkins/jenkins:lts
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
2. **Build Images**: Builds Docker images for both services
3. **Deploy**: Starts the services in detached mode
4. **Post-Deploy and Security Tests**: Runs integration tests and captures traffic
5. **Cleanup**: Tears down the environment

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
docker-compose down -v

# Remove all stopped containers
docker system prune -a
```

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

Marcus Eduardo Sena

