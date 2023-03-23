using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARFreeRoamQuestSupport : MonoBehaviour {

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
    [SerializeField] private Text freeformTagAddedText;
    [SerializeField] private Text freeformInputTextBox;
    [SerializeField] private Text participantNumberDescription;
    [SerializeField] private InputField freeformInputBox;
    [SerializeField] private GameObject verticalPlanePanel;
	[SerializeField] private GameObject DepthButtonsCanvas;
	[SerializeField] private GameObject mainMenuPanel;
	[SerializeField] private GameObject cubePlaneMenuPanel;
	[SerializeField] private GameObject freeformMenuPanel;
	[SerializeField] private GameObject taskSelectionPanel;
	[SerializeField] private GameObject verticalFreeformPanel;
	[SerializeField] private GameObject participantNumberPanel;
    [SerializeField] private GameObject freeformTagPanel;
	[SerializeField] private GameObject planePanelSaveButton;
	[SerializeField] private GameObject freeformPanelSaveButton;
	[SerializeField] private GameObject planePanelConfirmButton;
	[SerializeField] private GameObject freeformPanelConfirmButton;
    [SerializeField] private GameObject freeformTagPanelConfirmButton;
    [SerializeField] private GameObject undoButtonFreeform;
    [SerializeField] private GameObject undoButtonPlanes;
    [SerializeField] private GameObject undoButtonControllers;
    [SerializeField] private GameObject returnToMenuButtonFreeform;
    [SerializeField] private GameObject returnToMenuButtonPlanes;
    [SerializeField] private GameObject returnToMenuButtonControllers;
    [SerializeField] private GameObject returnToTechniqueSelectButton;

    [SerializeField] private ARPlaneManager planeDisplay;
	[SerializeField] private ARPointCloudManager pointDisplay;

    [SerializeField] private Text featurePointTogglePlaneText;
    [SerializeField] private Text featurePointToggleFreeformText;

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
        featurePointTogglePlaneText.color = Color.red;
        featurePointToggleFreeformText.color = Color.red;
        participantNumber = ParticipantNumberTracker.instance.participantNumber;
        participantNumberDescription.text = "Participant " + participantNumber;
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
			PlaneSelectionLoop ();
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
            currentCoroutine = FreeformPlaceMarkers();
            StartCoroutine (currentCoroutine);
		} 
		else
		{
            currentCoroutine = ContinuousPlanePlaceMarkers();
            StartCoroutine (currentCoroutine);
		}

	}

    /*
	//takes a AR point and result type and checks for colision. Returns true and sets the marker height.
	bool HitTestWithResultType (ARPoint point, ARHitTestResultType resultTypes, out Vector3 hitLocation)
	{
		List<ARHitTestResult> hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface ().HitTest (point, resultTypes);
		if (hitResults.Count > 0) {
			foreach (var hitResult in hitResults) {				
				hitLocation = UnityARMatrixOps.GetPosition (hitResult.worldTransform);
				return true;
			}
		}
		hitLocation = Vector3.zero;
		return false;
	}
    */


	void PlaneSelectionLoop()
	{
		if (Input.touchCount > 0 && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
		{
			var touch = Input.GetTouch(0);
			if (touch.phase == TouchPhase.Began)
			{

                if (m_RaycastManager.Raycast(touch.position, arHits, TrackableType.PlaneWithinPolygon))
                {
                    var hitLocation = arHits[0].pose;
                    bPlaneSelected = true;
                    markerHeight = hitLocation.position.y;
                }
			}
		}
	}

		
	void CreateMarker(Vector3 atPosition)
	{

		marker = (GameObject)Instantiate (markerPrefab, atPosition, Quaternion.identity);
		markerTotalCount++;
		markerGroupCount++;
		markerList.Add (marker);

		if (markerGroupCount <= 1)
		{
			currentGuideLineRenderer = (GameObject)Instantiate (guideLineRendererPrefab, marker.transform.position, marker.transform.rotation);
			guideLineRendererComponent = currentGuideLineRenderer.GetComponent<LineRenderer> ();
			guideLineRendererComponent.SetPosition (0, currentGuideLineRenderer.transform.position);
			guideFloorMarker1 = marker;
		} 
		else if(markerGroupCount == 2)
		{
			guideLineRendererComponent.SetPosition (1, marker.transform.position);
			guideFloorMarker2 = marker;

			//convert markers to 2D. Check if the second marker is not to the left of the first.
			if(IsLeft(new Vector2(markerList[markerTotalCount - 1].transform.position.x, markerList[markerTotalCount - 1].transform.position.z) , new Vector2(markerList[markerTotalCount - 2].transform.position.x, markerList[markerTotalCount - 2].transform.position.z)))
			{
				//if it isn't we swap them in the list
				Swap(markerList, markerTotalCount - 2, markerTotalCount - 1);
			}
		}

		props.SetColor("_Color", materialColour);

		MeshRenderer renderer = marker.GetComponent<MeshRenderer>();

		renderer.SetPropertyBlock(props);

	}

	/// <summary>
	/// Used when needing to create freeform markers. Could this be made universal?
	/// </summary>
	/// <param name="atPosition">At position.</param>
	void CreateMarkerFreeform(Vector3 atPosition)
	{
		marker = (GameObject)Instantiate (markerPrefab, atPosition, Quaternion.identity);
		freeformPlacedMarkerCountCurrent++;
		freeformPlacedMarkerCountTotal++;
		markerFreeformList [freeformObjectCount].freeformList.Add(marker);

		if (freeformPlacedMarkerCountCurrent == 1)
		{
			currentGuideLineRenderer = (GameObject)Instantiate (guideLineRendererPrefab, marker.transform.position, marker.transform.rotation);
			guideLineRendererComponent = currentGuideLineRenderer.GetComponent<LineRenderer> ();
            marker.tag = "MarkerFirst";
		} 
		else
		{
			guideLineRendererComponent.positionCount += 1;
		}
			
		guideLineRendererComponent.SetPosition (freeformPlacedMarkerCountCurrent - 1, markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - 1].transform.position);

		props.SetColor("_Color", materialColour);

		MeshRenderer renderer = marker.GetComponent<MeshRenderer>();

		renderer.SetPropertyBlock(props);
	}

	void CreateMarkerContinuous(Vector3 atPosition)
	{
		marker = (GameObject)Instantiate (markerPrefab, atPosition, Quaternion.identity);
		markerContinuousCountCurrent++;
		markerContinuousCountTotal++;
		markerContinuousList [continuousObjectCount].continuousGList.Add (marker);

		if (markerContinuousCountCurrent == 1)
		{
			currentGuideLineRenderer = (GameObject)Instantiate (guideLineRendererPrefab, marker.transform.position, marker.transform.rotation);
			guideLineRendererComponent = currentGuideLineRenderer.GetComponent<LineRenderer> ();
		} 
		else
		{
			guideLineRendererComponent.positionCount += 1;
		}

		guideLineRendererComponent.SetPosition (markerContinuousCountCurrent - 1, markerContinuousList[continuousObjectCount].continuousGList[markerContinuousCountCurrent - 1].transform.position);

		props.SetColor("_Color", materialColour);

		MeshRenderer renderer = marker.GetComponent<MeshRenderer>();

		renderer.SetPropertyBlock(props);
	}


    //Coroutine to deal with making 3D objects, not walls
	IEnumerator FreeformPlaceMarkers()
	{
        if (!bFreeformUndoFromVertical)
        {
            currentFreeformList = null;
            currentFreeformList = new FreeformListG();
            markerFreeformList.Add(currentFreeformList);
            freeformPlacedMarkerCountCurrent = 0;
            freeformObjectCount++;
            freeformPanelConfirmButton.SetActive(false);
            undoButtonFreeform.SetActive(true);
        }
        else
        {
            //guideLineRendererComponent.positionCount--;
            bFreeformUndoFromVertical = false;
        }

        bCreatingFreeformBase = true;

        freeformDescription.text = "Aim using the cube. Tap screen to place each marker cube.";

        while (bCreatingFreeformBase)
		{
            //Vector2 screenPosition = mainCamera.ScreenToViewportPoint (new Vector3 (Screen.width / 2, Screen.height / 2, mainCamera.transform.position.z));
            Vector2 screenPosition = new Vector2(Screen.width / 2, Screen.height / 2);

            if (m_RaycastManager.Raycast(screenPosition, arHits, trackableTypeToUse))
            {
                // Raycast hits are sorted by distance, so the first one
                // will be the closest hit.
                var hitLocation = arHits[0].pose;

				spawnLocation = new Vector3 (hitLocation.position.x, markerHeight, hitLocation.position.z);

				if (guideMarker == null)
				{
					guideMarker = Instantiate (markerPrefab, spawnLocation, Quaternion.identity);
					guideMarker.GetComponent<Renderer> ().material.color = guideColour;
				} 
				else
				{
					guideMarker.SetActive (true);
					guideMarker.transform.position = spawnLocation;
					if (guideLineRendererComponent && freeformPlacedMarkerCountCurrent != 0)
					{
						guideLineRendererComponent.SetPosition (freeformPlacedMarkerCountCurrent, guideMarker.transform.position);
					}
				}
			}
			else
			{
				guideMarker.SetActive (false);
			}
			
			

            //deals with checking if the marker is near the first placed. If so it automatically closes the loop.
            bool bFoundNearbyStartMarker = false;

			if (Input.touchCount > 0 && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject (Input.GetTouch (0).fingerId))
			{
                var touch = Input.GetTouch(0);

                Collider[] listOfFreeFormCurrentColliders = Physics.OverlapSphere(spawnLocation, 0.05f);

                if (touch.phase == TouchPhase.Began && listOfFreeFormCurrentColliders.Length >= 1)
                {
                    foreach (Collider collider in listOfFreeFormCurrentColliders)
                    {
                        if (collider.CompareTag("MarkerFirst"))
                        {
                            guideLineRendererComponent.SetPosition(freeformPlacedMarkerCountCurrent, markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - freeformPlacedMarkerCountCurrent].transform.position);
                            bFoundNearbyStartMarker = true;
                            //collider.tag = "Marker";
                            bCreatingFreeformBase = false;
                            //break;
                        }
                    }

                    if (touch.phase == TouchPhase.Began && !bFoundNearbyStartMarker)
                    {
                        CreateMarkerFreeform(new Vector3(spawnLocation.x, spawnLocation.y, spawnLocation.z));
                        returnToMenuButtonFreeform.SetActive(false);
                    }

                }
            }

            if (bUndoActive && freeformPlacedMarkerCountCurrent > 0)
            {
                bUndoActive = false;
                freeformPlacedMarkerCountCurrent--;
                freeformPlacedMarkerCountTotal--;
                GameObject markerToDelete = markerFreeformList[freeformObjectCount].freeformList[markerFreeformList[freeformObjectCount].freeformList.Count - 1];
                markerFreeformList[freeformObjectCount].freeformList.RemoveAt(markerFreeformList[freeformObjectCount].freeformList.Count - 1);
                Destroy(markerToDelete);
                guideLineRendererComponent.positionCount -= 1;
            }

            if (freeformPlacedMarkerCountCurrent <= 0)
            {
                returnToMenuButtonFreeform.SetActive(true);
            }

            if (bReturnToMenu)
            {
                markerFreeformList.Remove(currentFreeformList);
                bCreatingFreeformBase = false;
                bReturnToMenu = false;
                freeformObjectCount--;
                freeformMenuPanel.SetActive(false);
                taskSelectionPanel.SetActive(true);
                returnToMenuButtonFreeform.SetActive(false);
                StopCoroutine(currentCoroutine);
            }

            /*
			if (freeformPlacedMarkerCountCurrent >= maxSideCount) 
			{
				guideLineRendererComponent.SetPosition (freeformPlacedMarkerCountCurrent, markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - freeformPlacedMarkerCountCurrent].transform.position);
				bCreatingFreeformBase = false;
			}*/

            yield return null;
		}

		currentCoroutine = CreateFreeformVerticalPlane ();
		StartCoroutine (currentCoroutine);

	}

	IEnumerator CreateFreeformVerticalPlane()
	{

        GameObject verticalCurrentGuideLineRenderer;
        LineRenderer verticalguideLineRendererComponent;

        cubeLineList = new List<LineRenderer>();

        bCreatingFreeformVertical = true;
        //undoButtonFreeform.SetActive(false);
        //freeformPanelConfirmButton.SetActive (true);

        freeformDescription.text = "Tap <b>Up</b> or <b>Down</b> to adjust height. Tap <b>Complete Section</b> when done";

        verticalCurrentGuideLineRenderer = (GameObject)Instantiate (guideLineRendererPrefab, markerFreeformList[freeformObjectCount].freeformList[0].transform.position, markerFreeformList[freeformObjectCount].freeformList[0].transform.rotation);
        verticalguideLineRendererComponent = verticalCurrentGuideLineRenderer.GetComponent<LineRenderer> ();
        verticalguideLineRendererComponent.positionCount = freeformPlacedMarkerCountCurrent + 1;

        int topMarkerCount = freeformPlacedMarkerCountCurrent;

		//print ("Before 4 extra markers");
		//create markers for top vertices
        for (int i = topMarkerCount; i > 0; i--)
		{
            marker = (GameObject)Instantiate (markerPrefab, markerFreeformList[freeformObjectCount].freeformList[topMarkerCount - i].transform.position, Quaternion.identity);
			markerFreeformList [freeformObjectCount].freeformList.Add(marker);
			GameObject tempLineRenderer = (GameObject)Instantiate (guideLineRendererPrefab, marker.transform.position, marker.transform.rotation);
			cubeLineList.Add (tempLineRenderer.GetComponent<LineRenderer>());
		}

        freeformPlacedMarkerCountCurrent += topMarkerCount;
        freeformPlacedMarkerCountTotal += topMarkerCount;

		//print ("before extra line renderer");
		//set positions for linerenderer on top vertices
        for (int i = 0; i < topMarkerCount; i++)
		{
            verticalguideLineRendererComponent.SetPosition (i, markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - (topMarkerCount - i)].transform.position);
		}

		//print ("after extra line renderer");

        wallHeightLocation = markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - topMarkerCount].transform.position.y;

        //final linerenderer point goes back to the first marker
        verticalguideLineRendererComponent.SetPosition (topMarkerCount, markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - topMarkerCount].transform.position);
			
		verticalFreeformPanel.SetActive (true);

		//print ("before main loop");

		//update vertical height based on button presses
		while (bCreatingFreeformVertical)
		{
				
			if (bMovingVerticalUp)
			{
				wallHeightLocation += 0.3f * Time.deltaTime;
			}

			if (bMovingVerticalDown)
			{
				wallHeightLocation -= 0.3f * Time.deltaTime;
			}

			//print ("updating marker height");
			//update height position of top markers
            for (int i = topMarkerCount; i > 0; i--)
			{
				markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - i].transform.position = new Vector3 (markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - i].transform.position.x, wallHeightLocation, markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - i].transform.position.z);
			}

			//print ("updating linerenderer positions");
			//set positions for linerenderer on top vertices
            for (int i = 0; i < topMarkerCount; i++)
			{
                //shouldn't this just be (topMarkerCount - i)
                verticalguideLineRendererComponent.SetPosition (i, markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - (topMarkerCount - i)].transform.position);
			}

            //final linerenderer point goes back to the first marker
            verticalguideLineRendererComponent.SetPosition (topMarkerCount, markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - topMarkerCount].transform.position);

			//change i

			//print ("updating downward line renderers");
            for (int i = 0; i < topMarkerCount; i++)
			{
                cubeLineList [cubeLineList.Count - (topMarkerCount) + i].SetPosition (0, markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - (topMarkerCount - i)].transform.position);
                cubeLineList [cubeLineList.Count - (topMarkerCount) + i].SetPosition (1, markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - freeformPlacedMarkerCountCurrent + i].transform.position);
			}

			//print ("Finished updating downward line renderers");

			if (bMovingVerticalUp)
			{
				freeformPanelConfirmButton.SetActive (true);
			}

			if (bCompleteSection)
			{
				verticalFreeformPanel.SetActive (false);
                verticalguideLineRendererComponent = null;
				bCreatingFreeformVertical = false;
                bSelectingFreeformTag = true;
                bCompleteSection = false;
				freeformPanelConfirmButton.SetActive (false);
			}

            if (bUndoActive)
            {
                for (int i = 0; i < topMarkerCount; i++)
                {
                    var tempMarker = markerFreeformList[freeformObjectCount].freeformList[markerFreeformList[freeformObjectCount].freeformList.Count - 1];
                    markerFreeformList[freeformObjectCount].freeformList.Remove(tempMarker);
                    Destroy(tempMarker);
                }

                int verticalLineRendererCount = cubeLineList.Count;

                for (int i = 0; i < verticalLineRendererCount; i++)
                {
                    var tempLineRenderer = cubeLineList[cubeLineList.Count - 1];
                    cubeLineList.Remove(tempLineRenderer);
                    Destroy(tempLineRenderer);
                }

                Destroy(verticalCurrentGuideLineRenderer);

                verticalFreeformPanel.SetActive(false);
                freeformPanelConfirmButton.SetActive(false);

                freeformPlacedMarkerCountCurrent -= topMarkerCount;
                freeformPlacedMarkerCountTotal -= topMarkerCount;

                bCreatingFreeformVertical = false;

                bUndoActive = false;
                bFreeformUndoFromVertical = true;

                StopCoroutine(currentCoroutine);
                currentCoroutine = FreeformPlaceMarkers();
                StartCoroutine(currentCoroutine);
            }

            yield return null;

		}

		freeformMenuPanel.SetActive (false);
		freeformTagPanel.SetActive (true);

        currentCoroutine = FreeformTagSelection();
        StartCoroutine(currentCoroutine);


    }

    IEnumerator FreeformTagSelection()
    {
        while (bSelectingFreeformTag)
        {
            if (bCompleteSection)
            {
                markerFreeformList[freeformObjectCount].objectTag = currentTag;
                bSaveMode = true;
                bCompleteSection = false;
                bSelectingFreeformTag = false;
                freeformTagAddedText.text = "";
                freeformInputTextBox.text = "";
                freeformInputBox.text = "";
                freeformTagPanelConfirmButton.SetActive(false);
            }

            yield return null;
        }

        freeformTagPanel.SetActive(false);
        taskSelectionPanel.SetActive(true);
    }


    /// <summary>
    /// This is used to create continous ground markers for walls. Once user is finished placing ground markers they tap the Complete Section button
    /// this then moves the state to the vertical markers.
    /// </summary>
    IEnumerator ContinuousPlanePlaceMarkers()
	{
        if (!bPlaneUndoFromVertical)
        {
            currentContinuousList = null;
            currentContinuousList = new ContinuousListG();
            markerContinuousList.Add(currentContinuousList);
            //planePanelConfirmButton.SetActive(true);
            markerContinuousCountCurrent = 0;
            guideLineRendererComponent = null;
            continuousObjectCount++;
            undoButtonPlanes.SetActive(true);
        }
        else
        {
            guideLineRendererComponent.positionCount++;
            bPlaneUndoFromVertical = false;
        }

        bCreatingContinuousPlanes = true;

        cubePlaneDescription.text = "Aim using the cube. Tap screen to place a marker in corners. Press <b>Complete Section</b> when done.";

        while (bCreatingContinuousPlanes)
		{
            //Vector2 screenPosition = mainCamera.ScreenToViewportPoint (new Vector3 (Screen.width / 2, Screen.height / 2, mainCamera.transform.position.z));
            Vector2 screenPosition = new Vector2(Screen.width / 2, Screen.height / 2);

            if (m_RaycastManager.Raycast(screenPosition, arHits, trackableTypeToUse))
            {
                // Raycast hits are sorted by distance, so the first one
                // will be the closest hit.

                if (arHits.Count >= 1)
                {
                    var hitLocation = arHits[0].pose;
                    spawnLocation = new Vector3(hitLocation.position.x, markerHeight, hitLocation.position.z);
                }

				if (guideMarker == null)
				{
					guideMarker = Instantiate (markerPrefab, spawnLocation, Quaternion.identity);
					guideMarker.GetComponent<Renderer> ().material.color = guideColour;
				} 
				else
				{
					guideMarker.SetActive (true);
					guideMarker.transform.position = spawnLocation;
					if (guideLineRendererComponent && markerContinuousCountCurrent != 0)
					{
						guideLineRendererComponent.SetPosition (markerContinuousCountCurrent, guideMarker.transform.position);
					}
				}         
			}
			else
			{
                if (guideMarker != null)
                {
                    guideMarker.SetActive(false);
                }
			}


			if (Input.touchCount > 0 && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject (Input.GetTouch (0).fingerId))
			{
				var touch = Input.GetTouch (0);
				if (touch.phase == TouchPhase.Began)
				{
					CreateMarkerContinuous (new Vector3 (spawnLocation.x, spawnLocation.y, spawnLocation.z));
                    planePanelConfirmButton.SetActive(true);
                    returnToMenuButtonPlanes.SetActive(false);
                }
			}

            if (bUndoActive && markerContinuousCountCurrent > 0)
            {
                bUndoActive = false;
                markerContinuousCountCurrent--;
                markerContinuousCountTotal--;
                GameObject markerToDelete = markerContinuousList[continuousObjectCount].continuousGList[markerContinuousList[continuousObjectCount].continuousGList.Count - 1];
                markerContinuousList[continuousObjectCount].continuousGList.RemoveAt(markerContinuousList[continuousObjectCount].continuousGList.Count - 1);
                Destroy(markerToDelete);
                guideLineRendererComponent.positionCount -= 1;
            }

            if (bCompleteSection) 
			{
				guideLineRendererComponent.positionCount--;
				bCompleteSection = false;
				bCreatingContinuousPlanes = false;
			}

            if (markerContinuousCountCurrent <= 0)
            {
                returnToMenuButtonPlanes.SetActive(true);
                planePanelConfirmButton.SetActive(false);
            }

            if (bReturnToMenu)
            {
                markerContinuousList.Remove(currentContinuousList);
                bCreatingContinuousPlanes = false;
                bReturnToMenu = false;
                continuousObjectCount--;
                cubePlaneMenuPanel.SetActive(false);
                taskSelectionPanel.SetActive(true);
                returnToMenuButtonPlanes.SetActive(false);
                StopCoroutine(currentCoroutine);
            }

            yield return null;
		}

		currentCoroutine = CreateContinuousVerticalPlane ();
		StartCoroutine (currentCoroutine);

	}

	IEnumerator CreateContinuousVerticalPlane()
	{
        GameObject verticalCurrentGuideLineRenderer;
        LineRenderer verticalguideLineRendererComponent;

        bCreatingContinuousVertical = true;
        //undoButtonPlanes.SetActive(false);

        cubeLineList = new List<LineRenderer> ();

		cubePlaneDescription.text = "Tap <b>Up</b> or <b>Down</b> to adjust wall height. Tap <b>Complete Section</b> when done";

		int startOfCurrentCount = markerContinuousCountTotal - markerContinuousCountCurrent;
        int topMarkerCount = markerContinuousCountCurrent;

        //int halfwayPoint = markerContinuousCountCurrent / 2;

        verticalCurrentGuideLineRenderer = (GameObject)Instantiate (guideLineRendererPrefab, markerContinuousList[continuousObjectCount].continuousGList[0].transform.position, markerContinuousList[continuousObjectCount].continuousGList[0].transform.rotation);
        verticalguideLineRendererComponent = verticalCurrentGuideLineRenderer.GetComponent<LineRenderer> ();
        verticalguideLineRendererComponent.positionCount = markerContinuousCountCurrent;

		//print ("Before 4 extra markers");
		//create markers for top vertices
		for (int i = markerContinuousCountCurrent; i > 0; i--)
		{
			marker = (GameObject)Instantiate (markerPrefab, markerContinuousList[continuousObjectCount].continuousGList[markerContinuousCountCurrent - i].transform.position, Quaternion.identity);
			markerContinuousList[continuousObjectCount].continuousGList.Add(marker);
			GameObject tempLineRenderer = (GameObject)Instantiate (guideLineRendererPrefab, marker.transform.position, marker.transform.rotation);
			cubeLineList.Add (tempLineRenderer.GetComponent<LineRenderer>());
		}

		markerContinuousCountTotal += markerContinuousCountCurrent;
		markerContinuousCountCurrent += markerContinuousCountCurrent;

		int halfwayPoint = markerContinuousCountCurrent / 2;

		//print ("before extra line renderer");
		//set positions for linerenderer on top vertices
		for (int i = 0; i < halfwayPoint; i++)
		{
            verticalguideLineRendererComponent.SetPosition (i, markerContinuousList[continuousObjectCount].continuousGList[(markerContinuousCountCurrent - 1) - i].transform.position);
		}

		//print ("after extra line renderer");

		wallHeightLocation = markerContinuousList[continuousObjectCount].continuousGList[0].transform.position.y; 

		//final linerenderer point goes back to the first marker
		//guideLineRendererComponent.SetPosition (maxSideCount, markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - maxSideCount].transform.position);

		verticalPlanePanel.SetActive (true);

		//print ("before main loop");

		//update vertical height based on button presses
		while (bCreatingContinuousVertical)
		{

			if (bMovingVerticalUp)
			{
				wallHeightLocation += 0.3f * Time.deltaTime;
			}

			if (bMovingVerticalDown)
			{
				wallHeightLocation -= 0.3f * Time.deltaTime;
			}

			//print ("updating marker height");
			//update height position of top markers
			for (int i = halfwayPoint; i > 0; i--)
			{
				markerContinuousList[continuousObjectCount].continuousGList[markerContinuousCountCurrent - i].transform.position = new Vector3 (markerContinuousList[continuousObjectCount].continuousGList[markerContinuousCountCurrent - i].transform.position.x, wallHeightLocation, markerContinuousList[continuousObjectCount].continuousGList[markerContinuousCountCurrent - i].transform.position.z);
			}

			//print ("updating linerenderer positions");
			//set positions for linerenderer on top vertices
			for (int i = 0; i < halfwayPoint; i++)
			{
                verticalguideLineRendererComponent.SetPosition (i, markerContinuousList[continuousObjectCount].continuousGList[(markerContinuousCountCurrent - 1) - i].transform.position);
			}

			//final linerenderer point goes back to the first marker
			//guideLineRendererComponent.SetPosition (maxSideCount, markerFreeformList[freeformObjectCount].freeformList[freeformPlacedMarkerCountCurrent - maxSideCount].transform.position);

			//change i

			//print ("updating downward line renderers");
			for (int i = 0; i < halfwayPoint; i++)
			{
				cubeLineList [cubeLineList.Count - (halfwayPoint) + i].SetPosition (0, markerContinuousList[continuousObjectCount].continuousGList[(markerContinuousCountCurrent - 1) - i].transform.position);
				cubeLineList [cubeLineList.Count - (halfwayPoint) + i].SetPosition (1, markerContinuousList[continuousObjectCount].continuousGList[(markerContinuousCountCurrent - halfwayPoint - 1) - i].transform.position);
			}

			//print ("Finished updating downward line renderers");

			if (bCompleteSection)
			{
				bCompleteSection = false;
				verticalPlanePanel.SetActive (false);
				guideLineRendererComponent = null;
				bCreatingContinuousVertical = false;
                markerContinuousList[continuousObjectCount].wallTag = "Wall " + continuousObjectCount;
				bSaveMode = true;
			}

            if (bUndoActive)
            {
                for (int i = 0; i < topMarkerCount; i++)
                {

                    var tempMarker = markerContinuousList[continuousObjectCount].continuousGList[markerContinuousList[continuousObjectCount].continuousGList.Count - 1];
                    markerContinuousList[continuousObjectCount].continuousGList.Remove(tempMarker);
                    Destroy(tempMarker);
                }

                int verticalLineRendererCount = cubeLineList.Count;

                for (int i = 0; i < verticalLineRendererCount; i++)
                {
                    var tempLineRenderer = cubeLineList[cubeLineList.Count - 1];
                    cubeLineList.Remove(tempLineRenderer);
                    Destroy(tempLineRenderer);
                }

                Destroy(verticalCurrentGuideLineRenderer);

                verticalPlanePanel.SetActive(false);

                markerContinuousCountCurrent -= topMarkerCount;
                markerContinuousCountTotal -= topMarkerCount;

                bCreatingContinuousVertical = false;

                bUndoActive = false;
                bPlaneUndoFromVertical = true;

                StopCoroutine(currentCoroutine);
                currentCoroutine = ContinuousPlanePlaceMarkers();
                StartCoroutine(currentCoroutine);
            }

            yield return null;

		}

		cubePlaneMenuPanel.SetActive (false);
		taskSelectionPanel.SetActive (true);

	}

    //Quest Support - Creates Controller Location
    IEnumerator ControllerLocationPlaceMarkers()
    {
        bool bLeftCreated = false;
        bool bRightCreated = false;
        GameObject[] controllerMarkerList = new GameObject[2];
        bCompleteSection = false;
        controllersPanelConfirmButton.SetActive(false);
        returnToMenuButtonControllers.SetActive(true);
        controllerLocations = new ControllerListT();

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
        controllersPanelConfirmButton.SetActive(false);
        controllersMenuPanel.SetActive(false);
        taskSelectionPanel.SetActive(true);
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
					currentTask = Tool.Walls;
					PlaneCubeModeSelected ();
					break;
				}
			case 2:
				{
                    currentTask = Tool.Objects;
					FreeformModeSelected ();
					break;
				}
            case 3:
                {
                    currentTask = Tool.Controllers;
                    ControllerLocationModeSelected();
                    break;
                }
        }
	}

	public void PlaneCubeModeSelected()
	{
		mainMenuPanel.SetActive (false);
		participantNumberPanel.SetActive (false);
		freeformMenuPanel.SetActive (false);
		cubePlaneMenuPanel.SetActive (true);
        bTiming = true;


        if (bFirstTimeUse)
		{
			StartCoroutine (SelectPlane ());
		} 
		else
		{
			currentCoroutine = ContinuousPlanePlaceMarkers ();
			StartCoroutine (currentCoroutine);
		}
	}

	public void FreeformModeSelected()
	{
		mainMenuPanel.SetActive (false);
		cubePlaneMenuPanel.SetActive (false);
		freeformMenuPanel.SetActive (true);
		bCreatingFreeformBase = true;
        bTiming = true;

        if (bFirstTimeUse)
		{
			StartCoroutine (SelectPlane());
		} 
		else
		{
			currentCoroutine = FreeformPlaceMarkers ();
			StartCoroutine (currentCoroutine);
		}
	}

    //Quest Support
    public void ControllerLocationModeSelected()
    {
        mainMenuPanel.SetActive(false);
        cubePlaneMenuPanel.SetActive(false);
        freeformMenuPanel.SetActive(false);
        controllersMenuPanel.SetActive(true);
        //bCreatingFreeformBase = true;
        bTiming = true;

        currentCoroutine = ControllerLocationPlaceMarkers();
        StartCoroutine(currentCoroutine);
    }

    public void CompleteContinuousSection()
	{
		bCompleteSection = true;
	}

	public void PressedMoveVerticalMarkerUp()
	{
		bMovingVerticalUp = true;
	}

	public void ReleaseMoveVerticalMarkerUp()
	{
		bMovingVerticalUp = false;
	}

	public void PressedMoveVerticalMarkerDown()
	{
		bMovingVerticalDown = true;
	}

	public void ReleasedMoveVerticalMarkerDown()
	{
		bMovingVerticalDown = false;
	}

	public void PressedMoveDepthMarkerIn()
	{
		bMovingDepthIn = true;
	}

	public void ReleasedMoveDepthMarkerIn()
	{
		bMovingDepthIn = false;
	}

	public void PressedMoveDepthMarkerOut()
	{
		bMovingDepthOut = true;
	}

	public void ReleasedMoveDepthMarkerOut()
	{
		bMovingDepthOut = false;
	}

	public void SaveDataButton()
	{
        bTiming = false;
        saveUpload.SaveFileQuest(markerList, markerCubeList, markerFreeformList, markerContinuousList, participantNumber, "scene", controllerLocations, timeToComplete);
		bSaveMode = false;
        ParticipantNumberTracker.instance.bReturningToMenu = true;
        returnToTechniqueSelectButton.SetActive(true);
    }

    public void ChangeScene(string newScene)
    {
        SceneManager.LoadScene(newScene);
    }

    public void ConfirmParticipantNumber(string pNumber)
	{
		participantNumber = pNumber;
	}

    public void CustomFreeformTagAdded(string newTag)
    {
        currentTag = newTag;
        freeformTagAddedText.text = newTag + " Tag Added.";
        freeformTagPanelConfirmButton.SetActive(true);
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

	/// <summary>
	/// Clears out any markers on screen. Stops the current coroutine. Starts from scratch again.
	/// </summary>
	public void UndoLastMarker()
	{
		int numberToClear;

		StopCoroutine (currentCoroutine);

		if ((markerTotalCount % 4) == 0)
		{			
			verticalPlanePanel.SetActive (false);
			bCreatingVerticalPlanes = false;
			markerTotalCount -= 4;
			numberToClear = 4;
		} 
		else if (markerGroupCount == 2)
		{
			markerTotalCount -= 2;
			numberToClear = 2;
		} 
		else
		{
			markerTotalCount--;
			numberToClear = 1;
		}

		markerGroupCount = 0;

		if (marker != null)
		{
			for (int i = 0; i < numberToClear; i++)
			{
				var tmp = markerList[markerList.Count - 1];
				markerList.RemoveAt (markerList.Count - 1);
				Destroy (tmp);
			}

			Destroy (currentGuideLineRenderer);
			Destroy (marker);
		}

		//currentCoroutine = PlaceMarker ();
		StartCoroutine (currentCoroutine);
	}



}
