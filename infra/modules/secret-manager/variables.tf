variable "project_id" {
  description = "GCP project ID"
  type        = string
}

variable "secret_names" {
  description = "List of secret names to create"
  type        = list(string)
}

variable "region" {
  description = "Region for secret replication"
  type        = string
}

variable "accessor_service_account" {
  description = "Service account email to grant Secret Manager accessor role"
  type        = string
}
