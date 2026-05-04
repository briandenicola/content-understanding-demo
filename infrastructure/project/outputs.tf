output "project_id" {
  description = "Foundry Project resource ID"
  value       = azapi_resource.ai_foundry_project.id
}

output "project_endpoint" {
  description = "Foundry Project endpoint"
  value       = azapi_resource.ai_foundry_project.output.properties.endpoints
}

output "project_principal_id" {
  description = "Foundry Project managed identity principal ID"
  value       = azapi_resource.ai_foundry_project.output.identity.principalId
}
