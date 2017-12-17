﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData {

	public float uniformScale = 1f;
	public bool useFlatShading;
	public bool useFalloff;

	public float meshHeightMultiplayer;
	public AnimationCurve meshHeightCurve;

	public float minHeight {
		get {
			return uniformScale * meshHeightMultiplayer * meshHeightCurve.Evaluate (0);
		}
	}

	public float maxHeight {
		get {
			return uniformScale * meshHeightMultiplayer * meshHeightCurve.Evaluate (1);
		}
	}

}
