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

  float radius = 50;
  int photoCount = 16;

  public int mode = 0;
  public bool isZoom = false;

  GameObject[] Photos;
  Vector3[] prev_pos;
  private float angle = 0;//角度

  public bool imBeingGazed = false;
  public int[] currentPPIndex;

  public bool isRotate = false;
  public string beingRotateObjectName = "";

  private void ppTransform() {
    GameObject obj = GameObject.Find(beingGazedObjectName);
    Vector3 currentPostition = obj.transform.position;
    int length = beingGazedObjectName.Length;
    print(length);
    print(beingGazedObjectName.Substring(5));
    int obj_index = int.Parse(beingGazedObjectName.Substring(5))-1;

    // print("PPTransform objName: " + beingGazedObjectName);
    if (currentPPIndex[obj_index] == 1) {
      currentPPIndex[obj_index] = 2;
      obj.transform.position = new Vector3(currentPostition.x * 2, currentPostition.y, currentPostition.z * 2);
    } else if (currentPPIndex[obj_index] == 2) {
      currentPPIndex[obj_index] = 3;
      obj.transform.position = new Vector3(obj.transform.position.x * 1.5f, obj.transform.position.y, obj.transform.position.z * 1.5f);
    } else {
      currentPPIndex[obj_index] = 1;
      obj.transform.position = new Vector3(currentPostition.x / 3, currentPostition.y, currentPostition.z / 3);
    }
  }

  public void Start() {
    cardboard = gameObject.GetComponent<CardboardControl>();
    magnet = new ParsedMagnetData();
    touch = new ParsedTouchData();
    Photos = new GameObject[17];
    prev_pos = new Vector3[17];
    currentPPIndex = new int[16];
    for (int i=0; i<16; i++) {
      currentPPIndex[i] = 2;
    }
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
      if (Time.time - releaseTime > clickSpeedThreshold){
        if (beingGazedObjectName == "Button") {
          GameObject obj = GameObject.Find(beingGazedObjectName);
          obj.GetComponent<cakeslice.Outline>().color = 2;
          if (mode == 0) {
            mode = 1;
            savePos(photoCount);
          } else {
            mode = 0;
          }
          movePos(photoCount, mode);
        } 
        else {
          if (mode == 0) {
            GameObject obj = GameObject.Find(beingGazedObjectName);
            if (obj.GetComponent<cakeslice.Outline>().lock_color == true) {
              obj.GetComponent<cakeslice.Outline>().color = 1;
              obj.GetComponent<cakeslice.Outline>().lock_color = false;
            } else {
              obj.GetComponent<cakeslice.Outline>().color = 2;
              obj.GetComponent<cakeslice.Outline>().lock_color = true;
            }
          } 
          else if (beingGazedObjectName.Contains("Photo")){
            print("before PPTransform");
            ppTransform();
            print("after PPTransform");
          }
        }
      } else if (Time.time - releaseTime <= clickSpeedThreshold) {
        print("being Double Clicked");
        print("double click objname: " + beingGazedObjectName);
        if (!(beingGazedObjectName.Contains("Photo") || beingGazedObjectName.Contains("Button"))) {
          if (mode == 0) {
            // TODO maybe do sth
          } else {
            // turn on/off zoom mode
            isZoom = !isZoom;
            print("ZOOM MODE IS: " + isZoom);
          }
        } else if (beingGazedObjectName.Contains("Photo")) {
          if (mode == 1) {
            if (isRotate) {
              beingRotateObjectName = "";
            } else {
              beingRotateObjectName = beingGazedObjectName;
              // GameObject obj = GameObject.Find(beingGazedObjectName);
              // obj.transform.localEulerAngles = new Vector3(0f,0f,0f);
            }
            isRotate = !isRotate;
            print("ROTATE MODE IS: " + isRotate);
            // GameObject obj = GameObject.Find(beingGazedObjectName);
            // obj.transform.localEulerAngles = new Vector3(90,0f,0f);
          }
        }
        
      } else {
        clkCount = 0;
      }
      currentTriggerState = TriggerState.Down;
      clickStartTime = Time.time;
    }
  }

  private void ReportUp() {
    if (currentTriggerState == TriggerState.Down) {
      currentTriggerState = TriggerState.Up;
      if (beingGazedObjectName == "Button") {
        GameObject obj = GameObject.Find(beingGazedObjectName);
        obj.GetComponent<cakeslice.Outline>().color = 1;
      }
      // OnUp(this);
      // if (vibrateOnUp) Handheld.Vibrate();
      CheckForClick();
    }
  }

  public void movePos(int photoCount, int mode) {
    int validCount = 0;
    if (mode == 1) {
      for (int i = 1; i <= photoCount; i++) {
        Photos[i] = GameObject.Find("Photo" + i);
        if (Photos[i].GetComponent<cakeslice.Outline>().lock_color) {
          validCount++;
        }
      }

      print("movePos 1");
      GameObject.Find("Player").transform.position = new Vector3(0, -100, 0);
      Vector3 buttonPos = GameObject.Find("Button").transform.position;
      GameObject.Find("Button").transform.position = new Vector3(buttonPos.x, buttonPos.y-100, buttonPos.z);
      Vector3 planePos = GameObject.Find("Plane").transform.position;
      GameObject.Find("Plane").transform.position = new Vector3(planePos.x, planePos.y-105, planePos.z);
      for (int i = 1; i <= photoCount; i++) {
        print("Position changing");
        Photos[i] = GameObject.Find("Photo" + i);
        if (Photos[i].GetComponent<cakeslice.Outline>().lock_color) {
        
          float radian = (angle / 180) * Mathf.PI;
          float xx = radius * Mathf.Cos(radian);
          float zz = radius * Mathf.Sin(radian);

          Photos[i].transform.position = new Vector3(xx, -100, zz);
          Photos[i].transform.LookAt(GameObject.Find("Player").transform.position);

          print("current pos : " + Photos[i].transform.position);
          print("past pos : " + prev_pos[i]);
          angle += 360 / validCount;
        }
      }
    } else if (mode == 0) {
      print("movePos 0");
      GameObject.Find("Player").transform.position = new Vector3(0, 1, 0);
      for (int i=1; i <= photoCount; i++) {
        Photos[i].transform.position = prev_pos[i];
        Photos[i].transform.LookAt(GameObject.Find("Player").transform.position);
      }
      Vector3 buttonPos = GameObject.Find("Button").transform.position;
      GameObject.Find("Button").transform.position = new Vector3(buttonPos.x, buttonPos.y+100, buttonPos.z);
      Vector3 planePos = GameObject.Find("Plane").transform.position;
      GameObject.Find("Plane").transform.position = new Vector3(planePos.x, planePos.y+105, planePos.z);
    }
  }
  
  public void savePos(int photoCount) {
    print("savePos");
    for (int i = 1; i <= photoCount; i++) {
      Photos[i] = GameObject.Find("Photo" + i);
      prev_pos[i] = Photos[i].transform.position;
    }
    print("doneSavePos");
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
