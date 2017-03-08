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
    int getMaxTileIndex();
    Tile getTileAt(Vector3 pos, out int[] index);
    Tile getTileAt(Vector3 pos);
    Tile getTileAt(Vector2 index);
    List<Tile> getTileNeighbors(Vector3 tilePos);
    List<Tile> getTileNeighbors(Vector2 index);
    void updateRegion();
}
