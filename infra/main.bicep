// ============================================================
// main.bicep — LebcowBusinessForum Production Infrastructure
// ============================================================
// Profile:        web_plus_data
// Project slug:   lebcowbusinessforum
// Region:         southafricanorth
// Resource group: rg-lebcowbusinessforum-prod (dedicated, created by CI/CD workflow)
//
// All resources are provisioned new in the dedicated resource group:
//   - Log Analytics Workspace       : log-lebcowbusinessforum-prod
//   - Application Insights          : appi-lebcowbusinessforum-prod
//   - App Service Plan (Linux B1)   : asp-lebcowbusinessforum-prod
//   - App Service (Linux .NET 8)    : app-lebcowbusinessforum-web
//   - PostgreSQL Flexible Server    : pg-lebcowbusinessforum-prod
//   - Key Vault (Standard)          : kv-lebcow-prod *
//
// * Azure Key Vault names are limited to 24 characters.
//   'kv-lebcowbusinessforum-prod' (27 chars) exceeds this limit.
//   Using 'kv-lebcow-prod' (14 chars) as an abbreviated name.
//
// PREREQUISITE: Resource group rg-lebcowbusinessforum-prod must exist.
//   The CI/CD workflow creates it automatically via 'az group create'.
//
// NOTE: AZURE_WEBAPP_PUBLISH_PROFILE must be added to GitHub
//       repository secrets before the deploy_app job runs.
//       Obtain from: Azure Portal > App Service > app-lebcowbusinessforum-web > Get publish profile
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

@description('App Service Plan SKU — B1 or higher recommended (Free tier does not support AlwaysOn)')
@allowed(['B1', 'B2', 'B3', 'S1', 'S2', 'P0V3', 'P1V3', 'P2V3'])
param appServiceSku string = 'B1'

@description('Azure region for all new resources')
param location string = resourceGroup().location

// ── Variables ────────────────────────────────────────────────
var projectSlug      = 'lebcowbusinessforum'
var environment      = 'prod'
var appName          = 'app-${projectSlug}-web'
var aspName          = 'asp-${projectSlug}-${environment}'
var appInsightsName  = 'appi-${projectSlug}-${environment}'
var logWorkspaceName = 'log-${projectSlug}-${environment}'
var pgServerName     = 'pg-${projectSlug}-${environment}'
// Azure Key Vault names are capped at 24 characters; 'kv-lebcowbusinessforum-prod' is 27 chars
var keyVaultName     = 'kv-lebcow-${environment}'
var pgDbName         = 'lebcowdb'

// ── Log Analytics Workspace ──────────────────────────────
resource logWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logWorkspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ── Application Insights (workspace-based) ───────────────────
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logWorkspace.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ── App Service Plan (Linux) ───────────────────────────────
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: aspName
  location: location
  kind: 'linux'
  sku: {
    name: appServiceSku
  }
  properties: {
    reserved: true   // required for Linux hosting
  }
}

// ── App Service (Linux .NET 8) ─────────────────────────────
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
    }
  }
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

// Allow Azure services to connect (required for App Service -> PostgreSQL outbound)
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

// ── Key Vault RBAC: grant App Service identity read access to secrets ──────
// Role: Key Vault Secrets User (4633458b-17de-408a-b874-0445c86b69e0)
resource appServiceKvSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, appService.id, '4633458b-17de-408a-b874-0445c86b69e0')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e0')
    principalId: appService.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ── App Service configuration ────────────────────────────────
resource webAppSettings 'Microsoft.Web/sites/config@2023-01-01' = {
  parent: appService
  name: 'appsettings'
  properties: {
    ASPNETCORE_ENVIRONMENT: 'Production'
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString
    ApplicationInsightsAgent_EXTENSION_VERSION: '~3'
    // DefaultConnection is injected as a Key Vault reference via
    // az webapp config connection-string set after Key Vault is provisioned
  }
}

// ── Outputs ──────────────────────────────────────────────────
output webAppName string            = appService.name
output webAppHostname string        = appService.properties.defaultHostName
output pgServerFqdn string          = pgServer.properties.fullyQualifiedDomainName
output keyVaultName string          = keyVault.name
output pgSecretUri string           = kvSecretDbConn.properties.secretUri
output appInsightsConnString string = appInsights.properties.ConnectionString
