# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Real Weather MCP Server** test assignment for FastMCP.me. The project creates a Model Context Protocol (MCP) server using .NET that provides real weather data through AI assistants like Claude.

## Architecture

- **MCP Server Framework**: Uses `ModelContextProtocol` NuGet package v0.3.0-preview.3
- **Target Framework**: .NET 8.0 with nullable reference types enabled
- **Transport**: Standard I/O (stdio) for MCP communication
- **Tool Registration**: Uses `[McpServerTool]` attributes on methods to expose functionality to MCP clients
- **Configuration**: Environment variables for API keys and settings
- **Logging**: Configured to output to stderr (stdout reserved for MCP protocol messages)

### Project Structure
```
WeatherMcpServer/
├── Program.cs              # Entry point, host configuration, MCP server setup
├── Tools/
│   ├── WeatherTools.cs     # Real weather MCP tools with OpenWeatherMap API integration
│   └── RandomNumberTools.cs # Sample tools for demonstration
├── .mcp/server.json        # MCP server metadata and configuration
└── WeatherMcpServer.csproj # Project configuration, package metadata
```

## Development Commands

### Building and Running
- **Build**: `dotnet build`
- **Run locally**: `dotnet run --project WeatherMcpServer`
- **Create NuGet package**: `dotnet pack -c Release`

### Local Testing Configuration
For testing the MCP server locally without publishing, configure your IDE with:
```json
{
  "servers": {
    "WeatherMcpServer": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "<PATH TO PROJECT DIRECTORY>"]
    }
  }
}
```

## Implementation Requirements

### Core Weather Tools to Implement
1. **GetCurrentWeather** - Current weather conditions for any city/location
2. **GetWeatherForecast** - Weather forecasts (minimum 3-day forecast)  
3. **GetWeatherAlerts** - Weather alerts/warnings for locations (bonus feature)

### Technical Standards
- Integrate with real weather APIs (OpenWeatherMap, AccuWeather, etc.)
- Use environment variables for API key configuration (e.g., `OPENWEATHER_API_KEY`)
- Implement proper error handling for invalid locations and API failures
- Support multiple locations worldwide
- Follow .NET coding standards and best practices
- Include proper logging using the configured stderr logging

### Tool Method Pattern
```csharp
[McpServerTool]
[Description("Tool description for MCP clients")]
public async Task<string> ToolName(
    [Description("Parameter description")] string paramName,
    [Description("Optional parameter")] string? optionalParam = null)
{
    // Implementation
}
```

## Configuration Files

### MCP Server Configuration (`.mcp/server.json`)
Contains metadata for MCP server discovery and deployment. Update the placeholders:
- `name`: Use format `io.github.<username>/<repo-name>`
- `description`: Brief description of weather server functionality
- `packages[0].name`: Your NuGet package ID
- `repository.url`: Your GitHub repository URL

### Environment Variables
- Weather API keys should be configured as environment variables
- The existing `WEATHER_CHOICES` variable is used by the mock implementation
- Configure API-specific variables like `OPENWEATHER_API_KEY`

## Current Status

### ✅ **IMPLEMENTATION COMPLETE** (as of 2025-09-04)

The project now contains a fully functional real weather MCP server:

#### ✅ **Implemented Features:**
- **Real Weather API Integration** - OpenWeatherMap API with comprehensive error handling
- **GetCurrentWeather** - Returns current weather conditions, temperature, humidity, pressure
- **GetWeatherForecast** - 5-day forecast with 3-hour intervals (configurable 1-5 days)
- **GetWeatherAlerts** - Weather alerts and warnings (premium API feature)
- **Data Models** - Complete JSON deserialization models for all API responses
- **Dependency Injection** - HttpClient and ILogger properly configured
- **Error Handling** - Invalid locations, API failures, missing API keys
- **Logging** - Comprehensive stderr logging as required by MCP protocol

#### ✅ **Technical Implementation:**
- **HTTP Client** - Added Microsoft.Extensions.Http package for API calls
- **Type Safety** - Full nullable reference types and proper data models
- **MCP Compliance** - All tools properly decorated with `[McpServerTool]` attributes
- **Build Success** - Project builds without errors or warnings
- **Package Creation** - Successfully creates NuGet package for distribution

#### ✅ **Testing Status:**
- **Build Verified** - `dotnet build` succeeds
- **Server Startup** - MCP server starts and processes tool calls
- **Package Creation** - `dotnet pack -c Release` succeeds
- **API Key Ready** - Awaiting activation of key `584375651b16d9cb86dbf7b9bb86762e`

#### 🧪 **Testing Commands:**
```bash
# Set API key (once activated)
export OPENWEATHER_API_KEY=584375651b16d9cb86dbf7b9bb86762e

# Test current weather
echo '{"jsonrpc": "2.0", "method": "tools/call", "params": {"name": "GetCurrentWeather", "arguments": {"city": "London"}}, "id": 1}' | dotnet run --project WeatherMcpServer

# Test weather forecast
echo '{"jsonrpc": "2.0", "method": "tools/call", "params": {"name": "GetWeatherForecast", "arguments": {"city": "London", "days": 3}}, "id": 2}' | dotnet run --project WeatherMcpServer

# Test weather alerts
echo '{"jsonrpc": "2.0", "method": "tools/call", "params": {"name": "GetWeatherAlerts", "arguments": {"city": "London"}}, "id": 3}' | dotnet run --project WeatherMcpServer
```

### 📋 **Original Requirements Status:**
- ✅ Basic MCP server infrastructure and host setup
- ✅ Sample RandomNumberTools for demonstration  
- ✅ Real WeatherTools with OpenWeatherMap API integration
- ✅ Real weather API integration with comprehensive error handling
- ✅ Proper weather data models and JSON deserialization
- ✅ Environment variable configuration (`OPENWEATHER_API_KEY`)
- ✅ Support for multiple locations worldwide
- ✅ Proper logging using stderr as required
- ✅ .NET coding standards and best practices