#!/bin/bash

# Set the console encoding to UTF-8
export LANG=en_US.UTF-8

# Find the csproj file
csprojPath=$(find src -name "*.csproj")

# Extract the file name without extension
dllName=$(basename "$csprojPath" .csproj)

# Update version in csproj
echo "Updating version in $csprojPath"

version=$(xmllint --xpath "string(//Project/PropertyGroup/Version)" "$csprojPath")
if [[ $version =~ ([0-9]+)\.([0-9]+)\.([0-9]+)\.([0-9]+) ]]; then
    major=${BASH_REMATCH[1]}
    minor=${BASH_REMATCH[2]}
    build=$(( ${BASH_REMATCH[3]} + 1 ))
    revision=${BASH_REMATCH[4]}
    newVersion="$major.$minor.$build.$revision"
    echo "newVersion $newVersion."

    xmlstarlet ed -u "//Project/PropertyGroup/Version" -v "$newVersion" "$csprojPath" > temp.csproj && mv temp.csproj "$csprojPath"
else
    echo "Version format is incorrect."
    exit 1
fi

# Build the project
echo "Starting build process..."
cd src || exit
dotnet build -c Carbon
if [ $? -ne 0 ]; then
    echo "Build failed."
    exit 1
fi
cd ..

# Move the built file
sourcePath="src/bin/Carbon/netframework4.8.1/$dllName.dll"
destinationPath="build/carbon/extensions/"
if [ -f "$sourcePath" ]; then
    mv "$sourcePath" "$destinationPath"
    echo "Files moved successfully."
else
    echo "Failed to find the file."
    exit 1
fi

# Git operations
git checkout -b main

# Add files to git and commit
git add .
if [ $? -ne 0 ]; then
    echo "Failed to add files to git."
    exit 1
fi

read -p "Enter commit message: " commitMessage
git commit -m "$commitMessage"
if [ $? -ne 0 ]; then
    echo "Failed to commit changes."
    exit 1
fi

git push origin main
if [ $? -ne 0 ]; then
    echo "Failed to push to main branch."
    exit 1
fi

echo "Script execution completed."
