using System.Collections.Generic;
using UnityEngine;

using Tiles;

public interface ViewableRegion {
    List<Tile> getViewableTiles();
    int getMinimumElevation();
    int getMaximumElevation();
    int getAverageElevation();
    int getWaterLevelElevation();
    int computeMaximumElevation();
    int getViewableSize();
    long getViewableSeed();
    Tile getTileAt(Vector3 position, out int[] index);
}
