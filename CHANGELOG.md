# Changelog

## 1.0.3

Hearthmend only watches stations the hammer needs for building: workbench,
forge, stonecutter, artisan table, black forge, galdr table, and any other
station a build piece actually requires. Cauldrons, mead kettles, and food
prep tables no longer show the toggle.

## 1.0.2

Stations keep working now. Before, a station mended once when you turned it on
and then sat quiet until you slept, which looked like it had stopped. Now it
checks again every thirty seconds while you are nearby, still mends when you
wake up, and a station you left watching gets to work as soon as you come home.

The old Repair Interval setting is replaced by Mend Every (seconds) and Mend On
Wake. With several stations watching the same spot, the morning note now counts
each piece once, and stations no longer all fire on the same frame.

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
