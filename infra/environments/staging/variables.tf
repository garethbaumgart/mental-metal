variable "project_id" {
  description = "GCP project ID"
  type        = string
}

variable "region" {
  description = "GCP region"
  type        = string
  default     = "australia-southeast1"
}

variable "neon_org_id" {
  description = "Neon organization ID"
  type        = string
  sensitive   = true
}

variable "neon_project_id" {
  description = "Existing Neon project ID (optional, creates new if null)"
  type        = string
  default     = null
}

variable "neon_branch_id" {
  description = "Existing Neon branch ID (required when reusing a Neon project)"
  type        = string
  default     = null
}

variable "neon_endpoint_host" {
  description = "Existing Neon endpoint host (required when reusing a Neon project)"
  type        = string
  default     = null
}

variable "neondb_owner_password" {
  description = "Password for the neondb_owner role (from Neon console)"
  type        = string
  sensitive   = true

  validation {
    condition     = length(var.neondb_owner_password) > 0
    error_message = "neondb_owner_password must not be empty."
  }
}

variable "image" {
  description = "Container image to deploy to Cloud Run (managed by CD pipeline)"
  type        = string
  default     = "us-docker.pkg.dev/cloudrun/container/hello:latest"
}
