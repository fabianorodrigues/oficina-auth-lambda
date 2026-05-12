# oficina-auth-lambda

## Visão geral

Este repositório contém as funções serverless de autenticação da solução Oficina API. Ele é implantado depois que a infraestrutura existe e depois que as migrations do `oficina-api` criaram as tabelas necessárias no SQL Server.

São publicadas duas Lambdas:

- `oficina-auth-cpf`: autentica cliente, funcionário ou admin por CPF e, quando necessário, senha. Esta função acessa o RDS SQL Server e gera JWT.
- `oficina-jwt-authorizer`: valida JWT para integração futura com API Gateway, sem acessar o banco de dados.

A solução completa é composta por quatro repositórios, nesta ordem de implantação:

1. `oficina-infra-db`: rede, security groups e RDS.
2. `oficina-infra-k8s`: ECR, EKS e node group.
3. `oficina-api`: imagem Docker, migrations e deploy da API no EKS.
4. `oficina-auth-lambda`: Lambdas de autenticação por CPF e autorização JWT.

## Papel deste repositório

- Manter a Lambda de autenticação por CPF.
- Manter a Lambda Authorizer JWT.
- Executar build, testes e empacotamento.
- Publicar ou atualizar as Lambdas na AWS.
- Configurar VPC somente na Lambda `oficina-auth-cpf`.
- Usar a mesma configuração JWT do `oficina-api`.

## Integração e dependências

Este repositório consome o RDS criado pelo `oficina-infra-db` e a mesma configuração JWT usada pelo `oficina-api`. Ele não gera outputs Terraform. Os outputs dos repositórios de infraestrutura facilitam o provisionamento, a integração entre repositórios e a avaliação acadêmica do projeto como portfólio, mas não devem ser publicados em logs; consulte-os em ambiente autenticado e configure os valores necessários como GitHub Secrets.

| Valor consumido | Origem | Uso |
|---|---|---|
| `DB_CONNECTION_STRING` | Outputs `db_address`, `db_port` e `db_name` do `oficina-infra-db`, mais usuário e senha do banco | Conectar a Lambda Auth ao SQL Server |
| `LAMBDA_SUBNET_IDS` | Output `lambda_subnet_id` do `oficina-infra-db` | Informar o subnet ID único, exemplo `subnet-abc` |
| `LAMBDA_SECURITY_GROUP_IDS` | Output `lambda_security_group_id` do `oficina-infra-db` | Informar o security group ID único, exemplo `sg-abc` |
| `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRATION_MINUTES` | Mesma configuração usada no `oficina-api` | Emitir e validar tokens compatíveis com a API |

Os secrets da Lambda continuam com nomes plurais porque o comando da AWS aceita listas, mas neste projeto eles devem receber os valores singulares `lambda_subnet_id` e `lambda_security_group_id`. Não use o security group do RDS como security group da Lambda.

Modelo de connection string:

```text
Server=<db_address>,<db_port>;Database=<db_name>;User Id=<db-user>;Password=<db-password>;Encrypt=True;TrustServerCertificate=True;
```

## Configuração necessária

Configure os valores em `GitHub > Settings > Secrets and variables > Actions`.

| Nome | Tipo | Uso |
|---|---|---|
| `AWS_ACCESS_KEY_ID` | Secret | Autenticar na AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret | Autenticar na AWS |
| `AWS_SESSION_TOKEN` | Secret opcional | Usar credenciais temporárias |
| `AWS_REGION` | Secret | Região AWS usada pelo projeto |
| `AWS_LAMBDA_ROLE_ARN` | Secret | Role de execução das Lambdas |
| `DB_CONNECTION_STRING` | Secret | Conexão da Lambda Auth com SQL Server |
| `LAMBDA_SUBNET_IDS` | Secret | Valor do output `lambda_subnet_id` |
| `LAMBDA_SECURITY_GROUP_IDS` | Secret | Valor do output `lambda_security_group_id` |
| `JWT_SECRET` | Secret | Assinar e validar JWT |
| `JWT_ISSUER` | Secret | Issuer JWT |
| `JWT_AUDIENCE` | Secret | Audience JWT |
| `JWT_EXPIRATION_MINUTES` | Secret | Expiração dos tokens |
| `AUTH_FUNCTION_NAME` | Variable opcional | Nome da Lambda Auth; padrão `oficina-auth-cpf` |
| `AUTHORIZER_FUNCTION_NAME` | Variable opcional | Nome da Authorizer; padrão `oficina-jwt-authorizer` |

O `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` e `JWT_EXPIRATION_MINUTES` devem ser iguais aos usados pelo `oficina-api`. O `JWT_SECRET` deve ter pelo menos 32 caracteres.

## Como executar e validar na AWS

Em Pull Requests e push na `main`, o workflow `Lambda CI` executa restore, build e testes.

Após o merge na `main`, execute o deploy manualmente a partir da própria branch `main`:

```text
GitHub Actions > Deploy Lambda > Run workflow
```

O workflow valida configuração, gera o pacote `.zip`, cria ou atualiza as duas Lambdas e valida runtime, handlers, timeout, memória, variáveis de ambiente e VPC.

Para validar manualmente:

```powershell
aws lambda get-function-configuration --function-name oficina-auth-cpf --region <region>
aws lambda get-function-configuration --function-name oficina-jwt-authorizer --region <region>
```

Resultado esperado:

- `oficina-auth-cpf`: `State=Active`, `LastUpdateStatus=Successful` e VPC preenchida;
- `oficina-jwt-authorizer`: `State=Active`, `LastUpdateStatus=Successful` e sem VPC.

Valide autenticação com payload HTTP API v2:

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
| Deploy falha por role | `AWS_LAMBDA_ROLE_ARN` ausente ou inválido | Configure uma role válida da mesma conta AWS |
| Auth não conecta no RDS | Subnet, security group ou connection string incorretos | Use `lambda_subnet_id`, `lambda_security_group_id` e os outputs do RDS |
| Token válido é negado | JWT diferente do configurado na API | Alinhe `JWT_SECRET`, `JWT_ISSUER` e `JWT_AUDIENCE` |
| Auth retorna erro de usuário | Migrations ainda não executadas | Execute o deploy/migration do `oficina-api` antes da Lambda |
| Lambda Auth fica sem VPC | Secrets de subnet ou security group vazios | Configure `LAMBDA_SUBNET_IDS` e `LAMBDA_SECURITY_GROUP_IDS` com os outputs singulares |

## Como executar e validar localmente

Para build e testes locais:

```powershell
cd oficina-auth-lambda
dotnet restore Oficina.AuthLambda.sln
dotnet build Oficina.AuthLambda.sln --configuration Release --no-restore
dotnet test Oficina.AuthLambda.sln --configuration Release --no-build
```

A validação funcional com `aws lambda invoke` depende das Lambdas publicadas na AWS. Localmente, a validação suportada pelo projeto é o build e a suíte de testes.
