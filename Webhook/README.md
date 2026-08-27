# 🎬 Dreamstreaming Discord Bot

Automatically scan your Jellyfin server for newly added movies and series and post updates directly to Discord using a Discord webhook.

Built for **Jellyfin** and designed to make your media server feel a little more alive. 🍿

---

## ✨ Features

* 🎬 Automatically detects newly added movies
* 📺 Automatically detects newly added series
* 🔍 Scans your Jellyfin library for new content
* 💬 Posts new additions to Discord
* 🤖 Uses a Discord webhook
* ⚙️ Configurable directly through Jellyfin
* 🗓️ Supports scheduled scans
* 💾 Keeps track of previous scans to prevent duplicate notifications
* 🖥️ Runs directly as a Jellyfin plugin

---

## 📦 Add this to your Jellyfin Plugin Repository

Add the following **raw manifest URL** to your Jellyfin plugin repositories:

**Repository URL:**

https://raw.githubusercontent.com/certified-dumbass/Discord-Webhook/refs/heads/main/manifest.json

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

| Setting             | Description                                |
| ------------------- | ------------------------------------------ |
| 🔑 Jellyfin API Key | API key used to access the Jellyfin server |
| 🌐 Jellyfin URL     | URL of the Jellyfin server                 |
| 💬 Discord Webhook  | Discord webhook used to send notifications |
| 🕐 Scan Schedule    | Determines when the server is scanned      |

Save your configuration after making changes.

---

## 🔍 How It Works

The plugin periodically scans your Jellyfin server and checks for newly added content.

The detected content is separated into:

### 🎬 Movies

Newly added movies are collected and included in the Discord notification.

### 📺 Series

Newly added series are collected and included in the Discord notification.

The plugin keeps track of previously scanned content to prevent the same items from being announced repeatedly.

---

## 💬 Discord Notifications

When new content is detected, the plugin sends a notification to your configured Discord webhook.

Example:

```text
🎬 New Movies

• Deadpool & Wolverine
• The Batman
• Interstellar

📺 New Series

• Stranger Things
• Fallout
```

This allows your Discord community to automatically see when something new has been added to Jellyfin.

---

## 🔐 Discord Webhook

The plugin uses a Discord webhook to send messages to your Discord server.

To create a webhook:

1. Open your Discord server.
2. Go to **Server Settings**.
3. Open **Integrations**.
4. Select **Webhooks**.
5. Create a new webhook.
6. Select the channel where you want the notifications.
7. Copy the webhook URL.
8. Paste the URL into the plugin configuration.

> ⚠️ **Never publish your Discord webhook URL publicly.**

If your webhook URL is accidentally exposed, delete the webhook and create a new one.

---

## 🔄 Updating

When a new version is released:

1. Publish the new release.
2. Update the plugin manifest.
3. Open Jellyfin.
4. Go to **Dashboard → Plugins**.
5. Check for available updates.
6. Install the new version.
7. Restart Jellyfin.

> 💡 If Jellyfin does not immediately show the new version, restarting Jellyfin can force the plugin system to reload the repository information.

---

## 🛠️ Requirements

* **Jellyfin 10.11.x or newer**
* A Discord server
* A Discord webhook
* Jellyfin API access
* A compatible .NET runtime

---

## 🐛 Troubleshooting

### The plugin does not appear

Try restarting Jellyfin and check:

**Dashboard → Plugins → Installed**

If the plugin still does not appear, verify that the repository URL is correct.

---

### Discord does not receive notifications

Check that:

* The Discord webhook URL is correct.
* The webhook still exists.
* The selected Discord channel is accessible.
* The plugin configuration has been saved.
* The scheduled scan has run.
* Jellyfin has been restarted after installation.

---

### Content is not detected

Make sure the content has actually been added to your Jellyfin library.

The plugin compares the current library state with previous scan data to determine what is new.

---

### Duplicate notifications appear

The plugin uses stored scan information to keep track of previously detected content.

Do not delete or reset the plugin's stored scan data unless you intentionally want the plugin to perform a fresh comparison.

---

## 📋 Version

**Current version:** `1.0.0`

For the latest version and changes, check the project's GitHub releases.

---

## ❤️ Credits

Created by **Certified-dumbass** for the **Dreamstreaming** Jellyfin ecosystem.

The goal of this project is to make Jellyfin communities more interactive by automatically sharing new media additions with Discord.

---

## 📜 License

This project is open source.

See the repository license for more information.

---

## ⭐ Support the Project

If you enjoy the plugin, consider:

* ⭐ Starring the repository
* 🐛 Reporting bugs
* 💡 Suggesting features
* 🔧 Contributing improvements

Enjoy your Jellyfin server! 🍿🎬

**Dreamstreaming — Your media, your community.**

