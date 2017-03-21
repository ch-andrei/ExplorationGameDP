//
//Filename: KeyboardCameraControl.cs
//

using UnityEngine;
using System;

[AddComponentMenu("Camera-Control/Keyboard")]
public class CameraControl : MonoBehaviour {

    public static Vector3 restrictionCenterPoint, viewCenterPoint;

    static float viewCenterOffset = 200f;

    static int cameraLimitDistance = 2500;
    static int cameraToGroundOffset = 75;
    static int maxCameraToGroundDistance = 250;

    static float limiterInertia = 0.1f;
    static float cameraTooHighLimiterSpeed = 1.5f;
    static float cameraTooLowLimiterSpeed = 2.5f;

    static float global_sensitivity = 1F;

    // Keyboard axes buttons in the same order as Unity
    public enum KeyboardAxis { Horizontal = 0, Vertical = 1, None = 3 }

    [System.Serializable]
    // Handles left modifiers keys (Alt, Ctrl, Shift)
    public class Modifiers {
        public bool leftAlt;
        public bool leftControl;
        public bool leftShift;

        public bool checkModifiers() {
            return (!leftAlt ^ Input.GetKey(KeyCode.LeftAlt)) &&
                (!leftControl ^ Input.GetKey(KeyCode.LeftControl)) &&
                (!leftShift ^ Input.GetKey(KeyCode.LeftShift));
        }
    }

    [System.Serializable]
    // Handles common parameters for translations and rotations
    public class KeyboardControlConfiguration {

        public bool activate;
        public KeyboardAxis keyboardAxis;
        public Modifiers modifiers;
        public float sensitivity;

        public bool isActivated() {
            return activate && keyboardAxis != KeyboardAxis.None && modifiers.checkModifiers();
        }
    }

    // Yaw default configuration
    public KeyboardControlConfiguration yaw = new KeyboardControlConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new Modifiers { leftAlt = true }, sensitivity = global_sensitivity };

    // Pitch default configuration
    public KeyboardControlConfiguration pitch = new KeyboardControlConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new Modifiers { leftAlt = true }, sensitivity = global_sensitivity };

    // Roll default configuration
    public KeyboardControlConfiguration roll = new KeyboardControlConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new Modifiers { leftAlt = true, leftControl = true }, sensitivity = global_sensitivity };

    // Vertical translation default configuration
    public KeyboardControlConfiguration verticalTranslation = new KeyboardControlConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new Modifiers { leftControl = true }, sensitivity = global_sensitivity };

    // Horizontal translation default configuration
    public KeyboardControlConfiguration horizontalTranslation = new KeyboardControlConfiguration { keyboardAxis = KeyboardAxis.Horizontal, sensitivity = global_sensitivity };

    // Depth (forward/backward) translation default configuration
    public KeyboardControlConfiguration depthTranslation = new KeyboardControlConfiguration { keyboardAxis = KeyboardAxis.Vertical, sensitivity = global_sensitivity };

    // Default unity names for keyboard axes
    public string keyboardHorizontalAxisName = "Horizontal";
    public string keyboardVerticalAxisName = "Vertical";


    private string[] keyboardAxesNames;

    void Start() {
        keyboardAxesNames = new string[] { keyboardHorizontalAxisName, keyboardVerticalAxisName };

        float offset = 0; // MapGenerator.getRegion().getViewableSize() / 2;
        transform.position = new Vector3(offset, GameControl.gameSession.mapGenerator.getRegion().getMaximumElevation() * 1.2f, offset);

        restrictionCenterPoint = new Vector3(); // TODO get player

        viewCenterPoint = new Vector3();
    }

    // LateUpdate  is called once per frame after all Update are done
    void LateUpdate() {

        Vector3 cameraPos = this.transform.position,
            cameraDir = this.transform.forward;

        cameraPos.y = 0;
        cameraDir.y = 0;

        viewCenterPoint = cameraPos + cameraDir.normalized * viewCenterOffset;

        // COMPUTE MOVEMENT
        if (yaw.isActivated()) { // camera rotate left/right
            float rotationX = Input.GetAxis(keyboardAxesNames[(int)yaw.keyboardAxis]) * yaw.sensitivity;
            transform.Rotate(0, rotationX, 0, Space.World);
        }
        if (pitch.isActivated()) { // camera rotate up/down
            float rotationY = Input.GetAxis(keyboardAxesNames[(int)pitch.keyboardAxis]) * pitch.sensitivity;
            transform.Rotate(-rotationY, 0, 0);
        }
        //if (roll.isActivated()) {
        //    float rotationZ = Input.GetAxis(keyboardAxesNames[(int)roll.keyboardAxis]) * roll.sensitivity;
        //    transform.Rotate(0, 0, rotationZ, Space.World);
        //}
        if (verticalTranslation.isActivated()) {
            float translateY = Input.GetAxis(keyboardAxesNames[(int)verticalTranslation.keyboardAxis]) * verticalTranslation.sensitivity;
            transform.Translate(0, translateY, 0, Space.World);
        }
        if (horizontalTranslation.isActivated()) {
            Vector3 direction = transform.right;
            direction.y = 0;
            direction.Normalize();
            transform.Translate(Input.GetAxis(keyboardAxesNames[(int)horizontalTranslation.keyboardAxis]) * horizontalTranslation.sensitivity * direction, Space.World);    
        }
        if (depthTranslation.isActivated()) { // camera move forward/backword
            Vector3 direction = transform.forward;
            direction.y = 0;
            direction.Normalize();
            transform.Translate(Input.GetAxis(keyboardAxesNames[(int)depthTranslation.keyboardAxis]) * depthTranslation.sensitivity * direction, Space.World);
        }

        limitCamera();
    }

    public void limitCamera() {
        // check if camera is out of bounds 
        Vector3 posRelative = transform.position - restrictionCenterPoint;
        if (posRelative.x > cameraLimitDistance) {
            transform.position -= new Vector3(posRelative.x - cameraLimitDistance, 0, 0);
        } else if (posRelative.x < -cameraLimitDistance) {
            transform.position -= new Vector3(posRelative.x + cameraLimitDistance, 0, 0);
        }
        if (posRelative.z > cameraLimitDistance) {
            transform.position -= new Vector3(0, 0, posRelative.z - cameraLimitDistance);
        } else if (posRelative.z < -cameraLimitDistance) {
            transform.position -= new Vector3(0, 0, posRelative.z + cameraLimitDistance);
        }
        // adjust camera height based on terrain
        try {
            Vector3 tileBelow = GameControl.gameSession.mapGenerator.getRegion().getTileAt(transform.position).getPos();
            float waterLevel = GameControl.gameSession.mapGenerator.getRegion().getWaterLevelElevation();

            float heightAboveGround =  transform.position.y - (tileBelow.y) - cameraToGroundOffset;
            float heightAboveWater = transform.position.y - (waterLevel) - cameraToGroundOffset;
            float heightBelowCeiling = tileBelow.y + maxCameraToGroundDistance - (transform.position.y);

            if (heightAboveGround < 0) { // camera too low based on tile height
                transform.position -= new Vector3(0, heightAboveGround, 0) * limiterInertia * cameraTooLowLimiterSpeed;
            } else if (heightAboveWater < 0) { // camera too low based on water elevation
                transform.position -= new Vector3(0, heightAboveWater, 0) * limiterInertia * cameraTooLowLimiterSpeed;
            } else if (heightBelowCeiling < 0) { // camera too high 
                transform.position += new Vector3(0, heightBelowCeiling, 0) * limiterInertia * cameraTooHighLimiterSpeed;
            }
        } catch (NullReferenceException e) {
            // do nothing
        }
        
    }

    public void centerOnPlayer() {
        // TODO
    }
}