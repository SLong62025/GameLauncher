# GameLauncher
A Game Launcher that collects titles from Steam and Epic Games into one launcher client.

## How it Works
1. App collects Steam AppIDs and Epic URI's from locally-stored files.
2. Parse each title into its own game object.
3. List all titles into a grid.
4. Launch each title by interfacing with the client it originates from.

## Eisenhower Matrix
| Eisenhower Matrix | **Urgent**                                                                       |                           **Not Urgent**                           |
|-------------------|----------------------------------------------------------------------------------|:------------------------------------------------------------------:|
|   **Important**   | MVP: - Launch title - Display Steam Titles - Display EGS Titles                  | Version 2.0: - Display uninstalled titles - Track playtime         |
| **Not Important** | Delegate:  - Display Grid - Display local installations - Fetch artwork from web | Cut:  - Animated Art - Integrate storefronts - Achivement Tracking |

## Tech Stack
* C#
* .NET (WPF)
* Legendary
* Steam Web API
