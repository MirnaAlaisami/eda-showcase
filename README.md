# Event-Driven Architecture for the win - A successful strategy for a journey to the cloud

# Table of Contents

1. [About](#about)
2. [Abstract](#abstract)
3. [Prereqs](#prereqs)
    * [Azure Configuration](#azure-configuration)
    * [App Configuration](#app-configuration)

## About

Welcome to __EDA-Showcase__ repository!

Here you can find materials from my conference talk about implementing event-driven architecture using Azure cloud services .

* Slides: tbd
* Recording: tbd

For questions or comments, feel free to raise an issue or contact me:

[![Twitter URL](https://img.shields.io/twitter/url?label=%40alaisamiM&style=social&url=https%3A%2F%2Ftwitter.com%2FalaisamiM)](https://twitter.com/alaisamiM)

[![Linkedin](https://i.stack.imgur.com/gVE0j.png) MirnaAlaisami](https://www.linkedin.com/in/mirna-alaisami-030323124/)

## Abstract

*Event-driven architectures are getting increasingly popular and using Azure cloud services to implement them opens up a wide range of choices and considerations.*

*In this talk you will be taken through a step-by-step journey that we have made along with one of our customers to use event-driven architecture in Azure. It starts by the discussions made on moving to EDA and goes through the decision on choosing Azure event hub as an event streaming platform and Azure storage accounts to implement fast upload & download experiences. Moreover, it moves to implement various security aspects in Azure active directory and finishes with a live demo of the various steps.*

## Prereqs

### Azure Configuration

* Have an *Azure Tenant* and a *Subscription* in it
* Rigister an application within your directory (*Azure AD*)
* Add one *Client credential* to your *App registration*
* Add an *App role* to your app registration
* Assign the new role to a user of your Azure AD, who is allowed to upload data later on
* Create a *Resource group* within your *Subscrption*
* Create a *Storage Account* within your *Subscription* and your *Resource Group*
* Add a role assignment for your enterprise application to your storage account
* Create an *Event Hub Namespace* within your *Subscription* and your *Resource group*
* Add Event Hubs (Topics) to your Event Hub Namespace.
* Add a *Schema Group* to your *Schema Registry*
* Add a role assignment for your enterprise application to your event hub namespace
* Add a role assignment for your enterprise application to your schema registry
* Create a *Key Vault* within your *Subscription* and your *Resource Group*
* Add an *Encryption Key* to your key vault

### App Configuration

Set the following environment variables from the Azure configuration in the previous chapter.

* STORAGE_ACCOUNT_NAME
* DOMAIN
* AZURE_CLIENT_ID
* AZURE_TENANT_ID
* AZURE_CLIENT_SECRET
* KEY_NAME
* EVENTHUB_NAMESPACE
* SCHEMA_GROUP

```
```

&copy; 2022 Mirna Alaisami. Free for private purposes. (Re)distribution for commercial purposes not allowed without owner permissions.
