//
//Filename: KeyboardCameraControl.cs
//

using UnityEngine;
using System;

[AddComponentMenu("Camera-Control/Keyboard")]
public class CameraControl : MonoBehaviour {

    public static Vector3 restrictionCenterPoint, viewCenterPoint;

    static float viewCenterOffset = 200f;

    static int cameraLimitDistance = 15000;

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
        if (yaw.isActivated()) {
            float rotationX = Input.GetAxis(keyboardAxesNames[(int)yaw.keyboardAxis]) * yaw.sensitivity;
            transform.Rotate(0, rotationX, 0, Space.World);
        }
        if (pitch.isActivated()) {
            float rotationY = Input.GetAxis(keyboardAxesNames[(int)pitch.keyboardAxis]) * pitch.sensitivity;
            transform.Rotate(-rotationY, 0, 0);
        }
        //if (roll.isActivated()) {
        //    float rotationZ = Input.GetAxis(keyboardAxesNames[(int)roll.keyboardAxis]) * roll.sensitivity;
        //    transform.Rotate(0, 0, rotationZ, Space.World);
        //}
        if (verticalTranslation.isActivated()) {
            float translateY = Input.GetAxis(keyboardAxesNames[(int)verticalTranslation.keyboardAxis]) * verticalTranslation.sensitivity;
            transform.Translate(0, translateY, 0);
        }
        if (horizontalTranslation.isActivated()) {
            float translateX = Input.GetAxis(keyboardAxesNames[(int)horizontalTranslation.keyboardAxis]) * horizontalTranslation.sensitivity;
            transform.Translate(translateX, 0, 0);
            
        }
        if (depthTranslation.isActivated()) {
            Vector3 direction = transform.forward;
            direction.y = 0;
            direction.Normalize();
            transform.Translate(depthTranslation.sensitivity * direction * Input.GetAxis(keyboardAxesNames[(int)depthTranslation.keyboardAxis]), Space.World);
        }

        limitCamera();
    }

    public void limitCamera() {
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
    }
}