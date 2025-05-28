using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;
using VoxelToolkit.MagicaVoxel;

namespace VoxelToolkit
{
    /// <summary>
    /// Handles .vox files import
    /// </summary>
	public class MagicaVoxelImporter : VoxelImporter
	{
        /// <summary>
        /// Represents the scaler of the emmision factor (1.8 is close to what you see in MagicaVoxel)
        /// </summary>
        public float EmissionScaleFactor { get; set; } = 1.8f;

        private void ParseMainChunk(VoxelAsset asset, Reader reader)
        {
            reader.ConsumeString("MAIN");
            var size = reader.ConsumeInt();
            var childrenChunksSize = reader.ConsumeInt();

            if (size != 0)
                throw new VoxelDataReadException("Expected zero size MAIN chunk");
            
            var models = ParseModelChunk(asset, reader);
            var hierarchyElements = new List<HierarchyElement>();

            ParseHierarchy(hierarchyElements, reader);
            ParseLayers(asset, reader);
            ParsePalette(asset, reader);
            SkipChunk("IMAP", reader);
            ParseMaterials(asset, reader);
            
            asset.HierarchyRoot = FoldHierarchy(hierarchyElements, models).AddToAsset(asset, models);
        }

        private void SkipChunk(string name, Reader reader)
        {
            if (!reader.TryConsumeString(name))
                return;
            
            var size               = reader.ConsumeInt();
            var childrenChunksSize = reader.ConsumeInt();
            
            reader.Skip(size);
        }
        
        private void ParseTransform(List<HierarchyElement> elements, Reader reader)
        {
            var id = reader.ConsumeInt();
            var attributes = reader.ConsumeDictionary();

            var childId = reader.ConsumeInt();
            var reservedId = reader.ConsumeInt();
            if (reservedId != -1)
                throw new VoxelDataReadException($"Expected reserved Id be equal to -1 but got {reservedId}");

            var layerId = reader.ConsumeInt();
            var numberOfFrames = reader.ConsumeInt();
            var frames = new Dictionary<string, string>[numberOfFrames];
            for (var index = 0; index < numberOfFrames; index++)
                frames[index] = reader.ConsumeDictionary();
            
            elements.Add(new TransformElement(id, childId, layerId, frames, attributes));
        }

        private void ParsePalette(VoxelAsset asset, Reader reader)
        {
            reader.ConsumeString("RGBA");
            var size = reader.ConsumeInt();
            var childrenChunksSize = reader.ConsumeInt();
            
            for (var index = 0; index < 256; index++)
            {
                var r = reader.ConsumeByte();
                var g = reader.ConsumeByte();
                var b = reader.ConsumeByte();
                var a = reader.ConsumeByte();
                
                var color = new UnityEngine.Color32(r, g, b, a);
                
                var material = asset.GetPaletteMaterial(index);
                material.Color = color;
                asset.SetPaletteMaterial(index, material);
            }
        }

        private void ParseMaterials(VoxelAsset asset, Reader reader)
        {
            var hadMaterialInfo = false;
            while (reader.TryConsumeString("MATL"))
            {
                hadMaterialInfo = true;
                var size = reader.ConsumeInt();
                var childrenChunksSize = reader.ConsumeInt();
                
                var id = reader.ConsumeInt() - 1;
                var attributes = reader.ConsumeDictionary();
                var typeName = attributes.ContainsKey("_type") ? attributes["_type"] : "_diffuse";
                var type = MaterialType.Invalid;
                var isDiffuse = typeName == "_diffuse";
                
                var material = asset.GetPaletteMaterial(id);
                if (attributes.TryGetValue("_alpha", out var alpha) && !isDiffuse)
                {
                    var color = material.Color;
                    color.a = 1.0f - float.Parse(alpha, CultureInfo.InvariantCulture);
                    material.Color = color;
                }
                
                if (attributes.TryGetValue("_weight", out var weight) && !isDiffuse)
                {
                    var color = material.Color;
                    color.a = 1.0f - float.Parse(weight, CultureInfo.InvariantCulture);
                    material.Color = color;
                }

                if (isDiffuse || 
                    typeName == "_metal" ||
                    typeName == "_emit" ||
                    typeName == "_plastic")
                    type = MaterialType.Basic;
                else if (typeName == "_glass" || 
                         typeName == "_media")
                    type = MaterialType.Transparent;
                else if (typeName == "_blend")
                    type = material.Color.a < 1.0f ? MaterialType.Transparent : MaterialType.Basic;
                else
                    throw new VoxelDataReadException($"Unexpected material type '{type}' '{typeName}'");

                material.MaterialType = type;

                if (attributes.TryGetValue("_rough", out var roughness))
                    material.Roughness = isDiffuse ? 1.0f : float.Parse(roughness, CultureInfo.InvariantCulture);

                if (attributes.TryGetValue("_emit", out var emit))
                    material.Emit = isDiffuse ? 0.0f : float.Parse(emit, CultureInfo.InvariantCulture) * EmissionScaleFactor;
                
                if (attributes.TryGetValue("_attr", out var attr))
                    material.Attenuation = float.Parse(attr, CultureInfo.InvariantCulture);
                
                if (attributes.TryGetValue("_flux", out var flux))
                    material.Flux = isDiffuse ? 0.0f : float.Parse(flux, CultureInfo.InvariantCulture) * EmissionScaleFactor;
                
                if (attributes.TryGetValue("_ior", out var ior))
                    material.IOR = isDiffuse ? 0.0f : float.Parse(ior, CultureInfo.InvariantCulture);

                if (attributes.TryGetValue("_specular", out var specular))
                    material.Specular = isDiffuse ? 0.0f : float.Parse(specular, CultureInfo.InvariantCulture);

                if (attributes.TryGetValue("_plastic", out var plastic))
                    material.Plastic = isDiffuse ? 1.0f : float.Parse(plastic, CultureInfo.InvariantCulture);
                else
                    material.Plastic = 1.0f;

                if (attributes.TryGetValue("_metal", out var metal))
                    material.plastic = 1.0f - float.Parse(metal, CultureInfo.InvariantCulture);
                
                asset.SetPaletteMaterial(id, material);
            }
            
            if (hadMaterialInfo)
                return;

            for (var index = 0; index < 256; index++)
            {
                var material = asset.GetPaletteMaterial(index);

                material.MaterialType = material.Color.a < 0.99f ? 
                    MaterialType.Transparent :
                    MaterialType.Basic;
                
                material.Roughness = 1.0f;
                material.Attenuation = 0.0f;
                material.Specular = 0.0f;
                material.Emit = 0.0f;
                material.Flux = 0.0f;
                material.IOR = 1.0f;
                
                asset.SetPaletteMaterial(index, material);
            }
        }

        private void ParseLayers(VoxelAsset asset, Reader reader)
        {
            while (reader.TryConsumeString("LAYR"))
            {
                var sizeChunkSize = reader.ConsumeInt();
                var sizeChunkChildrenSize = reader.ConsumeInt();

                var id = reader.ConsumeInt();
                var attributes = reader.ConsumeDictionary();
                var reservedId = reader.ConsumeInt();
                if (reservedId != -1)
                    throw new VoxelDataReadException($"$Reserved id should always be -1 but got {reservedId}");

                var name = attributes.TryGetValue("_name", out string resultName) ? resultName : $"Layer {id}";

                var layer = ScriptableObject.CreateInstance<Layer>();
                layer.name = name;
                layer.Name = name;
                layer.ID = id;
                
                asset.AddLayer(layer);
            }
        }

        private void ParseGroup(List<HierarchyElement> elements, Reader reader)
        {
            var id = reader.ConsumeInt();
            var attributes = reader.ConsumeDictionary();
            var childCount = reader.ConsumeInt();

            var children = new int[childCount];
            for (var index = 0; index < childCount; index++)
                children[index] = reader.ConsumeInt();
            
            elements.Add(new GroupElement(id, attributes, children));
        }
        
        private void ParseShape(List<HierarchyElement> elements, Reader reader)
        {
            var id = reader.ConsumeInt();
            var attributes = reader.ConsumeDictionary();
            var modelsCount = reader.ConsumeInt();

            var children = new ModelReference[modelsCount];
            for (var index = 0; index < modelsCount; index++)
            {
                var modelId = reader.ConsumeInt();
                var modelAttributes = reader.ConsumeDictionary();
                children[index] = new ModelReference(modelId, modelAttributes);
            }

            elements.Add(new ShapeElement(id, attributes, children));
        }

        private HierarchyElement FoldHierarchy(List<HierarchyElement> elements, List<Model> models)
        {
            var tempElements = new List<HierarchyElement>(elements);
            foreach (var hierarchyElement in tempElements)
            {
                if (hierarchyElement is TransformElement transformElement)
                {
                    var child = elements.FindIndex(x => x.Id == transformElement.ChildId);
                    if (child != -1)
                        transformElement.Child = elements[child];
                }
                else if (hierarchyElement is GroupElement groupElement)
                {
                    groupElement.Children = new HierarchyElement[groupElement.ChildrenIds.Length];
                    var childIndex = 0;
                    foreach (var groupElementChildrenId in groupElement.ChildrenIds)
                    {
                        var child = elements.FindIndex(x => x.Id == groupElementChildrenId);
                        if (child != -1)
                        {
                            var item = elements[child];
                            groupElement.Children[childIndex] = item;
                        }

                        childIndex++;
                    }
                }
            }

            if (tempElements.Count != 0)
                return tempElements[0];

            var references = models
                .ConvertAll(x => new ModelReference(x.ID, new Dictionary<string, string>()))
                .ToArray();

            return new ShapeElement(0, new Dictionary<string, string>(), references);
        }

        private void ParseHierarchy(List<HierarchyElement> elements, Reader reader)
        {
            while (true)
            {
                var initialPosition = reader.Position;
                var currentNode = reader.ConsumeString(4);
                var sizeChunkSize = reader.ConsumeInt();
                var sizeChunkChildrenSize = reader.ConsumeInt();

                if (currentNode == "nTRN")
                    ParseTransform(elements, reader);
                else if (currentNode == "nGRP")
                    ParseGroup(elements, reader);
                else if (currentNode == "nSHP")
                    ParseShape(elements, reader);
                else
                {
                    reader.Position = initialPosition;
                    break;
                }
            }
        }

        private List<Model> ParseModelChunk(VoxelAsset asset, Reader reader)
        {
            var results = new List<Model>();
            var id = 0;
            while (reader.TryConsumeString("SIZE"))
            {
                var sizeChunkSize = reader.ConsumeInt();
                var sizeChunkChildrenSize = reader.ConsumeInt();
                if (sizeChunkSize != 12)
                    throw new VoxelDataReadException("Expected 'SIZE' chunk size of 12");

                if (sizeChunkChildrenSize != 0)
                    throw new VoxelDataReadException("Expected zero child chunk size for 'SIZE' chunk");

                var size = reader.ConsumeVector3IntInt32();
                (size.y, size.z) = (size.z, size.y);
                
                if (size.x <= 0 || size.y <= 0 || size.z <= 0)
                    throw new VoxelDataReadException($"Expected size larger than zero but got '{size}'");

                var xyziId = reader.ConsumeString(4);
                if (xyziId != "XYZI")
                    throw new VoxelDataReadException($"Expected 'XYZI' chunk but got '{xyziId}'");

                var xyziChunkSize = reader.ConsumeInt();
                var xyziChunkChildrenSize = reader.ConsumeInt();

                if (xyziChunkSize < 0)
                    throw new VoxelDataReadException("Expected 'XYZI' chunk size more or equal to zero");

                if (xyziChunkChildrenSize != 0)
                    throw new VoxelDataReadException("Expected zero 'XYZI' children chunk size");

                var voxelsCount = reader.ConsumeInt();

                var voxelData = new List<VoxelData>();
                for (var index = 0; index < voxelsCount; index++)
                {
                    var position = reader.ConsumeVector3IntByte();
                    (position.z, position.y) = (position.y, position.z);
                    
                    var color = reader.ConsumeByte() - 1;

                    voxelData.Add(new VoxelData(position, color));
                }

                var shape = ScriptableObject.CreateInstance<Model>();
                shape.ID = id++;
                shape.Size = size;
                shape.SetVoxels(voxelData);
                shape.name = $"Model {shape.ID}";
                results.Add(shape);

                shape.ParentAsset = asset;
            }

            return results;
        }

        private void ParseHeader(VoxelAsset asset, Reader reader)
        {
            reader.ConsumeString("VOX ");

            asset.InputSource = "Magica voxel file format";
        }

        private void ParseVersion(VoxelAsset asset, Reader reader)
        {
            var version = reader.ConsumeInt();
            if (version < 150)
                throw new VoxelDataReadException("Expected version 150 but got " + version);

            asset.Version = version.ToString();
        }

        /// <summary>
        /// Imports the asset from a reader
        /// </summary>
        /// <param name="reader">The reader to be used to read the voxel asset</param>
        /// <returns>Voxel asset for the given vox asset</returns>
        public override VoxelAsset ImportAsset(BinaryReader reader)
        {
            var readerHelper = new Reader(reader);
            var asset = ScriptableObject.CreateInstance<VoxelAsset>();

            ParseHeader(asset, readerHelper);
            ParseVersion(asset, readerHelper);
            ParseMainChunk(asset, readerHelper);
            
            asset.HierarchyRoot.name = "Root";

            var counters = DictionaryPool<string, int>.Get();
            counters.Clear();
            Rename(asset.HierarchyRoot, counters);
            
            DictionaryPool<string, int>.Release(counters);
            
            return asset;
        }

        private string GetName(string target, Dictionary<string, int> counters)
        {
            if (!counters.TryGetValue(target, out int count))
            {
                count = 0;
                counters[target] = count;
            }

            var result = $"{target} {count}";
            
            count++;
            counters[target] = count;
            
            return result;
        }
        
        private void Rename(HierarchyNode target, Dictionary<string, int> counters)
        {
            var namedObject = target as INamedObject;

            foreach (var related in target.RelatedObjects)
            {
                if (related is Transformation transformation)
                {
                    var name = !string.IsNullOrEmpty(transformation.Name) ? $"{transformation.Name} Transformation" : "Transformation";
                    transformation.name = GetName(name, counters);
                }

                if (namedObject == null)
                {
                    Rename(related as HierarchyNode, counters);
                    continue;
                }
                
                if (related is Shape shape)
                {
                    shape.name = GetName($"{namedObject.Name} Shape", counters);

                    for (var index = 0; index < shape.Models.Count; index++)
                    {
                        var model = shape.Models[index];
                        model.name = GetName($"{namedObject.Name} Model", counters);
                    }
                }

                if (related is Group group)
                {
                    var nameToBeSet = GetName($"{namedObject.Name} Group", counters);
                    group.name = nameToBeSet;
                    group.Name = nameToBeSet;
                }

                Rename(related as HierarchyNode, counters);
            }
        }
    }
}