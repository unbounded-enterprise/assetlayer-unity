# AssetLayer Unity SDK

This repository contains a package for integrating AssetLayer inside a Unity App.

## Installation

1. [Download AssetlayerUnitySdk.unitypackage (Left Click then download)](https://github.com/unbounded-enterprise/assetlayer-unity/blob/main/AssetlayerUnitySdk.unitypackage)
2. Import it in Unity:
    - Go to `Assets -> Import Package -> Custom Package`.
    - Select the downloaded `.unitypackage` file.

## Configuration

1. Create a `.env` file inside of your home directory.
    - In Windows, this would be `C:\Users\<username>\.env`.
2. Open the `.env` file in a text editor and enter two variables: 
    - `APP_SECRET = Your_Assetlayer_Appsecret`
    - `ASSETLAYER_APP_ID = Your_Assetlayer_AppId`

## Usage

1. Create an app and a slot on `AssetLayer.com`.
2. In Unity, create your first Collection by:
    - Selecting the prefab in the `ExamplePrefabs` folder.
    - Right-clicking on it, select `Create Assetlayer Collection`.
    - Enter your slotId and preferred CollectionName, image if you have, then click Create Bundle.
3. After the collection is created, you can go to your assets on `AssetLayer.com` and:
    - Lookup one of the `NftIds` on one of your NFTs that you just created.
    - Copy the `NftId` and open the `ExampleScene` in Unity.
    - Enter the `NftId` in the `AssetBundleImporter` component.
4. You are now ready to start the scene and see your created NFT displayed.

## Additional Steps

1. For the Login for your users to work, you need to run a server, you can checkout or run this: https://github.com/unbounded-enterprise/sample-app

