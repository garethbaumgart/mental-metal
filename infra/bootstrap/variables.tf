variable "org_id" {
  description = "GCP Organization ID"
  type        = string
  sensitive   = true
}

variable "billing_account" {
  description = "GCP Billing Account ID"
  type        = string
  sensitive   = true
}

variable "region" {
  description = "GCP region"
  type        = string
  default     = "australia-southeast1"
}
