using System.IO;
using System.Text;
using UnityEngine;

namespace VoxelToolkit.Demo
{
	public class Loader : MonoBehaviour
	{
		[SerializeField] private TextAsset asset;
		
		private void Awake()
		{
			if (asset == null)
			{
				Debug.LogError("The asset is null", this);
				return;
			}
			
			var importer = new MagicaVoxelImporter();

			using (var memoryStream = new MemoryStream(asset.bytes))
			{
				using (var binaryReader = new BinaryReader(memoryStream, Encoding.Default))
				{
					var asset = importer.ImportAsset(binaryReader);
					var gameObjectCreator = new GameObjectBuilder();

					gameObjectCreator.CreateGameObject(asset);
				}
			}
		}
	}
}