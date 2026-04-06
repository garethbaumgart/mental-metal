variable "neon_org_id" {
  description = "Neon organization ID"
  type        = string
  sensitive   = true
}

variable "neon_project_id" {
  description = "Existing Neon project ID. If null, a new project is created."
  type        = string
  default     = null
}

variable "neon_branch_id" {
  description = "Existing Neon branch ID. Required when reusing an existing project."
  type        = string
  default     = null

}

variable "neon_endpoint_host" {
  description = "Existing Neon endpoint host. Required when reusing an existing project."
  type        = string
  default     = null
}

variable "database_name" {
  description = "Name of the database to create"
  type        = string
}

variable "neondb_owner_password" {
  description = "Password for the neondb_owner role (from Neon console)"
  type        = string
  sensitive   = true
}
