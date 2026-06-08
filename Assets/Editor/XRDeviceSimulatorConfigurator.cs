using UnityEditor;
using UnityEngine;

public static class XRDeviceSimulatorConfigurator
{
	private const string SettingsAssetPath = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
	private const string SimulatorPrefabPath = "Assets/XRI/Samples/XR Device Simulator/XR Device Simulator.prefab";
	private const string EnableMenuPath = "Tools/XR/开启模拟器";
	private const string DisableMenuPath = "Tools/XR/关闭模拟器";

	[MenuItem (EnableMenuPath)]
	private static void EnableMenu ()
	{
		Configure (true, true);
	}

	[MenuItem (DisableMenuPath)]
	private static void DisableMenu ()
	{
		Configure (false, true);
	}

	[MenuItem (EnableMenuPath, true)]
	private static bool ValidateEnableMenu ()
	{
		bool enabled = IsSimulatorEnabled ();
		Menu.SetChecked (EnableMenuPath, enabled);
		Menu.SetChecked (DisableMenuPath, !enabled);
		return true;
	}

	[MenuItem (DisableMenuPath, true)]
	private static bool ValidateDisableMenu ()
	{
		bool enabled = IsSimulatorEnabled ();
		Menu.SetChecked (EnableMenuPath, enabled);
		Menu.SetChecked (DisableMenuPath, !enabled);
		return true;
	}

	private static void Configure (bool autoInstantiateValue, bool editorOnlyValue)
	{
		ScriptableObject settings = AssetDatabase.LoadAssetAtPath<ScriptableObject> (SettingsAssetPath);
		if (settings == null)
			return;

		GameObject simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject> (SimulatorPrefabPath);
		if (autoInstantiateValue && simulatorPrefab == null)
			return;

		SerializedObject serializedObject = new SerializedObject (settings);
		SerializedProperty autoInstantiate = serializedObject.FindProperty ("m_AutomaticallyInstantiateSimulatorPrefab");
		SerializedProperty editorOnly = serializedObject.FindProperty ("m_AutomaticallyInstantiateInEditorOnly");
		SerializedProperty simulator = serializedObject.FindProperty ("m_SimulatorPrefab");
		if (autoInstantiate == null || editorOnly == null || simulator == null)
			return;

		bool changed = false;
		if (autoInstantiate.boolValue != autoInstantiateValue) {
			autoInstantiate.boolValue = autoInstantiateValue;
			changed = true;
		}

		if (editorOnly.boolValue != editorOnlyValue) {
			editorOnly.boolValue = editorOnlyValue;
			changed = true;
		}

		if (autoInstantiateValue) {
			if (simulator.objectReferenceValue != simulatorPrefab) {
				simulator.objectReferenceValue = simulatorPrefab;
				changed = true;
			}
		} else if (simulator.objectReferenceValue != null) {
			simulator.objectReferenceValue = null;
			changed = true;
		}

		if (!changed)
			return;

		serializedObject.ApplyModifiedPropertiesWithoutUndo ();
		EditorUtility.SetDirty (settings);
		AssetDatabase.SaveAssets ();
		Debug.Log ("XR Device Simulator 设置已更新。", settings);
	}

	private static bool IsSimulatorEnabled ()
	{
		ScriptableObject settings = AssetDatabase.LoadAssetAtPath<ScriptableObject> (SettingsAssetPath);
		if (settings == null)
			return false;

		SerializedObject serializedObject = new SerializedObject (settings);
		SerializedProperty autoInstantiate = serializedObject.FindProperty ("m_AutomaticallyInstantiateSimulatorPrefab");
		if (autoInstantiate == null)
			return false;

		return autoInstantiate.boolValue;
	}
}
