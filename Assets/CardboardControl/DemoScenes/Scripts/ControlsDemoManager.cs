using UnityEngine;
using System.Collections;


public class ControlsDemoManager : MonoBehaviour {
  /*
  * Start by capturing the helper script on CardboardControlManager
  */
  public CardboardControl cardboard;
  float angle;
  Vector3 axis = Vector3.zero;

  public float xRotationSpeed = 50.0f;
  public float yRotationSpeed = 50.0f;
  public float zRotationSpeed = 1.0f;


  /*
  * CardboardControl has access to all the controls and their Delegates
  * Unity provides a good primer on delegates here:
  * http://unity3d.com/learn/tutorials/modules/intermediate/scripting/delegates
  */
  void Start () {
    cardboard.trigger.OnDown += CardboardDown;  // When the trigger goes down
    cardboard.trigger.OnUp += CardboardUp;      // When the trigger comes back up

    // When the magnet or touch goes down and up within the "click threshold" time
    // That click speed threshold is configurable in the Inspector
    cardboard.trigger.OnClick += CardboardClick;

    // When the thing we're looking at changes, determined by a gaze
    // The gaze distance and layer mask are public as configurable in the Inspector
    cardboard.gaze.OnChange += CardboardGazeChange;

    // When we've been staring at an object
    cardboard.gaze.OnStare += CardboardStare;

    // When we rotate the device into portrait mode
    cardboard.box.OnTilt += CardboardMagnetReset;
  }

  float zoom_ratio = 0.4f;

  /*
  * In this demo, we randomize object colours for triggered events
  */
  private void CardboardDown(object sender) {
    Debug.Log("Trigger went down");
    ChangeObjectColor("SphereDown");
  }

  private void CardboardUp(object sender) {
    Debug.Log("Trigger came up");
    ChangeObjectColor("SphereUp");
  }

  private void CardboardClick(object sender) {
    ChangeObjectColor("SphereClick");

    TextMesh textMesh = GameObject.Find("SphereClick/Counter").GetComponent<TextMesh>();
    int increment = int.Parse(textMesh.text) + 1;
    textMesh.text = increment.ToString();

    // With the cardboard object, we can grab information from various controls
    // If the raycast doesn't find anything then the focused object will be null
    string name = cardboard.gaze.IsHeld() ? cardboard.gaze.Object().name : "nothing";
    float count = cardboard.gaze.SecondsHeld();
    Debug.Log("We've focused on "+name+" for "+count+" seconds.");

    // If you need more raycast data from cardboard.gaze, the RaycastHit is exposed as gaze.Hit()
  }

  private void CardboardGazeChange(object sender) {
    // You can grab the data from the sender instead of the CardboardControl object
    CardboardControlGaze gaze = sender as CardboardControlGaze;
    // We can access to the object we're looking at
    // gaze.IsHeld will make sure the gaze.Object() isn't null
    if (gaze.IsHeld() && (gaze.Object().name.Contains("Photo") || gaze.Object().name.Contains("Button"))){
      ChangeObjectColor(gaze.Object().name);
      print(gaze.Object().transform.position);
    }
    // viewing mode + zoom mode
    if (gaze.IsHeld() && cardboard.trigger.mode == 1 && gaze.Object().name.Contains("Photo")) {
      cardboard.trigger.imBeingGazed = true;
      // print("zoom in " + gaze.Object().name);
      if(cardboard.trigger.isZoom) {
        print("zoomin");
        GameObject obj = GameObject.Find(gaze.Object().name);
        print("before zoom in" + obj.transform.position);
        obj.transform.position = new Vector3(obj.transform.position.x * zoom_ratio, obj.transform.position.y, obj.transform.position.z * zoom_ratio);
        print("done zoom in" + obj.transform.position);
      }
    }

    // viewing mode + not zoom mode
    // if(gaze.IsHeld() && !cardboard.trigger.isZoom && cardboard.trigger.mode == 1 && gaze.Object().name.Contains("Photo")) {
    //   // gaze.interactionButtons = new GameObject[4];
    //   // gaze.interactionButtons[0] = GUI.Button(new Rect(50, 50, 150, 35), "push in");
    //   // gaze.interactionButtons[1] = GUI.Button(new Rect(50, 50, 150, 35), "push in");
    //   // gaze.interactionButtons[2] = GUI.Button(new Rect(50, 50, 150, 35), "push in");
    //   // gaze.interactionButtons[3] = GUI.Button(new Rect(50, 50, 150, 35), "push in");
    // }

    if (gaze.WasHeld() && cardboard.trigger.mode == 1 && gaze.PreviousObject().name.Contains("Photo")) {
      cardboard.trigger.imBeingGazed = false;
      if (cardboard.trigger.isZoom) {
        print("zoomout");
        // print("zoom out " + gaze.Object().name);
        GameObject obj = GameObject.Find(gaze.PreviousObject().name);
        obj.transform.position = new Vector3(obj.transform.position.x / zoom_ratio, obj.transform.position.y, obj.transform.position.z / zoom_ratio);
      }
    }

    // We also can access to the last object we looked at
    // gaze.WasHeld() will make sure the gaze.PreviousObject() isn't null
    if (gaze.WasHeld() && (gaze.PreviousObject().name.Contains("Photo") || gaze.PreviousObject().name.Contains("Button"))) {
      ResetObjectColor(gaze.PreviousObject().name);
      // Use these to undo reticle hiding and highlighting
      cardboard.reticle.Show();
      cardboard.reticle.ClearHighlight();
    }

    // Be sure to set the Reticle Layer Mask on the CardboardControlManager
    // to grow the reticle on the objects you want. The default is everything.

    // Not used here are gaze.Forward(), gaze.Right(), and gaze.Rotation()
    // which are useful for things like checking the view angle or shooting projectiles
  }

  private void CardboardStare(object sender) {
    CardboardControlGaze gaze = sender as CardboardControlGaze;
    if (gaze.IsHeld() && (gaze.Object().name.Contains("Photo") || gaze.Object().name.Contains("Button"))) {
      // Be sure to hide the cursor when it's not needed
      cardboard.reticle.Hide();
    }
  }

  private void CardboardMagnetReset(object sender) {
    // Resetting the magnet will reset the polarity if up and down are confused
    // This occasionally happens when the device is inserted into the enclosure
    // or if the magnetometer readings are weak enough to cut in and out
    Debug.Log("Device tilted");
    cardboard.trigger.ResetMagnetState();
    ResetSpheres();
  }

  private void ChangeObjectColor(string name) {
    GameObject obj = GameObject.Find(name);
    cardboard.trigger.beingGazedObjectName = name;
    if (!obj.GetComponent<cakeslice.Outline>().lock_color) {
      obj.GetComponent<cakeslice.Outline>().color = 1;
    }
    // Color newColor = ;
    // obj.GetComponent<Renderer>().material.color = newColor;
  }

  private void ResetObjectColor(string name) {
    // GameObject.Find(name).GetComponent<Renderer>().material.color = Color.white;
    GameObject obj = GameObject.Find(name);
    cardboard.trigger.beingGazedObjectName = "";
    if (!obj.GetComponent<cakeslice.Outline>().lock_color) {
      obj.GetComponent<cakeslice.Outline>().color = 0;
    }
  }

  private void ResetSpheres() {
    string[] spheres = { "SphereDown", "SphereUp", "SphereClick" };
    foreach (string sphere in spheres) {
      GameObject obj = GameObject.Find(sphere);
      obj.GetComponent<Renderer>().material.color = Color.white;
    }
  }

  private Color RandomColor() {
    return new Color(Random.value, Random.value, Random.value);
  }



  /*
  * During our game we can utilize data from the CardboardControl API
  */
  void Update() {
    if (cardboard.trigger.isRotate) {

      GameObject obj = GameObject.Find(cardboard.trigger.beingRotateObjectName);
      GameObject main = GameObject.Find("Main Camera");
      obj.transform.rotation = main.transform.rotation;
      // obj.transform.localEulerAngles = new Vector3(0f, 30f, 0f);
      // obj.transform.Rotate(Vector3.up * (10 * yRotationSpeed));
      // obj.transform.Rotate(Vector3.forward * (10 * zRotationSpeed));
      // Quaternion q = Quaternion.AngleAxis(angle * Time.deltaTime, main.transform.right);
      // obj.transform.rotation = q * obj.transform.rotation;
      // print(obj.transform.rotation);



    }
  }


  /*
  * Be sure to unsubscribe before this object is destroyed
  * so the garbage collector can clean everything up
  */
  void OnDestroy() {
    cardboard.trigger.OnDown -= CardboardDown;
    cardboard.trigger.OnUp -= CardboardUp;
    cardboard.trigger.OnClick -= CardboardClick;
    cardboard.gaze.OnChange -= CardboardGazeChange;
    cardboard.gaze.OnStare -= CardboardStare;
    cardboard.box.OnTilt -= CardboardMagnetReset;
  }
}
