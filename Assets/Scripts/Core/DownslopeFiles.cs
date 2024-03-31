using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;


namespace Wozware.Downslope
{
	public static class DownslopeFiles
	{
		#region Events

		#endregion

		#region Public Members

		#endregion

		#region Private Members

		#endregion

		#region Public Methods

		public static void CheckLocalPersistentDirectoryStructure()
		{
			string mapDir = GetPersistentMapsFolderPath();
			if (!Directory.Exists(mapDir))
			{
				Directory.CreateDirectory(mapDir);
			}
		}

		public static void SaveMapToFile(MapFileData fileData, string fileName)
		{
			FileStream mapFile;
			BinaryFormatter formatter = new BinaryFormatter();

			string path = GetPersistentMapPath(fileName);

			if (File.Exists(path))
			{
				Debug.Log($"DownslopeFiles: SaveMapToFile: Overwriting existing Map File.");
				mapFile = File.Open(path, FileMode.Open);
			}
			else
			{
				Debug.Log($"DownslopeFiles: SaveMapToFile: Creating new Map File.");
				mapFile = File.Create(path);
			}

			formatter.Serialize(mapFile, fileData);
			mapFile.Close();
		}

		public static bool TryLoadMapFromFile(string fileName, out MapFileData mapFileData)
		{
			FileStream mapFile;
			BinaryFormatter formatter = new BinaryFormatter();
			mapFileData = new MapFileData();

			string path = GetPersistentMapPath(fileName);

			if (File.Exists(path))
			{
				Debug.Log($"DownslopeFiles: LoadMapFromFile: Map Save File Exists");
				mapFile = File.Open(path, FileMode.Open);
				mapFileData = (MapFileData)formatter.Deserialize(mapFile);
				mapFile.Close();
				return true;
			}

			Debug.Log($"DownslopeFiles: LoadMapFromFile: No Map at {path} exists.");
			return false;
		}

		public static bool TryDeleteMapFromPersistentPath(string fileName)
		{
			string path = GetPersistentMapPath(fileName);

			if (File.Exists(path))
			{
				Debug.Log($"DownslopeFiles: DeleteMapFromPersistentPath: Deleting map {fileName}.dsmap");
				File.Delete(path);
				return true;
			}

			Debug.Log($"DownslopeFiles: DeleteMapFromPersistentPath: No Map at PersistentPath {fileName}.dsmap exists.");
			return false;
		}

		public static string GetPersistentMapPath(string fileName)
		{
			return $"{GetPersistentMapsFolderPath()}{fileName}.{FilePathData.EXT_MAPS}";
		}

		public static MapFileData GetMapDataFromPath(string path)
		{
			FileStream mapFile;
			BinaryFormatter formatter = new BinaryFormatter();
			MapFileData data = new MapFileData();

			if (File.Exists(path))
			{
				Debug.Log($"DownslopeFiles: GetMapDataFromPath: Map File Exists");
				mapFile = File.Open(path, FileMode.Open);
				data = (MapFileData)formatter.Deserialize(mapFile);
				mapFile.Close();
				return data;
			}
			else
			{
				Debug.Log($"DownslopeFiles: GetMapDataFromPath: No Map at {path} exists.");
			}

			return data;
		}

		public static string GetPersistentMapsFolderPath()
		{
			return $"{FilePathData.PERSISTENT_PATH}/{FilePathData.FOLDER_MAPS}/";
		}

		public static string[] GetAllPersistentMapPaths()
		{
			string[] files = System.IO.Directory.GetFiles(GetPersistentMapsFolderPath(), $"*.{FilePathData.EXT_MAPS}");
			return files;
		}

		public static void SaveControlOverrides(Dictionary<string, string> data)
		{
			string path = $"{FilePathData.PERSISTENT_PATH}/{FilePathData.FILENAME_CNTRLS}.{FilePathData.EXT_CNTRLS}";
			FileStream file = new FileStream(path, FileMode.OpenOrCreate);
			BinaryFormatter bf = new BinaryFormatter();
			bf.Serialize(file, data);
			file.Close();
		}

		public static Dictionary<string, string> LoadControlOverrides()
		{
			string path = $"{FilePathData.PERSISTENT_PATH}/{FilePathData.FILENAME_CNTRLS}.{FilePathData.EXT_CNTRLS}";
			Dictionary<string, string> data = new();

			if (!File.Exists(path))
			{
				return data;
			}

			FileStream file = new FileStream(path, FileMode.OpenOrCreate);
			BinaryFormatter bf = new BinaryFormatter();
			data = bf.Deserialize(file) as Dictionary<string, string>;
			file.Close();

			foreach (var item in data)
			{
				string[] split = item.Key.Split(new string[] { " : " }, StringSplitOptions.None);
				Guid id = Guid.Parse(split[0]);
				int index = int.Parse(split[1]);
				DownslopeInput.ApplyBindingOverride(id, index, item.Value);
			}

			return data;
		}

		#endregion

		#region Private Methods

		#endregion
	}
}

