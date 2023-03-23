using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation.Samples;

public class ARFreeRoamQuestSupportMeshing : MonoBehaviour {

	[SerializeField] private GameObject markerPrefab;
	[SerializeField] private GameObject guideMarkerPrefab;
	[SerializeField] private GameObject guideLineRendererPrefab;

	private float markerHeight;
	private float wallHeightLocation;

	//UI References
	[SerializeField] private Text cubePlaneDescription;
	[SerializeField] private GameObject cubePlaneEndDescription;
	[SerializeField] private Text freeformDescription;
	[SerializeField] private Text modeDescription;
    [SerializeField] private Text participantNumberDescription;
	[SerializeField] private GameObject mainMenuPanel;
	[SerializeField] private GameObject meshingMenuPanel;
	[SerializeField] private GameObject taskSelectionPanel;
    [SerializeField] private GameObject undoButtonControllers;
    [SerializeField] private GameObject returnToMenuButtonControllers;
    [SerializeField] private GameObject meshingConfirmButton;
    [SerializeField] private GameObject saveButton;
    [SerializeField] private GameObject returnToTechniqueSelectButton;

    [SerializeField] private ARPlaneManager planeDisplay;
	[SerializeField] private ARPointCloudManager pointDisplay;

    [SerializeField] private Text featurePointTogglePlaneText;
    [SerializeField] private Text featurePointToggleFreeformText;

    //Meshing
    [SerializeField] private ARMeshManager meshManager;
    [SerializeField] private MeshClassificationFracking meshFrackingScript;
    private bool bMeshingScene;
    List<MeshFilter> listOfMeshFilters = new List<MeshFilter>();

    private SaveUpload saveUpload;
	private string participantNumber;
    private string currentTag;
	private string taskType;

	[SerializeField] private GameObject focusSquare;
	//private CustomFocusSquare focusSquareScript;
	private PlaneMeshCreator planeMeshCreator;

    //ARFoundation Changes
    List<ARRaycastHit> arHits = new List<ARRaycastHit>();
    [SerializeField] ARRaycastManager m_RaycastManager;
    private TrackableType trackableTypeToUse;
    private bool bUseFeaturePoints;

    [SerializeField] private Color materialColour;
	[SerializeField] private Color guideColour;
	[SerializeField] private Material planeMaterial;

	private IEnumerator currentCoroutine;
	private MaterialPropertyBlock props;
	private Vector3 spawnLocation;

	/// <summary>
	/// Markers always go bottom right, bottom left, top right and then top left.
	/// </summary>
	private GameObject guideMarker;
	private GameObject guideFloorMarker1; //bottom right
	private GameObject guideFloorMarker2; //bottom left
	private GameObject guideWallMarker1; //top right
	private GameObject guideWallMarker2; //top left
	private GameObject marker;
	private GameObject currentGuideLineRenderer;
	private LineRenderer guideLineRendererComponent;
	private bool bPlaneSelected;
	private bool bPlacingMarkers;
	private bool bCreatingVerticalPlanes;
	private bool bCreatingDepthPlanes;
	private bool bMovingVerticalUp;
	private bool bMovingVerticalDown;
	private bool bMovingDepthIn;
	private bool bMovingDepthOut;
	private bool bDepthMode;
	private bool bFirstTimeUse = true; //only show the plane detection on first use.
	private bool bSaveMode;
	private bool bCompleteSection; //used to finish ground level plane placement.
	private bool bCreatingContinuousPlanes;
	private bool bCreatingContinuousVertical;
    private bool bUndoActive; //used to control the undo system
    private bool bFreeformUndoFromVertical;
    private bool bPlaneUndoFromVertical;
    private bool bReturnToMenu;
    private bool bMeshDataAcquired;
    private bool bControllerLocationsAcquired;

	//used for the planes
	private int markerGroupCount; //used to detect when 2 have been placed.
	private int markerTotalCount; //keeps track of how many markers created for plane List
	private Vector3 perpendicularDepth;//stores the perpendicular vector to add depth.

	private int markerContinuousCountCurrent;//current count of markers for continuous plane mode
	private int markerContinuousCountTotal;//total count of markers for continuous plane mode;

	//used for the cubes
	private int markerCubeGroupCount;//detect when the two key markers are placed
	private int markerCubeTotalCount;//keeps track of all the markers created for cubes

	//used for freeform
	private bool bCreatingFreeformBase;
	private bool bCreatingFreeformVertical;
    private bool bSelectingFreeformTag;
	private int freeformPlacedMarkerCountCurrent;
	private int freeformPlacedMarkerCountTotal;
	private int freeformObjectCount = -1;
	private int continuousObjectCount = -1;

	[SerializeField] private int maxSideCount = 4;

	private List<GameObject> markerList = new List<GameObject>();
	private List<ContinuousListG> markerContinuousList = new List<ContinuousListG>();
	private List<GameObject> markerCubeList = new List<GameObject>();
	private List<FreeformListG> markerFreeformList = new List<FreeformListG> ();
	private List<LineRenderer> cubeLineList = new List<LineRenderer>();
	private FreeformListG currentFreeformList;
	private ContinuousListG currentContinuousList;

    //Quest Support

    private ControllerListT controllerLocations;
    [SerializeField] private Text controllersDescription;
    [SerializeField] private GameObject controllersPanelConfirmButton;
    [SerializeField] private GameObject controllersMenuPanel;
    private GameObject[] controllerMarkerList = new GameObject[2];
    private bool bLeftCreated = false;
    private bool bRightCreated = false;
    private float timeToComplete;
    private bool bTiming;

    private Camera mainCamera;

	private Tool currentTask = Tool.Walls;


	// Use this for initialization
	void Start () 
	{
		mainCamera = Camera.main;
		props = new MaterialPropertyBlock ();
		saveUpload = GetComponent<SaveUpload> ();
        trackableTypeToUse = TrackableType.PlaneWithinPolygon;
        participantNumber = ParticipantNumberTracker.instance.participantNumber;
        participantNumberDescription.text = "Participant " + participantNumber;
        //featurePointTogglePlaneText.color = Color.red;
        //featurePointToggleFreeformText.color = Color.red;

    }

    void Update()
    {
        if (bTiming)
        {
            timeToComplete += Time.deltaTime;
        }
    }

    //Coroutine is used to get a plane on the floor defined by the user
    IEnumerator SelectPlane()
	{
		//focusSquare.SetActive(true);

		cubePlaneDescription.text = "Aim at the floor, move phone side to side";
		freeformDescription.text = "Aim at the floor, move phone side to side";

        yield return new WaitForSeconds(1f);

        /*
		while (focusSquareScript.SquareState != CustomFocusSquare.FocusState.Found)
		{
			yield return null;
		}
        */
		cubePlaneDescription.text = "Tap the screen to select the correct floor plane";
		freeformDescription.text = "Tap the screen to select the correct floor plane";

		while (!bPlaneSelected)
		{
			//PlaneSelectionLoop ();
			yield return null;
		}

		cubePlaneDescription.text = "Floor plane selected.";
		freeformDescription.text = "Floor plane selected.";

        //Destroy(planeDisplay);
        //pointDisplay.SetActive (false);
        //focusSquare.SetActive(false);

        planeDisplay.SetTrackablesActive(false);
        planeDisplay.planePrefab.SetActive(false);
        pointDisplay.SetTrackablesActive(false);
        pointDisplay.pointCloudPrefab.SetActive(false);


        if (bCreatingFreeformBase)
		{
            //currentCoroutine = FreeformPlaceMarkers();
            StartCoroutine (currentCoroutine);
		} 
		else
		{
            //currentCoroutine = ContinuousPlanePlaceMarkers();
            StartCoroutine (currentCoroutine);
		}

	}

    IEnumerator MeshingScene()
    {
        float timer = 0f;

        meshManager.enabled = true;
        bMeshingScene = true;
        listOfMeshFilters.Clear();

        while (bMeshingScene)
        {
            timer += Time.deltaTime;

            if (timer >= 3)
            {
                meshingConfirmButton.SetActive(true);
            }

            if (bCompleteSection)
            {
                //Return to main menu
                /*
                foreach (var item in meshFrackingScript.listOfMeshFilters)
                {
                    listOfMeshFilters.Add(item);
                }
                */

                int numberOfEntriesInDictionary = 0;

                foreach (KeyValuePair<TrackableId, MeshFilter[]> item in meshFrackingScript.m_MeshFrackingMap)
                {
                    numberOfEntriesInDictionary++;
                    for (int i = 0; i < item.Value.Length; i++)
                    {
                        listOfMeshFilters.Add(item.Value[i]);
                    }
                }

                saveUpload.SaveFileQuestMeshing(listOfMeshFilters);

                print("There are " + numberOfEntriesInDictionary.ToString() + " entries in the dictionary");

                bCompleteSection = false;
                bMeshingScene = false;
                bMeshDataAcquired = true;
                ShowSaveButton();

                meshingConfirmButton.SetActive(false);
                meshingMenuPanel.SetActive(false);
                taskSelectionPanel.SetActive(true);
            }

            yield return null;
        }

        yield return null;
    }

        //Quest Support - Creates Controller Location
    IEnumerator ControllerLocationPlaceMarkers()
    {
        bCompleteSection = false;
        controllersPanelConfirmButton.SetActive(false);
        returnToMenuButtonControllers.SetActive(true);

        if (controllerLocations == null)
        {
            controllerLocations = new ControllerListT();
        }

        controllersDescription.text = "Aim using the cube. Tap the screen to place left controller location";

        while (!bCompleteSection)
        {
            //Vector2 screenPosition = mainCamera.ScreenToViewportPoint(new Vector3(Screen.width / 2, Screen.height / 2, mainCamera.transform.position.z));
            Vector2 screenPosition = new Vector2(Screen.width / 2, Screen.height / 2);

            if (m_RaycastManager.Raycast(screenPosition, arHits, TrackableType.PlaneWithinPolygon))
            {
                // Raycast hits are sorted by distance, so the first one
                // will be the closest hit.
                var hitLocation = arHits[0].pose;

                spawnLocation = new Vector3(hitLocation.position.x, hitLocation.position.y, hitLocation.position.z);

                if (guideMarker == null)
                {
                    guideMarker = Instantiate(markerPrefab, spawnLocation, Quaternion.identity);
                    guideMarker.GetComponent<Renderer>().material.color = guideColour;
                }
                else
                {
                    guideMarker.SetActive(true);
                    guideMarker.transform.position = spawnLocation;
                }
                
            }
            else
            {
                guideMarker.SetActive(false);
            }


            if (Input.touchCount > 0 && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                var touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began && !bLeftCreated)
                {
                    marker = (GameObject)Instantiate(markerPrefab, new Vector3(spawnLocation.x, spawnLocation.y, spawnLocation.z), Quaternion.identity);
                    controllerMarkerList[0] = marker;
                    controllerLocations.leftControllerLocation = new PositionV(marker.transform.position.x, marker.transform.position.y, marker.transform.position.z);
                    bLeftCreated = true;
                    controllersDescription.text = "Aim using the cube. Tap the screen to place right controller location";
                    undoButtonControllers.SetActive(true);
                    returnToMenuButtonControllers.SetActive(false);
                }
                else if (touch.phase == TouchPhase.Began && !bRightCreated)
                {
                    marker = (GameObject)Instantiate(markerPrefab, new Vector3(spawnLocation.x, spawnLocation.y, spawnLocation.z), Quaternion.identity);
                    controllerMarkerList[1] = marker;
                    controllerLocations.rightControllerLocation = new PositionV(marker.transform.position.x, marker.transform.position.y, marker.transform.position.z);
                    bRightCreated = true;
                    controllersDescription.text = "Controller locations recorded. Now press Complete Section below";
                    controllersPanelConfirmButton.SetActive(true);
                }
            }

            if (bUndoActive)
            {
                if (bRightCreated)
                {
                    GameObject tempMarker = controllerMarkerList[1];
                    controllerMarkerList[1] = null;
                    Destroy(tempMarker);
                    controllerLocations.rightControllerLocation = null;
                    bRightCreated = false;
                    controllersDescription.text = "Aim using the cube. Tap the screen to place right controller location";
                    controllersPanelConfirmButton.SetActive(false);
                }
                else if (bLeftCreated)
                {
                    GameObject tempMarker = controllerMarkerList[0];
                    controllerMarkerList[0] = null;
                    Destroy(tempMarker);
                    controllerLocations.leftControllerLocation = null;
                    bLeftCreated = false;
                    controllersDescription.text = "Aim using the cube. Tap the screen to place left controller location";
                    undoButtonControllers.SetActive(false);
                    returnToMenuButtonControllers.SetActive(true);
                }

                bUndoActive = false;
            }

            if (bReturnToMenu)
            {
                bReturnToMenu = false;
                returnToMenuButtonControllers.SetActive(false);
                controllersMenuPanel.SetActive(false);
                StopCoroutine(currentCoroutine);
                taskSelectionPanel.SetActive(true);
            }

            yield return null;
        }

        bCompleteSection = false;
        bControllerLocationsAcquired = true;
        ShowSaveButton();

        controllersPanelConfirmButton.SetActive(false);
        controllersMenuPanel.SetActive(false);
        taskSelectionPanel.SetActive(true);
    }

    void ShowSaveButton()
    {
        if (bMeshDataAcquired && bControllerLocationsAcquired)
        {
            saveButton.SetActive(true);
        }
    }

    /// <summary>
    /// Currently Broken
    /// </summary>
    public void SetNewColour()
	{
		float r = Random.Range(0.0f, 1.0f);
		float g = Random.Range(0.0f, 1.0f);
		float b = Random.Range(0.0f, 1.0f);

		materialColour = new Color (r, g, b);
	}

	/// <summary>
	/// Functions called from UI so that held mobile buttons can be detected
	/// </summary>

	public void ToolSelected(int toolNumber)
	{
        switch(toolNumber)
		{
			case 1:
				{
					currentTask = Tool.Meshing;
                    MeshingSceneSelected();
					break;
				}
            case 2:
                {
                    currentTask = Tool.Controllers;
                    ControllerLocationModeSelected();
                    break;
                }
        }
	}

    //Quest Support

    public void MeshingSceneSelected()
    {
        mainMenuPanel.SetActive(false);
        controllersMenuPanel.SetActive(false);
        taskSelectionPanel.SetActive(false);
        meshingMenuPanel.SetActive(true);
        bTiming = true;

        currentCoroutine = MeshingScene();
        StartCoroutine(currentCoroutine);
    }

    public void ControllerLocationModeSelected()
    {
        mainMenuPanel.SetActive(false);
        meshingMenuPanel.SetActive(false);
        taskSelectionPanel.SetActive(false);
        controllersMenuPanel.SetActive(true);

        currentCoroutine = ControllerLocationPlaceMarkers();
        StartCoroutine(currentCoroutine);
    }

    public void CompleteContinuousSection()
	{
		bCompleteSection = true;
	}

	public void SaveDataButton()
	{
        bTiming = false;
        saveUpload.SaveFileQuestMeshingFinal(participantNumber, "Meshing", controllerLocations, timeToComplete);
        returnToTechniqueSelectButton.SetActive(true);
        ParticipantNumberTracker.instance.bReturningToMenu = true;
        bSaveMode = false;
	}

    public void ChangeScene(string newScene)
    {
        SceneManager.LoadScene(newScene);
    }

    public void ConfirmParticipantNumber(string pNumber)
	{
		participantNumber = pNumber;
        participantNumberDescription.text = "Participant " + participantNumber;

    }

    public void ToggleFeaturePointTracking()
    {
        if (!bUseFeaturePoints)
        {
            trackableTypeToUse = TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint;
            featurePointTogglePlaneText.color = Color.green;
            featurePointToggleFreeformText.color = Color.green;
            bUseFeaturePoints = true;
        }
        else if(bUseFeaturePoints)
        {
            trackableTypeToUse = TrackableType.PlaneWithinPolygon;
            featurePointTogglePlaneText.color = Color.red;
            featurePointToggleFreeformText.color = Color.red;
            bUseFeaturePoints = false;
        }
    }

    public void UndoMarker()
    {
        bUndoActive = true;
    }

    public void ReturnToMainMenu()
    {
        bReturnToMenu = true;
    }

    /// <summary>
    /// Swap the specified list, indexA and indexB.
    /// </summary>
    public static void Swap<T>(IList<T> list, int indexA, int indexB)
	{
		T tmp = list[indexA];
		list[indexA] = list[indexB];
		list[indexB] = tmp;
	}

	/// <summary>
	/// Determines whether this instance is left the specified A B.
	/// </summary>
	bool IsLeft(Vector2 A, Vector2 B)
	{
		return -A.x * B.y + A.y * B.x < 0;
	}

}
