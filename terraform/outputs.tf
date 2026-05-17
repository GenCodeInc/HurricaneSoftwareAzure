output "resource_group_name" {
  description = "Resource group containing the Azure SQL resources."
  value       = var.resource_group_name
}

output "sql_server_name" {
  description = "Azure SQL logical server name."
  value       = azurerm_mssql_server.this.name
}

output "sql_server_fqdn" {
  description = "Azure SQL logical server fully qualified domain name."
  value       = azurerm_mssql_server.this.fully_qualified_domain_name
}

output "database_names" {
  description = "Azure SQL databases created by this configuration."
  value       = sort(keys(azurerm_mssql_database.databases))
}

output "function_app_name" {
  description = "Azure Function App name when deploy_function_app is enabled."
  value       = var.deploy_function_app ? azurerm_function_app_flex_consumption.this[0].name : null
}

output "function_default_hostname" {
  description = "Default hostname for the Azure Function App when deploy_function_app is enabled."
  value       = var.deploy_function_app ? azurerm_function_app_flex_consumption.this[0].default_hostname : null
}

output "function_storage_account_name" {
  description = "Storage account name used by the Azure Function App when deploy_function_app is enabled."
  value       = var.deploy_function_app ? azurerm_storage_account.function[0].name : null
}

output "function_app_service_plan_name" {
  description = "Flex Consumption plan name used by the Azure Function App when deploy_function_app is enabled."
  value       = var.deploy_function_app ? azurerm_service_plan.function[0].name : null
}

output "function_application_insights_name" {
  description = "Application Insights resource name used by the Azure Function App when deploy_function_app is enabled."
  value       = var.deploy_function_app ? azurerm_application_insights.function[0].name : null
}

output "function_log_analytics_workspace_name" {
  description = "Log Analytics workspace name used by the Azure Function App when deploy_function_app is enabled."
  value       = var.deploy_function_app ? azurerm_log_analytics_workspace.function[0].name : null
}

output "function_virtual_network_name" {
  description = "Virtual network name used for Function App regional VNet integration when enabled."
  value       = var.deploy_function_app && var.enable_function_vnet_integration ? azurerm_virtual_network.function[0].name : null
}

output "function_integration_subnet_id" {
  description = "Subnet resource ID used for Function App regional VNet integration when enabled."
  value       = var.deploy_function_app && var.enable_function_vnet_integration ? azurerm_subnet.function_integration[0].id : null
}

output "function_private_endpoint_subnet_id" {
  description = "Subnet resource ID used for SQL private endpoints when enabled."
  value       = var.deploy_function_app && var.enable_function_vnet_integration && var.enable_function_sql_private_endpoint ? azurerm_subnet.function_private_endpoints[0].id : null
}

output "function_private_dns_zone_name" {
  description = "Private DNS zone name used for Azure SQL private endpoint resolution when enabled."
  value       = var.deploy_function_app && var.enable_function_vnet_integration && var.enable_function_sql_private_endpoint ? azurerm_private_dns_zone.function_sql[0].name : null
}

output "function_sql_private_endpoint_id" {
  description = "Private endpoint resource ID used for Azure SQL when enabled."
  value       = var.deploy_function_app && var.enable_function_vnet_integration && var.enable_function_sql_private_endpoint ? azurerm_private_endpoint.function_sql[0].id : null
}

output "static_web_app_name" {
  description = "Azure Static Web App name when deploy_static_web_app is enabled."
  value       = var.deploy_static_web_app ? azurerm_static_web_app.website[0].name : null
}

output "static_web_app_default_hostname" {
  description = "Default hostname for the Azure Static Web App when deploy_static_web_app is enabled."
  value       = var.deploy_static_web_app ? azurerm_static_web_app.website[0].default_host_name : null
}

output "website_acs_email_service_name" {
  description = "ACS Email Service name when deploy_website_acs_email is enabled."
  value       = var.deploy_website_acs_email ? azapi_resource.website_acs_email_service[0].name : null
}

output "website_acs_communication_service_name" {
  description = "ACS Communication Service name when deploy_website_acs_email is enabled."
  value       = var.deploy_website_acs_email ? azapi_resource.website_acs_communication_service[0].name : null
}

output "website_acs_sender_domain" {
  description = "Azure-managed ACS sender domain when deploy_website_acs_email is enabled."
  value       = var.deploy_website_acs_email ? jsondecode(azapi_resource.website_acs_domain[0].output).properties.fromSenderDomain : null
}

output "website_acs_sender_address" {
  description = "Sender address for website-originated ACS email when deploy_website_acs_email is enabled."
  value       = var.deploy_website_acs_email ? format("%s@%s", var.website_acs_sender_username, jsondecode(azapi_resource.website_acs_domain[0].output).properties.fromSenderDomain) : null
}