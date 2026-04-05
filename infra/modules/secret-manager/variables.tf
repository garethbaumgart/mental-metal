variable "project_id" {
  description = "GCP project ID"
  type        = string
}

variable "secrets" {
  description = "Map of secret names to their values"
  type        = map(string)
  sensitive   = true
}

variable "accessor_service_account" {
  description = "Service account email to grant Secret Manager accessor role"
  type        = string
}
