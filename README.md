<p align="center">
  <img src="https://raw.githubusercontent.com/certified-dumbass/Discord-Webhook-re-rlease-/main/Banner.png" alt="Dreamstreaming Discord Bot" width="100%">
</p>

<h1 align="center">🎬 Dreamstreaming Discord Bot</h1>

<p align="center">
  A customizable Jellyfin plugin that scans your libraries for newly added content and sends clean update messages directly to Discord.
</p>

<p align="center">
  <strong>Jellyfin 10.11.x</strong> • <strong>.NET 9</strong> • <strong>Discord Webhooks</strong> • <strong>v1.2.4</strong>
</p>

---

## 🌙 About

Dreamstreaming Discord Bot was created because I wanted more control over when Jellyfin library updates are posted to Discord.

Instead of being limited to a fixed notification setup, the plugin lets you choose the scan schedule, select the Jellyfin libraries you actually want to monitor, customize how those libraries appear in Discord, and change the branding of the notification itself.

The plugin runs directly inside Jellyfin and uses a Discord webhook to send its updates.

---

## ✨ Features

- 🎬 Detects newly added movies
- 📺 Detects newly added series
- 📂 Dynamically loads your actual Jellyfin libraries
- 🎞️ Supports seasons and episodes
- 📦 Supports collections / box sets
- 🔘 Enable or disable individual libraries
- ✏️ Give libraries a custom Discord display name
- 😀 Give individual libraries their own emoji
- 📝 Optionally show episode names
- 🎨 Multiple Discord message styles
- 🏷️ Custom notification branding and update titles
- 🕐 Configurable scan schedule
- ▶️ Manual **Run Scan Now** option
- 🧪 Built-in Discord webhook test
- 💾 Remembers previous scans to prevent duplicate notifications
- 📣 Can notify `@everyone` when a real content update is posted
- ⚙️ Configurable directly from the Jellyfin Dashboard

---

## 📥 Installation

The easiest way to install the plugin is through Jellyfin's plugin repository system.

### 1. Add the repository

Open:

**Jellyfin Dashboard → Plugins → Repositories**

Click **+** and add the following repository URL:

```text
https://raw.githubusercontent.com/certified-dumbass/Discord-Webhook-re-rlease-/main/manifest.json
```

You can give the repository a name such as:

```text
Dreamstreaming Discord Bot
```

### 2. Install the plugin

After saving the repository:

1. Open **Plugins → Catalog**.
2. Find **Discord Bot**.
3. Install the latest version.
4. Restart Jellyfin.
5. Return to the Jellyfin Dashboard and open the plugin settings.

That's it — no separate bot application needs to run alongside Jellyfin. 🎉

---

## 🔗 Repository Manifest

For quick copying, this is the raw `manifest.json` used by Jellyfin:

```text
https://raw.githubusercontent.com/certified-dumbass/Discord-Webhook-re-rlease-/main/manifest.json
```

The manifest points Jellyfin to the current plugin release and allows new versions to appear in the Jellyfin plugin catalog when they are published.

---

## ⚙️ Configuration

After installation, configure the plugin from your Jellyfin Dashboard.

You will need your Jellyfin server information and a Discord webhook. **Never publish or share your Jellyfin API key or private Discord webhook URL.**

The plugin allows you to configure your scan schedule, Discord notification style, notification branding, and the libraries that should be included in updates.

### Dynamic Jellyfin libraries

The plugin loads the libraries that actually exist on your Jellyfin server. You are not required to name your libraries `Movies`, `Series`, `Anime`, or anything else specific.

For each supported library you can choose whether it is enabled and optionally configure a custom Discord name and emoji.

For TV libraries, season and episode information can also be included in the update.

Adding a new Jellyfin library does not require rebuilding the plugin. Open the plugin settings, load the Jellyfin libraries again, enable the new library, configure it if desired, and save.

---

## 💬 Discord Updates

A series update can look roughly like this:

```text
📺 Series

Fallout (2024)
└─ Season 2
   ├─ S02E01 — The Head
   ├─ S02E02 — The Golden Rule
   └─ S02E03 — The Handoff
```

The exact output depends on your enabled libraries and message settings.

Notification branding is also customizable. For example, the update title can be configured as a **Library Update**, **Server Update**, **Website Update**, or a completely custom title.

---

## 🧪 First Scan

The plugin keeps track of its previous scan so it knows which content is new.

On a fresh installation, allow the plugin to establish its initial scan state before relying on automatic update notifications. After that, newly detected content can be included in future Discord updates according to your configured schedule.

You can also use the built-in test option to verify that your Discord webhook is configured correctly.

---

## 🧩 Compatibility

Current release: **v1.2.4**

The current version is built for **Jellyfin 10.11.x** and has been tested with **Jellyfin 10.11.8**. The project targets **.NET 9**.

Compatibility with older or future Jellyfin versions is not guaranteed until tested.

---

## 🔄 Updating

If you installed the plugin using the repository URL, new releases can be distributed through the same Jellyfin plugin repository.

When an update becomes available, install it through Jellyfin and restart the server when required.

---

## 🐛 Bugs & Feedback

This project is still evolving, so feedback and bug reports are welcome.

If something behaves unexpectedly, please open an issue in this GitHub repository and include as much useful information as possible, such as:

- Jellyfin version
- Plugin version
- What you expected to happen
- What actually happened
- Relevant Jellyfin log output with API keys, webhook URLs, tokens, and other private information removed

---

## 🤖 AI Disclaimer

Some of the artwork and visual assets used in this repository were created with the assistance of generative AI.

The **Dreamstreaming Discord Bot** was also developed with the assistance of AI for programming, debugging, code review, and development guidance. The project itself has been manually built, tested, configured, released, and maintained by the repository owner.

**AI-assisted does not mean AI-maintained.** Issues, releases, testing, configuration decisions, and the direction of the project are handled by the project owner.

---

## ❤️ Credits

Built for the Jellyfin community by **Certified-dumbass**.

This is an independent community project and is not an official Jellyfin or Discord plugin.

Thanks to everyone who tests the plugin, reports bugs, suggests improvements, or simply gives it a try. 💜

<p align="center">
  <strong>🌙 Enjoy watching!</strong>
</p>
