# WebM to MP4 Telegram Bot

Telegram Bot to convert WebM videos to MP4. 

## Getting Started
### Prerequisites

- [.NET Core 3.1](https://dotnet.microsoft.com/download) or higher

**Telegram:**
1. Contact to [@BotFather](https://t.me/BotFather) in Telegram
2. Create new bot
3. Copy bot token

### Local launch:

To launch project locally you will need to install additional stuff:
- [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools) to run Azure Functions locally
- [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator) or [Azurite](https://github.com/Azure/Azurite) to emulate Azure Blob Storage

**Project:**

1. Launch [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator) or [Azurite](https://github.com/Azure/Azurite)
2. Update **WebMToMP4TelegramBot/local.settings.json** file 
3. [Run project locally](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local) via [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools)

### Azure:

To launch project on Azure you will need:
- [Azure Functions](https://azure.microsoft.com/en-us/services/functions/)
- [Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/)

1. Update Azure Function App Configuration 
2. [Publish](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#publish) project to Azure

## Usage

You can try this bot in [Telegram](https://t.me/WebMToMP4Bot)

## Built With

* [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) - .NET Client for Telegram Bot API
* [FFmpeg.NET](https://github.com/cmxl/FFmpeg.NET) - .NET wrapper for common ffmpeg tasks
* [Azure Functions](https://azure.microsoft.com/en-us/services/functions/) - More than just event-driven serverless compute

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.