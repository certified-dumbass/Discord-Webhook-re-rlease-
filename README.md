<p align="center">
  <img src="https://raw.githubusercontent.com/certified-dumbass/Discord-Webhook-re-rlease-/main/banner.png"
       alt="Dreamstreaming Discord Bot"
       width="100%">
</p>

# 🎬 Dreamstreaming Discord Bot

Automatically scan your Jellyfin server for newly added content and post updates directly to Discord using a Discord webhook.

Built for **Jellyfin** and designed to make your media server feel a little more alive. 🍿

---

## ✨ Features

* 🎬 Automatically detects newly added movies
* 📺 Automatically detects newly added series
* 🌸 Support for Anime
* 🎌 Support for Anime Movies
* 📚 Support for Collections
* 🔍 Scans your Jellyfin libraries for new content
* 💬 Posts new additions to Discord
* 🤖 Uses a Discord webhook
* ⚙️ Configurable directly through Jellyfin
* 🗓️ Supports scheduled scans
* ▶️ Manual **Run Scan Now** option
* 🧪 Built-in Discord webhook test
* 💾 Keeps track of previous scans to prevent duplicate notifications
* 🗂️ Configurable Jellyfin library mapping
* 🎨 Customizable Discord update messages
* 📋 Configurable category order
* 🖥️ Runs directly as a Jellyfin plugin

---

## 📦 Add this to your Jellyfin Plugin Repository

Add the following **raw manifest URL** to your Jellyfin plugin repositories:

**Repository URL:**

https://raw.githubusercontent.com/certified-dumbass/Discord-Webhook-re-rlease-/refs/heads/main/manifest.json

### How to add the repository

1. Open your **Jellyfin Dashboard**.
2. Go to **Plugins**.
3. Open **Repositories**.
4. Click **+** to add a new repository.
5. Enter the URL above.
6. Give the repository a name, for example:

   `Dreamstreaming Discord Bot`

7. Click **Save**.
8. Open the **Catalog**.
9. Find **Dreamstreaming Discord Bot**.
10. Install the plugin.
11. **Restart Jellyfin** after installation.

---

## ⚙️ Configuration

After installing the plugin, open the plugin configuration from the Jellyfin Dashboard.

The plugin provides settings for:

| Setting | Description |
| --- | --- |
| 🔑 Jellyfin API Key | API key used to access the Jellyfin server |
| 🌐 Jellyfin URL | URL of the Jellyfin server |
| 💬 Discord Webhook | Discord webhook used to send notifications |
| 🕐 Scan Schedule | Determines when the server is scanned |
| 🗂️ Library Mapping | Choose which Jellyfin libraries belong to each category |
| 🎨 Message Style | Customize how Discord updates look |

Save your configuration after making changes.

---

## 🗂️ Library Mapping

You can now choose which Jellyfin libraries belong to:

* 🎬 Movies
* 📺 Series
* 🌸 Anime
* 🎌 Anime Movies
* 📚 Collections

This means your libraries do not need to have specific names.

For example:

```text
Movies → Movies, 4K Movies
Series → TV Shows
Anime → Anime
Anime Movies → Anime Films
