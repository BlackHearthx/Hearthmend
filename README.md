# Hearthmend

Station mend for the homestead.

**By BlackHearthx.**

You already know which bench builds the wall and which forge owns the iron.
Hearthmend lets that same station keep an eye on what it built. Hold Use on a
workbench, forge, stonecutter, artisan table — any craft station — and it starts
watching. When morning comes (or on a timer you set), it mends the wear in range
the way the hammer would: each station only touches pieces that require it, and
pieces with no station requirement can be mended from any watching one. Ships
and carts count too.

You still place. You still hammer when you want. The station just stops letting
the rain win while you sleep.

## What you get

Any crafting station can watch. Hold Use for about half a second to turn watching
on or off — the hover line tells you the state. While it watches, it mends
damaged pieces in a circle around it (default twenty meters): walls, floors,
doors, roofs, ships, carts, anything with wear that belongs to that station.

Interval zero means one mend pass when you wake from sleep. Set a number of
seconds if you want it ticking while you are awake. Wards are respected by
default; other players’ builds can be included if you leave that switch on.

## Your first session

1. Build or find a craft station near the pieces you care about.
2. Look at it and hold Use until Hearthmend flips on.
3. Sleep once with Interval at 0, or set a timer in the config if you prefer.
4. Walk the circle in the morning and check the walls, the dock, the cart.
5. Turn watching off the same way — hold Use again — when you want the station
   quiet.

## Controls at a glance

| Input | What it does |
| --- | --- |
| Hold Use (~0.6s) on a craft station | Toggle Hearthmend watching on that station |

Everything else is in the config file.

## Compat and notes

Needs BepInEx and Jötunn. Each watching station only mends pieces that require
that station (same name rule as the vanilla hammer). Pieces with no station
requirement can mend from any watching station. Ships and carts are included
when they have Piece and WearNTear in range.

No known hard conflicts with other homestead mods; if another mod also
auto-repairs the same pieces, turn one of them off so they do not fight.

## Como usar (PT-BR)

Qualquer bancada de craft pode vigiar. Segure Usar ~0,6s para ligar ou desligar.
Cada estação só conserta o que exigiria ela no martelo; peças sem exigência
podem ser consertadas por qualquer estação vigiando. Inclui barcos e carros no
raio. Intervalo 0 = um conserto ao acordar; maior que 0 = timer. Configura raio,
ward, builds de outros e aviso no HUD.

## Requirements

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)

## Config

Written on first run to `BepInEx/config/com.blackhearthx.hearthmend.cfg`.

| Setting | Default | What it does |
| --- | --- | --- |
| Mod Enabled | on | Master switch for this profile |
| Repair Radius | 20 | How far from the watching station pieces get mended |
| Repair Interval (s) | 0 | 0 = mend on wake; above 0 = seconds between passes |
| Allow Repair Other | on | Also mend pieces built by other players |
| Respect Wards | on | Skip pieces inside wards you may not build in |
| Show Notification | off | Small HUD note when something gets mended |

## Identity

- Mod: Hearthmend
- Author: BlackHearthx
- GUID: `com.blackhearthx.hearthmend`
- GitHub: https://github.com/BlackHearthx/Hearthmend
