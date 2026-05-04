# oficina-auth-lambda

## Visão geral

Este repositório faz parte da Fase 3 do Tech Challenge FIAP e contém as funções serverless de autenticação da Oficina API.

Ele publica duas Lambdas:

- `oficina-auth-cpf`: autentica cliente, funcionário ou admin por CPF e, quando necessário, senha. Esta função consulta o RDS SQL Server e gera um JWT.
- `oficina-jwt-authorizer`: valida o JWT recebido pelo API Gateway e autoriza ou nega o acesso às rotas protegidas.

A infraestrutura do banco fica no repositório `oficina-infra-db`. A API principal fica no repositório `oficina-api`.

## Responsabilidade deste repositório

Este repositório contém:

- código da Lambda `oficina-auth-cpf`;
- código da Lambda `oficina-jwt-authorizer`;
- testes automatizados;
- workflow de CI;
- workflow de deploy manual.

## Arquitetura

Autenticação:

```text
Cliente/Postman
        |
        v
API Gateway
        |
        v
oficina-auth-cpf
        |
        v
RDS SQL Server
        |
        v
JWT
```

Rotas protegidas:

```text
Cliente/Postman
        |
        v
API Gateway
        |
        v
oficina-jwt-authorizer
        |
        v
API Oficina
```

A Lambda `oficina-auth-cpf` usa VPC porque acessa o RDS SQL Server.

A Lambda `oficina-jwt-authorizer` não usa VPC e não usa banco, porque apenas valida JWT.

## Organização da solução

| Pasta/arquivo | Responsabilidade |
|---|---|
| `Functions` | Entrada AWS Lambda, parse de eventos e mapeamento de responses |
| `Application` | Orquestração dos casos de uso |
| `Domain` | Conceitos puros como CPF, perfil e modelos de autenticação |
| `Infrastructure` | SQL Server, JWT, PasswordHasher e relógio do sistema |
| `Contracts` | DTOs de entrada e saída |
| `Configuration` | Leitura e validação de variáveis de ambiente |
| `Serialization` | Serialização JSON do runtime Lambda |
| `DependencyInjection.cs` | Composição das dependências |

## Lambdas publicadas

| Lambda | Responsabilidade | Usa VPC? | Usa banco? |
|---|---|---|---|
| `oficina-auth-cpf` | Autentica CPF/senha e gera JWT | Sim | Sim |
| `oficina-jwt-authorizer` | Valida JWT no API Gateway | Não | Não |

## GitHub Secrets

Configure os secrets em:

```text
GitHub > Settings > Secrets and variables > Actions
```

| Secret | Uso |
|---|---|
| `AWS_ACCESS_KEY_ID` | Credencial do AWS Academy |
| `AWS_SECRET_ACCESS_KEY` | Credencial do AWS Academy |
| `AWS_SESSION_TOKEN` | Token temporário do AWS Academy |
| `AWS_REGION` | Região AWS, usar `us-east-1` |
| `DB_CONNECTION_STRING` | Connection string do RDS usada pela Auth |
| `JWT_SECRET` | Mesmo segredo usado pela API principal |
| `JWT_ISSUER` | Mesmo issuer usado pela API principal |
| `JWT_AUDIENCE` | Mesmo audience usado pela API principal |
| `JWT_EXPIRATION_MINUTES` | Tempo de expiração do token |
| `AWS_LAMBDA_ROLE_ARN` | ARN da LabRole; opcional |
| `LAMBDA_SUBNET_IDS` | Subnets para a Lambda Auth |
| `LAMBDA_SECURITY_GROUP_IDS` | Security Group da Lambda Auth |

## GitHub Variables

Configure apenas se precisar publicar as funções com nomes diferentes dos padrões:

| Variável | Uso | Padrão |
|---|---|---|
| `AUTH_FUNCTION_NAME` | Nome da Lambda que autentica CPF/senha | `oficina-auth-cpf` |
| `AUTHORIZER_FUNCTION_NAME` | Nome da Lambda Authorizer JWT | `oficina-jwt-authorizer` |


O secret `AWS_LAMBDA_ROLE_ARN` é opcional. Se não for informado, o workflow monta automaticamente:

```text
arn:aws:iam::<account-id>:role/LabRole
```

Use sempre `lambda_security_group_ids` para configurar `LAMBDA_SECURITY_GROUP_IDS`. Não use o Security Group do RDS como Security Group da Lambda.

## Executar build/test local

Restaurar dependências:

```powershell
dotnet restore Oficina.AuthLambda.sln
```

Compilar:

```powershell
dotnet build Oficina.AuthLambda.sln --configuration Release
```

Executar testes:

```powershell
dotnet test Oficina.AuthLambda.sln --configuration Release
```

O projeto usa .NET 10 e runtime Lambda `dotnet10`.

## Deploy manual

No GitHub, execute:

```text
GitHub Actions > Deploy Lambda > Run workflow
```

O workflow `Deploy Lambda`:

- roda restore, build e testes;
- empacota o projeto;
- cria ou atualiza `oficina-auth-cpf`;
- cria ou atualiza `oficina-jwt-authorizer`;
- configura VPC apenas na `oficina-auth-cpf`;
- mantém `oficina-jwt-authorizer` sem VPC;

## Validar funções criadas

Validar `oficina-auth-cpf`:

```powershell
aws lambda get-function-configuration --function-name oficina-auth-cpf --region us-east-1 --query "{State:State,LastUpdateStatus:LastUpdateStatus,Runtime:Runtime,Handler:Handler,Timeout:Timeout,Memory:MemorySize,VpcConfig:VpcConfig}"
```

Validar `oficina-jwt-authorizer`:

```powershell
aws lambda get-function-configuration --function-name oficina-jwt-authorizer --region us-east-1 --query "{State:State,LastUpdateStatus:LastUpdateStatus,Runtime:Runtime,Handler:Handler,Timeout:Timeout,Memory:MemorySize,VpcConfig:VpcConfig}"
```

Resultado esperado:

| Lambda | Estado esperado |
|---|---|
| `oficina-auth-cpf` | `State=Active`, `LastUpdateStatus=Successful`, VPC preenchida |
| `oficina-jwt-authorizer` | `State=Active`, `LastUpdateStatus=Successful`, sem VPC |

## Testar Lambda Auth

Crie o payload HTTP API v2 para cliente:

```powershell
@{version='2.0';headers=@{'content-type'='application/json'};body='{"cpf":"<cpf-cliente>"}';isBase64Encoded=$false} | ConvertTo-Json -Compress | Set-Content payload-cliente.json
```

Invocar a Lambda Auth:

```powershell
aws lambda invoke --function-name oficina-auth-cpf --payload file://payload-cliente.json --cli-binary-format raw-in-base64-out --region us-east-1 response-cliente.json
```

Ler a resposta:

```powershell
Get-Content response-cliente.json -Raw
```

Extrair o token:

```powershell
$token = ((Get-Content response-cliente.json -Raw | ConvertFrom-Json).body | ConvertFrom-Json).accessToken; $token
```

Troque `<cpf-cliente>` por um CPF cadastrado na tabela `Clientes.Documento`.

Se a resposta tiver `statusCode=200`, a Lambda Auth conseguiu consultar o RDS e gerar o JWT.

## Testar Funcionário/Admin

Crie o payload HTTP API v2 com CPF e senha:

```powershell
@{version='2.0';headers=@{'content-type'='application/json'};body='{"cpf":"<cpf-funcionario>","senha":"<senha>"}';isBase64Encoded=$false} | ConvertTo-Json -Compress | Set-Content payload-funcionario.json
```

Invocar a Lambda Auth:

```powershell
aws lambda invoke --function-name oficina-auth-cpf --payload file://payload-funcionario.json --cli-binary-format raw-in-base64-out --region us-east-1 response-funcionario.json
```

Ler a resposta:

```powershell
Get-Content response-funcionario.json -Raw
```

Troque `<cpf-funcionario>` e `<senha>` por credenciais de teste cadastradas na tabela de funcionários.

## Testar Authorizer

Crie o payload com token válido:

```powershell
@{version='2.0';headers=@{authorization="Bearer $token"}} | ConvertTo-Json -Compress | Set-Content payload-authorizer.json
```

Invocar o Authorizer:

```powershell
aws lambda invoke --function-name oficina-jwt-authorizer --payload file://payload-authorizer.json --cli-binary-format raw-in-base64-out --region us-east-1 response-authorizer.json
```

Ler a resposta:

```powershell
Get-Content response-authorizer.json -Raw
```

Resultado esperado:

```json
{
  "isAuthorized": true
}
```

## Testar token inválido

Crie o payload com token inválido:

```powershell
@{version='2.0';headers=@{authorization='Bearer token-invalido'}} | ConvertTo-Json -Compress | Set-Content payload-authorizer-invalido.json
```

Invocar o Authorizer:

```powershell
aws lambda invoke --function-name oficina-jwt-authorizer --payload file://payload-authorizer-invalido.json --cli-binary-format raw-in-base64-out --region us-east-1 response-authorizer-invalido.json
```

Ler a resposta:

```powershell
Get-Content response-authorizer-invalido.json -Raw
```

Resultado esperado:

```json
{
  "isAuthorized": false
}
```
