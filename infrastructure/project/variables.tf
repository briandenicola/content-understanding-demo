variable "foundry_project" {
  type = object({
    name          = string
    resource_name = string
    location      = string
    ai_foundry = object({
      name = string
      id   = string
    })
    tag = string
    logs = object({
      workspace_id = string
    })
  })
}
