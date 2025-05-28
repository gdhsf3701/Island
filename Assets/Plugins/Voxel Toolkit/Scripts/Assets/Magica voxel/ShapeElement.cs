using System.Collections.Generic;
using UnityEngine;

namespace VoxelToolkit.MagicaVoxel
{
	public class ShapeElement : HierarchyElement
	{
		public ModelReference[] Models;

		public ShapeElement(int id, Dictionary<string, string> attributes, ModelReference[] models) : base(id, attributes)
		{
			Models = models;
		}

		public override HierarchyNode AddToAsset(VoxelAsset asset, List<Model> models)
		{
			var shape = ScriptableObject.CreateInstance<Shape>();
			shape.name = Attributes.TryGetValue("_name", out string shapeName) ? shapeName : $"Shape {Id}";

			foreach (var modelReference in Models)
				shape.AddModel(models.Find(x => x.ID == modelReference.Id));

			return shape;
		}
	}
}