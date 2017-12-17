using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject {

	public event System.Action OnValueUpdated;
	public bool autoUpdate;

	protected virtual void OnValidate () {
		if (autoUpdate)
			UnityEditor.EditorApplication.update += NotifyOfUpdateValues;
	}

	public void NotifyOfUpdateValues () {
		UnityEditor.EditorApplication.update -= NotifyOfUpdateValues;
		if (OnValueUpdated != null)
			OnValueUpdated ();
	}

}
