To run the project, use [Secret Manager](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) and setup following screts:

AzureOpenAI:Endpoint = ...

AzureOpenAI:DeploymentName = ...

AzureOpenAI:ApiKey = ...

- Use dotnet user-secrets list to check secrets

- Use:

    dotnet user-secrets set "AzureOpenAI:DeploymentName" "..."
  
    dotnet user-secrets set "AzureOpenAI:ChatDeploymentName" "..."
  
    dotnet user-secrets set "AzureOpenAI:Endpoint" "https://... .openai.azure.com/"
    
    To setup them
