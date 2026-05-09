# oficina-auth-lambda

## Visão geral

Este repositório contém as funções serverless de autenticação da Oficina API. Ele é a **etapa 4** da implantação, depois que o RDS existe e as migrations da API já criaram as tabelas.

São publicadas duas Lambdas:

- `oficina-auth-cpf`: autentica cliente, funcionário ou admin por CPF e, quando necessário, senha. Esta função acessa o RDS SQL Server e gera JWT.
- `oficina-jwt-authorizer`: valida JWT para o API Gateway sem acessar banco de dados.

## Ordem de implantação da solução

1. `oficina-infra-db`
2. `oficina-infra-k8s`
3. `oficina-api`
4. **`oficina-auth-lambda`**
5. `oficina-infra-k8s` novamente para API Gateway, quando essa etapa estiver implementada

## Responsabilidade

Este repositório é responsável por:

- manter o código da Lambda Auth CPF;
- manter o código da Lambda JWT Authorizer;
- executar build e testes;
- publicar ou atualizar as Lambdas;
- configurar VPC apenas na Lambda Auth CPF;
- configurar JWT nas duas Lambdas.

## Pré-requisitos

- `oficina-infra-db` aplicado com outputs disponíveis.
- `oficina-api` executada em modo migration ao menos uma vez.
- Conta AWS com permissão para Lambda, IAM pass role, VPC config e CloudWatch Logs.
- IAM Role para execução das Lambdas criada pelo usuário.
- .NET SDK 10.
- AWS CLI para validação.

## Configuração necessária

Configure os valores em `GitHub > Settings > Secrets and variables > Actions`.

| Nome | Tipo | Origem | Onde configurar | Uso |
|---|---|---|---|---|
| `AWS_ACCESS_KEY_ID` | Secret | Credencial AWS do usuário | GitHub Secrets deste repo | Autenticar na AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret | Credencial AWS do usuário | GitHub Secrets deste repo | Autenticar na AWS |
| `AWS_SESSION_TOKEN` | Secret | Credencial temporária, se aplicável | GitHub Secrets deste repo | Autenticar com sessão temporária |
| `AWS_REGION` | Secret | Região escolhida, por exemplo `us-east-1` | GitHub Secrets deste repo | Publicar e validar Lambdas |
| `AWS_LAMBDA_ROLE_ARN` | Secret | ARN criado pelo usuário | GitHub Secrets deste repo | Role de execução das Lambdas |
| `DB_CONNECTION_STRING` | Secret | Montada com outputs do `oficina-infra-db` | GitHub Secrets deste repo | Conexão da Lambda Auth com SQL Server |
| `LAMBDA_SUBNET_IDS` | Secret | Output `lambda_subnet_ids` do `oficina-infra-db` | GitHub Secrets deste repo | Subnets da Lambda Auth |
| `LAMBDA_SECURITY_GROUP_IDS` | Secret | Output `lambda_security_group_ids` do `oficina-infra-db` | GitHub Secrets deste repo | Security groups da Lambda Auth |
| `JWT_SECRET` | Secret | Mesmo valor do `oficina-api` | GitHub Secrets deste repo | Assinar e validar JWT |
| `JWT_ISSUER` | Secret | Mesmo valor do `oficina-api` | GitHub Secrets deste repo | Issuer JWT |
| `JWT_AUDIENCE` | Secret | Mesmo valor do `oficina-api` | GitHub Secrets deste repo | Audience JWT |
| `JWT_EXPIRATION_MINUTES` | Secret | Mesmo valor do `oficina-api` | GitHub Secrets deste repo | Expiração dos tokens |
| `AUTH_FUNCTION_NAME` | Variable opcional | Valor definido pelo usuário | GitHub Variables deste repo | Nome da Lambda Auth; padrão `oficina-auth-cpf` |
| `AUTHORIZER_FUNCTION_NAME` | Variable opcional | Valor definido pelo usuário | GitHub Variables deste repo | Nome da Authorizer; padrão `oficina-jwt-authorizer` |

Exemplo de ARN da role:

```text
arn:aws:iam::<account-id>:role/<lambda-role>
```

Modelo de `DB_CONNECTION_STRING`:

```text
Server=<db_address>,<db_port>;Database=<db_name>;User Id=<db-user>;Password=<db-password>;Encrypt=True;TrustServerCertificate=True;
```

Use sempre `lambda_security_group_ids` para `LAMBDA_SECURITY_GROUP_IDS`. Não use o security group do RDS como security group da Lambda.

## Como executar

### CI

O workflow `Lambda CI` roda em Pull Request e valida:

- restore;
- build Release;
- testes.

### Deploy manual

Execute manualmente:

```text
GitHub Actions > Deploy Lambda > Run workflow
```

O workflow `Deploy Lambda`:

- valida secrets obrigatórios;
- valida nomes e handlers das Lambdas;
- executa restore, build e testes;
- empacota o projeto;
- cria ou atualiza `oficina-auth-cpf`;
- cria ou atualiza `oficina-jwt-authorizer`;
- configura VPC somente na `oficina-auth-cpf`;
- mantém `oficina-jwt-authorizer` sem VPC.

### Build e testes locais

```powershell
dotnet restore Oficina.AuthLambda.sln
dotnet build Oficina.AuthLambda.sln --configuration Release
dotnet test Oficina.AuthLambda.sln --configuration Release --no-build
```

## Como validar

Valide a Lambda Auth:

```powershell
aws lambda get-function-configuration --function-name oficina-auth-cpf --region <region>
```

Valide a Authorizer:

```powershell
aws lambda get-function-configuration --function-name oficina-jwt-authorizer --region <region>
```

Resultado esperado:

- `oficina-auth-cpf`: `State=Active`, `LastUpdateStatus=Successful`, VPC preenchida;
- `oficina-jwt-authorizer`: `State=Active`, `LastUpdateStatus=Successful`, sem VPC.

Payload HTTP API v2 para autenticar cliente:

```powershell
@{version='2.0';headers=@{'content-type'='application/json'};body='{"cpf":"<cpf-cliente>"}';isBase64Encoded=$false} | ConvertTo-Json -Compress | Set-Content payload-cliente.json
```

Invocar a Lambda Auth:

```powershell
aws lambda invoke --function-name oficina-auth-cpf --payload file://payload-cliente.json --cli-binary-format raw-in-base64-out --region <region> response-cliente.json
```

Payload para Authorizer com token válido:

```powershell
@{version='2.0';headers=@{authorization="Bearer <jwt-valido>"}} | ConvertTo-Json -Compress | Set-Content payload-authorizer.json
```

Invocar a Authorizer:

```powershell
aws lambda invoke --function-name oficina-jwt-authorizer --payload file://payload-authorizer.json --cli-binary-format raw-in-base64-out --region <region> response-authorizer.json
```

Resposta esperada para token válido:

```json
{
  "isAuthorized": true
}
```

## Outputs para a próxima etapa

Este repositório não gera outputs Terraform. Após publicar as Lambdas, use os nomes abaixo na etapa futura de API Gateway no `oficina-infra-k8s`.

| Valor | Usado por | Configurar como |
|---|---|---|
| `oficina-auth-cpf` | `oficina-infra-k8s` | Nome ou ARN da Lambda Auth |
| `oficina-jwt-authorizer` | `oficina-infra-k8s` | Nome ou ARN da Lambda Authorizer |
| Configuração JWT | `oficina-api` e API Gateway | Deve permanecer compatível entre API e Lambdas |

## Problemas comuns

| Problema | Possível causa | Como resolver |
|---|---|---|
| Deploy falha por role | `AWS_LAMBDA_ROLE_ARN` ausente ou inválido | Configure um ARN de role válido da conta AWS |
| Auth não conecta no RDS | Subnets, security group ou connection string incorretos | Use outputs do `oficina-infra-db` |
| Authorizer nega token válido | JWT diferente do configurado na API | Alinhe `JWT_SECRET`, `JWT_ISSUER` e `JWT_AUDIENCE` |
| Auth retorna erro de usuário | Migrations ainda não executadas | Execute a etapa de migrations no `oficina-api` |
| Lambda fica sem VPC | Secrets de subnets/security groups vazios | Configure `LAMBDA_SUBNET_IDS` e `LAMBDA_SECURITY_GROUP_IDS` |

## Próxima etapa

Volte ao repositório `oficina-infra-k8s` para a etapa de API Gateway quando ela estiver implementada.
