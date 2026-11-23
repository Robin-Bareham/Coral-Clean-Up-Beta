using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using System.IO;
using TMPro;
using FMODUnity;



// Tutorial for screenshots: https://youtu.be/CM6GLD7VKJw?si=X26JtWQWk5CLUtQY 


public class PhotoCameraBehaviour : MonoBehaviour
{
    [SerializeField]
    private InputActionAsset PlayerControls;

    [SerializeField]
    private GameObject camera_text;

    [SerializeField] Image photo_showing_loc; 
    [SerializeField] GameManager manager;

    private InputAction takePhotoAction;
    private InputAction openCameraAction;
    private bool photoCameraActive = false;

    private List<GameObject> UIObjects = new List<GameObject>(); //Which UI got hidden.

    private bool photoJustTaken = false;
    private float photoShownFor = 5.0f; //5 seconds
    private float photoTimer;


    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    { 
        InputActionMap map = PlayerControls.FindActionMap("Player");
        //Links up action to input actions map
        takePhotoAction = map.FindAction("TakePhoto");
        openCameraAction = map.FindAction("OpenCamera");

        camera_text.gameObject.SetActive(false);
        photo_showing_loc.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        takePhotoAction.Enable();
        openCameraAction.Enable();
    }

    private void OnDisable()
    {
        takePhotoAction.Disable();
        openCameraAction.Disable();
    }
    // Update is called once per frame
    void Update()
    {
        //If the photo was just taken, show it for a bit then hide it
        if (photoJustTaken)
        {
            photoTimer -= Time.deltaTime;
            if (photoTimer <= 0.0f) //Once that time's elapsed, show everything again and let the player move
            {
                photo_showing_loc.gameObject.SetActive(false);
                photoJustTaken = false;
                showAllUI();
                manager.setCanMove(true);
            }
        }
        else //If not, you can continue taking photos.
        {
            //WasPressedThisFrame does a single activation for triggers. 
            if (takePhotoAction.WasPressedThisFrame() && photoCameraActive)
            {
                TakeAPhoto();
            }
            //Opens Camera
            if (openCameraAction.WasPressedThisFrame() && manager.getCanMove())
            {
                photoCameraActive = !photoCameraActive;
                //If the camera is active, the text will appear
                camera_text.gameObject.SetActive(photoCameraActive);
            }
        }
    }

    private void TakeAPhoto()
    {
        //Hides UI
        disableAllUI();

        //Takes and displays the photo
        StartCoroutine(TakePhotoAndShow()); 
    
        //Plays 'CameraShoot' Event 
        AudioManager.instance.PlayOneShot(FMODEvents.instance.CameraShoot, this.transform.position);


        ////Adds the time to the string, having to replace / spaces and : as they're not accepted in file names
        //string date = System.DateTime.Now.ToString();
        //date = date.Replace("/", "-");
        //date = date.Replace(" ", "_");
        //date = date.Replace(":", "-");
        ////Takes the screenshot of the sceen.
        //ScreenCapture.CaptureScreenshot(Application.dataPath + "/Screenshots/trials_" + date + ".png");
        //UnityEditor.AssetDatabase.Refresh();
        //Sets it to false to avoid multiple pictures at once + camera has been 'put down'
        manager.setCanMove(false);
        photoCameraActive = false;
        photoJustTaken = true;
        photoTimer = photoShownFor;
        manager.setPhotoCount(1);
    }


    private IEnumerator TakePhotoAndShow()
    {
        yield return new WaitForEndOfFrame(); //Waits for all the rendering to be done before taking a photo.

        Texture2D photo = ScreenCapture.CaptureScreenshotAsTexture();
        //Makes a photo around the screenshots
        Texture2D newPhoto = new Texture2D(photo.width, photo.height, TextureFormat.RGB24, false);
        newPhoto.SetPixels(photo.GetPixels());
        newPhoto.Apply();

        Destroy(photo);

        //Displaying as a sprite
        Sprite photoSprite = Sprite.Create(newPhoto, new Rect(0,0, newPhoto.width, newPhoto.height), new Vector2(0.5f,0.5f));
        photo_showing_loc.gameObject.SetActive(true);
        photo_showing_loc.enabled = true;
        photo_showing_loc.sprite = photoSprite;
    }

    private void disableAllUI()
    {
        UIObjects.Clear();
        camera_text.gameObject.SetActive(false);

        GameObject[] objects = GameObject.FindGameObjectsWithTag("UI");
        foreach (GameObject obj in objects)
        {
            UIObjects.Add(obj);
            obj.SetActive(false);
        }

    }

    private void showAllUI()
    {
        foreach (GameObject obj in UIObjects)
        {
            obj.SetActive(true);
        }
    }

}
