terraform {
  required_version = ">= 1.5"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = ">= 6.0"
    }
    neon = {
      source = "kislerdm/neon"
    }
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

provider "neon" {}

# --- Artifact Registry ---

module "artifact_registry" {
  source = "../../modules/artifact-registry"

  project_id    = var.project_id
  region        = var.region
  repository_id = "mental-metal"
}

# --- NeonDB ---

module "neondb" {
  source = "../../modules/neondb"

  neon_project_id = var.neon_project_id
  neon_branch_id  = var.neon_branch_id
  database_name   = "mentalmetalstaging"
  role_name       = "mentalmetalstaging"
}

# --- Secret Manager ---

module "secrets" {
  source = "../../modules/secret-manager"

  project_id   = var.project_id
  secret_names = ["DATABASE_URL"]
  secret_values = {
    "DATABASE_URL" = coalesce(var.database_connection_string, module.neondb.connection_uri)
  }
  accessor_service_account = var.service_account_email
}

# --- Cloud Run ---

module "cloud_run" {
  source = "../../modules/cloud-run"

  project_id   = var.project_id
  region       = var.region
  service_name = "mental-metal-staging"
  image        = var.image
  secret_ids   = module.secrets.secret_ids
}
