using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Viewable;
using Playable;
using Pathfinding;
using Tiles;

public class EntityView : MonoBehaviour {

    public class PlayerViewObjects : PlayerView {

        private static Vector3 playerHeightOffset = new Vector3(0, 5, 0);
        private static Vector3 playerStrengthIndicatorHeightOffset = new Vector3(0, 25, 0);

        // states for redrawing the views
        public enum ViewStates { playerModel, encampmentModel };

        // states for animating player movement
        public enum MoveStates { idle, moving };

        // private variables

        private int playerViewState, animatorMoveState;

        private bool dirty { get; set; }
        private bool withinDrawRange { get; set; }
        private bool drawStrength { get; set; }

        GameObject strengthIndicator = null;

        void Awake() {
            playerViewState = (int)ViewStates.playerModel;
            animatorMoveState = (int)MoveStates.idle;
            this.dirty = false;
            this.withinDrawRange = false;
            this.drawStrength = true;
        }

        void Update() {
            // update player view state
            if ((player as Player).getCampStatus() && playerViewState != (int)ViewStates.encampmentModel) {
                playerViewState = (int)ViewStates.encampmentModel;
                dirty = true;
            } else if (!(player as Player).getCampStatus() && playerViewState != (int)ViewStates.playerModel) {
                playerViewState = (int)ViewStates.playerModel;
                dirty = true;
            }
            if (withinDrawRange != (withinDrawRange = playerWithinDrawRange())) {
                dirty = true;
            }
            if (dirty) {
                outOfRangeDestroyGameObjects();
                if (playerWithinDrawRange()) {
                    redrawPlayerView();
                    dirty = false;
                }
            }
            redrawPlayerStrengthView();
        }

        public void initPlayerView(ViewablePlayer player) {
            transform.position = (player as Player).getPos();
            this.setPlayerView(player);
        }

        override
        public void notify() {
            this.dirty = true;
        }

        override
        public void notifyDestroy() {
            StartCoroutine(destroyPlayerView());
        }

        override
        public void notifyMovement(PathResult pr) {
            // if within draw range, animate move
            if ( playerWithinDrawRange() ) {
                StartCoroutine(animatePlayerMove(pr));
            } else { // else just set position
                Vector3 destination = pr.getTilesOnPath()[0].getPos();
                transform.position = destination;
            }
        }

        private IEnumerator destroyPlayerView() {
            while (animatorMoveState != (int)MoveStates.idle) {
                yield return null;
            }

            yield return playDeathAnimation();

            // destroy this object completely
            Destroy(transform.gameObject);
        }

        private IEnumerator playDeathAnimation() {
            while (animatorMoveState != (int)MoveStates.idle) {
                yield return null;
            }
            yield return animateRotation(90);
        }

        private bool playerWithinDrawRange() {
            Vector3 playerPos = (this.player as Player).getPos();
            playerPos.y = 0;
            return (CameraControl.viewCenterPoint - playerPos).magnitude < GlobalDrawDistance;
        }

        private void outOfRangeDestroyGameObjects() {
            foreach (Transform t in transform) {
                Destroy(t.gameObject);
            }
        }

        private void redrawPlayerStrengthView() {
            if (strengthIndicator != null)
                Destroy(strengthIndicator);

            strengthIndicator = Instantiate(Resources.Load("Prefabs/Text/TileCostObject"), this.transform) as GameObject;

            // set position
            Vector3 pos = (player as Player).getPos();
            strengthIndicator.transform.position = pos + playerStrengthIndicatorHeightOffset;

            // set text
            string playerStrength = (player as Player).computeStrength() + "S";
            strengthIndicator.transform.GetChild(0).GetComponent<TextMesh>().text = playerStrength;

            strengthIndicator.transform.LookAt(Camera.main.transform);
            strengthIndicator.transform.forward = -strengthIndicator.transform.forward; // fix for facing 
        }

        private void redrawPlayerView() {
            if (playerViewState == (int)ViewStates.encampmentModel) {
                string prefabName = "PlayerEncampment";
                GameObject villageObj = Instantiate(Resources.Load("Prefabs/Player/" + prefabName), this.transform) as GameObject;
                villageObj.name = "Player Village";

                if (villageObj != null) {
                    float randRotation = Random.Range(0f, 360f);
                    villageObj.transform.Rotate(0, randRotation, 0);
                }
            } else if (playerViewState == (int)ViewStates.playerModel) {
                if (this.player.GetType() == typeof(PlayablePlayer)) {
                    string prefabName = "Villager";
                    GameObject playerObj = Instantiate(Resources.Load("Prefabs/Player/" + prefabName), this.transform) as GameObject;
                    playerObj.name = "Player";

                    if (playerObj != null) {
                        playerObj.transform.position += playerHeightOffset;
                    }
                } else {
                    string prefabName = "PlayerCube";
                    GameObject playerObj = Instantiate(Resources.Load("Prefabs/Player/" + prefabName), this.transform) as GameObject;
                    playerObj.name = "AIplayer";

                    if (playerObj != null) {
                        playerObj.transform.position += playerHeightOffset;
                    }
                }
            }
        }

        private IEnumerator animateRotation(float rotate_angle, int frames_per_anim = 20) {
            animatorMoveState = (int)MoveStates.moving;
            float step = 0, _step = 1f / frames_per_anim, rotateAmount = rotate_angle * _step;
            while (step < 1) {
                step += _step;
                transform.Rotate(0, 0, rotateAmount);
                yield return null;
            }
            animatorMoveState = (int)MoveStates.idle;
        }

        private IEnumerator animatePlayerMove(PathResult pr, int frames_per_move = 5) {
            if (pr != null && pr.reachedGoal) {
                float step, _step = 1f / frames_per_move;
                foreach (Tile t in pr.getTilesOnPathStartFirst()) {
                    Vector3 destination = t.getPos();
                    step = 0;
                    while (step < 1) {
                        step += _step;
                        transform.position = Vector3.Lerp(transform.position, destination, step);
                        yield return null;
                    }
                    transform.position = destination;
                    yield return null;
                }
            }
            animatorMoveState = (int)MoveStates.idle;
        }
    }

    PlayerViewObjects humanPlayerView;
    List<PlayerViewObjects> nonHumanPlayerViews;

    void Start() {
        transform.position = new Vector3();
        nonHumanPlayerViews = new List<PlayerViewObjects>();

        // make views
        InitHumanPlayer();
        InitNonHumanPlayers();

        Debug.Log("Made player views.");
    }

    void InitHumanPlayer() {
        // init game object
        GameObject go = new GameObject("HumanPlayerView");
        go.transform.parent = this.transform;
        go.AddComponent<PlayerViewObjects>();

        humanPlayerView = go.GetComponent<PlayerViewObjects>();
        GameControl.gameSession.humanPlayer.setView(humanPlayerView);
        humanPlayerView.initPlayerView(GameControl.gameSession.humanPlayer);
        GameControl.gameSession.humanPlayer.notifyView();
    }

    void InitNonHumanPlayers() {
        foreach (NonPlayablePlayer p in GameControl.gameSession.AIPlayers) {
            PlayerViewObjects pvo;
            GameObject go;
            
            // init game object
            go = new GameObject("NonHumanPlayerView");
            go.transform.parent = this.transform;
            go.AddComponent<PlayerViewObjects>();

            // init observer on player and playerView
            pvo = go.GetComponent<PlayerViewObjects>();
            p.setView(pvo);
            pvo.initPlayerView(p);

            // notify observer
            p.notifyView();
        }
    }
}
