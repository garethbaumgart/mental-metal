terraform {
  required_version = ">= 1.5"

  required_providers {
    neon = {
      source  = "kislerdm/neon"
      version = "~> 0.13"
    }
  }
}

# Create project only when no existing project ID is provided
resource "neon_project" "this" {
  count = var.neon_project_id == null ? 1 : 0

  name      = "mental-metal"
  region_id = "aws-ap-southeast-2"
  org_id    = var.neon_org_id
}

locals {
  project_id = var.neon_project_id != null ? var.neon_project_id : neon_project.this[0].id
  # The default branch is created with the project; grab its endpoint for the connection string
  branch_id = var.neon_project_id != null ? var.neon_branch_id : neon_project.this[0].default_branch_id
}

resource "neon_database" "this" {
  project_id = local.project_id
  branch_id  = local.branch_id
  name       = var.database_name
  owner_name = "neondb_owner"

  lifecycle {
    precondition {
      condition     = var.neon_project_id == null || var.neon_branch_id != null
      error_message = "neon_branch_id is required when neon_project_id is provided."
    }
  }
}

locals {
  # Use the default endpoint host from the project
  endpoint_host = var.neon_project_id != null ? var.neon_endpoint_host : neon_project.this[0].database_host
}
