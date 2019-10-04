provider "azurerm" {
  version = "=1.34.0"
}

provider "azuread" {
  version = "=0.4.0"
}

resource "azuread_application" "sp" {
  name = "${var.service_principal}"
}

resource "azuread_service_principal" "sp" {
  application_id = "${azuread_application.sp.application_id}"
}

resource "azuread_service_principal_password" "sp" {
  service_principal_id = "${azuread_service_principal.sp.id}"
  value                = "${var.service_principal_password}"
  end_date             = "${timeadd(timestamp(), "8760h")}"
}

resource "azurerm_resource_group" "rg" {
  name     = "${var.resource_group}"
  location = "${var.location}"
}

resource "azurerm_container_registry" "cr" {
  name                = "${var.container_registry}"
  resource_group_name = "${azurerm_resource_group.rg.name}"
  location            = "${azurerm_resource_group.rg.location}"
  sku                 = "Basic"
  admin_enabled       = false
}

resource "azurerm_role_assignment" "ra_cr_push" {
  scope                = "${azurerm_container_registry.cr.id}"
  role_definition_name = "AcrPush"
  principal_id         = "${azuread_service_principal.sp.id}"
}

resource "azurerm_role_assignment" "ra_cr_pull" {
  scope                = "${azurerm_container_registry.cr.id}"
  role_definition_name = "AcrPull"
  principal_id         = "${azuread_service_principal.sp.id}"
}

resource "azurerm_kubernetes_cluster" "kub" {
  name                = "${var.cluster}"
  resource_group_name = "${azurerm_resource_group.rg.name}"
  location            = "${azurerm_resource_group.rg.location}"
  dns_prefix          = "${var.cluster}"
  kubernetes_version  = "1.15.4"
  depends_on          = [azuread_service_principal.sp, azuread_service_principal_password.sp]

  agent_pool_profile {
    name            = "default"
    count           = var.node_count
    vm_size         = "Standard_D1_v2"
    os_type         = "Linux"
    os_disk_size_gb = 30
  }

  service_principal {
    client_id     = "${azuread_application.sp.application_id}"
    client_secret = "${var.service_principal_password}"
  }
}

resource "azurerm_role_assignment" "ra_kub" {
  scope                = "${azurerm_kubernetes_cluster.kub.id}"
  role_definition_name = "Azure Kubernetes Service Cluster User Role"
  principal_id         = "${azuread_service_principal.sp.id}"
}

resource "null_resource" "init" {
  depends_on = [azurerm_kubernetes_cluster.kub, azurerm_role_assignment.ra_kub]

  provisioner "local-exec" {
    command = "..\\cluster\\init ${var.cluster} ${var.resource_group} ${var.kubernetes_namespace} ${var.container_registry} ${azuread_application.sp.application_id} ${var.service_principal_password}"
  }
}
