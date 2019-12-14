using UnityEngine;
using System.Collections;
using CardboardControlDelegates;

/**
* Creating a vision raycast and handling the data from it
* Relies on Google Cardboard SDK API's
*/
public class CardboardControlTrigger : MonoBehaviour {
  public float clickSpeedThreshold = 0.4f;
  public bool useEventCooldowns = true;
  public bool vibrateOnDown = false;
  public bool vibrateOnUp = false;
  public bool vibrateOnClick = true;
  public bool useMagnet = true;
  public bool useTouch = true;
  public KeyCode triggerKey = KeyCode.Space;
  public bool printDebugInfo = false;

  private int clkCount = 0;
  private float releaseTime = 0f;
  private bool clicked = false;

  public string beingGazedObjectName = "";

  private ParsedMagnetData magnet;
  private ParsedTouchData touch;
  private enum TriggerState { Up, Down }
  private TriggerState currentTriggerState = TriggerState.Up;
  private float clickStartTime = 0f;

  private int debugThrottle = 0;
  private int FRAMES_PER_DEBUG = 5;

  private CardboardControl cardboard;
  public CardboardControlDelegate OnUp = delegate {};
  public CardboardControlDelegate OnDown = delegate {};
  public CardboardControlDelegate OnClick = delegate {};


  public void Start() {
    cardboard = gameObject.GetComponent<CardboardControl>();
    magnet = new ParsedMagnetData();
    touch = new ParsedTouchData();
  }

  public void Update() {
    magnet.Update();
    touch.Update();
    // controlKey();
    CheckKey();
    if (useTouch) CheckTouch();
    if (useMagnet) CheckMagnet();   // check magnet signal
  }

  public void FixedUpdate() {
    if (printDebugInfo) PrintDebug();
  }

  public void controlKey() {
    if (Input.GetKeyDown(KeyCode.W)) {
      GameObject obj = GameObject.Find(beingGazedObjectName);
      // Debug.log("beingGazedObjectName: " + beingGazedObjectName);
      obj.GetComponent<cakeslice.Outline>().color = 2;
      obj.GetComponent<cakeslice.Outline>().lock_color = true;
    }
  }

  private bool KeyFor(string direction) {
    switch(direction) {
      case "down":
        return Input.GetKeyDown(triggerKey);
      case "up":
        return Input.GetKeyUp(triggerKey);
      default:
        return false;
    }
  }

  private void CheckKey() {
    // if (Input.GetKeyDown(triggerKey)) ReportDown();
    // if (Input.GetKeyUp(triggerKey)) ReportUp();
    if (KeyFor("down") && cardboard.EventReady("OnDown")) ReportDown();
    if (KeyFor("up") && cardboard.EventReady("OnUp")) ReportUp();
  }

  private void CheckMagnet() {
    if (magnet.IsDown() && cardboard.EventReady("OnDown")) ReportDown();
    if (magnet.IsUp() && cardboard.EventReady("OnUp")) ReportUp();
  }

  private void CheckTouch() {
    if (touch.IsDown() && cardboard.EventReady("OnDown")) ReportDown();
    if (touch.IsUp() && cardboard.EventReady("OnUp")) ReportUp();
  }

  private bool IsTouching() {
    return Input.touchCount > 0;
  }

  private void ReportDown() {
    if (currentTriggerState == TriggerState.Up) {

      GameObject obj = GameObject.Find(beingGazedObjectName);
      if (obj.GetComponent<cakeslice.Outline>().lock_color == true) {
        obj.GetComponent<cakeslice.Outline>().color = 1;
      obj.GetComponent<cakeslice.Outline>().lock_color = false;
      } else {
        obj.GetComponent<cakeslice.Outline>().color = 2;
        obj.GetComponent<cakeslice.Outline>().lock_color = true;
      }

      // if (clkCount == 1) {
      //   // GameObject obj = GameObject.Find(beingGazedObjectName);
      //   obj.GetComponent<cakeslice.Outline>().color = 1;
      //   obj.GetComponent<cakeslice.Outline>().lock_color = false;
      // }

      currentTriggerState = TriggerState.Down;
      // OnDown(this);

      if (vibrateOnDown) Handheld.Vibrate();
      clickStartTime = Time.time;
      // if (Time.time - releaseTime <= clickSpeedThreshold) {
      //   clkCount++;
      //   obj.GetComponent<cakeslice.Outline>().color = 1;
      //   obj.GetComponent<cakeslice.Outline>().lock_color = false;
      // } else {
      //   clkCount = 0;
      // }
    }
  }

  private void ReportUp() {
    if (currentTriggerState == TriggerState.Down) {
      currentTriggerState = TriggerState.Up;
      // OnUp(this);
      if (vibrateOnUp) Handheld.Vibrate();
      CheckForClick();
    }
  }

  private void CheckForClick() {
    bool withinClickThreshold = SecondsHeld() <= clickSpeedThreshold;
    // if (withinClickThreshold && cardboard.EventReady("OnClick")) ReportClick();
    if (clkCount > 0 && !withinClickThreshold) {
      while(clkCount-- > 0) {
        if (vibrateOnClick) Handheld.Vibrate();
      }
    }
    clickStartTime = 0f;
    releaseTime = Time.time;
  }

  private void ReportClick() {
    // OnClick(this);
    if (vibrateOnClick) Handheld.Vibrate();
  }

  public float SecondsHeld() {
    return Time.time - clickStartTime;
  }

  public int continuedClicks() {
    return clkCount;
  }

  public bool IsHeld() {
    return (currentTriggerState == TriggerState.Down);
  }

  public void ResetMagnetState() {
    magnet.ResetState();
  }

  private void PrintDebug() {
    debugThrottle++;
    if (debugThrottle >= FRAMES_PER_DEBUG) {
      magnet.PrintDebug();
      touch.PrintDebug();
      debugThrottle = 0;
    }
  }
}
