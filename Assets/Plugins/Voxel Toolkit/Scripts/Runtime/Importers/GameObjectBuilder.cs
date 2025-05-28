using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace VoxelToolkit
{
    /// <summary>
    /// Builds a game objects hierarchy for a specific asset
    /// </summary>
	public class GameObjectBuilder
    {
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        private static readonly int Palette = Shader.PropertyToID("_Palette");

        /// <summary>
        /// The hue shift to be applied to the materials
        /// </summary>
        public float HueShift { get; set; } = 0.0f;
        /// <summary>
        /// Saturation to be applied to the materials
        /// </summary>
        public float Saturation { get; set; } = 1.3f;
        /// <summary>
        /// Brightness to be applied to the materials
        /// </summary>
        public float Brightness { get; set; } = 0.0f;
        /// <summary>
        /// The opaque material to be used for the object built
        /// </summary>
        public UnityEngine.Material OpaqueMaterial { get; set; }
        /// <summary>
        /// The transparent material to be used for the object built
        /// </summary>
        public UnityEngine.Material TransparentMaterial { get; set; }
        /// <summary>
        /// How much should transparent edges of the mesh should be shifted (Helps with the incorrect shadows)
        /// </summary>
        public float TransparentEdgeShift { get; set; }
        /// <summary>
        /// How much should opaque edges of the mesh should be shifted (Helps with the incorrect shadows)
        /// </summary>
        public float OpaqueEdgeShift { get; set; }
        /// <summary>
        /// The scale of the mesh built
        /// </summary>
        public float Scale { get; set; } = 0.1f;
        /// <summary>
        /// The chunk size to be used to generate the mesh
        /// </summary>
        public int ChunkSize { get; set; } = 16;
        /// <summary>
        /// Generation approach to be taken to generate meshes
        /// </summary>
        public MeshGenerationApproach MeshGenerationApproach { get; set; } = MeshGenerationApproach.Textureless;
        /// <summary>
        /// Defines where material properties should be embedded to
        /// </summary>
        public MaterialPropertiesEmbeddingMode MaterialPropertiesEmbeddingMode  { get; set; } = MaterialPropertiesEmbeddingMode.Vertex;
        /// <summary>
        /// The origin mode of the objects to be generated
        /// </summary>
        public OriginMode OriginMode { get; set; } = OriginMode.Center;
        /// <summary>
        /// Index format to be used. If null UInt32 will be used if supported  
        /// </summary>
        public IndexFormat? IndexFormat { get; set; }
#if UNITY_EDITOR
        /// <summary>
        /// If set the mesh is going to have UV2 lightmap coords (Editor Only)
        /// </summary>
        public bool GenerateLightmapUV { get; set; }
#endif
        /// <summary>
        /// If set the resulting objects going to have mesh colliders with respected meshes
        /// </summary>
        public bool GenerateColliders { get; set; } = true;

        /// <summary>
        /// Texture optimization mode to use
        /// </summary>
        public TextureOptimizationMode TextureOptimizationMode { get; set; } = TextureOptimizationMode.None;
        
        private struct ShapeGenerationParameters
        {
            public readonly int ShapeID;
            public readonly int ModelID;
            public readonly VoxelObject VoxelObject;
            public readonly GameObject RootGameObject;

            public ShapeGenerationParameters(VoxelObject voxelObject, GameObject rootGameObject, Shape shape, Model model)
            {
                VoxelObject = voxelObject;
                RootGameObject = rootGameObject;
                ShapeID = shape.ID;
                ModelID = model.ID;
            }
        }

        /// <summary>
        /// Creates a game object for the given asset
        /// </summary>
        /// <param name="asset">The asset to generate game object hierarchy from</param>
        /// <returns>The game object built from the given asset</returns>
        public GameObject CreateGameObject(VoxelAsset asset)
        {
            var go = new GameObject("Root");

            var shapeGenerationParametersList = UnityEngine.Pool.ListPool<ShapeGenerationParameters>.Get();
            var group = AddHierarchyObject(asset, go, asset.HierarchyRoot, shapeGenerationParametersList);
            
            GenerateMeshes(shapeGenerationParametersList);

            UnityEngine.Pool.ListPool<ShapeGenerationParameters>.Release(shapeGenerationParametersList);
            
            if (group == null)
                return go;
            
            var childrenCount = group.transform.childCount;
            for (var index = childrenCount - 1; index >= 0; index--)
            {
                var child = group.transform.GetChild(index);
                child.transform.SetParent(go.transform);
            }
            
#if UNITY_EDITOR
            EnsureNamesAreUnique(go);
#endif
            
            Object.DestroyImmediate(group);
            
            return go;
        }

#if UNITY_EDITOR
        private static void EnsureNamesAreUnique(GameObject go)
        {
            UnityEditor.GameObjectUtility.EnsureUniqueNameForSibling(go);
            for (var index = 0; index < go.transform.childCount; index++)
                EnsureNamesAreUnique(go.transform.GetChild(index).gameObject);
        }
#endif

        private ShapeGenerationParameters? FindParametersWithID(int id, List<ShapeGenerationParameters> shapeGenerationParametersList)
        {
            foreach (var entry in shapeGenerationParametersList)
                if (entry.ModelID == id)
                    return entry;

            return null;
        }
        
        private GameObject AddHierarchyObject(VoxelAsset asset, GameObject parent, HierarchyNode node, List<ShapeGenerationParameters> shapeGenerationParametersList)
        {
            if (node is Group group)
                return AddGroupGameObject(asset, parent, group, shapeGenerationParametersList);

            if (node is Transformation transformation)
            {
                var transformationResult = AddTransformationElement(asset, parent, transformation, shapeGenerationParametersList);
                if (transformationResult.IsGroup)
                    return transformationResult.GameObject;
            }
            else if (node is Shape shape)
                AddShapeGameObject(asset, parent, shape, shapeGenerationParametersList);
            else
                throw new Exception("Unexpected hierarchy object type");

            return null;
        }
        
        private void GenerateMeshes(List<ShapeGenerationParameters> shapeGenerationParametersList)
        {
            var groupToUpdate = UnityEngine.Pool.ListPool<MeshUpdateParameters>.Get();

            foreach (var entry in shapeGenerationParametersList)
            {
                var format = IndexFormat ?? (SystemInfo.supports32bitsIndexBuffer
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16);

                var meshShift = OriginMode == OriginMode.Corner
                    ? Vector3.zero
                    : -((Vector3)(float3)entry.VoxelObject.Size / 2.0f);

                groupToUpdate.Add(new MeshUpdateParameters(format, entry.VoxelObject, meshShift));
            }

            var result = VoxelObject.UpdateVoxelObjectsGroup(groupToUpdate, TextureOptimizationMode);

            var opaqueName = "/VoxelToolkitDefaultOpaque";
            var transparentName = "/VoxelToolkitDefaultTransparent";

            var opaqueMaterial = OpaqueMaterial == null
                ? Resources.Load<UnityEngine.Material>($"{PathUtility.GetMaterialPath()}{opaqueName}")
                : OpaqueMaterial;

            var transparentMaterial = TransparentMaterial == null
                ? Resources.Load<UnityEngine.Material>($"{PathUtility.GetMaterialPath()}{transparentName}")
                : TransparentMaterial;

            if (MeshGenerationApproach == MeshGenerationApproach.Textured)
            {
                opaqueMaterial = new UnityEngine.Material(opaqueMaterial)
                    {
                        mainTexture = result.Atlas
                    };
                
                transparentMaterial = new UnityEngine.Material(transparentMaterial)
                    {
                        mainTexture = result.Atlas
                    };
                
                opaqueMaterial.SetTexture(Palette, result.PaletteAtlas);
                transparentMaterial.SetTexture(Palette, result.PaletteAtlas);
                
                opaqueMaterial.SetFloat(Keywords.PropertiesTextureID, result.PaletteAtlas == null ? 0.0f : 1.0f);
                opaqueMaterial.EnableKeyword(Keywords.PropertiesTexture);
                
                transparentMaterial.SetFloat(Keywords.PropertiesTextureID, result.PaletteAtlas == null ? 0.0f : 1.0f);
                transparentMaterial.EnableKeyword(Keywords.PropertiesTexture);

                opaqueMaterial.SetFloat(Keywords.TexturedID, 1.0f);
                opaqueMaterial.EnableKeyword(Keywords.Textured);
                
                transparentMaterial.SetFloat(Keywords.TexturedID, 1.0f);
                transparentMaterial.EnableKeyword(Keywords.Textured);
            }

            for (var index = 0; index < groupToUpdate.Count; index++)
            {
                var group = groupToUpdate[index];
                var entry = shapeGenerationParametersList[index];
                var go = entry.RootGameObject;
                
#if UNITY_EDITOR
                if (GenerateLightmapUV)
                    group.Object.GenerateLightmapUV();
#endif
                
                for (var modelIndex = 0; modelIndex < group.Object.MeshesCount; modelIndex++)
                {
                    var shift = (float3)(group.Object.Size / 2) * Scale;
                    
                    var descriptor = group.Object.GetMesh(modelIndex);
                    
                    var child = new GameObject(go.name);
                    var meshFilter = child.AddComponent<MeshFilter>();

                    child.transform.SetParent(go.transform);
                    child.transform.localPosition = -shift - group.Shift * Scale;
                    child.transform.localRotation = Quaternion.identity;

                    if (GenerateColliders)
                    {
                        var meshCollider = child.AddComponent<MeshCollider>();
                        meshCollider.sharedMesh = descriptor.Mesh;
                    }

                    var renderer = child.AddComponent<MeshRenderer>();
                    
                    MaterialSetupUtility.Setup(renderer, descriptor, opaqueMaterial, transparentMaterial);
                        
                    meshFilter.sharedMesh = descriptor.Mesh;

                    descriptor.Mesh.name = $"{go.name} {modelIndex}{entry.ShapeID}{index}";
                }
            }
            
            foreach (var group in groupToUpdate)
                group.Object.DisposeWithoutMeshes();

            for (var index = 0; index < shapeGenerationParametersList.Count; index++)
            {
                var entry = shapeGenerationParametersList[index];
                var go = entry.RootGameObject;

                if (go.transform.childCount != 1) 
                    continue;
                
                go.transform.GetChild(0).SetParent(go.transform.parent);
                Object.DestroyImmediate(go);
            }

            UnityEngine.Pool.ListPool<MeshUpdateParameters>.Release(groupToUpdate);
        }

        private GameObject AddShapeGameObject(VoxelAsset asset, GameObject parent, Shape shape, List<ShapeGenerationParameters> shapeGenerationParametersList)
        {
            var go = new GameObject(shape.name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            for (var index = 0; index < shape.ModelsCount; index++)
            {
                var model = shape[index];
                var currentShape = FindParametersWithID(model.ID, shapeGenerationParametersList);
                if (currentShape.HasValue)
                {
                    shapeGenerationParametersList.Add(new ShapeGenerationParameters(currentShape.Value.VoxelObject, go, shape, model));
                    continue;
                }

                var maxSize = Mathf.Max(model.Size.x, model.Size.y, model.Size.z);
                var voxelObject = VoxelObject.CreateFromModel(model, asset.Palette, Mathf.Min(maxSize, ChunkSize));
                voxelObject.Scale = Scale;
                voxelObject.OpaqueEdgeShift = OpaqueEdgeShift;
                voxelObject.TransparentEdgeShift = TransparentEdgeShift;
                voxelObject.MeshGenerationApproach = MeshGenerationApproach;
                voxelObject.MaterialPropertiesEmbeddingMode = MaterialPropertiesEmbeddingMode;

                voxelObject.HueShift = HueShift;
                voxelObject.Saturation = Saturation;
                voxelObject.Brightness = Brightness;
                
                shapeGenerationParametersList.Add(new ShapeGenerationParameters(voxelObject, go, shape, model));
            }

            return go;
        }

        private GameObject AddGroupGameObject(VoxelAsset asset, GameObject parent, Group group, List<ShapeGenerationParameters> shapeGenerationParametersList)
        {
            var go = new GameObject(group.Name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            for (var index = 0; index < group.ChildrenCount; index++)
                AddTransformationElement(asset, go, group[index] as Transformation, shapeGenerationParametersList);

            return go;
        }
        
        public static (int4x4 Transformation, int3 Scale) SeparateScale(int4x4 matrix)
        {
            var xAxis = matrix.c0.xyz;
            var yAxis = matrix.c1.xyz;
            var zAxis = matrix.c2.xyz;

            if (math.dot(math.cross(xAxis, yAxis), zAxis) >= 0)
                return (matrix, new int3(1, 1, 1));
            
            var scale = new int3(
                math.dot(matrix.c0.xyz, new int3(1, 0, 0)) < 0 ? -1 : 1,
                math.dot(matrix.c1.xyz, new int3(0, 1, 0)) < 0 ? -1 : 1,
                math.dot(matrix.c2.xyz, new int3(0, 0, 1)) < 0 ? -1 : 1
            );

            var restored = new int4x4(
                new int4(matrix.c0.xyz * scale.x, 0),
                new int4(matrix.c1.xyz * scale.y, 0),
                new int4(matrix.c2.xyz * scale.z, 0),
                new int4(matrix.c3.xyz, 1)
            );

            return (restored, scale);
        }
        
        private (GameObject GameObject, bool IsGroup) AddTransformationElement(VoxelAsset asset, GameObject parent, Transformation transformation, List<ShapeGenerationParameters> shapeGenerationParametersList)
        {
            var frameIndex = 0;
            var frame = transformation.Frames[frameIndex];

            GameObject result = null;
            string fallbackName = null;
            var isGroup = false;
            if (transformation.Child is Shape shape)
            {
                result = AddShapeGameObject(asset, parent, shape, shapeGenerationParametersList);
                fallbackName = "Shape";
            }
            else if (transformation.Child is Group group)
            {
                isGroup = true;
                result = AddGroupGameObject(asset, parent, group, shapeGenerationParametersList);
                fallbackName = "Group";
            }

            var separation = SeparateScale(frame.Transformation);
                
            result.transform.localPosition = (float3)math.mul(separation.Transformation, new int4(0, 0, 0, 1)).xyz * Scale;
            var rotation = new float3x3(
                separation.Transformation.c0.xyz,
                separation.Transformation.c1.xyz,
                separation.Transformation.c2.xyz);
                
            result.transform.localRotation = math.rotation(rotation);
            result.transform.localScale = (float3)separation.Scale;
            
            result.name = (transformation.Name ?? fallbackName) ?? string.Empty;
            
            return (result, isGroup);
        }
	}
}