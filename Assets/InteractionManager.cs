using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionManager : MonoBehaviour {

    public Camera camera;
    private Transform TransformofObjectClicked;
    private List<Transform> objectsTransform = new List<Transform>();
    private List<Renderer> objectsRender = new List<Renderer>();
    private List<GameObject> objectsSelected = new List<GameObject>();
    private List<Color> objectsOldColor = new List<Color>();

    private Transform objectTransform;
    private Renderer objectRender;
    private GameObject objectSelected;
    private Color objectOldColor;
    private Color selectedColor = Color.green;

    private List<GameObject> shapeList = new List<GameObject>();
    private int numShapes = 0;
    private int numShapesSelected = 0;
    private bool manipulateFast = false;

    void ClearClickedObjectBuffer()
    {
        TransformofObjectClicked = null;
        objectRender = null;
        objectSelected = null;
        objectOldColor = Color.white;
    }

    // Use this for initialization
    void Start () {
        ClearClickedObjectBuffer();
	}
	
	// Update is called once per frame
	void Update () {

        EventHandleSpaceBarPress();
        EventHandleLeftMouseClick();
        ManipulateSelectedObjects();
        EventHandleDeleteKeyPress();

    }

    //todo:  commit this project to github before adding the color buttons in order to test version control
    //todo:add change color buttons (buttons to up/down RGB values)


    void EventHandleSpaceBarPress()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            numShapes++;
            shapeList.Add(GameObject.CreatePrimitive(PrimitiveType.Cube));
            shapeList[numShapes - 1].name = (numShapes - 1).ToString();
            shapeList[numShapes - 1].transform.position = new Vector3(0, 1, 0);
            print("Shape number " + numShapes.ToString() + " added");
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
                for (int i = 0;i < numShapesSelected;i++)
                {
                    DeleteSingleObject(objectsSelected[i].name);
                }
                ClearSelectedObjectList(false);  //false means don't try to change the objects' color, b/c they don't exist anymore
            }
        }
    }

    void DeleteSingleObject(string objectName)
    {
        int deleteLocation = int.Parse(objectName);
        GameObject objToDestroy = shapeList[deleteLocation];
        shapeList.RemoveAt(deleteLocation);
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
                }
                if (Input.GetKey("a"))
                {
                    objectsTransform[i].eulerAngles += Vector3.left;
                }
                if (Input.GetKey("w"))
                {
                    objectsTransform[i].eulerAngles += Vector3.up;
                }
                if (Input.GetKey("s"))
                {
                    objectsTransform[i].eulerAngles += Vector3.down;
                }
                if (Input.GetKey("q"))
                {
                    objectsTransform[i].eulerAngles += Vector3.forward;
                }
                if (Input.GetKey("e"))
                {
                    objectsTransform[i].eulerAngles += Vector3.back;
                }
            }
            else
            {
                if (Input.GetKey("d"))
                {
                    objectsTransform[i].position += new Vector3(0.1f, 0, 0);
                }
                if (Input.GetKey("a"))
                {
                    objectsTransform[i].position += new Vector3(-0.1f, 0, 0);
                }
                if (Input.GetKey("w"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, 0.0f, 0.1f);
                }
                if (Input.GetKey("s"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, 0.0f, -0.1f);
                }
                if (Input.GetKey("q"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, 0.1f, 0.0f);
                }
                if (Input.GetKey("e"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, -0.1f, 0.0f);
                }
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                objectsTransform[i].localScale += new Vector3(0.1f, 0, 0);
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                objectsTransform[i].localScale += new Vector3(-0.1f, 0, 0);
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, 0, 0.1f);
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, 0, -0.1f);
            }
            if (Input.GetKey(KeyCode.PageUp))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, 0.1f, 0);
            }
            if (Input.GetKey(KeyCode.PageDown))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, -0.1f, 0);
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
                }
                if (Input.GetKeyDown("a"))
                {
                    objectsTransform[i].eulerAngles += Vector3.left;
                }
                if (Input.GetKeyDown("w"))
                {
                    objectsTransform[i].eulerAngles += Vector3.up;
                }
                if (Input.GetKeyDown("s"))
                {
                    objectsTransform[i].eulerAngles += Vector3.down;
                }
                if (Input.GetKeyDown("q"))
                {
                    objectsTransform[i].eulerAngles += Vector3.forward;
                }
                if (Input.GetKeyDown("e"))
                {
                    objectsTransform[i].eulerAngles += Vector3.back;
                }
            }
            else
            {
                if (Input.GetKeyDown("d"))
                {
                    objectsTransform[i].position += new Vector3(0.1f, 0, 0);
                }
                if (Input.GetKeyDown("a"))
                {
                    objectsTransform[i].position += new Vector3(-0.1f, 0, 0);
                }
                if (Input.GetKeyDown("w"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, 0.0f, 0.1f);
                }
                if (Input.GetKeyDown("s"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, 0.0f, -0.1f);
                }
                if (Input.GetKeyDown("q"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, 0.1f, 0.0f);
                }
                if (Input.GetKeyDown("e"))
                {
                    objectsTransform[i].position += new Vector3(0.0f, -0.1f, 0.0f);
                }
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                objectsTransform[i].localScale += new Vector3(0.1f, 0, 0);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                objectsTransform[i].localScale += new Vector3(-0.1f, 0, 0);
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, 0, 0.1f);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, 0, -0.1f);
            }
            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, 0.1f, 0);
            }
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                objectsTransform[i].localScale += new Vector3(0.0f, -0.1f, 0);
            }
        }

    }

    public void ToggleManipulateSpeed()
    {
        manipulateFast = !manipulateFast;
    }
}
