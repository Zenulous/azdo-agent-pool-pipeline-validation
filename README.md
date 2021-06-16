# Azure Devops Agent Pool Pipeline Validation

This project consists of a simple Azure Function written in C# that will approve or reject a pipeline based on whether it contains a task which is considered malicious. It can be useful to protect a build agent which is not cleaned after each run, such as an on-premise non-containerized build agent.

# Set-up Guide

This tutorial will go over how to test this function locally. Of course it can also be deployed to Azure, but for the purposes of future development the local scenario is described here.

## Starting local development environment

1. Start the Azure Function using VSCode or Visual Studio (see https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-local)
2. Download a tunneling tool like https://ngrok.com/. This is necessary to receive HTTP requests from AZDO.
3. In the case of ngrok, launch a command like `start cmd /k .\ngrok.exe http 7071 -host-header="localhost:7071"` to ensure you tunnel the Azure Functions port.


## Enabling Agent Pool validation

1. Browse to an Azure DevOps project, and in `Project Settings` select `Agent pools`. Click the triple dot menu and click `Approvals and checks`. You will need to either manually repeat this step for each project that needs the validation, or set it up using an automated system that uses the AZDO API

![/guide/1.png](/guide/1.png)

2. Click the plus and add an Azure Function approval

![/guide/2.png](/guide/2.png)

3. Ensure that you replace the following fields:


| Field  |  Value |
|---|---|
| Azure Function URL  |  your ngrok https url appended with the proper endpoint, e.g. https://....eu.ngrok.io/api/JobCheck  |
| Function key  | -  |
|  Method | POST  |
|  Headers | {"Content-Type":"application/json", }  
|  Query parameters |   |
|  Body | {"PlanUrl": "$(system.CollectionUri)", "ProjectId": "$(system.TeamProjectId)", "HubName": "$(system.HostType)", "PlanId": "$(system.PlanId)", "JobId": "$(system.JobId)", "TimelineId": "$(system.TimelineId)", "TaskInstanceId": "$(system.TaskInstanceId)", "AuthToken": "$(system.AccessToken)","BuildId": "$(Build.BuildId)"}  |

It should look like the following:

![/guide/3.png](/guide/3.png)

Note that `time between evaluations` is set to 0. This will immediately fail a pipeline if the function throws an error: it won't be re-evaluated in this case.

## Adding Pipeline

You can add the `legal-pipeline.yml` and `illegal-pipeline.yml` as examples to your project. Make sure you edit the `pool` property to include the agent pool. Note that the `illegal-pipeline.yml` will not run since it contains a CmdLine task. The legal pipeline one runs since the NuGet task is allowed according to the code. You can add more legal tasks to the list by changing the code in `AgentPoolJobCheck/JobCheck.cs`.

# Important Notes

This project serves as a proof of concept and may not be fool-proof. The template returned by the build logs may be interceptable to imperfections if a YAML pipeline is written in an unexpected way. I was unable to create such a scenario during baseline testing, although I would not be surprised if edge cases allow partial bypassing of this kind of YAML validation.