﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class SaveUpload : MonoBehaviour {

	private FTPUtility ftpClient;
	private string fileName;
    private WorldSaveDataMesh meshingData;

    // Use this for initialization
    void Start () 
	{
		ftpClient = new FTPUtility(@"server name", "username", "password");
	}
	
	public void SaveFile(List<GameObject> pointList, List<GameObject> depthList, List<FreeformListG> freeformList, List<ContinuousListG> continuousList, string participantNumber, string testType)
	{

		fileName = "/P_" + participantNumber + "_" + testType + ".dat";

		BinaryFormatter bf = new BinaryFormatter ();

		FileStream saveFile = File.Create (Application.persistentDataPath + fileName);

		WorldSaveData data = new WorldSaveData ();

        if (testType == "scene")
        {
            if (freeformList != null)
            {
                for (int i = 0; i < freeformList.Count; i++)
                {
                    FreeformListV tempVectorList = new FreeformListV();
                    string objectTag = freeformList[i].objectTag;
                    data.freeformPointLocationList.Add (tempVectorList);
                    for (int j = 0; j < freeformList[i].freeformList.Count; j++) 
                    {
                        PositionV pos = new PositionV (freeformList[i].freeformList[j].transform.position.x, freeformList[i].freeformList[j].transform.position.y, freeformList[i].freeformList[j].transform.position.z);
                        data.freeformPointLocationList[i].freeformList.Add (pos);
                        data.freeformPointLocationList[i].objectTag = objectTag;
                    }
                }   
            }

            if (continuousList != null)
            {
                for (int i = 0; i < continuousList.Count; i++)
                {
                    ContinuousListV tempVectorList = new ContinuousListV();
                    string wallTag = continuousList[i].wallTag;
                    data.continuousPointLocationList.Add (tempVectorList);
                    for (int j = 0; j < continuousList[i].continuousGList.Count; j++) 
                    {
                        PositionV pos = new PositionV (continuousList[i].continuousGList[j].transform.position.x, continuousList[i].continuousGList[j].transform.position.y, continuousList[i].continuousGList[j].transform.position.z);
                        data.continuousPointLocationList[i].continuousList.Add(pos);
                        data.continuousPointLocationList[i].wallTag = wallTag;
                    }
                }   
            }
        }

        if (freeformList != null && testType == "Task2Box" || testType == "Task3BoxWall" || testType == "Task4Walls")
		{
			for (int i = 0; i < freeformList.Count; i++)
			{
				FreeformListV tempVectorList = new FreeformListV();
				data.freeformPointLocationList.Add (tempVectorList);
				for (int j = 0; j < freeformList[i].freeformList.Count; j++) 
				{
					PositionV pos = new PositionV (freeformList[i].freeformList[j].transform.position.x, freeformList[i].freeformList[j].transform.position.y, freeformList[i].freeformList[j].transform.position.z);
					data.freeformPointLocationList[i].freeformList.Add (pos);
				}
			}	
		}

		if (continuousList != null && testType == "Task4Walls" || testType == "Task1Plane")
		{
			for (int i = 0; i < continuousList.Count; i++)
			{
				ContinuousListV tempVectorList = new ContinuousListV();
				data.continuousPointLocationList.Add (tempVectorList);
				for (int j = 0; j < continuousList[i].continuousGList.Count; j++) 
				{
					PositionV pos = new PositionV (continuousList[i].continuousGList[j].transform.position.x, continuousList[i].continuousGList[j].transform.position.y, continuousList[i].continuousGList[j].transform.position.z);
					data.continuousPointLocationList[i].continuousList.Add(pos);
				}
			}	
		}

		bf.Serialize (saveFile, data);

		saveFile.Close ();

		UploadFile ();

	}

    public void SaveFileQuest(List<GameObject> pointList, List<GameObject> depthList, List<FreeformListG> freeformList, List<ContinuousListG> continuousList, string participantNumber, string testType, ControllerListT controllerLocations, float timeToComplete)
    {

        fileName = "/P_" + participantNumber + "_" + testType + ".dat";

        BinaryFormatter bf = new BinaryFormatter();

        FileStream saveFile = File.Create(Application.persistentDataPath + fileName);

        WorldSaveData data = new WorldSaveData();

        if (testType == "scene")
        {
            if (freeformList != null)
            {
                for (int i = 0; i < freeformList.Count; i++)
                {
                    FreeformListV tempVectorList = new FreeformListV();
                    string objectTag = freeformList[i].objectTag;
                    data.freeformPointLocationList.Add(tempVectorList);
                    for (int j = 0; j < freeformList[i].freeformList.Count; j++)
                    {
                        PositionV pos = new PositionV(freeformList[i].freeformList[j].transform.position.x, freeformList[i].freeformList[j].transform.position.y, freeformList[i].freeformList[j].transform.position.z);
                        data.freeformPointLocationList[i].freeformList.Add(pos);
                        data.freeformPointLocationList[i].objectTag = objectTag;
                    }
                }
            }

            if (continuousList != null)
            {
                for (int i = 0; i < continuousList.Count; i++)
                {
                    ContinuousListV tempVectorList = new ContinuousListV();
                    string wallTag = continuousList[i].wallTag;
                    data.continuousPointLocationList.Add(tempVectorList);
                    for (int j = 0; j < continuousList[i].continuousGList.Count; j++)
                    {
                        PositionV pos = new PositionV(continuousList[i].continuousGList[j].transform.position.x, continuousList[i].continuousGList[j].transform.position.y, continuousList[i].continuousGList[j].transform.position.z);
                        data.continuousPointLocationList[i].continuousList.Add(pos);
                        data.continuousPointLocationList[i].wallTag = wallTag;
                    }
                }
            }
        }

        if (freeformList != null && testType == "Task2Box" || testType == "Task3BoxWall" || testType == "Task4Walls")
        {
            for (int i = 0; i < freeformList.Count; i++)
            {
                FreeformListV tempVectorList = new FreeformListV();
                data.freeformPointLocationList.Add(tempVectorList);
                for (int j = 0; j < freeformList[i].freeformList.Count; j++)
                {
                    PositionV pos = new PositionV(freeformList[i].freeformList[j].transform.position.x, freeformList[i].freeformList[j].transform.position.y, freeformList[i].freeformList[j].transform.position.z);
                    data.freeformPointLocationList[i].freeformList.Add(pos);
                }
            }
        }

        if (continuousList != null && testType == "Task4Walls" || testType == "Task1Plane")
        {
            for (int i = 0; i < continuousList.Count; i++)
            {
                ContinuousListV tempVectorList = new ContinuousListV();
                data.continuousPointLocationList.Add(tempVectorList);
                for (int j = 0; j < continuousList[i].continuousGList.Count; j++)
                {
                    PositionV pos = new PositionV(continuousList[i].continuousGList[j].transform.position.x, continuousList[i].continuousGList[j].transform.position.y, continuousList[i].continuousGList[j].transform.position.z);
                    data.continuousPointLocationList[i].continuousList.Add(pos);
                }
            }
        }

        if (controllerLocations != null)
        {
            data.controllerPositions.leftControllerLocation = controllerLocations.leftControllerLocation;
            data.controllerPositions.rightControllerLocation = controllerLocations.rightControllerLocation;
        }
        else
        {
            print("controllers are empty");
        }

        data.timeToComplete = timeToComplete;

        bf.Serialize(saveFile, data);

        saveFile.Close();

        UploadFile();

    }

    public void SaveFileQuestMeshing(List<MeshFilter> listOfMeshes)
    {
        meshingData = new WorldSaveDataMesh();

        if (listOfMeshes != null)
        {
            foreach (var item in listOfMeshes)
            {
                string materialName = item.gameObject.GetComponent<MeshRenderer>().sharedMaterial.name;
                if (materialName == "MeshMaterial_None" || materialName == "MeshMaterial_Seat" || materialName == "MeshMaterial_Table")
                {
                    MeshList mesh = new MeshList();
                    mesh.meshData = SCR_MeshSerializer.WriteMesh(item.mesh, true);
                    meshingData.listOfMeshData.Add(mesh);
                }
            }
        }
        
    }

    public void SaveFileQuestMeshingFinal(string participantNumber, string testType, ControllerListT controllerLocations, float timeToComplete)
    {

        fileName = "/P_" + participantNumber + "_" + testType + ".dat";

        BinaryFormatter bf = new BinaryFormatter();

        FileStream saveFile = File.Create(Application.persistentDataPath + fileName);

        if (meshingData == null)
        {
            meshingData = new WorldSaveDataMesh();
        }

        if (controllerLocations != null)
        {
            meshingData.controllerPositions.leftControllerLocation = controllerLocations.leftControllerLocation;
            meshingData.controllerPositions.rightControllerLocation = controllerLocations.rightControllerLocation;
        }
        else
        {
            print("controllers are empty");
        }

        meshingData.timeToComplete = timeToComplete;

        bf.Serialize(saveFile, meshingData);

        saveFile.Close();

        UploadFile();

    }

    void UploadFile()
	{
		print ("made it to UploadFile");
		string location = Application.persistentDataPath + fileName;
		ftpClient.upload(fileName, @location);
	}

}
