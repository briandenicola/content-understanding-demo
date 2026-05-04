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
  }
}
