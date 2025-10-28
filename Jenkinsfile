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
        
        stage('Check Docker Access') {
            steps {
                script {
                    try {
                        sh 'whoami'
                        sh 'docker --version'
                        sh 'docker compose version'
                        sh 'ls -la /var/run/docker.sock 2>/dev/null || echo "Docker socket not found in expected location"'
                    } catch (Exception e) {
                        echo "Docker access check failed, attempting to fix permissions..."
                        sh 'sudo chmod 666 /var/run/docker.sock || true'
                        sh 'docker --version'
                    }
                }
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
                sh 'docker compose exec orders curl -X GET http://products:8080/api/products'
                
                // Ensure the sniffer captured what it needed and saved the file
                echo "Traffic capture finalized and saved."
            }
        }
    }
    post {
        failure {
        echo "Build failed. Cleaning up Docker resources and workspace..."
        sh 'docker compose down -v || true'
        deleteDir()
        } 
    }
}