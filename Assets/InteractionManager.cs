using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;
using System;
using UnityEngine.UI;

public class InteractionManager : MonoBehaviour {

    //todo:  commit this project to github before adding the color buttons in order to test version control
    //todo:add change color buttons (buttons to up/down RGB values)
    //todo: save current frame, insert as next frame, insert after frame X, insert frame at end
    //todo: if changes made and click pre or next arrows ask if save or lose changes
    //todo: create play animation button

    public Camera camera;
    public InputField animationFolderInput;
    public InputField msDelayToNextFrame;
    public Text folderNotExistTextElement;
    public Button clearNoticeButton;
    public Button SaveNewFrameAtEndButton;
    public Button OverwriteFrameButton;
    public Button PreviousFrameButton;
    public Button NextFrameButton;
    private Transform TransformofObjectClicked;
    private List<Transform> objectsTransform = new List<Transform>();
    private List<Renderer> objectsRender = new List<Renderer>();
    private List<GameObject> objectsSelected = new List<GameObject>();
    private List<Color> objectsOldColor = new List<Color>();
    private List<string> animationFilePaths = new List<string>();

    private Transform objectTransform;
    private Renderer objectRender;
    private GameObject objectSelected;
    private Color objectOldColor;
    private Color selectedColor = Color.green;

    private List<GameObject> shapeList = new List<GameObject>();
    private List<string> shapeTypeList = new List<string>();
    private int numShapes = 0;
    private int numShapesSelected = 0;
    private int numAnimationFiles = 0;
    private int currentFrameNumber = 0;
    private bool manipulateFast = false;
    private bool changesMadeToFrame = false;
    private DirectoryInfo workingDirectory = null;
    private string oldAnimationFolderText;

    void Start () {
        SaveNewFrameAtEndButton.gameObject.SetActive(false);
        OverwriteFrameButton.gameObject.SetActive(false);
        ClearAndInvisErrorText();
        ClearClickedObjectBuffer();
	}
	
	void Update () {
        UpdateButtonVisibility();
        EventHandleSpaceBarPress();
        EventHandleLeftMouseClick();
        ManipulateSelectedObjects();
        EventHandleDeleteKeyPress();
    }

    private void UpdateButtonVisibility()
    {
        if (workingDirectory != null)
        {
            SaveNewFrameAtEndButton.gameObject.SetActive(workingDirectory.Exists);
        }
        OverwriteFrameButton.gameObject.SetActive(numAnimationFiles > 0); //Active if there are any files, inactive if no files exist
        NextFrameButton.gameObject.SetActive(currentFrameNumber < (numAnimationFiles - 1)); //Active if not at the end frame
        PreviousFrameButton.gameObject.SetActive(currentFrameNumber != 0); //Active if not at beginning frame
    }

    public void UpdateAnimationFiles(bool resetViewToFirstFrame = false)
    {
        // https://www.youtube.com/watch?v=6bVcLSZWqK8
        // https://docs.microsoft.com/en-us/dotnet/api/system.io.file?view=netframework-4.7.2
        workingDirectory = new DirectoryInfo(Application.dataPath + "/Animation/" + animationFolderInput.text + "/");
        if (workingDirectory.Exists)
        {
            SaveNewFrameAtEndButton.gameObject.SetActive(true);
            OverwriteFrameButton.gameObject.SetActive(numAnimationFiles > 0);
            oldAnimationFolderText = animationFolderInput.text;
            FileInfo[] fileInfoList = workingDirectory.GetFiles("*.txt");
            animationFilePaths.Clear();
            numAnimationFiles = fileInfoList.Length;

            if (resetViewToFirstFrame)
            {
                currentFrameNumber = 0;
            }

            for (int i = 0; i < numAnimationFiles; i++)
            {
                animationFilePaths.Add(fileInfoList[i].FullName);
            }

            LoadFrameFromFile(currentFrameNumber);
        }
        else
        {
            numAnimationFiles = 0;
            SetErrorText("FOLDER DOESNT EXIST!");
            //clearNoticeButton.interactable = false;   //this is how you grey out a button
            //animationFolderInput.text = oldAnimationFolderText;
        }
    }

    public void ClearAndInvisErrorText()
    {
        folderNotExistTextElement.enabled = false;   //make text invisible
        clearNoticeButton.gameObject.SetActive(false);   //make the button invisible and noninteractable
    }

    public void PrevFrameButtonPress()
    {
        if (changesMadeToFrame)
        {
            SetErrorText("CLICK SAVE BUTTONS OR LOSE CHANGES NEXT TIME!");
            changesMadeToFrame = false;
        }
        else
        {
            if (currentFrameNumber > 0)
            {
                currentFrameNumber--;
                LoadFrameFromFile(currentFrameNumber);
            }
            else
            {
                SetErrorText("ERROR: NOT ABLE TO MOVE TO PREV FRAME!");
            }
        }
    }

    //NEED TO MAKE NextFrameButtonPress METHOD!!!!!
    //NEED TO MAKE NextFrameButtonPress METHOD!!!!!
    //NEED TO MAKE NextFrameButtonPress METHOD!!!!!

    public void SaveNewFrameAtEnd()  //invoked when SaveNewFrameAtEnd Button is pressed
    {
        SaveCurrentChangesToFrameFile(numAnimationFiles);
    }

    private void SaveCurrentChangesToFrameFile(int framenumber)
    {
        string filePath = Application.dataPath + "/Animation/" + animationFolderInput.text + "/" + framenumber.ToString() + ".txt";
        if (!File.Exists(filePath))
        {
            string textToWrite = GetStringOfAllObjectInfo();
            if (textToWrite != "ERROR")
            {
                File.WriteAllText(filePath, textToWrite);   // Creates the file
                UpdateAnimationFiles();
                changesMadeToFrame = false;
            }
        }
    }

    private void LoadFrameFromFile(int FrameNumberToLoad)
    {
        DeleteAllExistingObjects();
        numShapes = 0;

        bool loadFrameError = false;
        string allFileContents;
        string[] arrayOfObjects, parametersForEachObject;
        int delayToNextFrame = 0;
   
        allFileContents = "";
        try
        {
            allFileContents = File.ReadAllText(animationFilePaths[FrameNumberToLoad]);
        }
        catch (Exception e)
        {
            loadFrameError = true;
            SetErrorText("FILE ERROR LOADING FRAME #" + FrameNumberToLoad.ToString());
            Debug.LogException(e, this);
        }

        if ((allFileContents != "") && (!loadFrameError))
        {
            arrayOfObjects = allFileContents.Split("***************".ToCharArray());
            delayToNextFrame = PullIntValueWithStringKeyFromStringBody(":msdelaytonextframe", arrayOfObjects);
            msDelayToNextFrame.text = delayToNextFrame.ToString();

            for (int objectNum = 1; objectNum < arrayOfObjects.Length; objectNum++)  //start at 1 b/c 0 is the framedelay string
            {
                if ((arrayOfObjects[objectNum].Length > 1) && (!loadFrameError))  //if not an empty line from the previous parse
                {
                    parametersForEachObject = arrayOfObjects[objectNum].Split(System.Environment.NewLine.ToCharArray());
                    string shapeType = "";
                    int shapeNumber = 0;
                    float positionX = 0.0f;
                    float positionY = 0.0f;
                    float positionZ = 0.0f;
                    float rotationX = 0.0f;
                    float rotationY = 0.0f;
                    float rotationZ = 0.0f;
                    float rotationW = 0.0f;
                    float localScaleX = 0.0f;
                    float localScaleY = 0.0f;
                    float localScaleZ = 0.0f;

                    try
                    {
                        shapeType = PullStringValueWithStringKeyFromStringBody(":shapetype", parametersForEachObject);
                        shapeNumber = PullIntValueWithStringKeyFromStringBody(":shapenumber", parametersForEachObject);
                        positionX = PullFloatValueWithStringKeyFromStringBody(":positionx", parametersForEachObject);
                        positionY = PullFloatValueWithStringKeyFromStringBody(":positiony", parametersForEachObject);
                        positionZ = PullFloatValueWithStringKeyFromStringBody(":positionz", parametersForEachObject);
                        rotationX = PullFloatValueWithStringKeyFromStringBody(":rotationx", parametersForEachObject);
                        rotationY = PullFloatValueWithStringKeyFromStringBody(":rotationy", parametersForEachObject);
                        rotationZ = PullFloatValueWithStringKeyFromStringBody(":rotationz", parametersForEachObject);
                        rotationW = PullFloatValueWithStringKeyFromStringBody(":rotationw", parametersForEachObject);
                        localScaleX = PullFloatValueWithStringKeyFromStringBody(":localScalex", parametersForEachObject);
                        localScaleY = PullFloatValueWithStringKeyFromStringBody(":localScaley", parametersForEachObject);
                        localScaleZ = PullFloatValueWithStringKeyFromStringBody(":localScalez", parametersForEachObject);
                    }
                    catch (Exception e)
                    {
                        loadFrameError = true;
                        SetErrorText("MALFORMED PARAMETERS IN FRAME FILE!");
                        Debug.LogException(e, this);
                    }

                    if (!loadFrameError)
                    {
                        if (shapeType == "cube")
                        {
                            if (numShapes == shapeNumber)  //shapenumber accounting between file and numShapes holder
                            {
                                numShapes++;
                                shapeTypeList.Add(shapeType);
                                shapeList.Add(GameObject.CreatePrimitive(PrimitiveType.Cube));
                                shapeList[numShapes - 1].name = (numShapes - 1).ToString();
                                shapeList[numShapes - 1].transform.position = new Vector3(positionX, positionY, positionZ);
                                shapeList[numShapes - 1].transform.rotation = new Quaternion(rotationX, rotationY, rotationZ, rotationW);
                                shapeList[numShapes - 1].transform.localScale = new Vector3(localScaleX, localScaleY, localScaleZ);
                                print("Shape number " + (numShapes - 1).ToString() + " added");
                            }
                            else
                            {
                                loadFrameError = true;
                                SetErrorText("Shape num mismatch found in frame! (shape#" + shapeNumber.ToString() + "should be #" + numShapes.ToString() + ")");
                            }
                        }
                        else
                        {
                            loadFrameError = true;
                            SetErrorText("Unsupported shape type found in frame! (shape#" + shapeNumber.ToString() + ")");
                        }
                    }
                }
            }
        }        
    }

    int PullIntValueWithStringKeyFromStringBody(string stringKey, string[] entireStringBody)
    {
        int parameterIndex = FindIndexInParameterArrayWithStringKey(stringKey, entireStringBody);
        //Find which parameter line contains the stringkey you're looking for

        return int.Parse(entireStringBody[parameterIndex].Substring(0, entireStringBody[parameterIndex].IndexOf(stringKey)));
        //pull and return the number that occurs before the string key on that particular line of the file
    }

    string PullStringValueWithStringKeyFromStringBody(string stringKey, string[] entireStringBody)
    {
        int parameterIndex = FindIndexInParameterArrayWithStringKey(stringKey, entireStringBody);
        //Find which parameter line contains the stringkey you're looking for

        return entireStringBody[parameterIndex].Substring(0, entireStringBody[parameterIndex].IndexOf(stringKey));
        //pull and return the string that occurs before the string key on that particular line of the file
    }

    float PullFloatValueWithStringKeyFromStringBody(string stringKey, string[] entireStringBody)
    {
        int parameterIndex = FindIndexInParameterArrayWithStringKey(stringKey, entireStringBody);
        //Find which parameter line contains the stringkey you're looking for

        return float.Parse(entireStringBody[parameterIndex].Substring(0, entireStringBody[parameterIndex].IndexOf(stringKey)));
        //pull and return the number that occurs before the string key on that particular line of the file
    }

    int FindIndexInParameterArrayWithStringKey(string stringKey, string[] objectParameterArray)
    {
        //each index in objectParameterArray is a single line in the file
        //Find and return which line contains the stringkey you're looking for

        int indexOfKey;
        for (int i = 0; i < objectParameterArray.Length; i++)
        {
            indexOfKey = (objectParameterArray[i].IndexOf(stringKey));
            if (indexOfKey > -1)
            {
                return i;
            }
        }
        return -1;
    }

    private void DeleteAllExistingObjects()
    {
        SelectAllObjects();
        DeleteAllSelectedObjects();
    }

    private void SelectAllObjects()
    {
        ClearClickedObjectBuffer();
        for (int i=0;i<numShapes;i++)
        {
            //select each object as if you were clicking on it
            objectSelected = shapeList[i];
            TransformofObjectClicked = objectSelected.transform;
            objectRender = objectSelected.GetComponent<Renderer>();
            objectOldColor = objectRender.material.color;
            AddObjectToSelectedList();
        }
    }

    public void ToggleManipulateSpeed()
    {
        manipulateFast = !manipulateFast;
    }

    void SetErrorText(string errorText)
    {
        folderNotExistTextElement.enabled = true;   //make the text visible
        folderNotExistTextElement.text = errorText;
        clearNoticeButton.gameObject.SetActive(true);  //make the button visible
    }

    void ClearClickedObjectBuffer()
    {
        TransformofObjectClicked = null;
        objectRender = null;
        objectSelected = null;
        objectOldColor = Color.white;
    }

    void EventHandleSpaceBarPress()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            numShapes++;
            shapeTypeList.Add("cube");
            shapeList.Add(GameObject.CreatePrimitive(PrimitiveType.Cube));
            shapeList[numShapes - 1].name = (numShapes - 1).ToString();
            shapeList[numShapes - 1].transform.position = new Vector3(0, 1, 0);
            print("Shape number " + (numShapes-1).ToString() + " added");
        }
    }

    void ManipulateSelectedObjects()
    {
        if ((TransformofObjectClicked != null) && (objectSelected.name != "Terrain"))
        {
            if (manipulateFast)
            {
                ManipulateObjectsQuickly();
            }
            else
            {
                ManipulateObjectsSlowly();
            }

            for (int i=0; i<numShapesSelected; i++)
            {
                objectsTransform[i].localScale = new Vector3(Mathf.Abs(objectsTransform[i].localScale.x), Mathf.Abs(objectsTransform[i].localScale.y), Mathf.Abs(objectsTransform[i].localScale.z));  //ensure that scale values don't go negative. strange stuff happens if that were allowed.
            }
        }
    }

    void EventHandleDeleteKeyPress()
    {
        if ((TransformofObjectClicked != null) && (objectSelected.name != "Terrain"))
        {
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                DeleteAllSelectedObjects();
            }
        }
    }

    void DeleteAllSelectedObjects()
    {
        for (int i = 0; i < numShapesSelected; i++)
        {
            DeleteSingleObject(objectsSelected[i].name);
        }
        ClearSelectedObjectList(false);  //false means don't try to change the objects' color, b/c they don't exist anymore
    }

    void DeleteSingleObject(string objectName)
    {
        int deleteLocation = int.Parse(objectName);
        GameObject objToDestroy = shapeList[deleteLocation];
        shapeList.RemoveAt(deleteLocation);
        shapeTypeList.RemoveAt(deleteLocation);
        numShapes--;
        GameObject.Destroy(objToDestroy);
        for (int i = deleteLocation; i < numShapes; i++)
        {
            shapeList[i].name = i.ToString();
        }
        print("Shape number " + deleteLocation.ToString() + " deleted");
    }

    void EventHandleLeftMouseClick()   //handle object selections from user with the left mouse click
    {
        bool LeftMousebuttonDownOnObject = Input.GetMouseButtonDown(0);
        var currentSelection = EventSystem.current.currentSelectedGameObject;
        if (currentSelection && currentSelection.GetComponent<IPointerDownHandler>() != null) //If a GUI element was clicked
        {
            LeftMousebuttonDownOnObject = false;  //Then don't register it as an object-selecting mouse click
        }
        if (LeftMousebuttonDownOnObject)
        {
            //remove the commented stuff here once you've completed the selected list stuff
            /*if (objectRender != null)
            {
                objectRender.material.color = objectOldColor;
            } */
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                TransformofObjectClicked = hit.transform;
                objectSelected = TransformofObjectClicked.gameObject;
                //print("Clicked on " + objectSelected.name);

                if (objectSelected.name == "Terrain")  //User clicked on a valid object
                {
                    ClearClickedObjectBuffer();
                }
                else
                {
                    objectRender = objectSelected.GetComponent<Renderer>();
                    objectOldColor = objectRender.material.color;
                }
            }
            else  //Left mouse click didn't intersect any objects
            {
                ClearClickedObjectBuffer();
            }

            if (TransformofObjectClicked != null) //if an object was actually clicked
            {
                if (Input.GetKey(KeyCode.LeftControl))  //If Left Control key is held down 
                {
                    bool isObjectAlreadySelected = false;
                    for (int i = 0; i < numShapesSelected; i++)
                    {
                        if (objectsSelected[i].name == objectSelected.name) //clicked object is already selected and is now left-ctrl clicked
                        {
                            isObjectAlreadySelected = true;
                            //remove object from selected list
                            objectsRender[i].material.color = objectsOldColor[i];
                            objectsTransform.RemoveAt(i);
                            objectsSelected.RemoveAt(i);
                            objectsRender.RemoveAt(i);
                            objectsOldColor.RemoveAt(i);
                            numShapesSelected--;
                            i = numShapesSelected; //object already found, finish this loop
                        }
                    }
                    if (isObjectAlreadySelected == false)  //selected object not in existing selected list
                    {
                        AddObjectToSelectedList();
                    }
                }
                else   //single object was clicked without left ctrl
                {
                    ClearSelectedObjectList();
                    AddObjectToSelectedList();
                }

            }
            else   //left clicked on no object
            {
                if (Input.GetKey(KeyCode.LeftControl)) //If Left Control key is held down 
                {
                    //left ctrl clicked nothing, so...do nothing I guess?
                }
                else   //just a simple left click on nothing. sky, or terrain
                {
                    ClearSelectedObjectList();
                }
            }
        }
    }

    void AddObjectToSelectedList()
    {
        objectsTransform.Add(TransformofObjectClicked);
        objectsSelected.Add(objectSelected);
        objectsRender.Add(objectRender);
        objectsOldColor.Add(objectOldColor);
        objectRender.material.color = selectedColor;   //change object's color to selected color
        numShapesSelected++;
    }

    void ClearSelectedObjectList(bool revertObjectsToOldColor = true)
    {
        if (revertObjectsToOldColor)
        {
            for (int i = 0; i < numShapesSelected; i++)
            {
                objectsRender[i].material.color = objectsOldColor[i];
            }
        }
        objectsTransform.Clear();
        objectsSelected.Clear();
        objectsRender.Clear();
        objectsOldColor.Clear();
        numShapesSelected = 0;
    }

    void ManipulateObjectsQuickly()
    {
        for (int i = 0; i < numShapesSelected; i++)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetKey("d"))
                {
                    objectsTransform[i].eulerAngles += Vector3.right;
                    changesMadeToFrame = true;
                }
                if (Input.GetKey("a"))
                {
                    objectsTransform[i].eulerAngles += Vector3.left;
                    changesMadeToFrame = true;
                }
                if (Input.GetKey("w"))
                {
                    objectsTransform[i].eulerAngles += Vector3.up;
                    changesMadeToFrame = true;
                }
                if (Input.GetKey("s"))
                {
                    objectsTransform[i].eulerAngles += Vector3.down;
                    changesMadeToFrame = true;
                }
                if (Input.GetKey("q"))
                {
                    objectsTransform[i].eulerAngles += Vector3.forward;
                    changesMadeToFrame = true;
                }
                if (Input.GetKey("e"))
                {
                    objectsTransform[i].eulerAngles += Vector3.back;
                    changesMadeToFrame = true;
                }
            }
            else
            {
                if (Input.GetKey("d"))
                {
                    objectsTransform[i].position += new Vector3(0.1f, 0, 0);
                    changesMadeToFrame = true;
                }
                if (Input.GetKey("a"))
                {
                    objectsTransform[i].position += new Vector3(-0.1f, 0, 0);
                    changesMadeToFrame = true;
                }
                if (Input.GetKey("w"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, 0.0f, 0.1f);
                    changesMadeToFrame = true;
                }
                if (Input.GetKey("s"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, 0.0f, -0.1f);
                    changesMadeToFrame = true;
                }
                if (Input.GetKey("q"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, 0.1f, 0.0f);
                    changesMadeToFrame = true;
                }
                if (Input.GetKey("e"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, -0.1f, 0.0f);
                    changesMadeToFrame = true;
                }
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                objectsTransform[i].localScale += new Vector3(0.1f, 0, 0);
                changesMadeToFrame = true;
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                objectsTransform[i].localScale += new Vector3(-0.1f, 0, 0);
                changesMadeToFrame = true;
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, 0, 0.1f);
                changesMadeToFrame = true;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, 0, -0.1f);
                changesMadeToFrame = true;
            }
            if (Input.GetKey(KeyCode.PageUp))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, 0.1f, 0);
                changesMadeToFrame = true;
            }
            if (Input.GetKey(KeyCode.PageDown))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, -0.1f, 0);
                changesMadeToFrame = true;
            }
        }
    }

    void ManipulateObjectsSlowly()
    {
        for (int i=0;i<numShapesSelected;i++)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetKeyDown("d"))
                {
                    objectsTransform[i].eulerAngles += Vector3.right;
                    changesMadeToFrame = true;
                }
                if (Input.GetKeyDown("a"))
                {
                    objectsTransform[i].eulerAngles += Vector3.left;
                    changesMadeToFrame = true;
                }
                if (Input.GetKeyDown("w"))
                {
                    objectsTransform[i].eulerAngles += Vector3.up;
                    changesMadeToFrame = true;
                }
                if (Input.GetKeyDown("s"))
                {
                    objectsTransform[i].eulerAngles += Vector3.down;
                    changesMadeToFrame = true;
                }
                if (Input.GetKeyDown("q"))
                {
                    objectsTransform[i].eulerAngles += Vector3.forward;
                    changesMadeToFrame = true;
                }
                if (Input.GetKeyDown("e"))
                {
                    objectsTransform[i].eulerAngles += Vector3.back;
                    changesMadeToFrame = true;
                }
            }
            else
            {
                if (Input.GetKeyDown("d"))
                {
                    objectsTransform[i].position += new Vector3(0.1f, 0, 0);
                    changesMadeToFrame = true;
                }
                if (Input.GetKeyDown("a"))
                {
                    objectsTransform[i].position += new Vector3(-0.1f, 0, 0);
                    changesMadeToFrame = true;
                }
                if (Input.GetKeyDown("w"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, 0.0f, 0.1f);
                    changesMadeToFrame = true;
                }
                if (Input.GetKeyDown("s"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, 0.0f, -0.1f);
                    changesMadeToFrame = true;
                }
                if (Input.GetKeyDown("q"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, 0.1f, 0.0f);
                    changesMadeToFrame = true;
                }
                if (Input.GetKeyDown("e"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, -0.1f, 0.0f);
                    changesMadeToFrame = true;
                }
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                objectsTransform[i].localScale += new Vector3(0.1f, 0, 0);
                changesMadeToFrame = true;
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                objectsTransform[i].localScale += new Vector3(-0.1f, 0, 0);
                changesMadeToFrame = true;
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, 0, 0.1f);
                changesMadeToFrame = true;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, 0, -0.1f);
                changesMadeToFrame = true;
            }
            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, 0.1f, 0);
                changesMadeToFrame = true;
            }
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, -0.1f, 0);
                changesMadeToFrame = true;
            }
        }

    }

    string GetStringOfAllObjectInfo()
    {
        string holderString = "";
        Transform holderTransform;

        if (msDelayToNextFrame.text == "")
        {
            SetErrorText("MS DELAY FIELD EMPTY!");
            holderString = "ERROR";
        }
        else
        {
            holderString = holderString + int.Parse(msDelayToNextFrame.text) + ":msdelaytonextframe" + System.Environment.NewLine;
            for (int i = 0; i < numShapes; i++)
            {
                holderString = holderString + "***************" + System.Environment.NewLine;
                holderString = holderString + shapeTypeList[i] + ":shapetype" + System.Environment.NewLine;
                holderString = holderString + i.ToString() + ":shapenumber" + System.Environment.NewLine;
                holderTransform = shapeList[i].transform;
                holderString = holderString + holderTransform.position.x.ToString() + ":positionx" +  System.Environment.NewLine;
                holderString = holderString + holderTransform.position.y.ToString() + ":positiony" + System.Environment.NewLine;
                holderString = holderString + holderTransform.position.z.ToString() + ":positionz" + System.Environment.NewLine;

                holderString = holderString + holderTransform.rotation.x.ToString() + ":rotationx" +  System.Environment.NewLine;
                holderString = holderString + holderTransform.rotation.y.ToString() + ":rotationy" + System.Environment.NewLine;
                holderString = holderString + holderTransform.rotation.z.ToString() + ":rotationz" + System.Environment.NewLine;
                holderString = holderString + holderTransform.rotation.w.ToString() + ":rotationw" + System.Environment.NewLine;

                holderString = holderString + holderTransform.localScale.x.ToString() + ":localScalex" + System.Environment.NewLine;
                holderString = holderString + holderTransform.localScale.y.ToString() + ":localScaley" + System.Environment.NewLine;
                holderString = holderString + holderTransform.localScale.z.ToString() + ":localScalez" + System.Environment.NewLine;
                
                //holderString = holderString + "***************" + System.Environment.NewLine;
                //holderString = holderString + System.Environment.NewLine;
            }
        }
        
        return holderString;
    }

}
