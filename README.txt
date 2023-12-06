# AssetLayer Unity SDK

This repository contains a package for integrating AssetLayer inside a Unity App.

## Installation

1. [Download AssetlayerUnitySdk.unitypackage (Left Click then download)](https://github.com/unbounded-enterprise/assetlayer-unity/blob/main/AssetlayerUnitySdk.unitypackage)
2. Import it in Unity:
    - Go to `Assets -> Import Package -> Custom Package`.
    - Select the downloaded `.unitypackage` file.

## Configuration

1. Create an Asset Layer App and copy the .env file you get to your home directory.
    - In Windows, this would be `C:\Users\<username>\.env`.
## Usage

1. Create a slot on Asset Layer.
2. In Unity, create your first Collection by:
    - Selecting the prefab in the `ExamplePrefabs` folder.
    - Right-clicking on it, select `Create Assetlayer Collection`.
    - Select the slot you want and preferred CollectionName, image if you have, then click Create Bundle.
3. Copy the slotId to the inventoryUIUnityUI gameobject in the STARTHERE scene.
4. You can now start the STARTHERE scene and press I for inventory or click the inventory button to see your assets.
5. Select one of your assets, this should load your asset as your character.

## Additional Steps

1. For the Login for your users to work, you need to run a server, you can checkout or run this: https://github.com/unbounded-enterprise/sample-app

