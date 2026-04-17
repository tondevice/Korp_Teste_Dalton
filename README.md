# 🧾 Sistema de Gestão de Notas Fiscais

Projeto desenvolvido como parte do processo seletivo de estágio da **Korp ERP**.

---

## 📋 Sobre o projeto

Este sistema simula um fluxo simples de emissão de notas fiscais com controle de estoque, separando as responsabilidades entre faturamento e estoque.

A proposta foi construir uma solução próxima de um cenário real, onde a consistência entre serviços é essencial — principalmente no momento da impressão da nota, que depende diretamente da disponibilidade de produtos.

---

## 🚀 Tecnologias utilizadas

### Backend

* .NET 8 (C#)
* ASP.NET Core
* Entity Framework Core
* SQLite

### Frontend

* Angular
* Angular Material
* RxJS
* TypeScript

---

## 🏗️ Arquitetura

A aplicação é composta por três partes:

```text
Frontend (Angular)
        ↓
Faturamento API (.NET)
        ↓
Estoque API (.NET)
        ↓
SQLite (banco local)
```

* o faturamento depende do estoque para validar e realizar operações
* a comunicação ocorre via HTTP
* cada serviço possui seu próprio banco de dados

---

## 💾 Banco de dados

Foi utilizado **SQLite** com o objetivo de simplificar a execução do projeto, evitando a necessidade de configuração de um banco externo e permitindo que o sistema funcione imediatamente após o clone.

---

## ⚙️ Pré-requisitos

* [.NET 8 SDK](https://dotnet.microsoft.com/pt-br/download/dotnet/8.0)
* [Node.js (LTS)](https://nodejs.org/en/download)

Instalação do Angular CLI:

```bash
npm install -g @angular/cli
```

---

## ▶️ Como executar

```bash
git clone <url-do-repositorio>
cd Korp_Case_Dalton
```

### Iniciar aplicação

```bash
.\start.bat
```

Ou execute diretamente o arquivo `start.bat` na raiz do projeto.

Esse processo irá iniciar automaticamente as APIs e o frontend.

---

## 🔄 Resetar dados

```bash
.\reset-data.bat
```

> Antes de executar:
>
> * feche todas as janelas/terminais do sistema
> * após o reset, execute novamente o `start.bat`

---

## 🔍 Endpoints da API

### Estoque

| Método | Endpoint                       |
| ------ | ------------------------------ |
| GET    | /produtos                      |
| GET    | /produtos/{id}                 |
| POST   | /produtos                      |
| PUT    | /produtos/{id}                 |
| DELETE | /produtos/{id}                 |
| GET    | /produtos/{id}/disponibilidade |
| POST   | /produtos/baixa                |

### Faturamento

| Método | Endpoint              |
| ------ | --------------------- |
| GET    | /notas                |
| GET    | /notas/{id}           |
| POST   | /notas                |
| PUT    | /notas/{id}           |
| DELETE | /notas/{id}           |
| POST   | /notas/{id}/impressao |

---

## 🔐 Idempotência

A impressão da nota utiliza uma chave única no header da requisição (**Idempotency-Key**), evitando inconsistências em múltiplas tentativas da mesma operação.

---

## 📚 Considerações técnicas

O frontend foi estruturado utilizando Angular com Angular Material, explorando o ciclo de vida dos componentes para carregamento de dados e organização da interface.

A comunicação com as APIs foi feita utilizando **RxJS**, permitindo lidar com chamadas assíncronas e tratamento de erros de forma controlada.

No backend, as APIs foram desenvolvidas com ASP.NET Core e Entity Framework, utilizando **LINQ** para consultas e manipulação dos dados.

Os erros são tratados tanto no backend quanto no frontend, garantindo que falhas como indisponibilidade do serviço de estoque ou falta de saldo sejam apresentadas de forma clara ao usuário, sem comprometer a consistência do sistema.

---

## 📝 Observações

* notas só podem ser editadas ou excluídas enquanto estiverem abertas
* a impressão depende da disponibilidade de estoque
* em caso de falha na comunicação entre serviços, a operação é interrompida sem afetar os dados
* o sistema mantém a consistência mesmo em cenários de erro

---

## 📄 Licença

Projeto desenvolvido para fins de avaliação técnica.

**Desenvolvido com muita dedicação por Dalton Santos**
