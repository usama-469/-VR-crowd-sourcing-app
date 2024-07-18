using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.XR;
using System.IO;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using System.Linq;

[System.Serializable]
public class Trial {
    public int no;
    public string scenario;
    public string video_id;     //USE THIS FOR identification
    public int yielding;
    public int eHMIOn;
    public int distPed;
    public int p1;
    public int p2;
    public int camera;
    public int group;
    public int video_length;
}


public class ConditionController : MonoBehaviour
{
    //------------for writing
    [System.Serializable]
    public class Data
    {
        public string vid_name;     //for the videoID
        public float A1;      //Answer 1-3
        public float A2;
        public string A3;
    }
    [System.Serializable]
    public class DataList
    {
        public Data[] _data;
    }

    public DataList myDataList = new DataList();
    public string writeFileName = "a";       //write the name of the file for storing the data into----------a for standard
    public string writeFilePath = "";           //where the file will be saved

    //------------for writing



    public bool conditionFinished = false;
    LightStripBumper lightStripBumper;      // script
    public GameObject LEDBumperObject;
    public GameObject tracker;
    public GameObject progress; 
    public GameObject projection; 
    
    CarMovement carMovementScript;
    PlayFabController playfabScript;
    public int conditionCounter = 0;
    public int numberConditions = 0;

    public int eHMIOn = 0;   // 0=no 1=slowly-pulsing light band
    public int yielding = 0; // 0=yes for P1 1=yes for P2 2=no
    public int distPed = 0;  // distance between P1 and P2 distances [2 .. +2 ..  20].
    public int p1 = 0;       // presence of Pedestrian 1
    public int p2 = 0;       // presence of Pedestrian 2
    public int camera = 0;   // location of camera
    public int duration = 0;

    public GameObject demoWelcomeCanvas; 
    public GameObject demoWalkCanvas;
    public GameObject demoInfoCanvas1;
    public GameObject demoInfoCanvas2;

    public GameObject trialWalkCanvas;
    public GameObject trialDemoCanvas;
    public GameObject trialStartCanvas;
    public GameObject trialEndCanvas;
    public GameObject ExperimentEndCanvas;

    public Text demoTitle; 
    public Text demoText;
    public Text trialTitle;

    // todo: cleanup
    public GameObject WillingnessToCross;
    public GameObject reticle;
    public GameObject CountDown; 
    public bool preview = false; 
    public bool trial = false;

    public AudioSource buttonSound;

    public Trial[] trials; // description of trials based on mapping

    public GameObject p1_object;  // object of P1
    public GameObject p2_object;  // object of P2
    public GameObject camera_object;  // camera object
    public GameObject black_canvas;  // canvas to be shown as black screen

    //RecorderController recorderController; // control interface for recording video
    //RecorderControllerSettings controllerSettings;
    //MovieRecorderSettings videoRecorder;

    public void Start()
    {
        writeFilePath = Application.dataPath +"/" +  writeFileName + ".csv";            //the patth to stroe the files with the given filename---------------------change it for unique files
         
        Debug.Log("Start");
        // Import trial data
        string filePath = Application.dataPath + "/../../public/videos/mapping.csv";
        string text = File.ReadAllText(filePath);
        trials = CSVSerializer.Deserialize<Trial>(text);
        numberConditions = trials.Length; // set number of conditions
        Debug.Log("Number of conditions: " + numberConditions);
        StartCoroutine(ActivatorVR("cardboard"));
        buttonSound = GetComponent<AudioSource>();

        //usama code
        myDataList._data = new Data[numberConditions]; // generating a data list of our total number of conditions

        Start2();



    }
    public IEnumerator ActivatorVR(string YESVR)
    {
        
       // XRSettings.LoadDeviceByName(YESVR);
        yield return null;
        //XRSettings.enabled = true;
    }
    public IEnumerator DectivatorVR(string NOVR)
    {
      //  XRSettings.LoadDeviceByName(NOVR);
        yield return null;
       // XRSettings.enabled = false;       
    }

    public float time1, time2 = 0f;

    public GameObject player1, player2;
    void Start2()
    {
        time1 = Time.time;
        carMovementScript = GameObject.Find("CarMovement").GetComponent<CarMovement>();
        playfabScript = GameObject.Find("PlayFabController").GetComponent<PlayFabController>();
        LEDBumperObject.SetActive(true);            // Turn on LED bumper
        tracker.SetActive(false);                   // Switch off tracker   
        progress.SetActive(false);                  // Switch off progressbar
        projection.SetActive(false);                // Switch off projection

        // Set variables for trial
        eHMIOn = trials[conditionCounter].eHMIOn;
        yielding = trials[conditionCounter].yielding;
        distPed = trials[conditionCounter].distPed;
        p1 = trials[conditionCounter].p1;
        p2 = trials[conditionCounter].p2;
        camera = trials[conditionCounter].camera;

        duration = trials[conditionCounter].video_length;

        Debug.Log(conditionCounter +  ":: eHMIOn=" + eHMIOn +  " yielding=" + yielding +  " distPed=" + distPed +
          " p1=" + p1 +  " p2=" + p2 + " camera=" + camera);

        // Make p1 present or not
        if (p1 == 0) {
            p1_object.SetActive(false);
            Debug.Log("P1 disabled");
        } else {
            p1_object.SetActive(true);
            Debug.Log("P1 enabled");
        }

        // Make p2 present or not
        if (p2 == 0) {
            p2_object.SetActive(false);
            Debug.Log("P2 disabled");
        } else {
            p2_object.SetActive(true);
            Debug.Log("P2 enabled");
        }

        // Distance between pedestrians
        // position of P1=(21.3, -3.316, -3.98272)
        float deltaDist = 2f * distPed; // change in x coordinate
        if (distPed != 0) {
            p2_object.transform.position = new Vector3(p1_object.transform.position.x - deltaDist, 
                                                       p1_object.transform.position.y,
                                                       p1_object.transform.position.z);
            Debug.Log("distance between pedestrians set to distPed=" + distPed + ": (posP1.x - " + 2 * distPed +
                ", posP1.y, posP1.z). coordinates of P2=(" + p2_object.transform.position.x + ", " +
                p2_object.transform.position.y + ", " + p2_object.transform.position.z + ")");
        } else {
            Debug.Log("distance between pedestrians not set (distPed=0)");
        }

        // Camera position
        Vector3 posCameraP1 = new Vector3(105.54f, -1.717f, 4.271f);  // position of camera for P1
        Vector3 rotCameraP1 = new Vector3(0f, -49.995f, 0f);   // rotation of camera for P1
        Vector3 rotCameraP2 = new Vector3(0f, -49.995f, 0f);   // rotation of camera for P2
        Vector3 posCamera3rd = new Vector3(108.53f, -0.47f, -2.68f);  // position of camera for 3rd person view
        Vector3 rotCamera3rd = new Vector3(0f, -49.995f, 0f);   // rotation of camera for 3rd person view
        Vector3 targetCameraPos = new Vector3();  // target for moving camera
        float transitionDuration = 0.0f;;  // duration of movement of camera
        if (camera == 0) {
            camera_object.transform.position = posCameraP1;
            camera_object.transform.eulerAngles = rotCameraP1;
            Debug.Log("Camera set to head of P1.");
        } else if (camera == 1) {
            camera_object.transform.position = new Vector3(posCameraP1.x - deltaDist,  // take into account movement of P2
                                                           posCameraP1.y,
                                                           posCameraP1.z);
            camera_object.transform.eulerAngles = rotCameraP1;
            Debug.Log("Camera set to head of P2.");
        } else if (camera == 2) {
            camera_object.transform.position = posCamera3rd;
            camera_object.transform.eulerAngles = rotCameraP2;
            Debug.Log("Camera set to 3rd person view.");
        } else if (camera == 3) {
            camera_object.transform.position = new Vector3(posCameraP1.x - deltaDist,  // take into account movement of P2
                                                           posCameraP1.y,
                                                           posCameraP1.z);
            camera_object.transform.eulerAngles = rotCameraP2;
            targetCameraPos = posCameraP1;
            // todo: dynamic value for time
            transitionDuration = 0.5f * deltaDist;
            Debug.Log("Camera set to P1 with going away from P2.");
        } else if (camera == 4) {
            camera_object.transform.position = posCameraP1;
            camera_object.transform.eulerAngles = rotCameraP1;
            targetCameraPos = new Vector3(posCameraP1.x - deltaDist,  // take into account movement of P2
                                          posCameraP1.y,
                                          posCameraP1.z);
            transitionDuration = 0.5f * deltaDist;  // no camera movement needed
            Debug.Log("Camera set to P2 with going away from P1.");
        } else {
            Debug.Log("Wrong value for camera given.");
        }

        // Make setup for recording video
        //controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        //recorderController = new RecorderController(controllerSettings);
        //videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        //videoRecorder.name = "Video recorder";
        //videoRecorder.Enabled = true;
        //videoRecorder.OutputFile = Application.dataPath + "/../../public/videos/" + trials[conditionCounter].video_id;
        //var fiOut = new FileInfo(videoRecorder.OutputFile + ".mp4");
        //controllerSettings.AddRecorderSettings(videoRecorder);
        //// controllerSettings.SetRecordModeToManual(); // will stop when closing
        //RecorderOptions.VerboseMode = false;
        //videoRecorder.ImageInputSettings = new GameViewInputSettings()
        //{
        //    OutputWidth = 3840,
        //    OutputHeight = 2160
        //};
        //videoRecorder.AudioInputSettings.PreserveAudio = true;
        //controllerSettings.AddRecorderSettings(videoRecorder);
        //controllerSettings.FrameRate = 60;
        //recorderController.PrepareRecording();
        //recorderController.StartRecording();
       
        //Debug.Log($"Started recording video to file '{fiOut.FullName}'");

        // Show black screen for 1 s
        StartCoroutine(BlackScreen(1f));
        // Start trial
        TrialStart(targetCameraPos, transitionDuration);
        // End recording video

        startNextStage = false;                             //-------next stage stops and waits for the input

        StartCoroutine(UI_duration(duration));
        // Question1();
    }

    IEnumerator UI_duration(int time_duration)
    {
        yield return new WaitForSeconds(time_duration/1000);
        Question1();
    }

    // Show black screen for 1 second
    IEnumerator BlackScreen(float t)
    {
        black_canvas.GetComponent<Image>().color = new Color(0, 0, 0, 255);
        yield return new WaitForSeconds(t);
        black_canvas.GetComponent<Image>().color = new Color(0, 0, 0, 0);
    }

    private void FixedUpdate()
    {
                             
                                    if (startNextStage == true)//writes and saves a file and starts the new cycle of data input
                                        { write_data = true; }
        // Update input data display
        UpdateInputDataDisplay();

        if (carMovementScript != null) {
            if (carMovementScript.conditionFinished && startNextStage == true)            //ONE experiment is done    //-------------------assuming this starts the next stage
            {
                // stop recording of video
               // recorderController.StopRecording();
                Debug.Log("Stopped video recording");
                // Experiment is finished
                if (conditionCounter == numberConditions - 1) {
                    Debug.Log("Experiment finished");
                    Application.Quit(); // quit
                }
                Debug.Log("FixedUpdate::trial end");
                WillingnessToCross.SetActive(false);
                reticle.SetActive(true);
                carMovementScript.conditionFinished = false;
                trial = false;                                                                                      
                                                                               
                conditionCounter = conditionCounter + 1;
                trialEndCanvas.SetActive(false);
                StartCoroutine(ActivatorVR("none"));
                if (startNextStage == true)          
                Start2();
                                                                                            //Question1();
            }

            
           
        }



    }

    // UI DEMO
    void DemoStart()
    {
        Debug.Log("DemoStart");
        demoWelcomeCanvas.SetActive(true);
    }
    public void DemoCanvas1()
    {
        Debug.Log("DemoCanvas1");
        demoWelcomeCanvas.SetActive(false);
        demoWalkCanvas.SetActive(true);
            }
    public void DemoCanvas2()
    {
        Debug.Log("DemoCanvas2");
        demoWalkCanvas.SetActive(false);
        StartCoroutine(WalkForward());
        demoInfoCanvas1.SetActive(true);
    }
    public void DemoCanvas3()
    {
        Debug.Log("DemoCanvas3");
        demoInfoCanvas1.SetActive(false);
        StartCoroutine(CountDownDemo());
    }
    public void DemoCanvas4()
    {
        Debug.Log("DemoCanvas4");
        demoInfoCanvas2.SetActive(false);      
        StartCoroutine(ActivatorVR("none"));
        SceneManager.LoadScene("Environment");
    }

    IEnumerator CountDownDemo()
    {
        Debug.Log("CountDownDemo");
        reticle.SetActive(false);
        CountDown.SetActive(true);
        carMovementScript.CountSound.Play();
        yield return new WaitForSecondsRealtime(3f);
        carMovementScript.AudioBeep.Play();
        yield return new WaitForSecondsRealtime(1f);
        carMovementScript.StartCarDemo();
        WillingnessToCross.SetActive(true);
    }

    IEnumerator WalkForward()
    {
        Debug.Log("WalkForward");
        yield return new WaitForSecondsRealtime(0.2f);
        GameObject.Find("CameraHolder").GetComponent<MoveCamera>().StartWalk = true;
        yield return new WaitForSecondsRealtime(3.0f);
    }

    // UI TRIALS
    void TrialStart(Vector3 targetCameraPos, float transitionDuration)
    {
        Debug.Log("TrialStart");
        TrialCanvas3(targetCameraPos, transitionDuration);
        //trialWalkCanvas.SetActive(true);
    }
    public void TrialCanvas1()
    {
        Debug.Log("TrialCanvas1");
        trialWalkCanvas.SetActive(false);
        StartCoroutine(WalkForward());
        trialDemoCanvas.SetActive(true);
    }
    public void TrialCanvas2()                  // Start preview
    {
        Debug.Log("TrialCanvas2");
        trialDemoCanvas.SetActive(false);
        preview = true;
        StartCoroutine(CountDownPreview());
    }

    IEnumerator CountDownPreview()
    {
        Debug.Log("CountDownPreview");
        reticle.SetActive(false);
        CountDown.SetActive(true);
        carMovementScript.CountSound.Play();
        yield return new WaitForSecondsRealtime(3f);
        carMovementScript.AudioBeep.Play();
        yield return new WaitForSecondsRealtime(1f);
        carMovementScript.StartCarPreview();
        WillingnessToCross.SetActive(true);
    }
    public void TrialCanvas3(Vector3 targetCameraPos, float transitionDuration)                  // Start trial
    {
        Debug.Log("TrialCanvas3");
        // trialStartCanvas.SetActive(false);
        carMovementScript.AudioBeep.Play();
        // move camera
        if (transitionDuration > 0) {
            StartCoroutine(MoveCamera(targetCameraPos, transitionDuration));
        }
        trial = true;
        carMovementScript.StartCar();
        
        // StartCoroutine(CountDownTrial());
    }

    // move camera between two points
    IEnumerator MoveCamera(Vector3 targetCameraPos, float transitionDuration) {
        Debug.Log("Moving camera");
        yield return new WaitForSeconds(1f);
        Debug.Log(camera_object.transform.position);
        Debug.Log(targetCameraPos);
        float t = 0.0f;
        while (t < 1.0f)
        {
            t += Time.deltaTime * (Time.timeScale/transitionDuration);
            camera_object.transform.position = Vector3.Lerp(camera_object.transform.position, // move from
                                                            targetCameraPos,  // move to
                                                            t);  // in amount of time
            Debug.Log(t);
            yield return 0;
        }
    }

    IEnumerator CountDownTrial()
    {
        Debug.Log("CountDownTrial");
        reticle.SetActive(false);
        CountDown.SetActive(true);
        carMovementScript.CountSound.Play(); 
        yield return new WaitForSecondsRealtime(3f);
        carMovementScript.AudioBeep.Play();
        yield return new WaitForSecondsRealtime(1f);
        playfabScript.deltaTime2 = Time.time;
        trial = true;
        carMovementScript.StartCar();
        WillingnessToCross.SetActive(true);
    }

    public void TrialCanvas4()
    {
        Debug.Log("TrialCanvas4");
        // Set next condition        
        // PlayerPrefs.SetInt("Condition Counter", conditionCounter + 1);
        trialEndCanvas.SetActive(false);
        StartCoroutine(ActivatorVR("none"));
        SceneManager.LoadScene("Environment");
    }

    public void RestartPreview()
    {
        Debug.Log("RestartPreview");
        trialStartCanvas.SetActive(false);
        trialDemoCanvas.SetActive(true);
    }

    public void Reset0()
    {
        Debug.Log("Reset0");
        PlayerPrefs.SetInt("Condition Counter", 1);
        StartCoroutine(ActivatorVR("none"));
        SceneManager.LoadScene("Environment");
    }

    public void ButtonSound()
    {
        Debug.Log("ButtonSound");
        buttonSound.Play();
    }



    //writing function

    public void writeCSV()
    {

        Debug.Log("----------file writing triggered");
        if (myDataList._data.Length > 0)
        {

                TextWriter tw = new StreamWriter(writeFilePath, false);
                tw.WriteLine("Video ID, Answer1, Answer2, Answer3");        //headings
                tw.Close();
            
            tw = new StreamWriter(writeFilePath, true);
            for (int i = 0; i < myDataList._data.Length; i++)
            {
                tw.WriteLine(myDataList._data[i].vid_name + "," + myDataList._data[i].A1 + "," + myDataList._data[i].A2 + "," + myDataList._data[i].A3);            //WRITING format for the data
            }
            tw.Close();   
        }
  
    }

    public int answer_element = 0;
    public GameObject Q1, Q2, Q3;       // the panels
    public Slider slider1;       // the first Q slider 
    public Slider slider2;       // the second Q slider 
    public bool startNextStage = false;
    public void Question1()     //call this after the end of every frame
    {
        Q1.SetActive(true);
        // Time.timeScale = 0f; // Pause the game by setting time scale to 0

        Debug.Log("Question 1 triggered--------------");
        //take the experiment number and put it as an array number
        answer_element = conditionCounter;
        myDataList._data[answer_element].vid_name = trials[conditionCounter].video_id;      //add the video ID name


 
    }

    public void Question2()     //call this on the press of next button on q1
    {
        myDataList._data[answer_element].A1 = slider1.value;
        Q1.SetActive(false);

        Q2.SetActive(true);

        Debug.Log("Question TWOOO triggered--------------");

    }
    public void Question3() //call this on the press of next button on q2
    {
        myDataList._data[answer_element].A2 = slider2.value;
        Q2.SetActive(false);

        Q3.SetActive(true);

        Debug.Log("Question THREE triggered--------------");

    }
    public ToggleGroup toggleGroup;
    public void LastQuestion()
    {

        Debug.Log("Question LASSTTTT triggered--------------");


        Toggle toggle = toggleGroup.ActiveToggles().FirstOrDefault();
        myDataList._data[answer_element].A3 = toggle.GetComponentInChildren<Text>().text;


        Q3.SetActive(false);
        //resetting values
        slider1.value = 0;
        slider2.value = 0;

        //Time.timeScale = 1f; // Play the game by setting time scale to 1

        startNextStage = true;

        writeCSV();
    }

    // Input data and display
    private InputData _inputData;
    private float _leftMaxScore = 0f;
    private float _rightMaxScore = 0f;

    public bool write_data = false;     //make this true whenever writing is required

    public string filePath;
    public string name;
    private List<string> csvData = new List<string>();

    string primaryButtonState = "False";
    string secondaryButtonState = "False";
    private void UpdateInputDataDisplay()
    {
        time2 = Time.time;
        string univ_timestamp = (time2 - time1).ToString();
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string rightMaxVelocity = "";

        string triggerButtonState = "False";
        string gripButtonState = "False";

        //if (_inputData._rightController.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 rightVelocity))
        //{
        //    _rightMaxScore = Mathf.Max(rightVelocity.magnitude, _rightMaxScore);
        //    rightMaxVelocity = _rightMaxScore.ToString("F2");
        //    Debug.Log("Right Controller Max Velocity: " + rightMaxVelocity);
        //}

        //if (_inputData._rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonValue))
        //{
        //    primaryButtonState = primaryButtonValue.ToString();
        //    if (primaryButtonValue)
        //    {
        //        Debug.Log("Right Controller Primary Button Pressed");
        //    }
        //}

        //if (_inputData._rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButtonValue))
        //{
        //    secondaryButtonState = secondaryButtonValue.ToString();
        //    if (secondaryButtonValue)
        //    {
        //        Debug.Log("Right Controller Secondary Button Pressed");
        //    }
        //}

        //if (_inputData._rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButtonValue))
        //{
        //    triggerButtonState = triggerButtonValue.ToString();
        //    if (triggerButtonValue)
        //    {
        //        Debug.Log("Right Controller Trigger Button Pressed");
        //    }
        //}

        //if (_inputData._rightController.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButtonValue))
        //{
        //    gripButtonState = gripButtonValue.ToString();
        //    if (gripButtonValue)
        //    {
        //        Debug.Log("Right Controller Grip Button Pressed");
        //    }
        //}

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            primaryButtonState = "True";
           
            Debug.Log("Left Trigger Pressed at: " + univ_timestamp);
        }

        // Check right controller trigger button
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            secondaryButtonState = "True";
            Debug.Log("Right Trigger Pressed at: " + univ_timestamp);
        }

        if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))
        {
            primaryButtonState = "False";
            
            Debug.Log("Left Trigger Released");
        }

        // Check right controller trigger button release
        if (OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger))
        {
            secondaryButtonState = "False";
            Debug.Log("Right Trigger Released");
        }

        // Log data to CSV
        // string csvLine = $"{univ_timestamp},{rightMaxVelocity},{primaryButtonState},{secondaryButtonState},{triggerButtonState},{gripButtonState}";
        string csvLine = $"{univ_timestamp},{rightMaxVelocity},{primaryButtonState},{secondaryButtonState},{triggerButtonState},{gripButtonState}";
        csvData.Add(csvLine);

        // write a condition to create a file with the video name and push stuff in it 
        //create a time global time instance and then time stamps relevant to it

        if (write_data == true) //time to write data   a scene ends
        {
            //use a writing data function with video name

            name = myDataList._data[answer_element].vid_name;            // the current video name
            filePath = Application.dataPath + "/" + name + ".csv";

            TextWriter tw = new StreamWriter(filePath, false);
            //tw.WriteLine("Video ID, Answer1, Answer2, Answer3");        //headings
            tw.Close();

            

            File.WriteAllLines(filePath, csvData);


            //tw = new StreamWriter(writeFilePath, true);
            //for (int i = 0; i < myDataList._data.Length; i++)
            //{
            //    tw.WriteLine(myDataList._data[i].vid_name + "," + myDataList._data[i].A1 + "," + myDataList._data[i].A2 + "," + myDataList._data[i].A3);            //WRITING format for the data
            //}
            //tw.Close();
            //File.WriteAllLines(filePath, csvData); 

            write_data = false;
            csvData.Clear();            //emoptyy the file
        } 
        
    }

}

