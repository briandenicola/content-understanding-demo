module "project_cus" {
  depends_on = [
    azapi_resource.content_understanding,
  ]
  source = "./project"

  foundry_project = {
    name          = "${local.resource_name}-project"
    location      = local.location
    resource_name = local.resource_name
    tag           = var.tags

    ai_foundry = {
      id   = azapi_resource.content_understanding.id
      name = azapi_resource.content_understanding.name
    }

    logs = {
      workspace_id = azurerm_log_analytics_workspace.main.id
    }

    models = [
      {
        name    = "gpt-5.4"
        version = "2026-03-05"
        format  = "OpenAI"
      },
      {
        name    = "gpt-4.1"
        version = "2025-04-14"
        format  = "OpenAI"
      },
      {
        name    = "gpt-4.1-mini"
        version = "2025-04-14"
        format  = "OpenAI"
      },
      {
        name     = "text-embedding-3-large"
        version  = "1"
        format   = "OpenAI"
        sku      = "Standard"
        capacity = 120
      }
    ]
  }
}
