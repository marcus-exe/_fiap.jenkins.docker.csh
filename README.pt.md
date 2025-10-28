# Arquitetura de Microsserviços com .NET

Um projeto de demonstração de arquitetura de microsserviços usando C# e .NET, containerizado com Docker e orquestrado via Docker Compose. Este projeto inclui um pipeline de CI/CD configurado para Jenkins.

[🇺🇸 Read in English](README.md)

## 🏗️ Arquitetura

Este projeto consiste em dois microsserviços:

- **Serviço de Produtos** (`service-products`): Serviço C# .NET para gerenciar produtos
- **Serviço de Pedidos** (`service-orders`): Serviço C# .NET para gerenciar pedidos que se comunica com o serviço de produtos

### Comunicação dos Serviços

- Os serviços se comunicam internamente usando nomes de serviços (ex: `http://products:8080`)
- O acesso externo é mapeado para portas diferentes para evitar conflitos
- Serviço de Produtos: `http://localhost:8082`
- Serviço de Pedidos: `http://localhost:8083`

## 📋 Pré-requisitos

- Docker Desktop (ou Docker Engine + Docker Compose)
- .NET SDK 8.0 (para desenvolvimento local, opcional)
- Jenkins (configurado para CI/CD, opcional)

## 🚀 Começando

### Desenvolvimento Local

1. Clone o repositório:
```bash
git clone https://github.com/marcus-exe/_fiap.jenkins.docker.csh.git
cd micro-service
```

2. Construa e execute os serviços:
```bash
docker compose up --build
```

3. Acesse os serviços:
- Products API: http://localhost:8082
- Orders API: http://localhost:8083
- Verificação de saúde: http://localhost:8082/health
- Verificação de saúde: http://localhost:8083/health

### Comandos do Docker Compose

- Iniciar serviços: `docker compose up -d`
- Parar serviços: `docker compose down`
- Ver logs: `docker compose logs -f`
- Reconstruir e reiniciar: `docker compose up -d --build`

## 🔧 Integração com Jenkins

### Configuração do Jenkins

Este repositório está configurado para trabalhar com Jenkins via SCM (Gerenciamento de Código Fonte). Sua instância Jenkins deve estar rodando na porta 8080 (conforme configurado em sua configuração).

### Configuração do Jenkins

Para o Jenkins funcionar com Docker, ele precisa do Docker instalado dentro do container. Aqui está como configurar:

#### Configuração Inicial (Instalação Fresca)

```bash
# Criar container do Jenkins
docker run -d \
  --name jenkins \
  -p 8080:8080 \
  -p 50000:50000 \
  -v jenkins_home:/var/jenkins_home \
  -v /var/run/docker.sock:/var/run/docker.sock \
  jenkins/jenkins:lts

# Instalar Docker dentro do container do Jenkins (configuração única)
docker exec -u root jenkins bash -c "apt-get update && apt-get install -y docker.io docker-compose"

# Corrigir permissões do socket do Docker
docker exec -u root jenkins chmod 666 /var/run/docker.sock

# Reiniciar Jenkins
docker restart jenkins
```

#### Verificar Acesso ao Docker

```bash
docker exec jenkins docker --version
docker exec jenkins docker compose version
```

### Criando um Job no Jenkins

1. Crie um novo job Pipeline no Jenkins
2. Configure "Pipeline script from SCM"
3. Selecione Git como SCM
4. Adicione o URL do seu repositório
5. Defina Branch Specifier para `*/main` (ou seu branch padrão)
6. Defina Script Path para `Jenkinsfile`
7. Salve e execute o build

### Estágios do Pipeline

O pipeline do Jenkins inclui os seguintes estágios:

1. **Checkout**: Clona o repositório via SCM
2. **Check Docker Access**: Verifica se Docker e Docker Compose estão disponíveis
3. **Build Images**: Constrói imagens Docker para ambos os serviços
4. **Deploy**: Inicia os serviços em modo detached
5. **Post-Deploy and Security Tests**: Executa testes de integração e comunicação entre serviços
6. **Cleanup** (Post-action): Limpa automaticamente os containers após o build

Nota: A limpeza acontece automaticamente via ações post do Jenkins, então os containers são removidos após cada execução do pipeline.

## 🗂️ Estrutura do Projeto

```
micro-service/
├── docker-compose.yml       # Orquestração Docker Compose
├── Jenkinsfile              # Pipeline CI/CD Jenkins
├── service-products/        # Microsserviço de produtos
│   ├── Dockerfile
│   ├── Products.Api.csproj
│   └── Program.cs
├── service-orders/          # Microsserviço de pedidos
│   ├── Dockerfile
│   ├── Orders.Api.csproj
│   └── Program.cs
└── captures/                # Arquivos de captura de rede (criados em tempo de execução)
```

## 🌐 API Endpoints

### Serviço de Produtos (Porta 8082)

- `GET /api/products` - Lista todos os produtos
- `GET /api/products/{id}` - Obtém produto por ID
- `POST /api/products` - Cria um novo produto
- `GET /health` - Endpoint de verificação de saúde

### Serviço de Pedidos (Porta 8083)

- `GET /api/orders` - Lista todos os pedidos
- `GET /api/orders/{id}` - Obtém pedido por ID
- `POST /api/orders` - Cria um novo pedido
- `GET /health` - Endpoint de verificação de saúde

## 🔒 Notas de Segurança

- O projeto inclui TShark para análise de tráfego de rede
- Os serviços atualmente se comunicam via HTTP (inseguro)
- Para produção, considere implementar HTTPS e soluções de service mesh
- As capturas de segurança são salvas no diretório `captures/`

## 🐛 Solução de Problemas

### Conflitos de Porta

Se você encontrar conflitos de porta:

```bash
# Verificar o que está usando as portas
lsof -i :8082
lsof -i :8083

# Ou verificar com docker
docker ps
```

### Limpar Ambiente Docker

```bash
# Remover todos os containers, redes e volumes
docker compose down -v

# Remover todos os containers parados
docker system prune -a
```

## 🧪 Testes

### Teste Rápido

```bash
# Verificações de saúde
curl http://localhost:8082/health
curl http://localhost:8083/health

# Obter produtos
curl http://localhost:8082/api/products

# Obter pedidos
curl http://localhost:8083/api/orders

# Criar um novo pedido (testa comunicação entre serviços)
curl -X POST http://localhost:8083/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerName":"Test User","productId":1,"quantity":1}'
```

### Swagger UI

- Documentação da API de Produtos: http://localhost:8082/swagger
- Documentação da API de Pedidos: http://localhost:8083/swagger

## 📝 Variáveis de Ambiente

### Serviço de Pedidos

- `PRODUCTS_URL`: URL interna do serviço de produtos (padrão: `http://products:8080`)
- `ASPNETCORE_ENVIRONMENT`: Configuração de ambiente (Docker, Development, Production)

## 🤝 Contribuindo

1. Faça um fork do repositório
2. Crie uma branch de funcionalidade
3. Faça commit das suas mudanças
4. Envie para a branch
5. Crie um Pull Request

## 📄 Licença

Este projeto é fornecido como está para fins educacionais e de demonstração.

## 👨‍💻 Autor

Marcus Sena

