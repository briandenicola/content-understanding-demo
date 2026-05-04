# Copilot Instructions — Content Understanding Demo

## Project Overview

This is a demo application showcasing Azure AI Content Understanding Service (CUS) for a banking use case — specifically PDF document upload and analysis for account opening workflows.

## Architecture

- **Infrastructure**: Terraform (Azure provider) — provisions Azure AI Services, Storage Account, and supporting resources
- **Backend**: .NET 8 Web API (C#) — handles file uploads, orchestrates CUS API calls, returns structured extraction results
- **Frontend**: Vue 3 + TypeScript + Vite — single-page app for uploading documents and viewing extracted data
- **Target Environment**: Local development machine calling into Azure CUS endpoints

## Project Structure

```
/infrastructure        — Terraform files for Azure resource provisioning
/src/backend           — .NET 8 Web API project
/src/frontend          — Vue 3 + Vite frontend project
```

## Key Design Decisions

- The backend proxies all calls to Azure CUS; the frontend never calls Azure directly
- Documents are uploaded to Azure Blob Storage via the backend, then submitted to CUS for analysis
- CUS analyzers are configured for banking documents (ID verification, proof of address, account application forms)
- All secrets/keys are managed via environment variables or Azure Key Vault — never hardcoded
- The demo is designed to run locally with `dotnet run` and `npm run dev`

## Coding Conventions

- **C#**: Use minimal APIs, async/await throughout, nullable reference types enabled
- **TypeScript/Vue**: Composition API with `<script setup>`, TypeScript strict mode
- **Terraform**: Use variables for all configurable values, output resource endpoints
- **Naming**: PascalCase for C#, camelCase for TypeScript, snake_case for Terraform

## Banking Use Case Context

The demo simulates a bank's digital account opening flow:
1. Customer uploads identity document (passport, driver's license)
2. Customer uploads proof of address (utility bill, bank statement)
3. CUS extracts structured fields (name, address, DOB, document number, etc.)
4. Backend returns extracted data for review in the UI

## Security Notes

- Never commit `.env` files, `appsettings.Development.json` with secrets, or `terraform.tfvars`
- Use `.gitignore` to exclude sensitive files
- Demo credentials should use Azure RBAC or short-lived keys

## Testing

- Backend: xUnit integration tests mocking CUS responses
- Frontend: Vitest + Vue Test Utils for component tests
- Infrastructure: `terraform validate` and `terraform plan` for IaC validation
