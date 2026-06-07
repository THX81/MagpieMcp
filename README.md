# MagpieMcp

A web search MCP (Model Context Protocol) server built with .NET 10, using SearXNG for web search functionality.

## Overview

MagpieMcp is a Model Context Protocol (MCP) server that provides web search capabilities through a secure, isolated environment. It uses SearXNG as the search backend, offering privacy-focused web search with configurable settings.

## Features

- **MCP Server**: Implements MCP protocol for AI assistants
- **SearXNG Integration**: Privacy-focused web search backend
- **Secure Containerization**: Docker-based deployment with security hardening
- **Resource Isolation**: Network-level isolation between services
- **.NET 10**: Built with the latest .NET framework

## Architecture

```
┌─────────────┐
│   AI Client │
└──────┬──────┘
       │ MCP Request
       ↓
┌─────────────┐
│  MagpieMcp  │
│   (API)     │
└──────┬──────┘
       │ HTTP
       ↓
┌─────────────┐
│  SearXNG    │
│ (Search)    │
└─────────────┘
```

## Prerequisites

- Docker\Podman (for containerized deployment)
- .NET 10 SDK (for local development)

## Installation

### Setup

* Change secret_key in searxng/settings.yml.
* Ports and IP addresses can be changes based on your preferences.

### Using Docker Compose

1. Clone the repository:
```bash
git clone <repository-url>
cd MagpieMcp
```

2. Start the services:
```bash
docker-compose up -d --build
```
or
```bash
podman-compose up -d --build
```

This will start:
- **mcp-api**: The MCP server on port 5066
- **searxng**: The search backend (internal only)

### Local Development

1. Restore dependencies:
```bash
dotnet restore
```

2. Build the project:
```bash
dotnet build
```

3. Run the API:
```bash
dotnet run --project MagpieMcp.Api
```

4. Run searxng from compose (use docker-compose.override.yml to expose on localhost):
```bash
./run_searxng_compose.sh
```

## Configuration

### MCP Client Configuration

Configure your MCP client (e.g., Claude Desktop) to connect to MagpieMcp:

```json
{
  "mcpServers": {
    "duckduckgo": {
      "type": "http",
      "url": "http://localhost:5066"
    }
  }
}
```

### SearXNG Settings

Edit `searxng/settings.yml` to customize search behavior:

```yaml
use_default_settings: true
server:
    secret_key: "your-secret-key"  # Change this for production
search:
  safe_search: 1
  default_lang: "en"
  formats:
    - html
    - json
```

## Security Features

The Docker configuration includes several security measures:

- **Read-only filesystem**: `/app` and `/etc` mounted as read-only
- **Non-root user**: Runs as non-root user
- **Capability dropping**: `cap_drop: ALL`
- **No new privileges**: `security_opt: no-new-privileges:true`
- **Resource limits**: CPU and memory constraints
- **Network isolation**: Internal network for API, separate network for SearXNG egress

## Usage

Once running, you can use the MCP server through your AI assistant:

1. Start the MCP server
2. Query your AI assistant with search requests
3. The assistant will use MagpieMcp to perform web searches

## Development

### Project Structure

```
MagpieMcp/
├── MagpieMcp.Api/           # Main API project
├── MagpieMcp.Api.Tests/     # Test project
├── searxng/                 # SearXNG configuration
├── docker-compose.yml       # Docker services
├── Dockerfile               # Build configuration
└── .mcp.json               # MCP client configuration
```

### Running Tests

```bash
dotnet test --project MagpieMcp.Api.Tests
```

## Running Scripts

Several helper scripts are provided:

- `run_config_compose.sh`: Check configuration
- `run_full_compose.sh`: Start full stack
- `run_searxng_compose.sh`: Start SearXNG only for debugging

