variable "project_id" {
  description = "GCP project ID"
  type        = string
}

variable "region" {
  description = "GCP region"
  type        = string
}

variable "service_name" {
  description = "Cloud Run service name"
  type        = string
}

variable "image" {
  description = "Container image to deploy"
  type        = string
}

variable "runtime_service_account" {
  description = "Service account email for the Cloud Run service to run as"
  type        = string
}

variable "secret_ids" {
  description = "Map of environment variable name to Secret Manager secret ID"
  type        = map(string)
  default     = {}
}

variable "env_vars" {
  description = "Map of plain (non-secret) environment variables to set on the container"
  type        = map(string)
  default     = {}
}

variable "allow_public_access" {
  description = "Whether to allow unauthenticated access to the service"
  type        = bool
  default     = false
}
