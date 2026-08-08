# SEPS Project Design & Requirements Specification

The PDD is being built chapter-by-chapter alongside implementation.

Current architectural decisions:

1. Space Engineers first: SEPS models the actual planet format rather than exposing raw image-editing concepts.
2. WYSIWYG: users edit the planet, not PNG channels.
3. Two workspaces: Globe and Face.
4. Face is the primary MVP workspace.
5. Globe starts as a basic combined-face preview and later becomes the home for global planet settings.
6. Terrain, Resources and Ecosystem are sub-modes of Face.
7. Shared rendering layers are used by both Globe and Face.
8. Water is a shared static visualization layer in MVP, based on the WaterMod modifier; it is not a gameplay constraint.
9. Tide/wave simulation is explicitly outside the MVP.
10. Original files are not modified until the user explicitly saves.
