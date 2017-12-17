using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {
	
	const float viewerMoveThresholdToChunkUpdate = 25f;
	float sqrViewerMoveThresholdToChunkUpdate = viewerMoveThresholdToChunkUpdate * viewerMoveThresholdToChunkUpdate;

	public LODInfo[] detailLevels;
	public static float maxViewDst;

	public Transform viewer;
	public Material mapMaterial;

	public static Vector2 viewerPosition;
	Vector2 viewerPositionOld;
	static MapGenerator mapGenerator;
	int chunkSize;
	int chunksVisiableInViewDst;

	Dictionary <Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk> ();
	static List <TerrainChunk> terrainChunksVisiableLastUpdate = new List<TerrainChunk> ();

	void Start () {

		mapGenerator = FindObjectOfType<MapGenerator> ();

		maxViewDst = detailLevels [detailLevels.Length - 1].visibleDstThreshold;
		chunkSize = mapGenerator.mapChunkSize - 1;
		chunksVisiableInViewDst = Mathf.RoundToInt (maxViewDst / chunkSize);

		UpdateVisiableChunks ();
	}

	void Update () {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;

		if ((viewerPosition - viewerPositionOld).sqrMagnitude > sqrViewerMoveThresholdToChunkUpdate) {
			viewerPositionOld = viewerPosition;
			UpdateVisiableChunks ();
		}
	}

	void UpdateVisiableChunks () {

		for (int i = 0; i < terrainChunksVisiableLastUpdate.Count; i++) {
			terrainChunksVisiableLastUpdate [i].SetVisiable (false);
		}

		terrainChunksVisiableLastUpdate.Clear ();

		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / chunkSize);

		for (int yOffset = -chunksVisiableInViewDst; yOffset <= chunksVisiableInViewDst; yOffset++) {
			for (int xOffset = -chunksVisiableInViewDst; xOffset <= chunksVisiableInViewDst; xOffset++) {

				Vector2 viewedChunkCoord = new Vector2 (xOffset + currentChunkCoordX, yOffset + currentChunkCoordY);

				if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
					terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
				} else {
					terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
				}
			}
		}
	}

	public class TerrainChunk {

		GameObject meshObject;
		Vector2 position;
		Bounds bounds;

		MeshRenderer meshRenderer;
		MeshFilter meshFilter;
		MeshCollider meshCollider;

		LODMesh[] lodMeshes;
		LODInfo[] detailLevels;
		LODMesh collisionLODMesh;

		MapData mapData;
		bool mapDataRecieved;
		int prevLODlIndex = -1;

		public TerrainChunk (Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {

			this.detailLevels = detailLevels;

			position = coord * size;
			Vector3 positionV3 = new Vector3 (position.x, 0, position.y);
			bounds = new Bounds (position, Vector2.one * size);

			meshObject = new GameObject ("Terrain Chunk");
			meshRenderer = meshObject.AddComponent <MeshRenderer> ();
			meshFilter = meshObject.AddComponent <MeshFilter> ();
			meshCollider = meshObject.AddComponent <MeshCollider> ();
			meshRenderer.material = material;

			meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
			meshObject.transform.SetParent (parent);
			meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
			SetVisiable (false);

			lodMeshes = new LODMesh[detailLevels.Length];
			for (int i = 0; i < lodMeshes.Length; i++) {
				lodMeshes[i] = new LODMesh (detailLevels[i].lod, UpdateTerrainChunk);
				if (detailLevels[i].useForCollider) 
					collisionLODMesh = lodMeshes[i];
			}

			mapGenerator.RequestMapData (position, OnMapDataRecieved);
		}

		void OnMapDataRecieved (MapData mapData) {
			this.mapData = mapData;
			mapDataRecieved = true;

			UpdateTerrainChunk ();
		}

		public void UpdateTerrainChunk () {

			if (mapDataRecieved) {

				float viewerDstFromNearestEdge = Mathf.Sqrt (bounds.SqrDistance (viewerPosition));
				bool visiable = viewerDstFromNearestEdge <= maxViewDst;

				if (visiable) {
					int lodIndex = 0;

					for (int i = 0; i < detailLevels.Length - 1; i++) {
						if (viewerDstFromNearestEdge > detailLevels [i].visibleDstThreshold) {
							lodIndex = i + 1;
						} else {
							break;
						}
					}

					if (lodIndex != prevLODlIndex) {
						LODMesh lodMesh = lodMeshes [lodIndex];
						if (lodMesh.hasMesh) {
							prevLODlIndex = lodIndex;
							meshFilter.mesh = lodMesh.mesh;
						} else if (!lodMesh.hasRequestedMesh) {
							lodMesh.RequestMesh (mapData);
						}
					}

					if (lodIndex == 0) {
						if (collisionLODMesh.hasMesh) {
							meshCollider.sharedMesh = collisionLODMesh.mesh;
						} else if (!collisionLODMesh.hasRequestedMesh) {
							collisionLODMesh.RequestMesh (mapData);
						}
					}

					terrainChunksVisiableLastUpdate.Add (this);
				}

				SetVisiable (visiable);
			}
		}

		public void SetVisiable (bool visiable) {
			meshObject.SetActive (visiable);
		}

		public bool IsVisiable () {
			return meshObject.activeSelf;
		}
	}

	public class LODMesh {

		public Mesh mesh;
		public bool hasRequestedMesh;
		public bool hasMesh;
		public int lod;
		System.Action updateCallback;

		public LODMesh (int lod, System.Action updateCallback) {
			this.lod = lod;
			this.updateCallback = updateCallback;
		}

		public void RequestMesh (MapData mapData) {
			hasRequestedMesh = true;
			mapGenerator.RequestMeshData (mapData, lod, OnMeshDataRecieved);
		}

		void OnMeshDataRecieved (MeshData meshData) {
			mesh = meshData.CreateMesh ();
			hasMesh = true;

			updateCallback ();
		}
	}

	[System.Serializable]
	public struct LODInfo {		
		public int lod; 
		public float visibleDstThreshold;
		public bool useForCollider;
	}

}
