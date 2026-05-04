# Content Understanding Demo

A demo application showcasing **Azure AI Content Understanding Service** for banking document processing (account opening workflow).

## Architecture

| Layer | Technology | Purpose |
|-------|-----------|---------|
| Infrastructure | Terraform | Azure resource provisioning |
| Backend | .NET 8 Web API | File upload, CUS orchestration |
| Frontend | Vue 3 + TypeScript | Document upload UI, results display |

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Terraform](https://www.terraform.io/downloads)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- An Azure subscription

### 1. Provision Infrastructure

```bash
cd infrastructure
terraform init
terraform apply
```

### 2. Run Backend

```bash
cd src/backend
dotnet run
```

### 3. Run Frontend

```bash
cd src/frontend
npm install
npm run dev
```

## Demo Scenario

1. Upload an identity document (passport, driver's license PDF)
2. Upload proof of address (utility bill, bank statement PDF)
3. View extracted structured data (name, address, DOB, document number)
4. Review confidence scores and field-level results
