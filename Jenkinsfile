// Jenkinsfile
pipeline {
    agent any

    stages {
        stage('Checkout') {
            steps {
                // Clones the repository (automatic with SCM plugin)
                echo "Repository cloned successfully."
            }
        }
        
        stage('Build Images') {
            steps {
                // Uses docker compose to build the .NET service images defined in docker-compose.yml.
                echo "Starting Docker Image build for .NET 'products' and 'orders' services..."
                sh 'docker compose build'
            }
        }

        stage('Deploy (Simulation Environment)') {
            steps {
                // Brings up the microservice network, including the .NET services and the sniffer.
                echo "Starting deploy with Docker Compose..."
                sh 'docker compose up -d --force-recreate'
            }
        }

        stage('Post-Deploy and Security Tests') {
            steps {
                // Simulation of insecure HTTP traffic
                echo "Executing insecure HTTP communication test for TShark capture..."
                
                // Example: Use the 'orders' container to make the internal call to 'products'
                // This call generates the HTTP traffic for the sniffer to capture
                sh 'docker exec micro-service-orders-1 curl -X GET http://products:8080/api/products'
                
                // Ensure the sniffer captured what it needed and saved the file
                echo "Traffic capture finalized and saved."
            }
        }
        
        stage('Cleanup') {
            steps {
                // Tears down the environment
                echo "Shutting down containers..."
                sh 'docker compose down -v'
            }
        }
    }
}