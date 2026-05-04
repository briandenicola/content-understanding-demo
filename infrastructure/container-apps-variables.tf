variable "cosmos_account_name" {
  description = "Name of the Cosmos DB account"
  type        = string
  default     = "cosmos-cus-demo"
}

variable "github_repo_owner" {
  description = "GitHub repository owner for container image references"
  type        = string
  default     = "briandenicola"
}
