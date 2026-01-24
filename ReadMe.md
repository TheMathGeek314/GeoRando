# GeoRando

This Randomizer connection adds every individual piece of geo from each of the following locations:
- Geo rocks
- Geo chests
- Colosseum rewards
- Grubfather rewards
Each of the above locations is a togglable option when generating a seed.

Location counts reflect the quantity of geo pieces thrown, not the monetary value (so 420 rock has 84 locations worth 5 geo each).

Rocks and chests that have been fully emptied but contain repeatable checks will be tinted slightly orange.

Since colosseums are normally an infinitely farmable supply of geo, the colo-specific geo items are persistent upon benching. 
This also means that the rewards from completing a colosseum are no longer persistent.


Unfortunately, due to how GeoRando modifies GeoControl objects, FStats is currently incompatible with this connection. 
I hope to fix this if I can.
