# oficina-auth-lambda

## Visão Geral

Este repositório contém as funções serverless de autenticação da solução Oficina:

- `oficina-auth-cpf`: autentica por CPF e, quando necessário, senha. Usa VPC para acessar o RDS e gerar JWT.
- `oficina-jwt-authorizer`: valida JWT para o API Gateway HTTP API. Não usa VPC nem banco de dados.

As Lambdas devem ser publicadas depois que a API executar as migrations e antes do root `api-gateway`.

## Responsabilidade

- Manter a Lambda de autenticação por CPF.
- Manter a Lambda Authorizer JWT.
- Executar build, testes e empacotamento.
- Publicar ou atualizar as Lambdas na AWS.
- Configurar VPC somente na Lambda `oficina-auth-cpf`.
- Usar a mesma configuração JWT da `oficina-api`.

## Ordem De Implantação

1. `oficina-infra-db`
2. `oficina-infra-k8s` core
3. `oficina-infra-k8s` addons
4. `oficina-api`
5. `oficina-auth-lambda`
6. `oficina-infra-k8s` API Gateway
7. Novo deploy da `oficina-api` com `EMAIL_BASE_URL_APROVA_RECUSA_ORCAMENTO`

## Configuração Necessária

Configure no GitHub Actions:

| Nome | Tipo | Uso |
| --- | --- | --- |
| `AWS_ACCESS_KEY_ID` | Secret | Autenticação AWS |
| `AWS_SECRET_ACCESS_KEY` | Secret | Autenticação AWS |
| `AWS_SESSION_TOKEN` | Secret opcional | Credenciais temporárias |
| `AWS_REGION` | Secret | Região AWS |
| `AWS_LAMBDA_ROLE_ARN` | Secret | Role de execução das Lambdas |
| `DB_CONNECTION_STRING` | Secret | Conexão da Lambda Auth com SQL Server |
| `LAMBDA_SUBNET_IDS` | Secret | Subnet privada da Lambda Auth |
| `LAMBDA_SECURITY_GROUP_IDS` | Secret | Security group da Lambda Auth |
| `JWT_SECRET` | Secret | Assinar e validar JWT |
| `JWT_ISSUER` | Secret | Issuer JWT |
| `JWT_AUDIENCE` | Secret | Audience JWT |
| `JWT_EXPIRATION_MINUTES` | Secret | Expiração dos tokens |
| `AUTH_FUNCTION_NAME` | Variable opcional | Padrão `oficina-auth-cpf` |
| `AUTHORIZER_FUNCTION_NAME` | Variable opcional | Padrão `oficina-jwt-authorizer` |

`JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` e `JWT_EXPIRATION_MINUTES` devem ser iguais aos usados pela API.

## Como Executar Na AWS

Após o deploy da API e execução das migrations, execute manualmente:

```text
GitHub Actions > Deploy Lambda > Run workflow
```

O workflow valida a configuração, gera os pacotes, cria ou atualiza as duas Lambdas e confere as configurações principais sem imprimir secrets, connection string, ARNs ou metadados sensíveis.

## Como Validar Na AWS

Valide pelo próprio workflow:

- `oficina-auth-cpf` ativa, com VPC configurada.
- `oficina-jwt-authorizer` ativa, sem VPC.
- Variáveis JWT configuradas nas duas Lambdas.

Para validação funcional, invoque as Lambdas em ambiente autenticado usando payloads controlados.

## Como Executar Localmente

```powershell
dotnet restore Oficina.AuthLambda.sln
dotnet build Oficina.AuthLambda.sln --configuration Release --no-restore
dotnet test Oficina.AuthLambda.sln --configuration Release --no-build
```

## Como Validar Localmente

A validação local suportada é build e testes automatizados. A validação funcional completa depende das Lambdas publicadas e da infraestrutura AWS.

## Valores Consumidos

| Valor | Origem | Uso |
| --- | --- | --- |
| `DB_CONNECTION_STRING` | GitHub Secret | Acesso da Lambda Auth ao RDS |
| `lambda_subnet_id` | `oficina-infra-db` | Subnet privada para a Lambda Auth |
| `lambda_security_group_id` | `oficina-infra-db` | Security group da Lambda Auth |
| Configuração JWT | GitHub Secrets | Emitir e validar tokens compatíveis com a API |

## Valores Gerados

| Valor | Destino | Uso |
| --- | --- | --- |
| `oficina-auth-cpf` | AWS Lambda | Autenticação pública via API Gateway |
| `oficina-jwt-authorizer` | AWS Lambda | Autorização de rotas protegidas |

## Próxima Etapa

Depois de publicar as Lambdas, aplique o root `api-gateway` do `oficina-infra-k8s`. Em seguida, execute novo deploy da API para consumir a URL pública do API Gateway.
