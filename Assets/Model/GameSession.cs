using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MapGeneration;

public class GameSession  {

    // private variables
    public Player player { get; }
    public MapGenerator mapGenerator { get; }

    public GameSession(MapGeneratorInput mgi) {
        player = new Player();
        mapGenerator = new MapGenerator(mgi);

        // player.setPos();
    }

    public void newTurn() {
        player.newTurn();
    }
}

