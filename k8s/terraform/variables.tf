variable "service_principal" {
  description = "The Azure service principal used to access ACR and AKS."
  default     = "akprod"
}

variable "service_principal_password" {
  description = "The password for the Azure service principal used to access ACR and AKS."
}

variable "resource_group" {
  description = "The name of the Azure Resource Group where everything lives."
  default     = "akrg"
}

variable "location" {
  description = "The location for all the resources."
  default     = "East US"
}

variable "container_registry" {
  description = "The name of the ACR resource."
  default     = "akprodcr"
}

variable "cluster" {
  description = "The name of the AKS cluster."
  default     = "akkube"
}

variable "kubernetes_namespace" {
  description = "The namespace in Kubernetes to use."
  default     = "ak-prod"
}

variable "node_count" {
  type        = number
  description = "The node count for Kubernetes."
  default     = 1
}