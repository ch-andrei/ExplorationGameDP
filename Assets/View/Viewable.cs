using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Pathfinding;

namespace Viewable {

    public class View : MonoBehaviour {
        public static float GlobalDrawDistance = 300f;
    }

    public abstract class ViewablePlayer {
        // Observer pattern
        List<PlayerView> playerViews;
        public ViewablePlayer() {
            playerViews = new List<PlayerView>();
        }
        public void setView(PlayerView playerView) {
            this.playerViews.Add(playerView);
        }
        public void notifyView() {
            foreach (PlayerView pv in playerViews)
                pv.notify();
        }
        public void notifyDestroy() {
            foreach (PlayerView pv in playerViews)
                pv.notifyDestroy();
        }
        public void notifyViewMovement(PathResult pr) {
            foreach (PlayerView pv in playerViews)
                pv.notifyMovement(pr);
        }
        private void removeViewWthID(long id) {
            for (int i = 0; i < playerViews.Count; i++) {
                PlayerView p = playerViews[i];
                if (p.id == id) {
                    playerViews.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public abstract class PlayerView : View {

        private ViewablePlayer vp;
        public ViewablePlayer player { get { return this.vp; } }

        public long id { get; set; }

        // Observer pattern
        public void setPlayerView(ViewablePlayer vp) {
            this.id = DateTime.Now.Ticks;
            this.vp = vp;
        }

        public abstract void notify();

        public abstract void notifyDestroy();

        public abstract void notifyMovement(PathResult pr);
    }




}
