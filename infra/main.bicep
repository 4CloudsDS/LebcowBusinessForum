// ============================================================
// main.bicep — LebcowBusinessForum Production Infrastructure
// ============================================================
// Profile:        web_plus_data
// Project slug:   lebcowbusinessforum
// Region:         southafricanorth
// Resource group: rg-webapps-0001 (existing — shared webapp RG)
//
// Existing resources (referenced, not recreated):
//   - App Service Plan : ASP-rgwebapps0001-86a8  (Free)
//   - Web App          : MarumaneMogoswane
//   - App Insights     : MarumaneMogoswane
//
// New resources (provisioned when absent):
//   - PostgreSQL Flexible Server : db-lebcowbusinessforum-prod
//   - Key Vault                  : kv-lebcow-prod
//
// NOTE: AZURE_WEBAPP_PUBLISH_PROFILE must be added to GitHub
//       repository secrets manually before the deployment job runs.
//       Obtain from: Azure Portal > App Service > MarumaneMogoswane > Get publish profile
// ============================================================

targetScope = 'resourceGroup'

// ── Parameters ──────────────────────────────────────────────
@description('PostgreSQL administrator login name')
param pgAdminLogin string = 'pgadmin'

@description('PostgreSQL administrator password — pass via --parameters or Key Vault reference; never hardcode')
@secure()
param pgAdminPassword string

@description('PostgreSQL SKU tier')
@allowed(['Burstable', 'GeneralPurpose', 'MemoryOptimized'])
param pgSkuTier string = 'Burstable'

@description('PostgreSQL compute SKU name within the tier')
param pgSkuName string = 'Standard_B1ms'

@description('PostgreSQL storage size in MB')
param pgStorageSizeMb int = 32768

@description('Azure region for all new resources')
param location string = resourceGroup().location

// ── Variables ────────────────────────────────────────────────
var projectSlug     = 'lebcowbusinessforum'
var environment     = 'prod'
var pgServerName    = 'db-${projectSlug}-${environment}'
var keyVaultName    = 'kv-lebcow-${environment}'
var pgDbName        = 'lebcowdb'
var existingWebApp  = 'MarumaneMogoswane'
var existingAiName  = 'MarumaneMogoswane'

// ── Existing resources (reference only) ─────────────────────
resource existingWebAppRef 'Microsoft.Web/sites@2023-01-01' existing = {
  name: existingWebApp
}

resource existingAppInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: existingAiName
}

// ── Key Vault ────────────────────────────────────────────────
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

// ── PostgreSQL Flexible Server ───────────────────────────────
resource pgServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: pgServerName
  location: location
  sku: {
    name: pgSkuName
    tier: pgSkuTier
  }
  properties: {
    administratorLogin: pgAdminLogin
    administratorLoginPassword: pgAdminPassword
    version: '15'
    storage: {
      storageSizeGB: pgStorageSizeMb / 1024
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    authConfig: {
      activeDirectoryAuth: 'Disabled'
      passwordAuth: 'Enabled'
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

resource pgDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: pgServer
  name: pgDbName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Allow Azure services to connect (required for App Service outbound)
resource pgFirewallAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = {
  parent: pgServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ── Key Vault Secret: DB connection string ───────────────────
resource kvSecretDbConn 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'DefaultConnection'
  properties: {
    value: 'Host=${pgServer.properties.fullyQualifiedDomainName};Port=5432;Database=${pgDbName};Username=${pgAdminLogin};Password=${pgAdminPassword};Ssl Mode=Require;Trust Server Certificate=true;'
  }
}

// ── App Service configuration ────────────────────────────────
// App settings are merged with existing; only the keys listed here are managed by IaC
resource webAppSettings 'Microsoft.Web/sites/config@2023-01-01' = {
  parent: existingWebAppRef
  name: 'appsettings'
  properties: {
    ASPNETCORE_ENVIRONMENT: 'Production'
    APPLICATIONINSIGHTS_CONNECTION_STRING: existingAppInsights.properties.ConnectionString
    ApplicationInsightsAgent_EXTENSION_VERSION: '~3'
    // Database connection string is injected via Key Vault reference at deploy time;
    // the actual @Microsoft.KeyVault(...) reference is set by the deployment workflow
    // using az webapp config connection-string set after Key Vault is provisioned.
  }
}

// ── Outputs ──────────────────────────────────────────────────
output webAppName string       = existingWebApp
output pgServerFqdn string     = pgServer.properties.fullyQualifiedDomainName
output keyVaultName string     = keyVault.name
output pgSecretUri string      = kvSecretDbConn.properties.secretUri
output appInsightsConnString string = existingAppInsights.properties.ConnectionString
