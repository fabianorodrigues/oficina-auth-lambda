# oficina-auth-lambda

Lambda oficial de autenticacao da Fase 3 da Oficina API.

Este repositorio contem apenas o codigo serverless de autenticacao por CPF/senha, o Lambda Authorizer JWT, testes automatizados e workflows de CI/deploy manual. A API principal fica no repositorio irmao `../oficina-api`; a infraestrutura do RDS SQL Server fica em `../oficina-infra-db`.

Nao ha Terraform, Kubernetes, Dockerfile da API, `.env` real, secrets reais, `terraform.tfvars` ou pasta `docs` neste repositorio.

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

A `SqlConnectionFactory` pode ser singleton, mas cada repository abre e descarta uma nova `SqlConnection` por operacao. Nenhuma conexao SQL e mantida como singleton.

## Compatibilidade com oficina-api

O contrato foi alinhado com `../oficina-api`:

- rota original: `POST /api/auth/cpf`;
- cliente: tabela `Clientes`, coluna `Documento`;
- funcionario/admin: tabela `Funcionarios`, colunas `Cpf`, `SenhaHash`, `Perfil`, `Ativo`;
- perfil interno: `1 = Funcionario`, `2 = Admin`;
- senha: `Microsoft.AspNetCore.Identity.PasswordHasher<object>`;
- roles: `Cliente`, `Funcionario`, `Admin`;
- JWT HS256 com `ClaimTypes.Role`, `cpf`, `clienteId` ou `funcionarioId`;
- issuer/audience padrao local: `Oficina.Api`.

O schema atual de `Clientes` nao possui coluna `Ativo`; por isso clientes encontrados no banco sao considerados ativos.

## Padrao de idioma

- README: portugues.
- Logs, excecoes tecnicas e mensagens internas: ingles.
- Campos JSON permanecem exatamente como a API espera: `cpf`, `senha`, `accessToken`, `expiresIn`, `perfil`, `clienteId`, `funcionarioId`, `erro`, `isAuthorized`, `context`.
- Valores de perfil nao sao traduzidos: `Cliente`, `Funcionario`, `Admin`.

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

No AWS Academy Learner Lab, use a role existente `LabRole`; este repo nao cria IAM Role.

## VPC para acesso ao RDS

Use os outputs do repo `../oficina-infra-db` como referencia:

- `public_subnet_ids` -> secret `LAMBDA_SUBNET_IDS`, separado por virgula;
- `db_security_group_id` -> secret `LAMBDA_SECURITY_GROUP_IDS`.

Exemplo:

```text
LAMBDA_SUBNET_IDS=subnet-abc,subnet-def
LAMBDA_SECURITY_GROUP_IDS=sg-abc
```

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

O workflow:

- restaura, compila e testa;
- publica o projeto em ZIP;
- cria ou atualiza `oficina-auth-cpf`;
- cria ou atualiza `oficina-jwt-authorizer`;
- configura handlers, memoria e timeout;
- configura VPC apenas na Lambda `oficina-auth-cpf`;
- usa `LabRole`;
- nao usa Terraform.

Handlers configurados:

```text
oficina-auth-cpf:
Oficina.AuthLambda::Oficina.AuthLambda.Functions.AuthCpfFunction::HandleAsync

oficina-jwt-authorizer:
Oficina.AuthLambda::Oficina.AuthLambda.Functions.JwtAuthorizerFunction::HandleAsync
```

Configuracoes aplicadas:

```text
oficina-auth-cpf: 256 MB, timeout 15s, VPC config
oficina-jwt-authorizer: 256 MB, timeout 5s, sem VPC
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

Crie `payload-cliente.json` com evento HTTP API v2:

```json
{
  "version": "2.0",
  "headers": {
    "content-type": "application/json"
  },
  "body": "{\"cpf\":\"39053344705\"}",
  "isBase64Encoded": false
}
```

Invoke:

```powershell
aws lambda invoke `
  --function-name oficina-auth-cpf `
  --payload file://payload-cliente.json `
  --cli-binary-format raw-in-base64-out `
  --region us-east-1 `
  response-cliente.json

Get-Content response-cliente.json
```

Para funcionario/admin, use body com senha:

```json
{
  "version": "2.0",
  "headers": {
    "content-type": "application/json"
  },
  "body": "{\"cpf\":\"39053344705\",\"senha\":\"Senha@123\"}",
  "isBase64Encoded": false
}
```

## Invoke do Authorizer

Crie `payload-authorizer.json`:

```json
{
  "version": "2.0",
  "headers": {
    "authorization": "Bearer <accessToken>"
  }
}
```

Invoke:

```powershell
aws lambda invoke `
  --function-name oficina-jwt-authorizer `
  --payload file://payload-authorizer.json `
  --cli-binary-format raw-in-base64-out `
  --region us-east-1 `
  response-authorizer.json

Get-Content response-authorizer.json
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

## Seguranca

- Nao versionar secrets, connection string real, `.env` real ou `terraform.tfvars`.
- Nao logar senha, JWT, connection string, `Jwt__Secret` ou CPF completo.
- A Lambda Auth retorna erro interno generico em falhas inesperadas.
- O Authorizer nega por padrao e trata tokens invalidos sem excecao nao tratada.
- A API principal tambem valida o JWT internamente como segunda camada.
