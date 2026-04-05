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

variable "secret_ids" {
  description = "Map of environment variable name to Secret Manager secret ID"
  type        = map(string)
  default     = {}
}
