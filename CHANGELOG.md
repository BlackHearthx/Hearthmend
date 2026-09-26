# Changelog

## 1.0.1

Ships and carts actually get mended now. The station was looking for pieces on
the wrong layers and missed anything parked on the vehicle layer, plus some
small pieces. It now sees the same things your hammer does.

You can see the mend now. Pieces fix one after another with the same puff and
sound the hammer makes, and turning a station on does a first pass right away
instead of waiting for morning. The small HUD note is on by default.

Big bases no longer lose pieces off the edge of the scan, and in multiplayer
the mend works even when another player owns the station.

## 1.0.0

First public release. Any crafting station can watch the area: hold Use to toggle,
and it mends pieces that belong to that station, like walls, floors, ships, carts,
and the rest of the wear in range. Morning mend on wake by default, or set a
timer. Radius, wards, other players’ builds, and HUD notes live in the config.
