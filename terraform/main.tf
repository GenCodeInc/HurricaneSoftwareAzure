locals {
  dacpac_directory = abspath(var.dacpac_directory)
  database_names   = toset(var.database_names)
}

resource "random_string" "server_suffix" {
  count   = var.sql_server_name == null ? 1 : 0
  length  = 6
  upper   = false
  lower   = true
  numeric = true
  special = false
}

resource "random_string" "function_suffix" {
  count   = var.deploy_function_app && (var.function_app_name == null || var.function_app_plan_name == null || var.function_storage_account_name == null) ? 1 : 0
  length  = 8
  upper   = false
  lower   = true
  numeric = true
  special = false
}

resource "random_string" "static_web_app_suffix" {
  count   = var.deploy_static_web_app && var.static_web_app_name == null ? 1 : 0
  length  = 8
  upper   = false
  lower   = true
  numeric = true
  special = false
}

resource "random_string" "website_acs_suffix" {
  count   = var.deploy_website_acs_email && (var.website_acs_email_service_name == null || var.website_acs_communication_service_name == null) ? 1 : 0
  length  = 8
  upper   = false
  lower   = true
  numeric = true
  special = false
}

locals {
  generated_server_suffix = var.sql_server_name == null ? random_string.server_suffix[0].result : null
  server_name             = var.sql_server_name != null ? var.sql_server_name : format("%s-%s", var.sql_server_name_prefix, local.generated_server_suffix)
  generated_function_suffix = var.deploy_function_app && (var.function_app_name == null || var.function_app_plan_name == null || var.function_storage_account_name == null) ? random_string.function_suffix[0].result : null
  function_app_name         = var.function_app_name != null ? var.function_app_name : (var.deploy_function_app ? format("%s-%s", var.function_app_name_prefix, local.generated_function_suffix) : null)
  function_plan_name        = var.function_app_plan_name != null ? var.function_app_plan_name : (var.deploy_function_app ? format("%s-%s", var.function_app_plan_name_prefix, local.generated_function_suffix) : null)
  function_storage_name     = var.function_storage_account_name != null ? var.function_storage_account_name : (var.deploy_function_app ? format("%s%s", var.function_storage_account_name_prefix, local.generated_function_suffix) : null)
  function_vnet_name        = var.function_vnet_name != null ? var.function_vnet_name : (var.deploy_function_app && var.enable_function_vnet_integration ? format("vnet-nhcparser-%s", local.generated_function_suffix) : null)
  function_sql_connection_string = var.function_sql_connection_string != null ? var.function_sql_connection_string : (var.deploy_function_app ? format("Server=tcp:%s,1433;Initial Catalog=%s;Persist Security Info=False;User ID=%s;Password=%s;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;", azurerm_mssql_server.this.fully_qualified_domain_name, var.function_sql_database_name, var.administrator_login, var.administrator_password) : null)
  generated_static_web_app_suffix = var.deploy_static_web_app && var.static_web_app_name == null ? random_string.static_web_app_suffix[0].result : null
  static_web_app_name             = var.static_web_app_name != null ? var.static_web_app_name : (var.deploy_static_web_app ? format("%s-%s", var.static_web_app_name_prefix, local.generated_static_web_app_suffix) : null)
  generated_website_acs_suffix            = var.deploy_website_acs_email && (var.website_acs_email_service_name == null || var.website_acs_communication_service_name == null) ? random_string.website_acs_suffix[0].result : null
  website_acs_email_service_name          = var.website_acs_email_service_name != null ? var.website_acs_email_service_name : (var.deploy_website_acs_email ? format("%s-%s", var.website_acs_email_service_name_prefix, local.generated_website_acs_suffix) : null)
  website_acs_communication_service_name  = var.website_acs_communication_service_name != null ? var.website_acs_communication_service_name : (var.deploy_website_acs_email ? format("%s-%s", var.website_acs_communication_service_name_prefix, local.generated_website_acs_suffix) : null)
}

resource "azurerm_resource_group" "this" {
  count    = var.create_resource_group ? 1 : 0
  name     = var.resource_group_name
  location = var.location
}

data "azurerm_resource_group" "existing" {
  count = var.create_resource_group ? 0 : 1
  name  = var.resource_group_name
}

locals {
  effective_resource_group_name = var.resource_group_name
  effective_resource_group_id   = var.create_resource_group ? azurerm_resource_group.this[0].id : data.azurerm_resource_group.existing[0].id
}

resource "azurerm_mssql_server" "this" {
  name                         = local.server_name
  resource_group_name          = local.effective_resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.administrator_login
  administrator_login_password = var.administrator_password
}

resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  count            = var.allow_azure_services ? 1 : 0
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.this.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_mssql_firewall_rule" "client_ip" {
  count            = var.client_ip_address == null ? 0 : 1
  name             = "ClientIp"
  server_id        = azurerm_mssql_server.this.id
  start_ip_address = var.client_ip_address
  end_ip_address   = var.client_ip_address
}

resource "azurerm_mssql_database" "databases" {
  for_each          = local.database_names
  name              = each.key
  server_id         = azurerm_mssql_server.this.id
  sku_name          = var.sku_name
  max_size_gb       = var.max_size_gb
  storage_account_type = var.storage_account_type
}

resource "null_resource" "publish_dacpacs" {
  count = var.publish_dacpacs ? 1 : 0

  triggers = {
    server_name          = azurerm_mssql_server.this.name
    database_names       = join(",", sort(tolist(local.database_names)))
    dacpac_directory     = local.dacpac_directory
    sqlpackage_path      = coalesce(var.sqlpackage_path, "sqlpackage")
    exclude_object_types = join(";", var.exclude_object_types)
  }

  provisioner "local-exec" {
    interpreter = ["PowerShell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File"]
    command     = "${path.module}/scripts/publish-dacpacs.ps1"

    environment = {
      DACPAC_DIRECTORY      = local.dacpac_directory
      SQLPACKAGE_PATH       = coalesce(var.sqlpackage_path, "sqlpackage")
      SQL_SERVER_NAME       = azurerm_mssql_server.this.name
      SQL_ADMIN_USER        = var.administrator_login
      SQL_ADMIN_PASSWORD    = var.administrator_password
      DATABASE_NAMES        = join(",", sort(tolist(local.database_names)))
      EXCLUDE_OBJECT_TYPES  = join(";", var.exclude_object_types)
    }
  }

  depends_on = [
    azurerm_mssql_database.databases,
    azurerm_mssql_firewall_rule.allow_azure_services,
    azurerm_mssql_firewall_rule.client_ip,
  ]
}

resource "azurerm_storage_account" "function" {
  count                    = var.deploy_function_app ? 1 : 0
  name                     = local.function_storage_name
  resource_group_name      = local.effective_resource_group_name
  location                 = var.location
  account_tier             = var.function_storage_account_tier
  account_replication_type = var.function_storage_account_replication_type
  min_tls_version          = "TLS1_2"

  allow_nested_items_to_be_public = false
}

resource "azurerm_storage_container" "function_deployment" {
  count                 = var.deploy_function_app ? 1 : 0
  name                  = "app-package"
  storage_account_id    = azurerm_storage_account.function[0].id
  container_access_type = "private"
}

resource "azurerm_service_plan" "function" {
  count               = var.deploy_function_app ? 1 : 0
  name                = local.function_plan_name
  resource_group_name = local.effective_resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = var.function_app_plan_sku_name
}

resource "azurerm_log_analytics_workspace" "function" {
  count               = var.deploy_function_app ? 1 : 0
  name                = var.function_log_analytics_workspace_name != null ? var.function_log_analytics_workspace_name : format("log-%s", local.generated_function_suffix)
  resource_group_name = local.effective_resource_group_name
  location            = var.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_application_insights" "function" {
  count               = var.deploy_function_app ? 1 : 0
  name                = var.function_application_insights_name != null ? var.function_application_insights_name : local.function_app_name
  resource_group_name = local.effective_resource_group_name
  location            = var.location
  workspace_id        = azurerm_log_analytics_workspace.function[0].id
  application_type    = "web"
}

resource "azurerm_virtual_network" "function" {
  count               = var.deploy_function_app && var.enable_function_vnet_integration ? 1 : 0
  name                = local.function_vnet_name
  resource_group_name = local.effective_resource_group_name
  location            = var.location
  address_space       = [var.function_vnet_address_space]
}

resource "azurerm_subnet" "function_integration" {
  count                = var.deploy_function_app && var.enable_function_vnet_integration ? 1 : 0
  name                 = var.function_integration_subnet_name
  resource_group_name  = local.effective_resource_group_name
  virtual_network_name = azurerm_virtual_network.function[0].name
  address_prefixes     = [var.function_integration_subnet_address_prefix]
  service_endpoints    = ["Microsoft.Sql"]

  delegation {
    name = "function-app-delegation"

    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
}

resource "azurerm_subnet" "function_private_endpoints" {
  count                = var.deploy_function_app && var.enable_function_vnet_integration && var.enable_function_sql_private_endpoint ? 1 : 0
  name                 = var.function_private_endpoint_subnet_name
  resource_group_name  = local.effective_resource_group_name
  virtual_network_name = azurerm_virtual_network.function[0].name
  address_prefixes     = [var.function_private_endpoint_subnet_address_prefix]

  private_endpoint_network_policies = "Disabled"
}

resource "azurerm_mssql_virtual_network_rule" "function" {
  count      = var.deploy_function_app && var.enable_function_vnet_integration ? 1 : 0
  name       = var.function_sql_virtual_network_rule_name
  server_id  = azurerm_mssql_server.this.id
  subnet_id  = azurerm_subnet.function_integration[0].id

  depends_on = [
    azurerm_subnet.function_integration,
  ]
}

resource "azurerm_private_dns_zone" "function_sql" {
  count               = var.deploy_function_app && var.enable_function_vnet_integration && var.enable_function_sql_private_endpoint ? 1 : 0
  name                = var.function_private_dns_zone_name
  resource_group_name = local.effective_resource_group_name
}

resource "azurerm_private_dns_zone_virtual_network_link" "function_sql" {
  count                 = var.deploy_function_app && var.enable_function_vnet_integration && var.enable_function_sql_private_endpoint ? 1 : 0
  name                  = var.function_private_dns_zone_vnet_link_name
  resource_group_name   = local.effective_resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.function_sql[0].name
  virtual_network_id    = azurerm_virtual_network.function[0].id
  registration_enabled  = false
}

resource "azurerm_private_endpoint" "function_sql" {
  count               = var.deploy_function_app && var.enable_function_vnet_integration && var.enable_function_sql_private_endpoint ? 1 : 0
  name                = var.function_sql_private_endpoint_name
  resource_group_name = local.effective_resource_group_name
  location            = var.location
  subnet_id           = azurerm_subnet.function_private_endpoints[0].id

  private_service_connection {
    name                           = "sql-private-link"
    private_connection_resource_id = azurerm_mssql_server.this.id
    is_manual_connection           = false
    subresource_names              = ["sqlServer"]
  }

  private_dns_zone_group {
    name                 = var.function_sql_private_dns_zone_group_name
    private_dns_zone_ids = [azurerm_private_dns_zone.function_sql[0].id]
  }
}

resource "azurerm_function_app_flex_consumption" "this" {
  count                      = var.deploy_function_app ? 1 : 0
  name                       = local.function_app_name
  resource_group_name        = local.effective_resource_group_name
  location                   = var.location
  service_plan_id            = azurerm_service_plan.function[0].id
  storage_container_type      = "blobContainer"
  storage_container_endpoint  = "${azurerm_storage_account.function[0].primary_blob_endpoint}${azurerm_storage_container.function_deployment[0].name}"
  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.function[0].primary_access_key
  runtime_name                = "dotnet-isolated"
  runtime_version             = "8.0"
  maximum_instance_count      = var.function_maximum_instance_count
  instance_memory_in_mb       = var.function_instance_memory_in_mb
  https_only                 = true
  virtual_network_subnet_id   = var.enable_function_vnet_integration ? azurerm_subnet.function_integration[0].id : null

  app_settings = {
    APPLICATIONINSIGHTS_CONNECTION_STRING   = azurerm_application_insights.function[0].connection_string
    APPINSIGHTS_INSTRUMENTATIONKEY          = azurerm_application_insights.function[0].instrumentation_key
    NHC_TIMER_SCHEDULE                      = var.function_timer_schedule
    NHCParser__CurrentYearOnly              = tostring(var.function_current_year_only)
    NHCParser__ProbeDatabaseOnStartup       = tostring(var.function_probe_database_on_startup)
    NHCParser__SqlConnectionString          = local.function_sql_connection_string
    WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED  = "1"
  }

  always_ready {
    name           = "function:${var.function_always_ready_function_name}"
    instance_count = var.function_always_ready_instance_count
  }

  site_config {
    minimum_tls_version = "1.2"
    vnet_route_all_enabled = var.enable_function_vnet_integration ? var.function_vnet_route_all_enabled : false
  }
}

resource "azurerm_static_web_app" "website" {
  count               = var.deploy_static_web_app ? 1 : 0
  name                = local.static_web_app_name
  resource_group_name = local.effective_resource_group_name
  location            = var.static_web_app_location != null ? var.static_web_app_location : var.location
  sku_tier            = var.static_web_app_sku_tier
  sku_size            = var.static_web_app_sku_size
}

resource "azapi_resource" "website_acs_email_service" {
  count                     = var.deploy_website_acs_email ? 1 : 0
  type                      = "Microsoft.Communication/emailServices@2023-03-31"
  name                      = local.website_acs_email_service_name
  parent_id                 = local.effective_resource_group_id
  location                  = var.website_acs_location
  schema_validation_enabled = false

  body = {
    properties = {
      dataLocation = var.website_acs_data_location
    }
  }
}

resource "azapi_resource" "website_acs_domain" {
  count                     = var.deploy_website_acs_email ? 1 : 0
  type                      = "Microsoft.Communication/emailServices/domains@2023-03-31"
  name                      = var.website_acs_domain_name
  parent_id                 = azapi_resource.website_acs_email_service[0].id
  location                  = var.website_acs_location
  schema_validation_enabled = false

  body = {
    properties = {
      domainManagement       = "AzureManaged"
      userEngagementTracking = "Disabled"
    }
  }
}

resource "azapi_resource" "website_acs_sender_username" {
  count                     = var.deploy_website_acs_email ? 1 : 0
  type                      = "Microsoft.Communication/emailServices/domains/senderUsernames@2023-03-31"
  name                      = var.website_acs_sender_username
  parent_id                 = azapi_resource.website_acs_domain[0].id
  schema_validation_enabled = false

  body = {
    properties = {
      displayName = var.website_acs_sender_display_name
      username    = var.website_acs_sender_username
    }
  }
}

resource "azapi_resource" "website_acs_communication_service" {
  count                     = var.deploy_website_acs_email ? 1 : 0
  type                      = "Microsoft.Communication/communicationServices@2023-03-31"
  name                      = local.website_acs_communication_service_name
  parent_id                 = local.effective_resource_group_id
  location                  = var.website_acs_location
  schema_validation_enabled = false

  body = {
    properties = {
      dataLocation  = var.website_acs_data_location
      linkedDomains = [azapi_resource.website_acs_domain[0].id]
    }
  }
}