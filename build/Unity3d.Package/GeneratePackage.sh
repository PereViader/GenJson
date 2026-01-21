#!/bin/bash

# Extract version from the .env file
version=$(source .env.shared && echo $VERSION)

# Check if we extracted a version
if [ -z "$version" ]; then
    echo "Version not found in .env.shared"
    exit 1
fi

echo "Remove previous package"
rm -rf GenJson.Unity3d.Package
mkdir -p "GenJson.Unity3d.Package/"

echo Copy Static files
cp -r "build/Unity3d.Package/static/." "GenJson.Unity3d.Package/"
cp README.md "GenJson.Unity3d.Package/README.md"
cp LICENSE.md "GenJson.Unity3d.Package/LICENSE.md"

echo "Update version in package to $version"
sed -i "s/\"version\": \"\$version\"/\"version\": \"$version\"/g" "GenJson.Unity3d.Package/package.json"

echo Copy Main folder
cp -r "src/GenJson/." "GenJson.Unity3d.Package/"

echo Delete unnecesary files from common
rm "GenJson.Unity3d.Package/GenJson.csproj"
rm -rf "GenJson.Unity3d.Package/bin"
rm -rf "GenJson.Unity3d.Package/obj"
rm -rf "GenJson.Unity3d.Package/Properties"

echo Build generator
dotnet build src --configuration Release

echo Copy generator dll to package
cp "src/GenJson.Generator/bin/Release/netstandard2.0/GenJson.Generator.dll" "GenJson.Unity3d.Package/GenJson.Generator.dll"

sh ./build/Unity3d.Package/GenerateUnity3dMetas.sh
cd GenJson.Unity3d.Package
npm pack