using UnityEditor;
using UnityEngine;

public static class XRDeviceSimulatorConfigurator
{
	private const string SettingsAssetPath = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
	private const string SimulatorPrefabPath = "Assets/XRI/Samples/XR Device Simulator/XR Device Simulator.prefab";

	[InitializeOnLoadMethod]
	private static void ConfigureOnLoad ()
	{
		EditorApplication.delayCall += Configure;
	}

	[MenuItem ("Tools/XR/Configure XR Device Simulator")]
	private static void ConfigureMenu ()
	{
		Configure ();
	}

	private static void Configure ()
	{
		ScriptableObject settings = AssetDatabase.LoadAssetAtPath<ScriptableObject> (SettingsAssetPath);
		GameObject simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject> (SimulatorPrefabPath);
		if (settings == null || simulatorPrefab == null) {
			return;
		}

		SerializedObject serializedObject = new SerializedObject (settings);
		SerializedProperty autoInstantiate = serializedObject.FindProperty ("m_AutomaticallyInstantiateSimulatorPrefab");
		SerializedProperty editorOnly = serializedObject.FindProperty ("m_AutomaticallyInstantiateInEditorOnly");
		SerializedProperty simulator = serializedObject.FindProperty ("m_SimulatorPrefab");
		if (autoInstantiate == null || editorOnly == null || simulator == null) {
			return;
		}

		bool changed = false;
		if (!autoInstantiate.boolValue) {
			autoInstantiate.boolValue = true;
			changed = true;
		}
		if (!editorOnly.boolValue) {
			editorOnly.boolValue = true;
			changed = true;
		}
		if (simulator.objectReferenceValue != simulatorPrefab) {
			simulator.objectReferenceValue = simulatorPrefab;
			changed = true;
		}

		if (!changed) {
			return;
		}

		serializedObject.ApplyModifiedPropertiesWithoutUndo ();
		EditorUtility.SetDirty (settings);
		AssetDatabase.SaveAssets ();
		Debug.Log ("XR Device Simulator 已配置完成。", settings);
	}
}
