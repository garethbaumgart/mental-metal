variable "project_id" {
  description = "GCP project ID"
  type        = string
}

variable "region" {
  description = "GCP region"
  type        = string
  default     = "australia-southeast1"
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

variable "image" {
  description = "Container image to deploy to Cloud Run"
  type        = string
}
