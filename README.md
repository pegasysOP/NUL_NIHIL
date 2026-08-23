# NUL_NIHIL

2D Metroidvania. Pixel perfect at 320x180 / 16 PPU

- Player: Dynamic RB but velocity fully script driven (gravity scale 0, frictionless). Boxcast ground snap, coyote time, jump buffer, jump cut. Animator code driven.
- Rooms: One trigger BoxCollider2D per room = membership + camera confinement.
- Camera: Cinemachine + custom CameraBoxConfiner. Transitions freeze player, slide camera, exact handoff.

### Specifics

- New Room: duplicate one, paint tiles, right click Room -> Fit Bounds To Tiles.
- Layers: 6 Room, 7 Solid, 8 OneWay (unused for now), 9 Hazard (unused for now), 10 Player, 11 Enemy.
