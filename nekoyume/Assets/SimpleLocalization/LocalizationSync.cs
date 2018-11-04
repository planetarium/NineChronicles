using UnityEngine;

namespace Assets.SimpleLocalization
{
	/// <summary>
	/// Downloads spritesheets from Google Spreadsheet and saves them to Resources. My laziness made me to create it.
	/// </summary>
	[ExecuteInEditMode]
	public class LocalizationSync : MonoBehaviour
	{
		/// <summary>
		/// Table id on Google Spreadsheet.
		/// Let's say your table has the following url https://docs.google.com/spreadsheets/d/1RvKY3VE_y5FPhEECCa5dv4F7REJ7rBtGzQg9Z_B_DE4/edit#gid=331980525
		/// So your table id will be "1RvKY3VE_y5FPhEECCa5dv4F7REJ7rBtGzQg9Z_B_DE4" and sheet id will be "331980525" (gid parameter)
		/// </summary>
		public string TableId;

		/// <summary>
		/// Table sheet contains sheet name and id. First sheet has always zero id. Sheet name is used when saving.
		/// </summary>
		public Sheet[] Sheets;

		/// <summary>
		/// Folder to save spreadsheets. Must be inside Resources folder.
		/// </summary>
		public Object SaveFolder;

		private const string UrlPattern = "https://docs.google.com/spreadsheets/d/{0}/export?format=csv&gid={1}";

		#if UNITY_EDITOR

		/// <summary>
		/// Sync spreadsheets.
		/// </summary>
		public void Sync()
		{
			var folder = UnityEditor.AssetDatabase.GetAssetPath(SaveFolder);
			var sheets = Sheets.Length;

			Debug.Log("<color=yellow>Sync started, please wait for confirmation message. If it doesn't work, try to run it in Play mode.</color>");

			foreach (var sheet in Sheets)
			{
				var url = string.Format(UrlPattern, TableId, sheet.Id);

				Downloader.Download(url, www =>
				{
					if (www.error == null)
					{
						var path = System.IO.Path.Combine(folder, sheet.Name + ".csv");

						System.IO.File.WriteAllBytes(path, www.bytes);
						Debug.LogFormat("Sheet {0} saved to {1}", sheet.Id, path);
					}

					UnityEditor.AssetDatabase.Refresh();

					if (--sheets == 0)
					{
						Debug.Log("<color=green>Localization successfully synced!</color>");
						DestroyImmediate(Downloader.Instance.gameObject);
					}
				});
			}
		}

		#endif
	}
}