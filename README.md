# NUL_NIHIL

2D Metroidvania. Pixel perfect at 320x180 / 16 PPU

- Player: Dynamic RB but velocity fully script driven (gravity scale 0, frictionless). Boxcast ground snap, coyote time, jump buffer, jump cut. Animator code driven.
- Health: everything damages via PlayerHealth.TakeDamage(amount, sourcePoint) = knockback away from source and upwards, short control lock, i-frames with white flash. Hazard tilemaps damage on trigger stay.
- Enemies: Kinematic RB + trigger BoxCollider2D, contact damage passes own centre as source. Find player in Start for now (*TODO: rooms manage enemies + spawns*).
  - Scout Fly: hovers, chases straight (through walls) inside detect range, red = chasing. Deaggro range larger.
  - Service Crawler: patrols flat surfaces only, raycasts reverse it at wall/ledge/slope. Floor and Wall variant.
- Rooms: One trigger BoxCollider2D per room = membership + camera confinement.
- Camera: Cinemachine + custom CameraBoxConfiner. Transitions freeze player, slide camera, exact handoff.

### Specifics

- New Room: duplicate one, paint tiles, right click Room -> Fit Bounds To Tiles.
- New Enemy: layer 11, kinematic RB + trigger collider, sorting layer Player order -1, hold still while player IsFrozen (*TODO: Replace with more robust system*).
- Layers: 6 Room, 7 Solid, 8 OneWay (unused for now), 9 Hazard, 10 Player, 11 Enemy.
