# oficina-auth-lambda

## Visão geral

Repositório das funções serverless de autenticação e autorização da solução Oficina, em .NET 10:

- `oficina-auth-cpf`: valida CPF, consulta cliente ou funcionário no SQL Server e emite JWT.
- `oficina-jwt-authorizer`: valida JWT nas rotas protegidas do API Gateway.

As Lambdas são publicadas depois do primeiro deploy da API (banco já migrado) e antes da criação do API Gateway.

## Tecnologias utilizadas

- .NET 10
- AWS Lambda com runtime `dotnet10`
- AWS API Gateway Authorizer
- AWS VPC e RDS SQL Server
- AWS CloudWatch Logs (logs estruturados)
- GitHub Actions

## Solução integrada

A solução Oficina é composta por 4 repositórios independentes que, juntos, formam um sistema de gestão de oficina mecânica na AWS.

```mermaid
graph LR
  DB[oficina-infra-db<br/>VPC + RDS] --> K8S_CORE[oficina-infra-k8s core<br/>EKS + ECR + NLB]
  DB --> LMB[oficina-auth-lambda<br/>auth-cpf + jwt-authorizer]
  K8S_CORE --> API[oficina-api<br/>.NET 10 no EKS]
  K8S_CORE --> APIGW[oficina-infra-k8s api-gateway<br/>HTTP API + VPC Link]
  LMB --> APIGW
  API --> APIGW
```

| Passo | Repositório | Workflow | Quando aplicar |
|---|---|---|---|
| 1 | `oficina-infra-db` | Terraform Apply | sempre |
| 2 | `oficina-infra-k8s` root `terraform` (core) | Terraform Apply | sempre |
| 2a | `oficina-infra-k8s` root `terraform/addons` | Terraform Apply | apenas se `LOAD_BALANCER_PROVISIONING_MODE=aws_lbc` |
| 3 | `oficina-api` | Deploy API | sempre |
| 4 | `oficina-auth-lambda` | Deploy Lambda | sempre |
| 5 | `oficina-infra-k8s` root `terraform/api-gateway` | Terraform API Gateway Apply | sempre |
| 6 | `oficina-api` | Deploy API (redeploy) | se o pod precisar refletir `public-base-url` recém-criado em e-mails |

Cada README detalha apenas a responsabilidade do seu repositório. Para o passo a passo dos demais, consulte os READMEs correspondentes.

## Responsabilidade deste repositório

- Constrói o pacote ZIP .NET 10 das Lambdas, executa testes e empacota o artefato.
- Cria ou atualiza as duas funções Lambda na conta AWS (idempotente).
- Configura VPC e ambiente apenas onde aplicável: `auth-cpf` tem VPC e connection string; `jwt-authorizer` não.
- Não cria a IAM role das Lambdas (pré-requisito manual) nem o API Gateway (provisionado pelo `oficina-infra-k8s` root `api-gateway`).

## Arquitetura

```mermaid
graph LR
  subgraph DEPLOY[Deploy Lambda]
    GH[GitHub Actions] --> ZIP[Pacote ZIP .NET 10]
    ZIP --> AC[Lambda oficina-auth-cpf]
    ZIP --> AZ[Lambda oficina-jwt-authorizer]
  end
  Role[IAM Role manual] --> AC
  Role --> AZ
  subgraph VPC[VPC oficina - subnets privadas]
    AC -.acessa.-> RDS[(RDS SQL Server)]
  end
  APIGW{API Gateway HTTP} -. POST /api/auth/cpf .-> AC
  APIGW -. JWT Authorizer .-> AZ
```

## As duas Lambdas

| Função | Memória | Timeout | VPC | Acesso ao RDS | Variáveis |
| --- | --- | --- | --- | --- | --- |
| `oficina-auth-cpf` | 256 MB | 15 s | sim | sim | JWT (4) + `ConnectionStrings__SqlServer` |
| `oficina-jwt-authorizer` | 256 MB | 5 s | não | não | apenas JWT (4) |

## Pré-requisito manual — IAM Role das Lambdas

O workflow **não cria** a IAM role; ela é referenciada pelo Secret `AWS_LAMBDA_ROLE_ARN`. Uma única role é compartilhada pelas duas funções.

- Trust policy: `lambda.amazonaws.com`
- Políticas gerenciadas:
  - `AWSLambdaBasicExecutionRole` (logs CloudWatch — necessária para ambas)
  - `AWSLambdaVPCAccessExecutionRole` (necessária para `auth-cpf`; a `jwt-authorizer` herda sem prejuízo)

Para criar a role (PowerShell):

```powershell
$env:AWS_REGION="<regiao>"

$trust = @'
{
  "Version": "2012-10-17",
  "Statement": [
    { "Effect": "Allow", "Principal": { "Service": "lambda.amazonaws.com" }, "Action": "sts:AssumeRole" }
  ]
}
'@

$trust | Out-File -Encoding ascii -FilePath trust-lambda.json

aws iam create-role --role-name "oficina-auth-lambda-role" --assume-role-policy-document file://trust-lambda.json --query "Role.RoleName"

aws iam attach-role-policy --role-name "oficina-auth-lambda-role" --policy-arn "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
aws iam attach-role-policy --role-name "oficina-auth-lambda-role" --policy-arn "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"

aws iam get-role --role-name "oficina-auth-lambda-role" --query "Role.Arn" --output text
```

Configure o ARN retornado como o Secret `AWS_LAMBDA_ROLE_ARN`.

## Valores consumidos

| Origem | Valor | Como é consumido |
| --- | --- | --- |
| `oficina-infra-db` | subnets privadas e SG da Lambda | configurados como CSV em `LAMBDA_SUBNET_IDS` e `LAMBDA_SECURITY_GROUP_IDS` |
| `oficina-infra-db` | endpoint, porta e nome do banco | compõem `DB_CONNECTION_STRING` |
| `oficina-api` | JWT `secret/issuer/audience/expiration` | devem ser **idênticos** aos configurados no `oficina-api` |

## Valores gerados

- Função Lambda `oficina-auth-cpf` (ou nome customizado via `AUTH_FUNCTION_NAME`) — consumida pelo root `api-gateway` do `oficina-infra-k8s` como integração da rota `POST /api/auth/cpf`.
- Função Lambda `oficina-jwt-authorizer` (ou nome customizado via `AUTHORIZER_FUNCTION_NAME`) — consumida pelo root `api-gateway` como authorizer da rota `ANY /api/{proxy+}`.

## Configuração necessária

> **JWT idêntico**: `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` e `JWT_EXPIRATION_MINUTES` devem ser os mesmos valores configurados no `oficina-api`. Tokens emitidos por estas Lambdas só são validados pela API se as quatro variáveis baterem.

| Nome | Tipo | Obrigatório | Origem ou Default | Descrição |
| --- | --- | --- | --- | --- |
| `AWS_ACCESS_KEY_ID` | Secret | Sim | — | Credencial AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret | Sim | — | Credencial AWS |
| `AWS_SESSION_TOKEN` | Secret | Não | — | Credenciais temporárias (STS) |
| `AWS_REGION` | Secret | Sim | — | Região AWS |
| `AWS_LAMBDA_ROLE_ARN` | Secret | Sim | Criada manualmente (ver pré-requisito) | ARN da IAM role compartilhada |
| `DB_CONNECTION_STRING` | Secret | Sim | Composto a partir de `oficina-infra-db` | Connection string da Lambda Auth com o SQL Server |
| `LAMBDA_SUBNET_IDS` | Secret | Sim | CSV obtido de `oficina-infra-db` | IDs das subnets privadas (separados por vírgula) |
| `LAMBDA_SECURITY_GROUP_IDS` | Secret | Sim | CSV obtido de `oficina-infra-db` | IDs dos Security Groups (separados por vírgula) |
| `JWT_SECRET` | Secret | Sim | Idêntico ao `oficina-api` | Chave de assinatura JWT (mínimo 32 caracteres) |
| `JWT_ISSUER` | Secret | Sim | Idêntico ao `oficina-api` | Issuer JWT |
| `JWT_AUDIENCE` | Secret | Sim | Idêntico ao `oficina-api` | Audience JWT |
| `JWT_EXPIRATION_MINUTES` | Secret | Sim | Idêntico ao `oficina-api` | Expiração dos tokens em minutos |
| `AUTH_FUNCTION_NAME` | Variable | Não | `oficina-auth-cpf` | Nome da Lambda de autenticação |
| `AUTHORIZER_FUNCTION_NAME` | Variable | Não | `oficina-jwt-authorizer` | Nome da Lambda authorizer |

### Auto-provisionado pelo workflow

- Criação ou atualização das duas funções Lambda com runtime, memória, timeout, VPC config (apenas na `auth-cpf`) e variáveis de ambiente.

### Obtendo LAMBDA_SUBNET_IDS e LAMBDA_SECURITY_GROUP_IDS

Após o deploy do `oficina-infra-db`:

```powershell
$env:AWS_REGION="<regiao>"
$env:PROJECT_NAME="oficina"

aws ec2 describe-subnets --region $env:AWS_REGION `
  --filters "Name=tag:Name,Values=*$($env:PROJECT_NAME)*private*" `
  --query "Subnets[*].SubnetId" --output text

aws ec2 describe-security-groups --region $env:AWS_REGION `
  --filters "Name=tag:Name,Values=*$($env:PROJECT_NAME)*lambda*" `
  --query "SecurityGroups[*].GroupId" --output text
```

Configure os valores como `LAMBDA_SUBNET_IDS` e `LAMBDA_SECURITY_GROUP_IDS`, separados por vírgula quando houver mais de um ID.

## Como executar

O deploy manual deve ser disparado a partir da branch `main`:

```text
GitHub Actions > Deploy Lambda > Run workflow
```

O workflow valida configuração, compila, testa, empacota, cria ou atualiza as duas Lambdas e valida a configuração final sem imprimir secrets, connection string, ARNs ou dados sensíveis.

## Como validar pela AWS

### Console

- Em Lambda, confirme as duas funções ativas.
- Na Lambda Auth, confirme VPC, subnets e Security Groups configurados.
- Na Lambda Authorizer, confirme ausência de VPC.
- Em Configuration > Environment variables, confirme variáveis JWT existentes sem expor seus valores.

### CLI (PowerShell)

```powershell
$env:AWS_REGION="<regiao>"
$env:AUTH_FUNCTION_NAME="oficina-auth-cpf"
$env:AUTHORIZER_FUNCTION_NAME="oficina-jwt-authorizer"
$lambdaConfigQuery = '{State:State,LastUpdateStatus:LastUpdateStatus,Runtime:Runtime,Timeout:Timeout,MemorySize:MemorySize,SubnetCount:length(not_null(VpcConfig.SubnetIds, `[]`)),SecurityGroupCount:length(not_null(VpcConfig.SecurityGroupIds, `[]`))}'

aws lambda get-function-configuration --function-name $env:AUTH_FUNCTION_NAME --region $env:AWS_REGION --query $lambdaConfigQuery
aws lambda get-function-configuration --function-name $env:AUTHORIZER_FUNCTION_NAME --region $env:AWS_REGION --query $lambdaConfigQuery
```

Resultado esperado: `auth-cpf` com `SubnetCount >= 1` e `SecurityGroupCount >= 1`; `authorizer` com ambos iguais a `0`.

## Como executar localmente

Não há Docker Compose. Localmente é possível apenas compilar e rodar os testes unitários. Validação funcional requer Lambda já implantada.

Build e testes:

```powershell
dotnet restore Oficina.AuthLambda.sln
dotnet build Oficina.AuthLambda.sln --configuration Release --no-restore
dotnet test Oficina.AuthLambda.sln --configuration Release --no-build
```

Invocação com payloads de exemplo (requer AWS CLI e Lambdas já implantadas). Crie os arquivos na raiz do repositório:

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

aws lambda invoke --function-name $env:AUTH_FUNCTION_NAME --region $env:AWS_REGION `
  --payload file://payload-cliente.json --cli-binary-format raw-in-base64-out `
  response-local.json; Get-Content response-local.json

aws lambda invoke --function-name $env:AUTHORIZER_FUNCTION_NAME --region $env:AWS_REGION `
  --payload file://payload-authorizer.json --cli-binary-format raw-in-base64-out `
  response-authorizer-local.json; Get-Content response-authorizer-local.json
```

## Monitoramento e Observabilidade

As Lambdas emitem logs estruturados em JSON no CloudWatch usando `correlationId = context.AwsRequestId`. Registram sucesso e falha de autenticação por CPF e allow, deny ou falha do authorizer, sem expor CPF completo, senha, JWT ou connection string.

### Configurar

- Não há secrets adicionais. A IAM role configurada como `AWS_LAMBDA_ROLE_ARN` já tem `AWSLambdaBasicExecutionRole` (pré-requisito), o que habilita logs em CloudWatch automaticamente.
- Não há New Relic Lambda Forwarder nem New Relic Lambda Layer como requisito padrão. A coleta centralizada pode ser feita posteriormente por estratégia de logs da conta, sem acoplar IAM adicional a estas funções.

### Executar

Nada a executar. Após o deploy, os logs são publicados automaticamente em CloudWatch Logs nos grupos `/aws/lambda/<AUTH_FUNCTION_NAME>` e `/aws/lambda/<AUTHORIZER_FUNCTION_NAME>`.

### Validar

Console (CloudWatch > Logs > Log groups):

- Invoque a Lambda Auth com payload válido e confirme log com `eventType = AutenticacaoCpf`, `outcome = success` e `correlationId`.
- Invoque a Lambda Auth com payload inválido e confirme `outcome = failure`, sem CPF completo no log.
- Invoque o Authorizer com token válido e inválido e confirme `eventType = JwtAuthorizer` com `outcome = allow` ou `deny`.
- Confirme que os logs não contêm `Authorization`, JWT, senha, connection string nem CPF completo.

CLI (PowerShell):

```powershell
$env:AWS_REGION="<regiao>"
$env:AUTH_FUNCTION_NAME="oficina-auth-cpf"

aws logs describe-log-streams --log-group-name "/aws/lambda/$($env:AUTH_FUNCTION_NAME)" `
  --region $env:AWS_REGION --order-by LastEventTime --descending --max-items 1 `
  --query "logStreams[0].logStreamName"

aws logs filter-log-events --log-group-name "/aws/lambda/$($env:AUTH_FUNCTION_NAME)" `
  --region $env:AWS_REGION --filter-pattern "eventType" --max-items 5 `
  --query "events[*].message"
```

## Próxima etapa

Aplicar o root `terraform/api-gateway` do `oficina-infra-k8s` para criar a entrada pública e integrar a API, a Lambda Auth e a Lambda Authorizer.
