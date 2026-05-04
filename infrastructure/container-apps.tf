# Container Apps deployment (stretch goal)
# Adds Azure Container Apps with a persistent backend (Cosmos DB)

resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-cus-demo"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_container_app_environment" "main" {
  name                       = "cae-cus-demo"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
}

resource "azurerm_cosmosdb_account" "main" {
  name                = var.cosmos_account_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = azurerm_resource_group.main.location
    failover_priority = 0
  }
}

resource "azurerm_cosmosdb_sql_database" "main" {
  name                = "AccountOpeningDB"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  throughput          = 400
}

resource "azurerm_cosmosdb_sql_container" "applications" {
  name                = "applications"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_sql_database.main.name
  partition_key_paths = ["/applicantId"]
  throughput          = 400
}

resource "azurerm_container_app" "backend" {
  name                         = "ca-cus-backend"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  template {
    container {
      name   = "backend"
      image  = "ghcr.io/${var.github_repo_owner}/content-understanding-demo/backend:latest"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "Azure__ContentUnderstandingEndpoint"
        value = azurerm_cognitive_account.content_understanding.endpoint
      }

      env {
        name        = "Azure__ContentUnderstandingKey"
        secret_name = "cus-key"
      }

      env {
        name  = "Azure__StorageConnectionString"
        value = azurerm_storage_account.documents.primary_connection_string
      }

      env {
        name  = "Azure__CosmosConnectionString"
        value = azurerm_cosmosdb_account.main.primary_sql_connection_string
      }
    }

    min_replicas = 0
    max_replicas = 3
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  secret {
    name  = "cus-key"
    value = azurerm_cognitive_account.content_understanding.primary_access_key
  }
}

output "container_app_url" {
  description = "Container App FQDN"
  value       = azurerm_container_app.backend.latest_revision_fqdn
}

output "cosmos_endpoint" {
  description = "Cosmos DB endpoint"
  value       = azurerm_cosmosdb_account.main.endpoint
}
