variable "resource_group_name" {
  description = "Name of the Azure Resource Group"
  type        = string
  default     = "rg-content-understanding-demo"
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "eastus"
}

variable "storage_account_name" {
  description = "Name of the Storage Account (must be globally unique)"
  type        = string
  default     = "stcusdemo"
}

variable "cognitive_account_name" {
  description = "Name of the Azure AI Services account"
  type        = string
  default     = "cog-cus-demo"
}
