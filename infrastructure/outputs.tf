output "cognitive_endpoint" {
  description = "Azure AI Services endpoint URL"
  value       = azurerm_cognitive_account.content_understanding.endpoint
}

output "cognitive_key" {
  description = "Azure AI Services primary access key"
  value       = azurerm_cognitive_account.content_understanding.primary_access_key
  sensitive   = true
}

output "storage_connection_string" {
  description = "Storage Account connection string"
  value       = azurerm_storage_account.documents.primary_connection_string
  sensitive   = true
}

output "storage_account_name" {
  description = "Storage Account name"
  value       = azurerm_storage_account.documents.name
}
