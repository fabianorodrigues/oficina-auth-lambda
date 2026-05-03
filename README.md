# oficina-auth-lambda

Lambda oficial de autenticacao da Fase 3 da Oficina API.

Este repositorio contem apenas o codigo serverless de autenticacao por CPF, o Lambda Authorizer JWT, testes automatizados e workflows de CI/deploy manual.


## Arquitetura

Fluxo de autenticacao:

```text
Cliente/Postman
  -> API Gateway
  -> POST /api/auth/cpf
  -> Lambda oficina-auth-cpf
  -> RDS SQL Server
  -> JWT
```

Fluxo de rotas protegidas:

```text
Cliente/Postman
  -> API Gateway
  -> Lambda oficina-jwt-authorizer
  -> API Oficina
```

`oficina-auth-cpf` fica dentro da VPC porque consulta o RDS. `oficina-jwt-authorizer` nao fica na VPC porque apenas valida JWT.

## Organizacao da solution

```text
src/Oficina.AuthLambda/
  Functions/
  Application/
  Domain/
  Infrastructure/
  Contracts/
  Configuration/
  Serialization/
  DependencyInjection.cs
```

Responsabilidades:

- `Functions`: entrada AWS Lambda, parse de eventos, mapeamento de responses e tratamento de erros controlados.
- `Application`: orquestracao dos casos de uso. `AuthService` autentica cliente/funcionario; `JwtAuthorizerService` valida o header Bearer e monta resposta do authorizer.
- `Domain`: conceitos puros como CPF, perfil, modelos de autenticacao e excecoes controladas.
- `Infrastructure`: detalhes externos como SQL Server, JWT, PasswordHasher e relogio do sistema.
- `Contracts`: DTOs JSON de entrada/saida da Lambda Auth e do Authorizer.
- `Configuration`: leitura e validacao de variaveis de ambiente.
- `Serialization`: serializer JSON usado pelo runtime Lambda.
- `DependencyInjection.cs`: cria um `ServiceProvider` unico por container Lambda e reaproveitado entre invocacoes.

## Variaveis de ambiente da Lambda

`oficina-auth-cpf`:

```text
ConnectionStrings__SqlServer
Jwt__Secret
Jwt__Issuer
Jwt__Audience
Jwt__ExpirationMinutes
```

`oficina-jwt-authorizer`:

```text
Jwt__Secret
Jwt__Issuer
Jwt__Audience
Jwt__ExpirationMinutes
```

Exemplo de connection string, sem valores reais:

```text
Server=<rds-endpoint>,1433;Database=OficinaDb;User Id=<usuario>;Password=<senha>;Encrypt=True;TrustServerCertificate=True
```

## GitHub Secrets

Configure em `Settings > Secrets and variables > Actions`:

```text
AWS_ACCESS_KEY_ID
AWS_SECRET_ACCESS_KEY
AWS_SESSION_TOKEN
AWS_REGION
DB_CONNECTION_STRING
JWT_SECRET
JWT_ISSUER
JWT_AUDIENCE
JWT_EXPIRATION_MINUTES
AWS_LAMBDA_ROLE_ARN
LAMBDA_SUBNET_IDS
LAMBDA_SECURITY_GROUP_IDS
```

`AWS_LAMBDA_ROLE_ARN` e opcional. Se nao for informado, o workflow monta:

```text
arn:aws:iam::<account-id>:role/LabRole
```

## VPC para acesso ao RDS

Use os outputs do repo `../oficina-infra-db` como referencia:

- `public_subnet_ids` -> secret `LAMBDA_SUBNET_IDS`, separado por virgula;
- `db_security_group_id` -> secret `LAMBDA_SECURITY_GROUP_IDS`.

Exemplo:

```text
LAMBDA_SUBNET_IDS=subnet-xxx,subnet-yyy
LAMBDA_SECURITY_GROUP_IDS=sg-xxx
DB_CONNECTION_STRING=Server=<db_address>,1433;Database=OficinaDb;User Id=<usuario>;Password=<senha>;Encrypt=True;TrustServerCertificate=True
```

Use sempre `lambda_security_group_ids`, nao o Security Group do RDS. O `oficina-jwt-authorizer` nao usa VPC e nao recebe `DB_CONNECTION_STRING`.

## Executar localmente

```powershell
dotnet restore Oficina.AuthLambda.sln
dotnet build Oficina.AuthLambda.sln --configuration Release
dotnet test Oficina.AuthLambda.sln --configuration Release
```

O projeto usa .NET 10 e runtime Lambda `dotnet10`. Se o AWS Academy nao aceitar `dotnet10`, o fallback e alterar os projetos/workflows para `net8.0` e o deploy para `dotnet8`.

## Deploy manual

No GitHub:

```text
Actions > Deploy Lambda > Run workflow
```

## Validar funcoes

```powershell
aws lambda get-function `
  --function-name oficina-auth-cpf `
  --region us-east-1

aws lambda get-function `
  --function-name oficina-jwt-authorizer `
  --region us-east-1
```

Resultado esperado: `Configuration.State = Active`.

## Invoke da Lambda Auth

Criar payload de cliente com evento HTTP API v2:

```powershell
@{version='2.0';headers=@{'content-type'='application/json'};body='{"cpf":"39053344705"}';isBase64Encoded=$false} | ConvertTo-Json -Compress | Set-Content payload-cliente.json
```

Invocar Auth:

```powershell
aws lambda invoke --function-name oficina-auth-cpf --payload file://payload-cliente.json --cli-binary-format raw-in-base64-out --region us-east-1 response-cliente.json
```

Ler response:

```powershell
Get-Content response-cliente.json -Raw
```

Extrair token:

```powershell
$token = ((Get-Content response-cliente.json -Raw | ConvertFrom-Json).body | ConvertFrom-Json).accessToken
```

Para funcionario/admin, crie o payload com senha:

```powershell
@{version='2.0';headers=@{'content-type'='application/json'};body='{"cpf":"39053344705","senha":"Senha@123"}';isBase64Encoded=$false} | ConvertTo-Json -Compress | Set-Content payload-funcionario.json
```

## Invoke do Authorizer

Criar payload do Authorizer:

```powershell
@{version='2.0';headers=@{authorization="Bearer $token"}} | ConvertTo-Json -Compress | Set-Content payload-authorizer.json
```

Invocar Authorizer:

```powershell
aws lambda invoke --function-name oficina-jwt-authorizer --payload file://payload-authorizer.json --cli-binary-format raw-in-base64-out --region us-east-1 response-authorizer.json
```

Ler response:

```powershell
Get-Content response-authorizer.json -Raw
```

Resposta autorizada esperada:

```json
{
  "isAuthorized": true,
  "context": {
    "cpf": "...",
    "role": "Cliente",
    "clienteId": "..."
  }
}
```

Sem token, token invalido ou token expirado retorna:

```json
{
  "isAuthorized": false
}
```

Testar token invalido:

```powershell
@{version='2.0';headers=@{authorization='Bearer token-invalido'}} | ConvertTo-Json -Compress | Set-Content payload-authorizer-invalido.json
```

```powershell
aws lambda invoke --function-name oficina-jwt-authorizer --payload file://payload-authorizer-invalido.json --cli-binary-format raw-in-base64-out --region us-east-1 response-authorizer-invalido.json
```

```powershell
Get-Content response-authorizer-invalido.json -Raw
```

Ver logs da Auth:

```powershell
aws logs tail /aws/lambda/oficina-auth-cpf --since 15m --follow --region us-east-1
```

Ver logs do Authorizer:

```powershell
aws logs tail /aws/lambda/oficina-jwt-authorizer --since 15m --follow --region us-east-1
```
