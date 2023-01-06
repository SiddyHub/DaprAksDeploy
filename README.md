# DaprAksDeploy
Deploy Dapr(ized) eShop App on AKS

Welcome to the Part 4 of the [Dapr Series](https://github.com/SiddyHub/Dapr/tree/eshop_daprized).
  
  This sample demonstrates how to deploy our Dapr(ized) eShop Application to Azure Kubernetes Service.

This version of the code uses **Dapr 1.7**

## Pre-Requisites to Run the Application

- VS Code
- Visual Studio (2019 or 2022)
- .NET Core 3.1 SDK
- Docker installed (e.g. Docker Desktop for Windows)
- [Dapr CLI installed](https://docs.dapr.io/getting-started/install-dapr-cli/)
- Access to an Azure subscription

## Note

The Deployment Scripts are alreay provided with the solution files under Deploy folder

## Set up Kubernetes (AKS) with dapr

To create a Kubernetes cluster in Azure, these are the steps we will be going through:
- Creating an Azure resource group
- Creating an AKS cluster
- Connecting to the AKS cluster
- Installing Dapr on Kubernetes

1. Creating an Azure resource group
In a Windows terminal, log in to Azure by using the Azure CLI. We could also use the portal here, but the CLI helps us keep a consistent experience between Azure and Kubernetes:
`az login`

Connect to the subscription that we want to provision the cluster in. 

`az account set --subscription "<Your Subscription GUID>"`

All the commands that we will be issuing via the Azure CLI will be issued in the context of the specified Azure subscription.

2. Creating an AKS cluster
Now, we can create the AKS cluster. Please check the walk-through available at [this link](https://docs.microsoft.com/en-us/azure/aks/kubernetes-walkthrough) for additional information.

```
az aks create --resource-group daprrgdb --name dapraksdb `
    --node-count 3 --node-vm-size Standard_D2s_v3 `
    --enable-addons monitoring `
    --vm-set-type VirtualMachineScaleSets `
    --generate-ssh-keys
```

After waiting a few minutes for the cluster to be created, we can verify the status of the AKS cluster resource with the following command:
`az aks show --name dapraksdb --resource-group daprrgdb`

Once we have successfully created the AKS cluster, we can connect to it.

3. Connecting to the AKS cluster
Once the cluster has been created, we need to install the Kubernetes tools on our development environment, namely the kubectl CLI, which is facilitated by the Azure CLI, with the following command:
`az aks install-cli`

With the Azure CLI, we can also retrieve the credentials we need to gain administrative access to the cluster. These credentials will be merged into the default location for the Kubernetes configuration file:
`az aks get-credentials --name dapraksdb --resource-group daprrgdb`
> Merged "daprk8saksdb” as current context in C:\Users\user\.kube\config

4. Installing Dapr on Kubernetes

You can check the current Kubernetes context, and make sure to install Dapr on the same Kubernetes cluster we created in Azure.

`kubectl config current-context`

From your development machine on which you have the Dapr CLI installed, you can set up Dapr on the Kubernetes cluster currently configured, as follows:

`dapr init -k`

Alternatively, you can install Dapr on Kubernetes with a Helm v3 chart. You can find more details [here](https://docs.dapr.io/getting-started/install-dapr-kubernetes/#install-with-helm-advanced).

To verify the installation completed successfully, execute this command:

`kubectl get pods --namespace dapr-system`


## Building Docker Images of our Application and Upload it to Azure Container Registry

** You can use ready-to-use docker images used in the Kubernetes yaml files in this solution (Under Deploy -> k8s) to expedite your development, and skip this section. **

For more information on how to publish ASP.NET projects with the Docker container format, I suggest that you read the documentation [here](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-3.1)

For a walkthrough on how to create an ACR instance, please take a look at the documentation provided [here](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal)

The Azure extension in VS Code offers an integrated developer experience by easing authentication and access to our Azure resources: we can push the Docker images we just built to the ACR instance with just a click. Check [here](https://code.visualstudio.com/docs/containers/quickstart-container-registries), and find a step-by-step guide on how to do this.

*Note - If using Azure Container Registry, we need to connect them to AKS. From a terminal window, we can launch the following command with the Azure CLI:
`az aks update --name dapraksdb --resource-group daprrgdb --attach-acr dapracrdb`

## Migrate database to Azure SQL

We need to migrate our Local DB SQL Server to Azure, and use Azure SQL to work with our application deployed to AKS Cluster.

We have to create database for our API services with following steps:

1. Create SQL Server in Azure using Azure Portal

2. Create database for each service.

   For example to create an Order SQL database execute Azure CLI command by runing following command

   `az sql db create -g daprrgdb -s globoticket -n GloboTicketOrderDb --service-objective Basic`

   Similarly, create database for ShoppingDB and DiscountDB using above command

3. Run `sqlcmd` with the Migration Scripts (created using `dotnet ef migration script` and already added in Solution files for services)

   So for order service in command prompt, `cd` inside `GloboTicket.Services.Ordering` directory, run sqlcmd with values from Step 2 and with `orderDb-create.sql` created and added for this service.

   `sqlcmd -S tcp:globoticket.database.windows.net,1433 -d GloboTicketOrderDb -P **** -U globoticket-user -i OrderDb-create.sql`

*Follow above steps for ShoppingDB and DiscountDB
     
## Create Secrets

We need to create Secrets for our Database Connection Strings to be used in kubernetes deployment files and Dapr Components.

We use Azure Redis Cache as Dapr State Store and Azure Service bus as Dapr PubSub component, and the key or connection strings of each components would be passed as a secret.

Also for our Catalog categories we are calling from CosmosDB, so we need to create secrets for CosmosDB as well.

For your convenience, the **`Deploy\deploy-secrets.ps1`** file contains all the steps to delete and create secrets needed for our solution and Dapr components.

*Note - Just edit the secrets file with your own Azure config values and run the script.

## Application Deployments

We need to deploy our `External.PaymentGateway` API under `Fake` folder to Azure. For this I have used `API App`.
The `API App` can be found under `Marketplace` if you search for `api` in Azure.
(When creating the `API App` we can publish it as `Docker Container` using "siddocker29/external.paymentgateway:latest" image).

Since we would be using Zipkin and Email services in our solution, we need to create deployment and expose deployment to be used in our solution.

Inside **`Deploy\deploy-solution.ps1`** the necessary scripts are present, to deploy Zipkin, Email (Maildev) and all services.

To test Zipkin deployment, execute following commad:
`kubectl port-forward svc/zipkin 9412:9411` and navigate to http://localhost:9412

To test Maildev deployment, execute following command:
`kubectl port-forward svc/maildev 8081:1080` and navigate to http://localhost:8081

## Ingress Deployment

We are going to create a Static Public IP for Ingress in Azure AKS and associate that Public IP to Ingress Controller during installation.

Following are the steps to create Static Public IP

```
# Get the resource group name of the AKS cluster 
az aks show --resource-group aks-rg1 --name aksdemo1 --query nodeResourceGroup -o tsv

# TEMPLATE - Create a public IP address with the static allocation
az network public-ip create --resource-group <REPLACE-OUTPUT-RG-FROM-PREVIOUS-COMMAND> --name myAKSPublicIPForIngress --sku Standard --allocation-method static --query publicIp.ipAddress -o tsv
```

Make a note of Static IP which we will use in next step when installing Ingress Controller.

We also need a secret with a certificate. Use the following commands:

```
openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout tls.key -out tls.crt -subj "/CN=app.YOUR-IP.nip.io"
kubectl create secret tls tls-secret --key tls.key --cert tls.crt

(Replace "YOUR-IP" with the Static IP address of the Ingress Controller)
```

We need to configure our Kubernetes cluster with an ingress controller (IC). For this, we can use NGINX. A detailed step-by-step configuration is available in the Azure documentation [here](https://docs.microsoft.com/en-us/azure/aks/ingress-basic)

Inside `Deploy\deploy-nginx-dapr.ps1` file in the solution, contains all the steps, charted with Helm, for preparing the Nginx IC and the ingresses according to our sample solution. The Static IP Address noted previously needed to be used in this script.

We are using NGINX IC in combination with Dapr configuration, which is available in the `Deploy\nginx-dapr-annotations.yaml` file, which makes it a proper Dapr application, with its local Dapr sidecar, and therefore with access to any other application, without the need for us to configure Kubernetes services for our APIs.

With this configuration we can influence the deployment of the NGINX IC with Dapr-specific annotations, and once deployed, the Pods of the NGINX IC will be injected with the Dapr sidecar container.

## Access our Application

After all Dapr Components, Kubernetes files and NGINX Ingress Controller is deployed we can access our application using our Static IP Address. 
It’s best to use an incognito session or private window.

`https://<YOUR-STATIC-IP-ADDRESS>`

Your browser will warn you of security risks because the certificate is not trusted. Proceed anyway!

## Dapr Dashboard

With `dapr dashboard -k` command, we can see that all our Dapr applications and components have now been configured in Kubernetes.

## Troubleshooting

- Make Sure all the secrets have been configured for the Azure services like Cosmos DB, Azure Redis Cache, Azure Service bus and SQL Connection Strings and inputted in `~\Deploy\deploy-secrets.ps1`
- If getting issues with Azure Redis Cache statestore, make sure Non-SSL port is enabled
- Make sure External Payment gateway under `Fake` folder is deployed and it's URL configured in `payment.yaml` file under `~\Deploy\k8s` folder.