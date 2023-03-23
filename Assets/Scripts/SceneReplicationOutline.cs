using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

//[ExecuteInEditMode]
public class SceneReplicationOutline : MonoBehaviour
{

    private FTPUtility ftpClient;

    [SerializeField] private string fileName = "/P1_Task1Plane.dat";
    [SerializeField] private GameObject meshPrefab;

    private GameObject groupGO;
    private GameObject tempObject;

    //public Mesh tempMesh;

    public Material planeMaterial;
    public Material nonTwoSidedMaterial;
    public float timeToComplete;

    // Use this for initialization
    void Start()
    {
        ftpClient = new FTPUtility(@"server name", "username", "password");
        groupGO = new GameObject("GroupObject");
        groupGO.transform.position = Vector3.zero;
        DownloadFile();
    }

    public void DownloadFile()
    {

        //downloads the file from the FTP server and stores it at default location
        string location = Application.persistentDataPath + fileName;
        ftpClient.download(fileName, @location);

        LoadFile();
    }

    //loads the data from the file.
    void LoadFile()
    {
        if (File.Exists(Application.persistentDataPath + fileName))
        {

            BinaryFormatter bf = new BinaryFormatter();
            FileStream saveFile = File.Open(Application.persistentDataPath + fileName, FileMode.Open);
            WorldSaveData worldData = new WorldSaveData();
            worldData = (WorldSaveData)bf.Deserialize(saveFile);
            saveFile.Close();

            timeToComplete = worldData.timeToComplete;

        }
    }
}