# UiPathOrch

PowerShell Drive and Cmdlets for managing UiPath Orchestrator.

## Overview

UiPathOrch is a PowerShell module that enables you to mount multiple Orchestrator tenants as PSDrives, allowing you to navigate through their folders using familiar commands like `cd`, `dir`, `mkdir`, `rmdir` within the PowerShell console. Beyond simple folder navigation, it facilitates the manipulation of various entities through a range of cmdlets and wildcards.

This module uses the Orchestrator API to mount modern folders as local drives, making them accessible from the PowerShell console with file system-like operations.

**Note:** This module functions even in environments where Studio/Assistant is not installed.

## Key Features

### Bulk Processing
Execute bulk operations across multiple folders and tenants with a single command, eliminating the need for scripting or CSV preparation.

### Advanced Scripting
Perform advanced operations like downloading only active packages or detecting long-pending jobs to trigger email notifications.

### Tenant Migration
Facilitate tenant migrations by copying various entities between tenants efficiently.

### Entity Management
- **Tenant entities:** Manage libraries, packages, assets, queues, triggers, webhooks, and more
- **Folder entities:** Handle processes, jobs, machines, robots, schedules, and other folder-specific items
- **Platform Management:** User, group, license, and authentication management
- **Document Understanding:** Project and queue management
- **Test Manager:** Test set operations

## Installation

### Prerequisites
- PowerShell 7.4.2 or later
- UiPath Orchestrator access with appropriate API credentials

### Install from PowerShell Gallery

```powershell
Install-Module UiPathOrch
Import-Module UiPathOrch
```

For detailed installation steps, refer to the documentation.

## Quick Start

### Basic Navigation

```powershell
# Mount Orchestrator as a drive
cd Orch1:\

# List folders
dir

# Navigate to a shared folder
cd Shared

# List assets in current folder
Get-OrchAsset
```

### Working with Multiple Tenants

```powershell
# Target multiple folders across tenants
Get-OrchAsset -Path Orch1:\Shared,Orch2:\Shared

# Operate on all folder entities recursively
Get-OrchAsset -Recurse
```

### Listing Available Commands

```powershell
# List all cmdlets in UiPathOrch
Get-Command -Module UiPathOrch

# Get help for a specific cmdlet
Get-Help Get-OrchAsset -Full
```

## Documentation

- **Manual:** Comprehensive documentation is available in the module installation directory
- **Marketplace:** [UiPathOrch on UiPath Marketplace](https://marketplace.uipath.com/listings/uipathorch)
- **Cmdlet Help:** Use `Get-Help <cmdlet-name>` to view OAuth scopes and usage details

## Requirements

- PowerShell 7.4.2+
- UiPath Orchestrator API access
- Appropriate OAuth scopes configured for the cmdlets you wish to use

## Features in Detail

### Supported Operations
- Library and package management
- Asset management
- Queue and queue item operations
- Job execution and monitoring
- Process and schedule management
- Machine and robot configuration
- User and group management
- Trigger and webhook configuration
- Test set execution
- Document Understanding workflows

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## Support

**Important**: This is a community-driven tool and is not officially supported by UiPath Technical Support. For issues related to this module, please use the channels below instead of contacting UiPath Technical Support.

For support and questions, you can reach out through any of these channels:

- **UiPath Marketplace Q&A**: [Ask questions on the Marketplace listing](https://marketplace.uipath.com/listings/uipathorch/questions)
- **GitHub Discussions**: [Start a discussion](https://github.com/UiPath-Services/UiPathOrch/discussions) for general questions and ideas
- **GitHub Issues**: [Report bugs or request features](https://github.com/UiPath-Services/UiPathOrch/issues)
- **UiPath Community Forum**: [Get help from the community](https://forum.uipath.com/)

## Publisher

Internal Labs - UiPath

---

**Caution:** This module is under active development. Please test thoroughly in non-production environments before use in production.
