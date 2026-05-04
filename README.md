# Content Understanding Demo

A demo application showcasing **Azure AI Content Understanding Service (CUS)** for a banking account-opening workflow. Upload identity documents and proof-of-address PDFs, extract structured fields with CUS, and review results with an AI agent powered by Microsoft Agent Framework.

## Architecture

```
┌─────────────────┐       ┌──────────────────────────┐       ┌──────────────────────────┐
│   Vue 3 + TS    │──────▶│  .NET 10 Web API         │──────▶│  Azure AI Services       │
│   (Vite dev)    │ :5173 │  (Minimal API)           │ :5038 │  (Content Understanding) │
│                 │       │                          │       │  (OpenAI models)         │
└─────────────────┘       └──────────────────────────┘       └──────────────────────────┘
                                    │
                                    ▼
                          ┌──────────────────────────┐
                          │  Microsoft Agent         │
                          │  Framework v1.0          │
                          │  (Document Review Agent) │
                          └──────────────────────────┘
```

| Layer | Technology | Purpose |
|-------|-----------|---------|
| Infrastructure | Terraform (AzureRM v4 + azapi) | Azure resource provisioning |
| Backend | .NET 10 Minimal API | File upload, CUS SDK calls, agent orchestration |
| Frontend | Vue 3 + TypeScript + Vite | Document upload UI, results display |
| AI | Azure AI Content Understanding SDK | PDF extraction (prebuilt analyzers) |
| Agent | Microsoft Agent Framework v1.0 | Intelligent document review & validation |
| Observability | OpenTelemetry + Console Exporter | Traces, metrics, structured logging |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [Terraform 1.0+](https://www.terraform.io/downloads)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (logged in via `az login`)
- [Task](https://taskfile.dev/) (task runner)
- An Azure subscription with access to AI Services in **West US** region

## Quick Start

```bash
# 1. Deploy Azure infrastructure
task up

# 2. Run the application (backend + frontend)
task run
```

That's it. The `task up` command initializes Terraform, creates all Azure resources, and deploys required AI models. The `task run` command reads Terraform outputs and passes them as environment variables to the backend.

## Task Commands

| Command | Description |
|---------|-------------|
| `task up` | Full environment setup (init + apply) |
| `task apply` | Apply Terraform changes only |
| `task down` | Destroy Azure resources + clean all build files |
| `task build` | Build backend (.NET) and frontend (Vite) |
| `task run` | Run backend + frontend locally (hot-reload) |
| `task test` | Run all tests |
| `task clean` | Clean build artifacts |

## Project Structure

```
.
├── Taskfile.yml                    # Dev workflow orchestration
├── generate_pdfs.py                # Script to regenerate sample PDFs
├── assets/                         # Sample banking PDFs for demo
│   ├── sample_id_01..08.pdf        # Driver's licenses & passports
│   ├── sample_proof_of_address_01..04.pdf  # Utility bills
│   ├── sample_application_01..05.pdf       # Account applications
│   ├── sample_statement_01..02.pdf         # Bank statements
│   └── sample_tax_w2_01.pdf               # W-2 form
├── infrastructure/                 # Terraform IaC
│   ├── main.tf                     # Core resources (RG, Storage, AI Services)
│   ├── ai_foundry_project.tf       # Foundry project module call
│   ├── container-apps.tf           # Container Apps + Cosmos DB (stretch goal)
│   ├── outputs.tf                  # Terraform outputs consumed by Taskfile
│   └── project/                    # Foundry project submodule
│       ├── ai_foundry_project.tf          # Project resource
│       ├── ai_foundry_project_models.tf   # Model deployments (serialized)
│       └── outputs.tf                     # Project endpoint output
└── src/
    ├── backend/ContentUnderstanding.Api/
    │   ├── Program.cs              # App startup, OTEL, auth, endpoints
    │   ├── Services/
    │   │   ├── ContentUnderstandingService.cs  # CUS SDK integration
    │   │   └── DocumentAgentService.cs        # Agent Framework integration
    │   ├── Agents/
    │   │   └── DocumentProcessingSquad.cs     # Multi-agent squad
    │   └── Models/
    │       └── DocumentAnalysisResult.cs      # Response DTOs
    └── frontend/
        ├── src/                    # Vue 3 components
        ├── vite.config.ts          # Dev server + API proxy
        └── package.json
```

## Azure Resources Deployed

| Resource | Purpose |
|----------|---------|
| Resource Group | `{random}-rg` container |
| AI Services (S0) | Content Understanding + model hosting |
| Foundry Project | AI project with model deployments |
| Storage Account | Document blob storage |
| Log Analytics | Centralized logging |
| Container App Environment | Deployment target (stretch goal) |
| Cosmos DB | Application persistence (stretch goal) |

### Model Deployments

The following models are deployed sequentially (Azure requires serial deployment):

| Model | SKU | Purpose |
|-------|-----|---------|
| `gpt-5.4` | GlobalStandard | Agent document review |
| `gpt-4.1` | GlobalStandard | CUS prebuilt analyzers (invoice, receipt, ID) |
| `gpt-4.1-mini` | GlobalStandard | CUS RAG analyzers (documentSearch) |
| `text-embedding-3-large` | Standard | Semantic search & embeddings |

## Authentication

**RBAC-only** — no API keys. The application uses a `ChainedTokenCredential`:

1. `AzureCliCredential` — local development (your `az login` session)
2. `EnvironmentCredential` — CI/CD or service principal scenarios
3. `ManagedIdentityCredential` — Container Apps deployment

Required role assignments (auto-provisioned by Terraform):
- **Cognitive Services User** on the AI Services resource
- **Storage Blob Data Contributor** on the Storage Account

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/documents/upload` | Upload PDF → CUS binary analysis → returns extracted fields |
| POST | `/api/documents/analyze-agent` | Upload PDF → CUS analysis → Agent review → structured result |
| POST | `/api/documents/squad-process` | Upload PDF → CUS analysis → Multi-agent squad pipeline |

## How It Works

### Document Processing Flow

1. **Upload** — User selects a PDF in the Vue frontend
2. **Binary Analysis** — Backend sends raw bytes to CUS via `AnalyzeBinaryAsync` (no blob storage needed)
3. **Field Extraction** — CUS `prebuilt-documentSearch` analyzer extracts text/markdown
4. **Agent Review** — Agent Framework reviews extraction for account-opening eligibility
5. **Response** — Structured result with document type, extracted fields, confidence scores, and agent summary

### CUS Model Defaults (One-Time Setup)

On first startup, the backend calls `UpdateDefaultsAsync()` to configure model deployment mappings for CUS prebuilt analyzers. This maps:
- `gpt-4.1` → your gpt-4.1 deployment
- `gpt-4.1-mini` → your gpt-4.1-mini deployment
- `text-embedding-3-large` → your text-embedding-3-large deployment

## Demo Scenario (Banking Account Opening)

1. **Identity Verification** — Upload a driver's license or passport PDF
2. **Address Verification** — Upload a utility bill or bank statement
3. **Application Review** — Upload a completed account application form
4. **AI Review** — Agent validates completeness, flags missing fields or expired documents

### Sample Documents

The `assets/` folder contains 20 pre-generated sample PDFs covering all document types. Regenerate with:

```bash
python generate_pdfs.py
```

## Observability

The application is fully instrumented with OpenTelemetry:

- **Traces** — HTTP requests, CUS API calls, agent processing (custom ActivitySource)
- **Metrics** — ASP.NET Core, Kestrel, HTTP client metrics
- **Logs** — Structured logging with console exporter, debug-level in development

Startup banner shows configuration status:
```
=== Content Understanding Demo ===
CUS Endpoint:     https://teal-22081-cus.cognitiveservices.azure.com/
Foundry Project:  https://teal-22081-cus.services.ai.azure.com/api/projects/teal-22081-project
Storage Account:  ✅ Configured
Auth chain:       AzureCliCredential → EnvironmentCredential → ManagedIdentityCredential
✅ Entra ID token acquired successfully
✅ CUS model defaults configured successfully
```

## Deployment (Stretch Goal)

Infrastructure for Azure Container Apps + Cosmos DB is pre-provisioned. To deploy:

1. Build a container image for the backend
2. Push to a container registry
3. Update the Container App image reference in Terraform
4. Cosmos DB provides persistent storage for application submissions

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `Azure:FoundryProjectEndpoint is not configured` | Run `task up` or `task apply` to deploy infrastructure |
| `RBAC authentication failed` | Ensure you're logged in with `az login` and have Cognitive Services User role |
| `DeploymentNotFound (404)` | Models need time to deploy. Wait 5 minutes after `task apply` |
| `RequestConflict (409)` on Terraform apply | Azure rejects concurrent model deployments — models are now serialized with `depends_on` |
| Vite proxy ECONNREFUSED | Ensure backend is running on `http://localhost:5038` |
| Content Understanding only in West US | CUS is currently limited to the `westus` region |

## License

MIT
