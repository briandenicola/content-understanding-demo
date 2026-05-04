output "cognitive_endpoint" {
  description = "Azure AI Services endpoint URL"
  value       = azapi_resource.content_understanding.output.properties.endpoint
}

output "storage_account_name" {
  description = "Storage Account name"
  value       = azurerm_storage_account.documents.name
}

output "resource_name" {
  description = "Base resource name"
  value       = local.resource_name
}

output "CONTENT_UNDERSTANDING_ENDPOINT" {
  description = "CUS endpoint for app consumption"
  value       = azapi_resource.content_understanding.output.properties.endpoint
}

output "STORAGE_CONNECTION_STRING" {
  description = "Storage connection string for app consumption"
  value       = azurerm_storage_account.documents.primary_connection_string
  sensitive   = true
}
