
# Carbonmod Extension Development Template

## Project Overview

This project is a template designed to simplify the development of extensions for Carbonmod. It includes scripts to build and deploy extensions both in a development environment and for production.

## Prerequisites

- .NET Framework 4.8.1
- Git
- PowerShell (for Windows scripts)
- Bash (for Linux scripts)

## File Structure

```
.
├── build_and_deploy_dev.bat
├── build_and_deploy_dev.ps1
├── build_and_deploy_dev.sh
├── build_and_deploy_main.bat
├── build_and_deploy_main.ps1
├── build_and_deploy_main.sh
├── src
│   ├── CarbonExtension.Template.csproj
│   └── ExtensionEntrypoint.cs
└── .gitignore
```

## Usage

### Windows

For development:
```sh
build_and_deploy_dev.bat
```

For production:
```sh
build_and_deploy_main.bat
```

### Linux (Ubuntu/Arch)

For development:
```sh
chmod +x build_and_deploy_dev.sh
./build_and_deploy_dev.sh
```

For production:
```sh
chmod +x build_and_deploy_main.sh
./build_and_deploy_main.sh
```

## Git Ignore

To prevent certain files from being tracked by Git, you should add them to your `.gitignore` file. After cloning the repository, uncomment the lines for the scripts.

```
# Ignore bin and obj directories
src/bin/
src/obj/

# Ignore PowerShell build scripts
# build_and_deploy_main.bat
# build_and_deploy_main.ps1
# build_and_deploy_dev.bat
# build_and_deploy_dev.ps1

# Ignore Linux build scripts
# build_and_deploy_dev.sh
# build_and_deploy_main.sh
```

## Contribution

To contribute to this project, follow these steps:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature-branch`).
3. Make your changes.
4. Commit your changes (`git commit -am 'Add new feature'`).
5. Push to the branch (`git push origin feature-branch`).
6. Create a new Pull Request.
