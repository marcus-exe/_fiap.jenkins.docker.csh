# Fixing Docker Permission Issue in Jenkins

## Problem
Jenkins is getting "Permission denied" when trying to access docker.

## Solutions

### Option 1: Recreate Jenkins Container (Recommended)

Stop and remove the current Jenkins container:

```bash
docker stop jenkins
docker rm jenkins
```

Recreate it with proper group setup:

```bash
docker run -d \
  --name jenkins \
  --group-add $(stat -c %g /var/run/docker.sock) \
  -p 8080:8080 \
  -p 50000:50000 \
  -v jenkins_home:/var/jenkins_home \
  -v /var/run/docker.sock:/var/run/docker.sock \
  jenkins/jenkins:lts
```

**Note:** This uses `--group-add` to add the jenkins user to the docker group, which gives it access to the docker socket without running as root.

### Option 2: Run Jenkins as Root (Quick Fix)

If you need a quick solution and don't mind running Jenkins as root:

```bash
docker stop jenkins
docker rm jenkins

docker run -d \
  --name jenkins \
  -u root \
  -p 8080:8080 \
  -p 50000:50000 \
  -v jenkins_home:/var/jenkins_home \
  -v /var/run/docker.sock:/var/run/docker.sock \
  jenkins/jenkins:lts
```

**Note:** Running as root (`-u root`) is less secure but will work immediately.

### Option 3: Install Docker inside Jenkins Container

SSH into your Jenkins container and install docker:

```bash
docker exec -it -u root jenkins bash
apt-get update
apt-get install -y docker.io docker-compose
```

Then restart Jenkins:
```bash
docker restart jenkins
```

### Option 4: Use Docker Compose for Jenkins

Create a `docker-compose.jenkins.yml` file and run:

```bash
docker-compose -f docker-compose.jenkins.yml up -d
```

This provides better configuration management.

## Verification

After recreating Jenkins, verify docker access works:

```bash
# Check if Jenkins is running
docker ps | grep jenkins

# Test docker access (if you can exec into container)
docker exec -u root jenkins docker --version
```

## Access Jenkins

After Jenkins restarts, it will still have your data (stored in the jenkins_home volume), but you may need to:
1. Wait 30-60 seconds for Jenkins to fully start
2. Access http://localhost:8080
3. Unlock Jenkins if prompted (check the initial admin password in the container logs)

## Update Jenkinsfile

I've updated the Jenkinsfile to include diagnostic stages. After fixing Jenkins docker access, the pipeline should work.

## Why This Happened

The error "Permission denied" occurs because:
- Your Jenkins container was mounted with docker socket
- But the jenkins user doesn't have permission to access it
- Even with `-u root`, the Jenkins service itself runs as the jenkins user

The `--group-add` solution adds jenkins to the docker group, which is the proper way to handle this.

