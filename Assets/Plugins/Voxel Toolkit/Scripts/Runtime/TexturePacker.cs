using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelToolkit
{
    public static class TexturePacker
    {
        public static TexturePackerResult Pack(Texture2D[] textures, TextureOptimizationMode optimizationMode, bool linear)
        {
            var atlas = new Texture2D(1, 1, TextureFormat.RGBA32, false, linear);
            atlas.filterMode = FilterMode.Point;
            atlas.wrapMode = TextureWrapMode.Clamp;

            var root = new List<TextureNode>();
            for (var index = 0; index < textures.Length; index++)
                root.Add(new TextureNode(textures[index], float3x3.identity, index, optimizationMode));
            
            var singlePixelTextures = root.FindAll(x => x.Area == 1);
            root.RemoveAll(x => x.Area == 1);

            root.Sort((x, y) => y.Area.CompareTo(x.Area));
            
            for (var index = root.Count - 2; index >= 0; index--)
            {
                if (optimizationMode == TextureOptimizationMode.None)
                    break;
                
                var entry = root[index];
                
                var jobHandles = new NativeList<JobHandle>(root.Count, Allocator.Temp);
                var jobs = new List<FindFitJob>();

                for (var innerIndex = index + 1; innerIndex < root.Count; innerIndex++)
                {
                    var innerEntry = root[innerIndex];
                    
                    var job = new FindFitJob();

                    job.Target = entry.Handle;
                    job.Candidate = innerEntry.Handle;

                    job.Transformation = new NativeArray<float3x3>(1, Allocator.TempJob);
                    job.Result = new NativeArray<bool>(1, Allocator.TempJob);

                    jobs.Add(job);
                    jobHandles.Add(job.Schedule());
                }

                JobHandle.CompleteAll(jobHandles);
                jobHandles.Dispose();

                var texturesToReparent = new List<TextureNode>();
                for (var jobIndex = 0; jobIndex < jobs.Count; jobIndex++)
                {
                    var job = jobs[jobIndex];
                    if (job.Result[0])
                    {
                        var texture = root[index + jobIndex + 1];
                        texturesToReparent.Add(texture);
                        texture.Location = job.Transformation[0];
                    }

                    job.Result.Dispose();
                    job.Transformation.Dispose();
                }

                foreach (var textureToReparent in texturesToReparent)
                {
                    root.Remove(textureToReparent);
                    entry.AddChildren(textureToReparent);
                }
            }
            
            var packetRects = atlas.PackTextures(root.ConvertAll(x => x.Texture).ToArray(), 0, SystemInfo.maxTextureSize);

            foreach (var singlePixelTexture in singlePixelTextures)
            {
                singlePixelTexture.Location = 
                    new float3x3(1, 0, -100,
                        0, 1, -100,
                        0, 0, 1);
            }

            for (var index = 0; index < root.Count; index++)
            {
                var entry = root[index];
                var rect = packetRects[index];

                var rectTransform = new float3x3(
                    rect.width, 0.0f, rect.x,
                    0.0f, rect.height, rect.y,
                    0.0f, 0.0f, 1.0f);

                entry.Location = math.mul(rectTransform, entry.Location);
            }

            var uvs = new NativeArray<float3x3>(textures.Length, Allocator.TempJob);

            foreach (var entry in root)
                entry.RecalculateUVs();

            foreach (var entry in root)
                entry.FillLocations(uvs);
            
            foreach (var entry in singlePixelTextures)
                entry.FillLocations(uvs);
            
            foreach (var entry in root)
                entry.Dispose();
            
            foreach (var entry in singlePixelTextures)
                entry.Dispose();

            atlas.Apply(false);

            return new TexturePackerResult(atlas, uvs);
        }
    }
}