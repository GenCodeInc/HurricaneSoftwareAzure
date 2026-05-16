variable "subscription_id" {
  description = "Azure subscription ID. Leave null to use the active Azure CLI or environment context."
  type        = string
  default     = null
}

variable "tenant_id" {
  description = "Azure tenant ID. Leave null to use the active Azure CLI or environment context."
  type        = string
  default     = null
}

variable "create_resource_group" {
  description = "Create the resource group if it does not already exist."
  type        = bool
  default     = false
}

variable "resource_group_name" {
  description = "Resource group that will contain the Azure SQL resources."
  type        = string
}

variable "location" {
  description = "Azure region for the SQL logical server and databases. Use the full Azure name, for example centralus."
  type        = string
}

variable "sql_server_name" {
  description = "Optional globally unique Azure SQL logical server name. Leave null to generate one."
  type        = string
  default     = null
}

variable "sql_server_name_prefix" {
  description = "Prefix used when sql_server_name is not provided."
  type        = string
  default     = "sql-gencode"
}

variable "administrator_login" {
  description = "SQL administrator login for the logical server."
  type        = string
}

variable "administrator_password" {
  description = "SQL administrator password for the logical server."
  type        = string
  sensitive   = true
}

variable "allow_azure_services" {
  description = "Whether to create the 0.0.0.0 firewall rule that allows Azure-hosted services to connect."
  type        = bool
  default     = false
}

variable "client_ip_address" {
  description = "Optional public client IP to allow through the SQL firewall. Leave null to skip the client rule."
  type        = string
  default     = null
}

variable "database_names" {
  description = "Names of the Azure SQL databases to create."
  type        = set(string)
  default     = ["TTE"]
}

variable "sku_name" {
  description = "Azure SQL database SKU. Basic was the lowest-friction cheap option that worked here."
  type        = string
  default     = "Basic"
}

variable "max_size_gb" {
  description = "Maximum size in GB for each database."
  type        = number
  default     = 2
}

variable "storage_account_type" {
  description = "Backup storage redundancy. Local is the cheapest option."
  type        = string
  default     = "Local"
}

variable "publish_dacpacs" {
  description = "Whether Terraform should run SqlPackage locally after creating the databases."
  type        = bool
  default     = false
}

variable "dacpac_directory" {
  description = "Path to the directory containing DACPAC files. Relative paths are resolved from the terraform folder."
  type        = string
  default     = "../DAC Packages"
}

variable "sqlpackage_path" {
  description = "Optional explicit path to SqlPackage.exe. Leave null to use sqlpackage from PATH."
  type        = string
  default     = null
}

variable "exclude_object_types" {
  description = "Object types excluded during DACPAC publish for Azure SQL compatibility."
  type        = list(string)
  default     = ["Users", "Logins", "StoredProcedures"]
}

variable "deploy_function_app" {
  description = "Whether to create the Azure Function App infrastructure for the NHC parser on Flex Consumption."
  type        = bool
  default     = false
}

variable "function_app_name" {
  description = "Optional globally unique Function App name. Leave null to generate one."
  type        = string
  default     = null
}

variable "function_app_name_prefix" {
  description = "Prefix used when function_app_name is not provided."
  type        = string
  default     = "func-nhcparser-flex"
}

variable "function_app_plan_name" {
  description = "Optional Flex Consumption plan name for the Function App. Leave null to generate one."
  type        = string
  default     = null
}

variable "function_app_plan_name_prefix" {
  description = "Prefix used when function_app_plan_name is not provided."
  type        = string
  default     = "asp-nhcparser-flex"
}

variable "function_app_plan_sku_name" {
  description = "Flex Consumption plan SKU for the Function App. Keep this at FC1 unless Azure adds another Flex plan SKU."
  type        = string
  default     = "FC1"
}

variable "function_maximum_instance_count" {
  description = "Maximum number of Flex Consumption instances the Function App can scale out to."
  type        = number
  default     = 100
}

variable "function_instance_memory_in_mb" {
  description = "Per-instance memory size for the Flex Consumption Function App. Supported values currently include 512, 2048, and 4096."
  type        = number
  default     = 512
}

variable "function_always_ready_function_name" {
  description = "Function name to keep warm with Flex Consumption Always Ready."
  type        = string
  default     = "NHCParserTimer"
}

variable "function_always_ready_instance_count" {
  description = "Always Ready instance count for the configured function on Flex Consumption."
  type        = number
  default     = 1
}

variable "function_storage_account_name" {
  description = "Optional globally unique storage account name for the Function App. Leave null to generate one."
  type        = string
  default     = null
}

variable "function_storage_account_name_prefix" {
  description = "Prefix used when function_storage_account_name is not provided. Must stay lowercase letters and numbers only."
  type        = string
  default     = "stnhcparser"
}

variable "function_log_analytics_workspace_name" {
  description = "Optional Log Analytics workspace name for the Function App monitoring data. Leave null to generate one."
  type        = string
  default     = null
}

variable "function_application_insights_name" {
  description = "Optional Application Insights resource name for the Function App. Leave null to reuse the function app name."
  type        = string
  default     = null
}

variable "function_storage_account_tier" {
  description = "Storage tier for the Function App storage account."
  type        = string
  default     = "Standard"
}

variable "function_storage_account_replication_type" {
  description = "Replication type for the Function App storage account. LRS is the cheapest baseline."
  type        = string
  default     = "LRS"
}

variable "function_sql_database_name" {
  description = "Database used by the Function App connection string when one is generated automatically."
  type        = string
  default     = "TTE"
}

variable "function_sql_connection_string" {
  description = "Optional explicit SQL connection string for the Function App. Leave null to build one from the SQL server inputs in this module."
  type        = string
  default     = null
  sensitive   = true
}

variable "function_timer_schedule" {
  description = "Timer schedule for the Function App in NCRONTAB format."
  type        = string
  default     = "0 */5 * * * *"
}

variable "function_current_year_only" {
  description = "Whether the Function App should only persist advisories for the current year."
  type        = bool
  default     = true
}

variable "function_probe_database_on_startup" {
  description = "Whether the Function App should probe the advisory stored procedure on startup."
  type        = bool
  default     = false
}

variable "enable_function_vnet_integration" {
  description = "Whether to create a dedicated VNet/subnet for the Function App and bind Azure SQL access to that subnet."
  type        = bool
  default     = true
}

variable "function_vnet_name" {
  description = "Optional virtual network name for the Function App integration network. Leave null to generate one."
  type        = string
  default     = null
}

variable "function_vnet_address_space" {
  description = "Address space for the Function App integration virtual network."
  type        = string
  default     = "10.20.0.0/24"
}

variable "function_integration_subnet_name" {
  description = "Subnet name used for regional VNet integration from the Function App."
  type        = string
  default     = "snet-nhcparser-functions"
}

variable "function_integration_subnet_address_prefix" {
  description = "Address prefix for the Function App integration subnet."
  type        = string
  default     = "10.20.0.0/27"
}

variable "function_sql_virtual_network_rule_name" {
  description = "Azure SQL virtual network rule name that grants the Function App subnet access to the SQL server."
  type        = string
  default     = "NHCParserFunctionSubnet"
}

variable "function_vnet_route_all_enabled" {
  description = "Whether the Function App should route all outbound traffic through the integrated virtual network."
  type        = bool
  default     = true
}

variable "enable_function_sql_private_endpoint" {
  description = "Whether to create a SQL private endpoint and private DNS zone for the Function App VNet."
  type        = bool
  default     = true
}

variable "function_private_endpoint_subnet_name" {
  description = "Subnet name used for SQL private endpoints in the Function App VNet."
  type        = string
  default     = "snet-nhcparser-private-endpoints"
}

variable "function_private_endpoint_subnet_address_prefix" {
  description = "Address prefix for the SQL private endpoint subnet."
  type        = string
  default     = "10.20.0.32/27"
}

variable "function_private_dns_zone_name" {
  description = "Private DNS zone name used for Azure SQL private endpoint resolution."
  type        = string
  default     = "privatelink.database.windows.net"
}

variable "function_private_dns_zone_vnet_link_name" {
  description = "Virtual network link name for the SQL private DNS zone."
  type        = string
  default     = "link-nhcparser-sql"
}

variable "function_sql_private_endpoint_name" {
  description = "Private endpoint name for Azure SQL in the Function App VNet."
  type        = string
  default     = "pe-sql-gencode-cu66c7"
}

variable "function_sql_private_dns_zone_group_name" {
  description = "Private DNS zone group name attached to the SQL private endpoint."
  type        = string
  default     = "sql-dns-zone-group"
}