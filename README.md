# EBB Feed Procurement Service

## Summary of the solution
This service is designed to fetch and process EBB feeds from various sources (currently, ANR).
The service can be extended to include additional pipelines and notification systems. 
It's implemented in C# using .NET 9.0 `BackgroundService` and follows a modular architecture to allow for easy extension and maintenance.
[BackgroundService](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.backgroundservice) provides a clean, reliable, and testable way to implement background processing in .NET applications, reducing boilerplate and improving maintainability.

## How to run the app 
1. Clone the repository (ebb-feed) to your machine.
2. Open the solution in Visual Studio or your preferred IDE.
3. Ensure you have the .NET SDK installed (version 9.0 or later).
4. Restore the NuGet packages by running `dotnet restore` in the terminal or using the IDE's package manager.
5. Configure the application settings in `appsettings.json`:
   - Set the `EbbFeedUrl` to the URL of the EBB feed you want to process.
   - Set the `SlackWebhookUrl` to your Slack webhook URL for notifications (optional).
6. Run the application using `dotnet run` in the terminal or by starting the project in your IDE.
7. The service will start fetching EBB feeds and processing them according to the configured settings.

# Instructions for setting up Slack notifications 
1. Follow the instructions at https://api.slack.com/messaging/webhooks to create a Slack app and obtain a webhook URL.
2. Set the configuration in appsettings.json under `SlackWebhookUrl` to the obtained webhook URL.

# How this service could be extended
The service can be extended to process EBB feeds for other pipelines by implementing `INoticeExtractor` specifically for that EBB feed. 
Configuration file can be modified to include additional settings for new pipelines or notification systems.


# ===========
Copyright © 2025 Ivan Petrouchtchak. All Rights Reserved.

This software and its documentation are protected by copyright law and international treaties. 

Unauthorized reproduction or distribution of this software, or any portion of it, may result in severe civil and criminal penalties, and will be prosecuted to the maximum extent possible under law.
No part of this software may be reproduced, distributed, transmitted, in any form by any means without the prior written permission of the owner