using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class TextTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

    // customizable
    public bool alwaysOn;
    public string tooltipText;
    public int verticalOffset;
    public int horizontalOffset;

    // constants
    private static int textFieldHorizontalSize = 0;
    private static int textFieldVerticalSize = 0;

    enum TooltipStates { idle, delayed, interrupted }

    // dependent
    public Color tooltipTextColor;

    GUIStyle guiStyleFore;
    bool mouseOver = false, delayedMouseOver = false;
    int tooltipState = (int)TooltipStates.idle;
    long mouseOverID;

    void Start() {
        guiStyleFore = new GUIStyle();
        guiStyleFore.normal.textColor = tooltipTextColor;
        guiStyleFore.fontSize = 10;
        guiStyleFore.alignment = TextAnchor.UpperCenter;
        guiStyleFore.wordWrap = true;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        mouseOverID = DateTime.Now.Ticks;
        mouseOver = true;
        tooltipState = (int)TooltipStates.delayed;
        StartCoroutine(delayedTooltipOn(mouseOverID));
    }

    public void OnPointerExit(PointerEventData eventData) {
        mouseOver = false;
        delayedMouseOver = false;
        tooltipState = (int)TooltipStates.interrupted;
    }

    IEnumerator delayedTooltipOn(long id, float secondsToDelay = 1) {
        yield return new WaitForSeconds(secondsToDelay);
        if (tooltipState == (int)TooltipStates.delayed && mouseOverID == id) {
            delayedMouseOver = true;
            tooltipState = (int)TooltipStates.idle;
        }
    }

    void OnGUI() {
        if (alwaysOn || mouseOver && delayedMouseOver) {
            int x = (int)this.transform.position.x - horizontalOffset;
            int y = -(int)this.transform.position.y + Screen.height + verticalOffset;
            GUI.color = tooltipTextColor;
            GUI.Label(new Rect(x, y, textFieldHorizontalSize, textFieldVerticalSize), tooltipText, guiStyleFore);
        }
    }
}
