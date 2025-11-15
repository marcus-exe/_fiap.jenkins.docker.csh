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
├── test-security.sh         # Automated security testing script
├── demo-tshark-jwt.sh       # TShark JWT capture demonstration
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

**Public Endpoints:**
- `POST /api/auth/login` - Authenticate and receive JWT token
- `GET /health` - Health check endpoint

**Protected Endpoints (Require JWT Token):**
- `GET /api/products` - List all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create a new product

### Orders Service (Port 8083)

**Public Endpoints:**
- `POST /api/auth/login` - Authenticate and receive JWT token
- `GET /health` - Health check endpoint

**Protected Endpoints (Require JWT Token):**
- `GET /api/orders` - List all orders
- `GET /api/orders/{id}` - Get order by ID
- `POST /api/orders` - Create a new order

## 🔒 Security & Authentication

### JWT Authentication

The services use JWT (JSON Web Tokens) for authentication. All API endpoints (except `/health` and `/api/auth/login`) require a valid JWT token.

**How to authenticate:**

1. **Login to get a JWT token:**
```bash
# Login to Products Service
curl -X POST http://localhost:8082/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Login to Orders Service
curl -X POST http://localhost:8083/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600
}
```

2. **Use the token in API requests:**
```bash
# Get products (requires JWT token)
curl -X GET http://localhost:8082/api/products \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"

# Create an order (requires JWT token)
curl -X POST http://localhost:8083/api/orders \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{"customerName":"Test User","productId":1,"quantity":1}'
```

**Default Users:**
- Username: `admin`, Password: `admin123`
- Username: `user`, Password: `user123`

**JWT Configuration:**
- Token expiration: 1 hour
- Secret key: Configured via `JWT_SECRET` environment variable
- Issuer/Audience: Configured via `JWT_ISSUER` and `JWT_AUDIENCE` environment variables

### Security Features Implemented

✅ **Password Hashing**: Passwords are hashed using BCrypt with work factor 12 (secure and slow enough to prevent brute force attacks)

✅ **Rate Limiting**: Login endpoints are protected with rate limiting (5 attempts per 15 minutes per username) to prevent brute force attacks

✅ **Input Validation**: All endpoints include comprehensive input validation using data annotations and custom business rules

✅ **JWT Security**: 
- JWT secret validation (minimum 32 characters required)
- Token expiration (1 hour)
- Secure token generation with JTI (JWT ID) claims

✅ **Security Best Practices**:
- Passwords are never stored in plain text
- User existence is not revealed on failed login (prevents user enumeration)
- Rate limit is reset on successful login
- Input validation prevents injection attacks and invalid data

### Security Notes & Recommendations

⚠️ **Current Limitations:**
- Services communicate over HTTP (insecure) - JWT tokens are transmitted in plain text
- In-memory user storage (not persistent, for demo purposes only)
- Simple rate limiting (in production, use Redis or dedicated rate limiting service)

🔒 **For Production:**
- **Implement HTTPS/TLS** for encrypted communication
- **Use a database** for user storage with proper password hashing (already using BCrypt)
- **Use secrets management** (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault) for JWT_SECRET
- **Implement Redis-based rate limiting** for distributed systems
- **Add logging and monitoring** for security events
- **Consider service mesh** solutions (Istio, Linkerd) for mTLS between services
- **Implement CORS policies** if exposing APIs to web clients
- **Add API versioning** for backward compatibility
- **Regular security audits** and dependency updates

📝 **Security Captures:**
- The project includes TShark for network traffic analysis
- Security captures are saved in the `captures/` directory
- JWT tokens are forwarded between services for inter-service communication

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
# Health checks (public endpoints)
curl http://localhost:8082/health
curl http://localhost:8083/health

# First, login to get a JWT token
TOKEN=$(curl -s -X POST http://localhost:8082/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | jq -r '.token')

# Get products (requires JWT token)
curl -X GET http://localhost:8082/api/products \
  -H "Authorization: Bearer $TOKEN"

# Get orders (requires JWT token)
curl -X GET http://localhost:8083/api/orders \
  -H "Authorization: Bearer $TOKEN"

# Create a new order (tests inter-service communication)
curl -X POST http://localhost:8083/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"customerName":"Test User","productId":1,"quantity":1}'
```

### Automated Security Testing

The project includes automated test scripts to verify all security features:

#### Security Test Script

Run the comprehensive security test suite:

```bash
./test-security.sh
```

This script automatically tests:
- ✅ Health endpoints accessibility
- ✅ Protected endpoints require authentication
- ✅ Login with correct credentials
- ✅ Login with incorrect password (rejected)
- ✅ User enumeration protection
- ✅ Rate limiting (5 attempts per 15 minutes)
- ✅ Protected endpoint access with valid token
- ✅ Input validation (invalid data rejected)
- ✅ Invalid token rejection

**Expected output:**
```
🔒 Security Features Test Script
================================

1. Testing Health Endpoints (Public)...
✅ PASS: Health checks accessible

2. Testing Protected Endpoints Without Token...
✅ PASS: Protected endpoints reject requests without token

...

Test Summary:
Passed: 11
Failed: 0

🎉 All security tests passed!
```

#### TShark JWT Capture Demonstration

Demonstrate that TShark can capture JWT tokens in plain text (because we're using HTTP):

```bash
./demo-tshark-jwt.sh
```

This script:
- Makes a login request to get a JWT token
- Makes an authenticated API request
- Shows that TShark captures all traffic in plain text
- Demonstrates why HTTPS is needed for production

**Note:** This demonstrates that JWT provides authentication but doesn't encrypt traffic. For production, implement HTTPS/TLS.

### Swagger UI

- Products API Docs: http://localhost:8082/swagger
- Orders API Docs: http://localhost:8083/swagger

### Testing TShark Network Capture

The TShark sniffer captures network traffic between the orders and products services. Here's how to test it:

#### 1. Verify TShark Container is Running

```bash
# Check if the sniffer container is running
docker ps | grep tshark_sniffer

# View TShark logs
docker logs tshark_sniffer

# Or using docker compose
docker compose logs sniffer

# Check all containers (including stopped ones)
docker compose ps -a

# If the container exited, check the logs for errors
docker compose logs sniffer
```

**Note:** The TShark container runs as `root` user (configured in docker-compose.yml) which is required for packet capture permissions. You may see a warning about this in the logs, which is expected and safe for this use case.

#### 2. Generate Traffic to Capture

Since TShark is configured to capture traffic on port 8080 between the orders and products services, generate some inter-service communication:

```bash
# Create an order (this will trigger orders service to call products service)
curl -X POST http://localhost:8083/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerName":"Test User","productId":1,"quantity":1}'

# Make multiple requests to generate more traffic
for i in {1..5}; do
  curl -X POST http://localhost:8083/api/orders \
    -H "Content-Type: application/json" \
    -d "{\"customerName\":\"User $i\",\"productId\":$i,\"quantity\":$i}"
  sleep 1
done
```

#### 3. Check the Capture File

The capture file is written to `/captures/insecure_http.pcap` inside the container (absolute path from root), but due to the volume mount (`./captures:/captures`), it's also accessible on your host machine.

**Important:** Inside the container, use the absolute path `/captures/insecure_http.pcap` (not relative paths like `captures/insecure_http.pcap` which would be relative to your current working directory).

**From the host machine (recommended):**
```bash
# List capture files
ls -lh captures/

# Check if the pcap file was created and has content
ls -lh captures/insecure_http.pcap

# View basic info about the capture file (if you have tshark installed locally)
tshark -r captures/insecure_http.pcap -c 10
```

**From inside the container:**
```bash
# Enter the container
docker exec -it tshark_sniffer sh

# Note: The container's working directory is /home/tshark, but the capture file is at the root
# Use the absolute path /captures/insecure_http.pcap

# Check if the file exists and its size
ls -lh /captures/insecure_http.pcap

# View captured packets
tshark -r /captures/insecure_http.pcap -c 10

# View HTTP traffic only
tshark -r /captures/insecure_http.pcap -Y http

# Exit the container
exit
```

**Quick check without entering the container:**
```bash
# View packets directly from host
docker exec tshark_sniffer tshark -r /captures/insecure_http.pcap -c 10
```

#### 4. Analyze the Capture File

If you have Wireshark or tshark installed locally:

```bash
# View packet summary
tshark -r captures/insecure_http.pcap

# View detailed packet information
tshark -r captures/insecure_http.pcap -V

# Filter for HTTP traffic only
tshark -r captures/insecure_http.pcap -Y http

# View HTTP requests and responses
tshark -r captures/insecure_http.pcap -Y http -T fields -e http.request.method -e http.request.uri -e http.response.code

# Open in Wireshark GUI (if installed)
wireshark captures/insecure_http.pcap
```

#### 5. Test TShark Container Directly

You can also execute commands directly in the TShark container:

```bash
# Enter the container
docker exec -it tshark_sniffer sh

# Inside the container, you can run tshark commands:
# List available interfaces
tshark -D

# Capture live traffic (if needed)
tshark -i eth0 -f "port 8080" -c 10

# Exit the container
exit
```

#### 6. Verify Capture is Working

```bash
# Check container logs for any errors
docker compose logs sniffer

# Verify the capture file is being written to
watch -n 1 'ls -lh captures/'

# Stop the sniffer and check final file size
docker compose stop sniffer
ls -lh captures/insecure_http.pcap
```

**Note**: The TShark container uses `network_mode: service:orders`, which means it shares the network namespace with the orders service. This allows it to capture traffic on the same network interface that the orders service uses to communicate with the products service. The container runs as `root` user to have the necessary permissions for packet capture. The capture file is written to `/captures/insecure_http.pcap` (absolute path) inside the container and is accessible on the host via the volume mount at `./captures/insecure_http.pcap`.

## 📝 Environment Variables

### Products Service

- `JWT_SECRET`: Secret key for JWT token signing (default: hardcoded demo key)
- `JWT_ISSUER`: JWT token issuer (default: `ProductsService`)
- `JWT_AUDIENCE`: JWT token audience (default: `ProductsService`)
- `ASPNETCORE_ENVIRONMENT`: Environment setting (Docker, Development, Production)

### Orders Service

- `PRODUCTS_URL`: Internal URL of the products service (default: `http://products:8080`)
- `JWT_SECRET`: Secret key for JWT token signing (must match Products Service)
- `JWT_ISSUER`: JWT token issuer (must match Products Service)
- `JWT_AUDIENCE`: JWT token audience (must match Products Service)
- `ASPNETCORE_ENVIRONMENT`: Environment setting (Docker, Development, Production)

**Important:** For production, use strong, unique JWT secrets and store them securely (e.g., environment variables, secrets management).

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

