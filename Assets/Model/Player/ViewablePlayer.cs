using UnityEngine;
using System.Collections.Generic;

using TileAttributes;
using Tiles;

public interface ViewablePlayer {

    // getters
    Vector3 getPos();
    Tile getPosTile();
    Vector2 getPosIndex();

    int getActionPoints();
    int getMaxActionPoints();
    List<Resource> getResources();
    int getSupplies();
    bool getCampStatus(out string message);
    bool getCurrentUpgrades(out string message);
    bool getPossibleUpgrades(out string message);

    // setters
    void setPos(Tile pos);
    void loseActionPoints(int actionPointsToLose);

    // actions
    bool attemptedMove(Vector3 moveToPos, out string message);
    bool changeCampStatus(out string message);
    bool doQuest(out string message);
    bool doUpgrade(out string message);
    bool gatherResource(out string message);

    // updates
    void newTurn();
}
