# oficina-auth-lambda

## Visão geral

Este repositório contém as funções serverless de autenticação da solução Oficina API. Ele é implantado depois que o banco existe e as migrations da API já criaram as tabelas necessárias.

São publicadas duas Lambdas:

- `oficina-auth-cpf`: autentica cliente, funcionário ou admin por CPF e, quando necessário, senha. Esta função acessa o RDS SQL Server e gera JWT.
- `oficina-jwt-authorizer`: valida JWT para integração futura com API Gateway, sem acessar o banco de dados.

## Arquitetura e ordem de implantação

1. `oficina-infra-db`: cria VPC, subnets, security groups e RDS.
2. `oficina-infra-k8s`: cria ECR, EKS e node group.
3. `oficina-api`: publica a imagem no ECR, executa migrations e sobe no EKS.
4. **`oficina-auth-lambda`**: publica as Lambdas de autenticação e autorização.
5. `oficina-infra-k8s`: etapa futura para API Gateway.

## Responsabilidade deste repositório

- Manter a Lambda de autenticação por CPF.
- Manter a Lambda Authorizer JWT.
- Executar build e testes.
- Publicar ou atualizar as Lambdas na AWS.
- Configurar VPC somente na Lambda `oficina-auth-cpf`.
- Usar a mesma configuração JWT do `oficina-api`.

## Integração com os outros repositórios

Este repositório consome a conexão do banco criada pelo `oficina-infra-db` e a mesma configuração JWT usada pelo `oficina-api`. Ele não gera outputs Terraform.

### Valores consumidos

| Valor | Origem | Uso |
|---|---|---|
| `DB_CONNECTION_STRING` | Outputs `db_address`, `db_port` e `db_name` do `oficina-infra-db`, mais usuário e senha do banco | Conectar a Lambda Auth ao SQL Server |
| `LAMBDA_SUBNET_IDS` | Output `lambda_subnet_ids` do `oficina-infra-db` | Informar em CSV, exemplo `subnet-abc,subnet-def` |
| `LAMBDA_SECURITY_GROUP_IDS` | Output `lambda_security_group_ids` do `oficina-infra-db` | Informar em CSV, exemplo `sg-abc,sg-def` |
| `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRATION_MINUTES` | Mesmos valores configurados no `oficina-api` | Emitir e validar tokens compatíveis com a API |

### Valores gerados

| Valor | Usado por | Uso |
|---|---|---|
| `oficina-auth-cpf` | `oficina-infra-k8s`, na etapa futura de API Gateway | Nome ou ARN da Lambda Auth |
| `oficina-jwt-authorizer` | `oficina-infra-k8s`, na etapa futura de API Gateway | Nome ou ARN da Lambda Authorizer |

Modelo de `DB_CONNECTION_STRING`:

```text
Server=<db_address>,<db_port>;Database=<db_name>;User Id=<db-user>;Password=<db-password>;Encrypt=True;TrustServerCertificate=True;
```

Use sempre `lambda_security_group_ids` em `LAMBDA_SECURITY_GROUP_IDS`. Não use o security group do RDS como security group da Lambda.

## Configuração necessária

Configure os valores em `GitHub > Settings > Secrets and variables > Actions`.

| Nome | Tipo | Uso |
|---|---|---|
| `AWS_ACCESS_KEY_ID` | Secret | Autenticar na AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret | Autenticar na AWS |
| `AWS_SESSION_TOKEN` | Secret opcional | Autenticar com credencial temporária |
| `AWS_REGION` | Secret | Região AWS, exemplo `us-east-1` |
| `AWS_LAMBDA_ROLE_ARN` | Secret | Role de execução das Lambdas |
| `DB_CONNECTION_STRING` | Secret | Conexão da Lambda Auth com SQL Server |
| `LAMBDA_SUBNET_IDS` | Secret | Subnets da Lambda Auth em CSV |
| `LAMBDA_SECURITY_GROUP_IDS` | Secret | Security groups da Lambda Auth em CSV |
| `JWT_SECRET` | Secret | Assinar e validar JWT |
| `JWT_ISSUER` | Secret | Issuer JWT |
| `JWT_AUDIENCE` | Secret | Audience JWT |
| `JWT_EXPIRATION_MINUTES` | Secret | Expiração dos tokens |
| `AUTH_FUNCTION_NAME` | Variable opcional | Nome da Lambda Auth; padrão `oficina-auth-cpf` |
| `AUTHORIZER_FUNCTION_NAME` | Variable opcional | Nome da Authorizer; padrão `oficina-jwt-authorizer` |

Exemplo de ARN da role:

```text
arn:aws:iam::<account-id>:role/<lambda-role>
```

O `JWT_SECRET` deve ser o mesmo usado pelo `oficina-api`. Para gerar um valor forte no PowerShell:

```powershell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})
```

## Como executar

Em Pull Requests, o workflow `Lambda CI` executa restore, build e testes.

Após o merge na `main`, execute manualmente:

```text
GitHub Actions > Deploy Lambda > Run workflow
```

Para build e testes locais:

```powershell
dotnet restore Oficina.AuthLambda.sln
dotnet build Oficina.AuthLambda.sln --configuration Release
dotnet test Oficina.AuthLambda.sln --configuration Release --no-build
```

## Como validar

Valide a configuração das Lambdas:

```powershell
aws lambda get-function-configuration --function-name oficina-auth-cpf --region <region>
aws lambda get-function-configuration --function-name oficina-jwt-authorizer --region <region>
```

Resultado esperado:

- `oficina-auth-cpf`: `State=Active`, `LastUpdateStatus=Successful` e VPC preenchida;
- `oficina-jwt-authorizer`: `State=Active`, `LastUpdateStatus=Successful` e sem VPC.

Valide a autenticação com payload HTTP API v2:

```powershell
@{version='2.0';headers=@{'content-type'='application/json'};body='{"cpf":"<cpf-cliente>"}';isBase64Encoded=$false} | ConvertTo-Json -Compress | Set-Content payload-cliente.json
aws lambda invoke --function-name oficina-auth-cpf --payload file://payload-cliente.json --cli-binary-format raw-in-base64-out --region <region> response-cliente.json
```

Para validar a Authorizer, envie um token válido:

```powershell
@{version='2.0';headers=@{authorization="Bearer <jwt-valido>"}} | ConvertTo-Json -Compress | Set-Content payload-authorizer.json
aws lambda invoke --function-name oficina-jwt-authorizer --payload file://payload-authorizer.json --cli-binary-format raw-in-base64-out --region <region> response-authorizer.json
```

Resposta esperada:

```json
{
  "isAuthorized": true
}
```

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
