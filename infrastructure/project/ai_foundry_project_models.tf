resource "azapi_resource" "model_gpt54" {
  type      = "Microsoft.CognitiveServices/accounts/deployments@2025-06-01"
  name      = "gpt-5.4"
  parent_id = var.foundry_project.ai_foundry.id

  body = {
    properties = {
      model = {
        format  = "OpenAI"
        name    = "gpt-5.4"
        version = "2026-03-05"
      }
    }
    sku = {
      name     = "GlobalStandard"
      capacity = 250
    }
  }
}

resource "azapi_resource" "model_gpt41" {
  depends_on = [azapi_resource.model_gpt54]
  type       = "Microsoft.CognitiveServices/accounts/deployments@2025-06-01"
  name       = "gpt-4.1"
  parent_id  = var.foundry_project.ai_foundry.id

  body = {
    properties = {
      model = {
        format  = "OpenAI"
        name    = "gpt-4.1"
        version = "2025-04-14"
      }
    }
    sku = {
      name     = "GlobalStandard"
      capacity = 250
    }
  }
}

resource "azapi_resource" "model_gpt41_mini" {
  depends_on = [azapi_resource.model_gpt41]
  type       = "Microsoft.CognitiveServices/accounts/deployments@2025-06-01"
  name       = "gpt-4.1-mini"
  parent_id  = var.foundry_project.ai_foundry.id

  body = {
    properties = {
      model = {
        format  = "OpenAI"
        name    = "gpt-4.1-mini"
        version = "2025-04-14"
      }
    }
    sku = {
      name     = "GlobalStandard"
      capacity = 250
    }
  }
}

resource "azapi_resource" "model_embedding" {
  depends_on = [azapi_resource.model_gpt41_mini]
  type       = "Microsoft.CognitiveServices/accounts/deployments@2025-06-01"
  name       = "text-embedding-3-large"
  parent_id  = var.foundry_project.ai_foundry.id

  body = {
    properties = {
      model = {
        format  = "OpenAI"
        name    = "text-embedding-3-large"
        version = "1"
      }
    }
    sku = {
      name     = "Standard"
      capacity = 120
    }
  }
}
