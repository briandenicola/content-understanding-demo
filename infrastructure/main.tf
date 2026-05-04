terraform {
  required_version = ">= 1.5.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.100"
    }
  }
}

provider "azurerm" {
  features {}
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
  name                     = local.storage_account_name
  resource_group_name      = azurerm_resource_group.this.name
  location                 = azurerm_resource_group.this.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

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
  name                  = "document-uploads"
  storage_account_name  = azurerm_storage_account.documents.name
  container_access_type = "private"
}

resource "azurerm_cognitive_account" "content_understanding" {
  name                = local.cognitive_account_name
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name
  kind                = "AIServices"
  sku_name            = "S0"

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_role_assignment" "cognitive_blob_access" {
  scope                = azurerm_storage_account.documents.id
  role_definition_name = "Storage Blob Data Reader"
  principal_id         = azurerm_cognitive_account.content_understanding.identity[0].principal_id
}

resource "azurerm_role_assignment" "user_cognitive_access" {
  scope                = azurerm_cognitive_account.content_understanding.id
  role_definition_name = "Cognitive Services User"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_role_assignment" "user_blob_access" {
  scope                = azurerm_storage_account.documents.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = data.azurerm_client_config.current.object_id
}
