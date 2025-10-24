# Empty Skulls

**Empty Skulls** is a 2D roguelike dungeon crawler developed in **Unity 6 (URP)**.  
It combines procedural generation, fast-paced combat, and autonomous AI agents capable of exploring, fighting, and learning within a dynamic environment (to be added).

---

## Overview

The game is inspired by titles such as *Realm of the Mad God* and *Enter the Gungeon*.  
Players guide an AI-driven explorer through procedurally generated dungeons, where every run is unique and every death resets progress.

The project serves both as a gameplay prototype and as a research platform for **neuro-symbolic AI** — combining rule-based reasoning with adaptive learning.

---

## Core Features

- **Procedural dungeon generation** with seeded layouts  
- **AI-driven exploration and combat** (Agent learns from player)  
- **Player and enemy stats system** (HP, MP, DEX, SPD, VIT, XP, etc.)  
- **Projectile combat system** supporting multiple patterns and modifiers  
- **Loot and inventory triggers** (interactive world objects such as floppy disks)  
- **Animated tilemaps** and **URP-compatible outline shaders**  
- **Clean UI for health, mana, and experience bars**

---

## Development Roadmap

| Stage | Description |
|--------|-------------|
| **Core Loop MVP** | Basic dungeon generation, enemy encounters, permadeath system |
| **AI Agent v1** | Learns to emulate player for exploration, survival, and combat |
| **Polish Pass** | Hitstop, screenshake, lighting polish, accessibility improvements |
| **Netcode MVP** | Deterministic multiplayer prototype with synced dungeon seeds |
| **Meta Layer** | Persistent unlocks, blueprints, and seed-based replayability |
| **Public Beta** | Tuning, telemetry, difficulty balancing, and community testing |

---

## Technical Stack

- **Engine:** Unity 6 (URP)
- **Language:** C#
- **Architecture:** Modular component-based design with ScriptableObjects
- **AI Framework:** Behavior Trees / GOAP with long-term plan for neuro-symbolic expansion
- **Version Control:** Git and GitHub
- **Rendering:** 2D URP with custom shaders (outline and lighting integration)

---

## Setup Instructions

1. Clone the repository:
   ```bash
   git clone https://github.com/ZachariusSousa/Empty-Skulls.git
