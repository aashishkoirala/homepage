output "aks_cluster_fqdn" {
	value = "${azurerm_kubernetes_cluster.kub.fqdn}"
}

output "acr_login_server" {
	value = "${azurerm_container_registry.cr.login_server}"
}

output "service_principal_app_id" {
	value = "${azuread_application.sp.application_id}"
}