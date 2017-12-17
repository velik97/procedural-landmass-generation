using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode {NoiseMap, Mesh, FalloffMap}
	public DrawMode drawMode;

	public TerrainData terrainData;
	public NoiseData noiseData;
	public TextureData textureData;

	public Material terrainMaterial;

	[Range(0,6)]
	public int editorPreviewLOD;

	public bool autoUpdate;

	float [,] falloffMap;

	Queue <MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue <MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	void OnValuesUpdated () {
		if (!Application.isPlaying) {
			falloffMap = FalloffGenerator.GenerateFolloffMap (mapChunkSize + 2);
			DrawMapInEditor ();
		}
	}

	void OnTexturesValuesUpdated () {
		textureData.UpdateMeshHeights (terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
		textureData.ApplyToMaterial (terrainMaterial);
	}

	public int mapChunkSize {
		get {
			return terrainData.useFlatShading ? 95 : 239;
		}
	}

	public void DrawMapInEditor () {

		MapData mapData = GenerateMapData (Vector2.zero);

		MapDisplay display = FindObjectOfType <MapDisplay> ();
		if (drawMode == DrawMode.NoiseMap) {

			display.DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.heightMap));
		} else if (drawMode == DrawMode.Mesh) {

			display.DrawMesh (MeshGenerator.GenerateTerrainMesh (mapData.heightMap, terrainData.meshHeightMultiplayer, terrainData.meshHeightCurve,
				editorPreviewLOD, terrainData.useFlatShading));
		} else if (drawMode == DrawMode.FalloffMap) {

			display.DrawTexture (TextureGenerator.TextureFromHeightMap (FalloffGenerator.GenerateFolloffMap (mapChunkSize)));
		}
	}

	public void RequestMapData (Vector2 centre, Action <MapData> callback) {

		ThreadStart threadStart = delegate {
			MapDataThread (centre, callback);
		};

		new Thread (threadStart).Start ();
	}

	void MapDataThread (Vector2 centre, Action <MapData> callback) {
		
		MapData mapData = GenerateMapData (centre);
		lock (mapDataThreadInfoQueue) {
			mapDataThreadInfoQueue.Enqueue (new MapThreadInfo<MapData> (callback, mapData));
		}
	}

	public void RequestMeshData (MapData mapData, int lod, Action<MeshData> callback) {

		ThreadStart threadStart = delegate {
			MeshDataThread (mapData, lod, callback);
		};

		new Thread (threadStart).Start();
	}

	void MeshDataThread (MapData mapData, int lod, Action<MeshData> callback) {
		
		MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, terrainData.meshHeightMultiplayer,
			terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue (new MapThreadInfo<MeshData> (callback, meshData));
		}
	}

	void Update () {
		
		while (mapDataThreadInfoQueue.Count > 0) {
			MapThreadInfo <MapData> threadInfo = mapDataThreadInfoQueue.Dequeue ();
			threadInfo.callback (threadInfo.parametr);
		}

		while (meshDataThreadInfoQueue.Count > 0) {
			MapThreadInfo <MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue ();
			threadInfo.callback (threadInfo.parametr);
		}


	}

	MapData GenerateMapData (Vector2 centre) {
		float[,] noiseMap = Noise.GenerateMap (mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves,
			noiseData.persistance, noiseData.lacunarity, centre + noiseData.offset, noiseData.normilizeMode);

		if (terrainData.useFalloff) {

			if (falloffMap == null)
				falloffMap = FalloffGenerator.GenerateFolloffMap (mapChunkSize + 2);

			for (int y = 0; y < mapChunkSize + 2; y++) {
				for (int x = 0; x < mapChunkSize + 2; x++) {
					noiseMap [x, y] = Mathf.Clamp01 (noiseMap [x, y] - falloffMap [x, y]);
				}
			}
		}

		return new MapData (noiseMap);
	}

	void OnValidate () {

		if (terrainData != null) {
			terrainData.OnValueUpdated -= OnValuesUpdated;
			terrainData.OnValueUpdated += OnValuesUpdated;
		}

		if (noiseData != null) {
			noiseData.OnValueUpdated -= OnValuesUpdated;
			noiseData.OnValueUpdated += OnValuesUpdated;
		}

		if (textureData != null) {
			textureData.OnValueUpdated -= OnTexturesValuesUpdated;
			textureData.OnValueUpdated += OnTexturesValuesUpdated;
		}
			

	}

	struct MapThreadInfo <T> {
		
		public readonly Action <T> callback;
		public readonly T parametr;

		public MapThreadInfo (Action<T> callback, T parametr)
		{
			this.callback = callback;
			this.parametr = parametr;
		}
		
	}
}


public struct MapData {
	public readonly float[,] heightMap;

	public MapData (float[,] heightMap) {
		this.heightMap = heightMap;
	}
	
}
