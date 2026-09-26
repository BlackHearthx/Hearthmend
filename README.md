# Hearthmend

Station mend for the homestead.

**By BlackHearthx.**

You already know which bench builds the wall and which forge owns the iron.
Hearthmend lets that same station keep an eye on what it built. Hold Use on a
workbench, forge, stonecutter, artisan table, or any other craft station, and it
starts watching. While you are around, it keeps mending the wear in range the
way the hammer would: each station only touches pieces that require it, and
pieces with no station requirement can be mended from any watching one. Ships
and carts count too.

You still place. You still hammer when you want. The station just stops letting
the rain win while you sleep.

## What you get

Any crafting station can watch. Hold Use for about half a second to turn watching
on or off, and the hover line tells you the state. While it watches, it mends
damaged pieces in a circle around it (default twenty meters): walls, floors,
doors, roofs, ships, carts, anything with wear that belongs to that station.
You see it happen, one piece after another, with the same little puff the
hammer makes.

The moment you turn a station on, it does a first pass right away. After that
it checks again every thirty seconds while you are nearby, and once more every
time you wake up. Come home to a station you left watching and it gets to work
as soon as you are close. Wards are respected by default; other players’ builds
can be included if you leave that switch on.

## Your first session

1. Build or find a craft station near the pieces you care about.
2. Look at it and hold Use until Hearthmend flips on. Damaged pieces nearby
   start mending right there.
3. Take a swing at a wall or bump the cart around. Within half a minute the
   station patches it up again.
4. Sleep, wake up, and walk the circle: the walls, the dock, the cart.
5. Hold Use again whenever you want the station to go quiet.

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
raio. Assim que você liga, ela já faz uma primeira passada, peça por peça, com o
mesmo efeito do martelo. Depois continua remendando a cada 30 segundos enquanto
você está por perto, e de novo toda vez que você acorda. Voltou pra base? A
estação começa a trabalhar assim que você chega. Na config dá pra mexer no
tempo, raio, ward, builds de outros e aviso no HUD.

## Requirements

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)

## Config

Written on first run to `BepInEx/config/com.blackhearthx.hearthmend.cfg`.

| Setting | Default | What it does |
| --- | --- | --- |
| Mod Enabled | on | Master switch for this profile |
| Repair Radius | 20 | How far from the watching station pieces get mended |
| Mend Every (s) | 30 | How often a watching station mends while you are near; 0 turns it off |
| Mend On Wake | on | Stations near you also mend when you wake up |
| Allow Repair Other | on | Also mend pieces built by other players |
| Respect Wards | on | Skip pieces inside wards you may not build in |
| Show Notification | on | Small HUD note when something gets mended |

## Identity

- Mod: Hearthmend
- Author: BlackHearthx
- GUID: `com.blackhearthx.hearthmend`
- GitHub: https://github.com/BlackHearthx/Hearthmend
