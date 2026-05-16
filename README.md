# oficina-auth-lambda

## Visão geral

Este repositório contém as funções serverless de autenticação da solução Oficina:

- `oficina-auth-cpf`: valida CPF, consulta cliente ou funcionário no SQL Server e emite JWT.
- `oficina-jwt-authorizer`: valida JWT nas rotas protegidas do API Gateway.

As Lambdas são publicadas depois do primeiro deploy da API, quando o banco já recebeu as migrations, e antes da criação do API Gateway.

## Diagrama de arquitetura

```text
POST /api/auth/cpf  ──►  Lambda oficina-auth-cpf
  { "cpf": "..." }        VPC, subnet privada
                               │
                               ├─ Valida CPF
                               ├─ Consulta cliente no RDS SQL Server
                               └─ Gera e retorna JWT

Authorization: Bearer <JWT>  ──►  Lambda oficina-jwt-authorizer
                                  sem VPC
                                        │
                                        └─ Valida assinatura JWT
                                           ├─ allow → encaminha para API
                                           └─ deny  → 403
```

## Tecnologias utilizadas

- .NET 10
- AWS Lambda com runtime `dotnet10`
- AWS API Gateway Authorizer
- AWS VPC
- AWS RDS SQL Server
- GitHub Actions

## Sequência de Deploy (modo padrão `terraform_nlb`)

| Passo | Repositório | O que provisiona |
|-------|-------------|-----------------|
| 1 | oficina-infra-db | VPC, subnets, RDS SQL Server |
| 2 | oficina-infra-k8s core | EKS, ECR, NLB interno |
| 3 | oficina-api | Migrations, Deployment, Service |
| **4** | **oficina-auth-lambda ← este** | Lambdas de autenticação |
| 5 | oficina-infra-k8s API Gateway | Entrada pública (HTTP API) |
| 6 | oficina-api (opcional) | Redeploy para URL pública em e-mails |

## Configuração necessária

Configure no GitHub Actions:

| Nome | Tipo | Uso |
| --- | --- | --- |
| `AWS_ACCESS_KEY_ID` | Secret | Autenticação AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret | Autenticação AWS |
| `AWS_SESSION_TOKEN` | Secret opcional | Credenciais temporárias |
| `AWS_REGION` | Secret | Região AWS |
| `AWS_LAMBDA_ROLE_ARN` | Secret | ARN da role IAM pré-existente de execução das Lambdas |
| `DB_CONNECTION_STRING` | Secret | String de conexão da Lambda Auth com o SQL Server |
| `LAMBDA_SUBNET_IDS` | Secret | IDs das subnets privadas da Lambda Auth, separados por vírgula |
| `LAMBDA_SECURITY_GROUP_IDS` | Secret | IDs dos Security Groups da Lambda Auth, separados por vírgula |
| `JWT_SECRET` | Secret | Chave de assinatura JWT (mínimo 32 caracteres) |
| `JWT_ISSUER` | Secret | Issuer JWT |
| `JWT_AUDIENCE` | Secret | Audience JWT |
| `JWT_EXPIRATION_MINUTES` | Secret | Tempo de expiração dos tokens em minutos |
| `AUTH_FUNCTION_NAME` | Variable opcional | Padrão `oficina-auth-cpf` |
| `AUTHORIZER_FUNCTION_NAME` | Variable opcional | Padrão `oficina-jwt-authorizer` |

**`JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` e `JWT_EXPIRATION_MINUTES` devem ser idênticos aos do `oficina-api`.**

A Lambda de autenticação usa VPC para acessar o RDS. A Lambda Authorizer não usa VPC nem connection string.

### Obtendo LAMBDA_SUBNET_IDS e LAMBDA_SECURITY_GROUP_IDS

Esses valores são provisionados pelo `oficina-infra-db`. Após o deploy desse repositório, obtenha os IDs via CLI:

```powershell
$env:AWS_REGION="<regiao>"
$env:PROJECT_NAME="oficina"
$env:ENVIRONMENT="<ambiente>"

# Subnet privada da Lambda (tag Name contendo o nome do projeto)
aws ec2 describe-subnets --region $env:AWS_REGION `
  --filters "Name=tag:Name,Values=*$($env:PROJECT_NAME)*private*" `
  --query "Subnets[*].SubnetId" --output text

# Security Group da Lambda Auth
aws ec2 describe-security-groups --region $env:AWS_REGION `
  --filters "Name=tag:Name,Values=*$($env:PROJECT_NAME)*lambda*" `
  --query "SecurityGroups[*].GroupId" --output text
```

Configure os IDs retornados como secrets `LAMBDA_SUBNET_IDS` e `LAMBDA_SECURITY_GROUP_IDS`, separados por vírgula quando houver mais de um valor.

## Como executar

O deploy manual deve ser executado a partir da branch `main`:

```text
GitHub Actions > Deploy Lambda > Run workflow
```

O workflow valida configuração, compila, testa, empacota, cria ou atualiza as duas Lambdas e valida a configuração final sem imprimir secrets, connection string, ARNs ou dados sensíveis.

## Como validar pela AWS

Console:

- Em Lambda, confirme as duas funções ativas.
- Na Lambda Auth, confirme VPC, subnets e Security Groups configurados.
- Na Lambda Authorizer, confirme ausência de VPC.
- Em Configuration, confirme variáveis JWT existentes sem expor seus valores.

CLI:

```powershell
$env:AWS_REGION="<regiao>"
$env:AUTH_FUNCTION_NAME="oficina-auth-cpf"
$env:AUTHORIZER_FUNCTION_NAME="oficina-jwt-authorizer"
$lambdaConfigQuery = '{State:State,LastUpdateStatus:LastUpdateStatus,Runtime:Runtime,Timeout:Timeout,MemorySize:MemorySize,SubnetCount:length(not_null(VpcConfig.SubnetIds, `[]`)),SecurityGroupCount:length(not_null(VpcConfig.SecurityGroupIds, `[]`))}'

aws lambda get-function-configuration --function-name $env:AUTH_FUNCTION_NAME --region $env:AWS_REGION --query $lambdaConfigQuery
aws lambda get-function-configuration --function-name $env:AUTHORIZER_FUNCTION_NAME --region $env:AWS_REGION --query $lambdaConfigQuery
```

Resultado esperado: Auth com `SubnetCount >= 1` e `SecurityGroupCount >= 1`; Authorizer com ambos iguais a `0`.

## Como validar localmente

Build e testes:

```powershell
dotnet restore Oficina.AuthLambda.sln
dotnet build Oficina.AuthLambda.sln --configuration Release --no-restore
dotnet test Oficina.AuthLambda.sln --configuration Release --no-build
```

Invocação com payloads de exemplo (requer `aws` CLI e Lambda já implantada).

Crie os arquivos na raiz do repositório com o conteúdo abaixo antes de executar os comandos de invocação.

`payload-cliente.json`:

```json
{
  "version": "2.0",
  "headers": {
    "content-type": "application/json"
  },
  "isBase64Encoded": false,
  "body": "{\"cpf\":\"<cpf-do-cliente>\"}"
}
```

`payload-authorizer.json`:

```json
{
  "version": "2.0",
  "headers": {
    "authorization": "Bearer <jwt-gerado-pela-lambda-auth>"
  }
}
```

No payload do authorizer, substitua `<jwt-gerado-pela-lambda-auth>` pelo token retornado pela Lambda `oficina-auth-cpf`.

```powershell
$env:AWS_REGION="<regiao>"
$env:AUTH_FUNCTION_NAME="oficina-auth-cpf"
$env:AUTHORIZER_FUNCTION_NAME="oficina-jwt-authorizer"

# Autenticar um cliente pelo CPF
aws lambda invoke --function-name $env:AUTH_FUNCTION_NAME --region $env:AWS_REGION `
  --payload file://payload-cliente.json --cli-binary-format raw-in-base64-out `
  response-local.json; Get-Content response-local.json

# Validar um JWT
aws lambda invoke --function-name $env:AUTHORIZER_FUNCTION_NAME --region $env:AWS_REGION `
  --payload file://payload-authorizer.json --cli-binary-format raw-in-base64-out `
  response-authorizer-local.json; Get-Content response-authorizer-local.json
```

## Próxima etapa

Aplicar o root `terraform/api-gateway` do `oficina-infra-k8s` para criar a entrada pública e integrar API, Lambda Auth e Lambda Authorizer.
