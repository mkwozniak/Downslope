using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Wozware.Downslope
{
	struct MapChunk
	{
		public Vector3 Position;
		public string ChunkID;
	}

	class MapData
	{

	}

	public class LevelCreator : MonoBehaviour
	{
		#region Events

		#endregion

		#region Public Members
		public AssetPack Assets;

		#endregion

		#region Private Members
		private string _currentMapPath;
		#endregion

		#region Public Methods

		#endregion

		#region Private Methods

		private void SaveMapToFile()
		{
			/*
			BinaryFormatter newFormatter = new BinaryFormatter();
			FileStream saveFile = File.Open(Application.persistentDataPath + "/map0.dat", FileMode.Open);
			Debug.Log("Saving map to file.");
			GameSave data = new GameSave();

			//serialize and close
			newFormatter.Serialize(saveFile, data);
			saveFile.Close();
			*/
		}


		private void LoadMapFromFile()
		{
			/*
			if (File.Exists(Application.persistentDataPath + "/map0.dat"))
			{
				BinaryFormatter newFormatter = new BinaryFormatter();
				FileStream saveFile = File.Open(Application.persistentDataPath + "/map0.dat", FileMode.Open);
				GameSave data = (GameSave)newFormatter.Deserialize(saveFile);
				Debug.Log("Map Save File Exists, loading from it now.");
				//close
				saveFile.Close();
			}
			else
			{
				BinaryFormatter newFormatter = new BinaryFormatter();
				FileStream saveFile = File.Create(Application.persistentDataPath + "/map0.dat");
				Debug.Log("Game Save File Doesnt Exist, creating new.");
				GameSave data = new GameSave();
				//serialize and close
				newFormatter.Serialize(saveFile, data);
				saveFile.Close();
			}
			*/
		}

		#endregion

		#region Unity Methods

		private void Awake()
		{
			_currentMapPath = Application.dataPath + "/Maps/";
		}

		private void Start()
		{

		}

		private void Update()
		{

		}

		#endregion
	}
}

