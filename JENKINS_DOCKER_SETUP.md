# Configuring Docker Access in Jenkins

## Current Status
Jenkins container is running but needs Docker access configured.

## Steps to Configure

### 1. Access Jenkins
Open http://localhost:8080 in your browser

### 2. Get Initial Admin Password
```bash
docker logs jenkins | grep -A 5 "Please use the following password"
```
Copy the password shown.

### 3. Unlock Jenkins
- Paste the password in the Jenkins unlock page
- Click "Continue"

### 4. Install Recommended Plugins
- Click "Install recommended plugins"
- Wait for installation to complete

### 5. Create Admin User
- Set up your admin user credentials
- Click "Save and Continue"

### 6. Configure Docker Access

After Jenkins is set up, you need to configure Docker access. There are two approaches:

#### Option A: Install Docker in Jenkins Container (Recommended)

1. Install Docker inside the Jenkins container:
```bash
docker exec -u root -it jenkins bash
apt-get update
apt-get install -y docker.io docker-compose
exit
```

2. Restart Jenkins:
```bash
docker restart jenkins
```

#### Option B: Use Docker-in-Docker (DinD)

This approach runs a separate Docker daemon inside Jenkins:

1. Update your Jenkins run command to include DinD:
```bash
docker stop jenkins
docker rm jenkins

docker run -d \
  --name jenkins \
  -p 8080:8080 \
  -p 50000:50000 \
  -v jenkins_home:/var/jenkins_home \
  --privileged \
  docker:dind \
  jenkins/jenkins:lts
```

### 7. Configure Your Pipeline Job

1. In Jenkins, click "New Item"
2. Enter name: "fiap-pipeline"
3. Select "Pipeline"
4. Click OK
5. Under Pipeline configuration:
   - Definition: Pipeline script from SCM
   - SCM: Git
   - Repository URL: https://github.com/marcus-exe/_fiap.jenkins.docker.csh
   - Branch Specifier: */main
   - Script Path: Jenkinsfile
6. Click Save
7. Click "Build Now"

### 8. Test Docker Access

After configuring, you can test if Docker works by running a simple pipeline:

```groovy
pipeline {
    agent any
    stages {
        stage('Test Docker') {
            steps {
                sh 'docker --version'
                sh 'docker compose version'
            }
        }
    }
}
```

## Troubleshooting

### If Docker commands fail in pipeline:

1. Check if Jenkins user has docker access:
```bash
docker exec -u root jenkins whoami
docker exec jenkins groups
```

2. Check docker socket permissions:
```bash
docker exec jenkins ls -la /var/run/docker.sock
```

3. If needed, fix permissions:
```bash
docker exec -u root jenkins chmod 666 /var/run/docker.sock
```

## Quick Test Command

After completing setup, test Jenkins docker access:
```bash
docker exec -u root jenkins docker --version
```

If this works, your Jenkins pipelines should also work!

