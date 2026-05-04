variable "region" {
  description = "Azure region to deploy resources"
  type        = string
  default     = "eastus2"
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = string
  default     = "Content Understanding Demo"
}
