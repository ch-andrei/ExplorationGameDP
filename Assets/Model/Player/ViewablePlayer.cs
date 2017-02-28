using UnityEngine;
using System.Collections.Generic;

using TileAttributes;

public interface ViewablePlayer {

    // getters
    Vector3 getPos();
    int getActionPoints();
    List<Resource> getResources();
    int getSupplies();
    bool getCampStatus(out string message);
    bool getCurrentUpgrades(out string message);
    bool getPossibleUpgrades(out string message);

    // actions
    bool attemptMove(Vector3 moveToPos, out string message);
    bool changeCampStatus(out string message);
    bool doQuest(out string message);
    bool doUpgrade(out string message);
    bool gatherResource(out string message);

    // updates
    void newTurn();
}
