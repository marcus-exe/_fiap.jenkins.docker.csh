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

**Endpoints Públicos:**
- `POST /api/auth/login` - Autenticar e receber token JWT
- `GET /health` - Endpoint de verificação de saúde

**Endpoints Protegidos (Requerem Token JWT):**
- `GET /api/products` - Lista todos os produtos
- `GET /api/products/{id}` - Obtém produto por ID
- `POST /api/products` - Cria um novo produto

### Serviço de Pedidos (Porta 8083)

**Endpoints Públicos:**
- `POST /api/auth/login` - Autenticar e receber token JWT
- `GET /health` - Endpoint de verificação de saúde

**Endpoints Protegidos (Requerem Token JWT):**
- `GET /api/orders` - Lista todos os pedidos
- `GET /api/orders/{id}` - Obtém pedido por ID
- `POST /api/orders` - Cria um novo pedido

## 🔒 Segurança e Autenticação

### Autenticação JWT

Os serviços usam JWT (JSON Web Tokens) para autenticação. Todos os endpoints da API (exceto `/health` e `/api/auth/login`) requerem um token JWT válido.

**Como autenticar:**

1. **Login para obter um token JWT:**
```bash
# Login no Serviço de Produtos
curl -X POST http://localhost:8082/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Login no Serviço de Pedidos
curl -X POST http://localhost:8083/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

**Resposta:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600
}
```

2. **Usar o token nas requisições da API:**
```bash
# Obter produtos (requer token JWT)
curl -X GET http://localhost:8082/api/products \
  -H "Authorization: Bearer SEU_TOKEN_JWT_AQUI"

# Criar um pedido (requer token JWT)
curl -X POST http://localhost:8083/api/orders \
  -H "Authorization: Bearer SEU_TOKEN_JWT_AQUI" \
  -H "Content-Type: application/json" \
  -d '{"customerName":"Usuário Teste","productId":1,"quantity":1}'
```

**Usuários Padrão:**
- Usuário: `admin`, Senha: `admin123`
- Usuário: `user`, Senha: `user123`

**Configuração JWT:**
- Expiração do token: 1 hora
- Chave secreta: Configurada via variável de ambiente `JWT_SECRET`
- Emissor/Audiência: Configurado via variáveis de ambiente `JWT_ISSUER` e `JWT_AUDIENCE`

### Funcionalidades de Segurança Implementadas

✅ **Hash de Senhas**: Senhas são hasheadas usando BCrypt com work factor 12 (seguro e lento o suficiente para prevenir ataques de força bruta)

✅ **Rate Limiting**: Endpoints de login são protegidos com rate limiting (5 tentativas por 15 minutos por usuário) para prevenir ataques de força bruta

✅ **Validação de Entrada**: Todos os endpoints incluem validação abrangente de entrada usando anotações de dados e regras de negócio personalizadas

✅ **Segurança JWT**: 
- Validação do segredo JWT (mínimo de 32 caracteres obrigatório)
- Expiração de token (1 hora)
- Geração segura de token com claims JTI (JWT ID)

✅ **Boas Práticas de Segurança**:
- Senhas nunca são armazenadas em texto plano
- Existência de usuário não é revelada em login falho (previne enumeração de usuários)
- Rate limit é resetado em login bem-sucedido
- Validação de entrada previne ataques de injeção e dados inválidos

### Notas de Segurança e Recomendações

⚠️ **Limitações Atuais:**
- Serviços se comunicam via HTTP (inseguro) - tokens JWT são transmitidos em texto plano
- Armazenamento de usuários em memória (não persistente, apenas para demonstração)
- Rate limiting simples (em produção, use Redis ou serviço dedicado de rate limiting)

🔒 **Para Produção:**
- **Implementar HTTPS/TLS** para comunicação criptografada
- **Usar banco de dados** para armazenamento de usuários com hash de senhas adequado (já usando BCrypt)
- **Usar gerenciamento de segredos** (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault) para JWT_SECRET
- **Implementar rate limiting baseado em Redis** para sistemas distribuídos
- **Adicionar logging e monitoramento** para eventos de segurança
- **Considerar soluções de service mesh** (Istio, Linkerd) para mTLS entre serviços
- **Implementar políticas CORS** se expondo APIs para clientes web
- **Adicionar versionamento de API** para compatibilidade retroativa
- **Auditorias de segurança regulares** e atualizações de dependências

📝 **Capturas de Segurança:**
- O projeto inclui TShark para análise de tráfego de rede
- As capturas de segurança são salvas no diretório `captures/`
- Tokens JWT são encaminhados entre serviços para comunicação entre serviços

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
# Verificações de saúde (endpoints públicos)
curl http://localhost:8082/health
curl http://localhost:8083/health

# Primeiro, faça login para obter um token JWT
TOKEN=$(curl -s -X POST http://localhost:8082/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | jq -r '.token')

# Obter produtos (requer token JWT)
curl -X GET http://localhost:8082/api/products \
  -H "Authorization: Bearer $TOKEN"

# Obter pedidos (requer token JWT)
curl -X GET http://localhost:8083/api/orders \
  -H "Authorization: Bearer $TOKEN"

# Criar um novo pedido (testa comunicação entre serviços)
curl -X POST http://localhost:8083/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"customerName":"Usuário Teste","productId":1,"quantity":1}'
```

### Swagger UI

- Documentação da API de Produtos: http://localhost:8082/swagger
- Documentação da API de Pedidos: http://localhost:8083/swagger

### Testando a Captura de Rede com TShark

O sniffer TShark captura o tráfego de rede entre os serviços de pedidos e produtos. Aqui está como testá-lo:

#### 1. Verificar se o Container TShark está Rodando

```bash
# Verificar se o container sniffer está rodando
docker ps | grep tshark_sniffer

# Ver logs do TShark
docker logs tshark_sniffer

# Ou usando docker compose
docker compose logs sniffer

# Verificar todos os containers (incluindo os parados)
docker compose ps -a

# Se o container saiu, verificar os logs para erros
docker compose logs sniffer
```

**Nota:** O container TShark roda como usuário `root` (configurado no docker-compose.yml) que é necessário para permissões de captura de pacotes. Você pode ver um aviso sobre isso nos logs, o que é esperado e seguro para este caso de uso.

#### 2. Gerar Tráfego para Capturar

Como o TShark está configurado para capturar tráfego na porta 8080 entre os serviços de pedidos e produtos, gere alguma comunicação entre serviços:

```bash
# Criar um pedido (isso vai fazer o serviço de pedidos chamar o serviço de produtos)
curl -X POST http://localhost:8083/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerName":"Usuário Teste","productId":1,"quantity":1}'

# Fazer múltiplas requisições para gerar mais tráfego
for i in {1..5}; do
  curl -X POST http://localhost:8083/api/orders \
    -H "Content-Type: application/json" \
    -d "{\"customerName\":\"Usuário $i\",\"productId\":$i,\"quantity\":$i}"
  sleep 1
done
```

#### 3. Verificar o Arquivo de Captura

O arquivo de captura é escrito em `/captures/insecure_http.pcap` dentro do container (caminho absoluto a partir da raiz), mas devido ao volume mount (`./captures:/captures`), também é acessível na sua máquina host.

**Importante:** Dentro do container, use o caminho absoluto `/captures/insecure_http.pcap` (não caminhos relativos como `captures/insecure_http.pcap` que seriam relativos ao diretório de trabalho atual).

**Da máquina host (recomendado):**
```bash
# Listar arquivos de captura
ls -lh captures/

# Verificar se o arquivo pcap foi criado e tem conteúdo
ls -lh captures/insecure_http.pcap

# Ver informações básicas sobre o arquivo de captura (se você tem tshark instalado localmente)
tshark -r captures/insecure_http.pcap -c 10
```

**De dentro do container:**
```bash
# Entrar no container
docker exec -it tshark_sniffer sh

# Nota: O diretório de trabalho do container é /home/tshark, mas o arquivo de captura está na raiz
# Use o caminho absoluto /captures/insecure_http.pcap

# Verificar se o arquivo existe e seu tamanho
ls -lh /captures/insecure_http.pcap

# Ver pacotes capturados
tshark -r /captures/insecure_http.pcap -c 10

# Ver apenas tráfego HTTP
tshark -r /captures/insecure_http.pcap -Y http

# Sair do container
exit
```

**Verificação rápida sem entrar no container:**
```bash
# Ver pacotes diretamente do host
docker exec tshark_sniffer tshark -r /captures/insecure_http.pcap -c 10
```

#### 4. Analisar o Arquivo de Captura

Se você tem Wireshark ou tshark instalado localmente:

```bash
# Ver resumo de pacotes
tshark -r captures/insecure_http.pcap

# Ver informações detalhadas dos pacotes
tshark -r captures/insecure_http.pcap -V

# Filtrar apenas tráfego HTTP
tshark -r captures/insecure_http.pcap -Y http

# Ver requisições e respostas HTTP
tshark -r captures/insecure_http.pcap -Y http -T fields -e http.request.method -e http.request.uri -e http.response.code

# Abrir no Wireshark GUI (se instalado)
wireshark captures/insecure_http.pcap
```

#### 5. Testar o Container TShark Diretamente

Você também pode executar comandos diretamente no container TShark:

```bash
# Entrar no container
docker exec -it tshark_sniffer sh

# Dentro do container, você pode executar comandos tshark:
# Listar interfaces disponíveis
tshark -D

# Capturar tráfego ao vivo (se necessário)
tshark -i eth0 -f "port 8080" -c 10

# Sair do container
exit
```

#### 6. Verificar se a Captura está Funcionando

```bash
# Verificar logs do container para erros
docker compose logs sniffer

# Verificar se o arquivo de captura está sendo escrito
watch -n 1 'ls -lh captures/'

# Parar o sniffer e verificar o tamanho final do arquivo
docker compose stop sniffer
ls -lh captures/insecure_http.pcap
```

**Nota**: O container TShark usa `network_mode: service:orders`, o que significa que ele compartilha o namespace de rede com o serviço de pedidos. Isso permite que ele capture tráfego na mesma interface de rede que o serviço de pedidos usa para se comunicar com o serviço de produtos. O container roda como usuário `root` para ter as permissões necessárias para captura de pacotes. O arquivo de captura é escrito em `/captures/insecure_http.pcap` (caminho absoluto) dentro do container e é acessível no host via volume mount em `./captures/insecure_http.pcap`.

## 📝 Variáveis de Ambiente

### Serviço de Produtos

- `JWT_SECRET`: Chave secreta para assinatura de tokens JWT (padrão: chave demo hardcoded)
- `JWT_ISSUER`: Emissor do token JWT (padrão: `ProductsService`)
- `JWT_AUDIENCE`: Audiência do token JWT (padrão: `ProductsService`)
- `ASPNETCORE_ENVIRONMENT`: Configuração de ambiente (Docker, Development, Production)

### Serviço de Pedidos

- `PRODUCTS_URL`: URL interna do serviço de produtos (padrão: `http://products:8080`)
- `JWT_SECRET`: Chave secreta para assinatura de tokens JWT (deve corresponder ao Serviço de Produtos)
- `JWT_ISSUER`: Emissor do token JWT (deve corresponder ao Serviço de Produtos)
- `JWT_AUDIENCE`: Audiência do token JWT (deve corresponder ao Serviço de Produtos)
- `ASPNETCORE_ENVIRONMENT`: Configuração de ambiente (Docker, Development, Production)

**Importante:** Para produção, use chaves JWT fortes e únicas e armazene-as com segurança (ex: variáveis de ambiente, gerenciamento de segredos).

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

