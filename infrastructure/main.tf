terraform {
  required_version = ">= 1.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4"
    }
    azapi = {
      source  = "Azure/azapi"
      version = "~> 2"
    }
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

locals {
  location                 = var.region
  resource_name            = "${random_pet.this.id}-${random_id.this.dec}"
  cognitive_account_name   = "${local.resource_name}-cus"
  storage_account_name     = "${substr(replace(random_uuid.guid.result, "-", ""), 0, 22)}sa"
  loganalytics_name        = "${local.resource_name}-logs"
  aca_name                 = "${local.resource_name}-env"
  cosmos_account_name      = "${local.resource_name}-cosmos"
}

data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "this" {
  name     = "${local.resource_name}-rg"
  location = local.location
  tags = {
    Application = var.tags
    DeployedOn  = timestamp()
    AppName     = local.resource_name
  }
}

resource "azurerm_storage_account" "documents" {
  name                          = local.storage_account_name
  resource_group_name           = azurerm_resource_group.this.name
  location                      = azurerm_resource_group.this.location
  account_tier                  = "Standard"
  account_replication_type      = "LRS"
  shared_access_key_enabled     = true

  blob_properties {
    cors_rule {
      allowed_headers    = ["*"]
      allowed_methods    = ["GET", "POST", "PUT"]
      allowed_origins    = ["http://localhost:5173"]
      exposed_headers    = ["*"]
      max_age_in_seconds = 3600
    }
  }
}

resource "azurerm_storage_container" "uploads" {
  name               = "document-uploads"
  storage_account_id = azurerm_storage_account.documents.id
}

resource "azapi_resource" "content_understanding" {
  type                      = "Microsoft.CognitiveServices/accounts@2025-10-01-preview"
  name                      = local.cognitive_account_name
  parent_id                 = azurerm_resource_group.this.id
  location                  = azurerm_resource_group.this.location
  schema_validation_enabled = false

  body = {
    kind = "AIServices"
    sku = {
      name = "S0"
    }
    identity = {
      type = "SystemAssigned"
    }
    properties = {
      disableLocalAuth    = true
      customSubDomainName = local.cognitive_account_name
    }
  }

  response_export_values = [
    "properties.endpoint",
    "identity.principalId"
  ]
}

data "azurerm_cognitive_account" "content_understanding" {
  depends_on          = [azapi_resource.content_understanding]
  name                = local.cognitive_account_name
  resource_group_name = azurerm_resource_group.this.name
}

resource "azurerm_role_assignment" "cognitive_blob_access" {
  scope                = azurerm_storage_account.documents.id
  role_definition_name = "Storage Blob Data Reader"
  principal_id         = azapi_resource.content_understanding.output.identity.principalId
}

resource "azurerm_role_assignment" "user_cognitive_access" {
  scope                = azapi_resource.content_understanding.id
  role_definition_name = "Cognitive Services User"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_role_assignment" "user_blob_access" {
  scope                = azurerm_storage_account.documents.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = data.azurerm_client_config.current.object_id
}
