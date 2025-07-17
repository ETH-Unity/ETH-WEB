# ETH-WEB

A Unity project demonstrating Ethereum blockchain interactions in a client-server multiplayer environment using WebGL clients.

## Core Concepts

This project is a proof-of-concept exploring the integration of browser-based wallets (MetaMask) within a real-time multiplayer environment.

-  **Client-Server Model**: The Unity Editor acts as the authoritative server, managing game state. WebGL builds connect as clients.

-  **Networking**: Uses Unity's Netcode for GameObjects, with the Unity Transport configured for WebSockets to support browser-based clients.
  
-  **Blockchain Integration**: Communication between Unity's C# environment and the browser's JavaScript is handled by a custom `MetaMaskPlugin.jslib` library. This allows for wallet interactions and transaction signing.

## Features

-  **Multiplayer**: Real-time player position and state synchronization.

-  **Identity**: Player identity is linked to their Ethereum wallet address.

-  **Smart Contracts**: Interaction among wallets with smart contracts in-game.

## Project Setup

### Prerequisites

-  **Unity**: `2022.3.x` or later (developed on `Unity 6`)

-  **MetaMask**: Browser extension for wallet interaction.

-  **Ethereum network and smart contracts**: [A private/local network and smart contracts.](https://github.com/ETH-Unity/EthNetwork)

### Prebuilt Releases

Prebuilt server executables and WebGL client builds are available in the Releases section.

The release zip includes:

- The server executable in a folder with a config.json you must edit with your contract addresses, private keys, RPC URL, and chain ID.

- The WebGL build folder, ready to be served with any local web server.

- WebGL Build: Download and serve the WebGL build using any local web server. Clients connect to the server using the configured IP and port.

### Installation in Unity (Alternative)

1. Clone the repository.

2. Open the project in Unity Hub. Unity will import all the required packages listed in the manifest.

### Configuration

-  **Network**: Server IP and port can be configured in the inspector through NetworkManager GameObject. It defaults to `127.0.0.1:7777`.
-  **Smart Contracts**: Contract addresses deployed by the user must be manually set in the Inspector. Open the relevant GameObjects or prefabs (e.g., the player prefab) and assign the contract address to the serialized fields of the scripts that use it.

### How to Run

### 1. Start the Server

- Open the `Assets/Scenes/Web.unity` scene.

- Enter Play Mode in the Unity Editor. The server will start automatically.

### 2. Run the Client

- Go to `File > Build Settings`.

- Select `WebGL` as the platform.

- Click `Build and Run`. Unity will build the project and host it on a local server.

- Alternatively, you can use a pre-built version located in the `/Web` directory and serve it with a local web server.

- Open the local URL in a browser that has MetaMask installed.

- The client will connect to the server running in the Editor.

## License

This project is licensed under the MIT License. See the LICENSE file for details.
