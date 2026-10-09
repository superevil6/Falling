mars-cliff-wall — a CrunchyTile wall strip

LICENCE: CC0 1.0 (public domain dedication). Use it for anything, commercial included, with no
attribution required. Every pixel was produced by deterministic code: no generative AI, no samples.
https://creativecommons.org/publicdomain/zero/1.0/

WHAT IS HERE
  wall-1.png .. wall-4.png   256 x 256 px chunks of one surface (left wall).
      Line them up vertically in ANY order, any number of times: every chunk continues any other.
      The surface is a wall on the LEFT, open to the right. Mirror the image for a right-hand wall.
  fill-1.png .. fill-4.png   256 px square tiles for what is exposed behind
      a wall. Lay them in ANY grid: every tile's edges match every other tile's.
      When you mirror the wall for the other side, mirror its fill tiles too, or a slanted texture meets
      its own mirror image at the wall's back edge in a V.
  decoration-1.png .. decoration-3.png   things that grow out of the wall.
      DRAW THEM BEHIND THE WALL (a lower z-index than the wall chunks; in Falling, DecorationZIndex -1).
      Each has a root that reaches back past the wall's whole wander range, so it emerges from under
      the surface's lip wherever the edge is. Pin the point (anchorX, anchorY) in the JSON to the surface's
      mean edge (edge.mean from the solid side). Glowing spots are in decorations[].lights - a light on each.
  mars-cliff-wall.json   sizes and where the wall's surface sits (edge.mean is a good collider width).

SCALE
  Drawn at 64 x 64 art pixels and exported at 4x by nearest-neighbour, so one art
  pixel is 4 x 4 image pixels. Use Nearest texture filtering.
