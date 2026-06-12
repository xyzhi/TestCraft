using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ModelShadowTool
{
	private const string DisableMenuPath = "Tools/模型阴影/关闭选中物体阴影";
	private const string EnableMenuPath = "Tools/模型阴影/开启选中物体阴影";

	[MenuItem (DisableMenuPath)]
	private static void DisableSelectedShadows ()
	{
		SetSelectedShadows (false);
	}

	[MenuItem (EnableMenuPath)]
	private static void EnableSelectedShadows ()
	{
		SetSelectedShadows (true);
	}

	[MenuItem (DisableMenuPath, true)]
	[MenuItem (EnableMenuPath, true)]
	private static bool ValidateSelection ()
	{
		return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
	}

	private static void SetSelectedShadows (bool enabled)
	{
		Renderer[] renderers = GetSelectedRenderers ();
		if (renderers.Length == 0) {
			EditorUtility.DisplayDialog ("模型阴影", "选中的物体和子物体没有 Renderer。", "确定");
			return;
		}

		Undo.RecordObjects (renderers, enabled ? "开启模型阴影" : "关闭模型阴影");

		HashSet<Scene> dirtyScenes = new HashSet<Scene> ();
		HashSet<Object> dirtyAssets = new HashSet<Object> ();
		foreach (Renderer renderer in renderers) {
			renderer.shadowCastingMode = enabled ? ShadowCastingMode.On : ShadowCastingMode.Off;
			renderer.receiveShadows = enabled;

			PrefabUtility.RecordPrefabInstancePropertyModifications (renderer);
			EditorUtility.SetDirty (renderer);

			if (EditorUtility.IsPersistent (renderer)) {
				dirtyAssets.Add (renderer);
			} else if (renderer.gameObject.scene.IsValid ()) {
				dirtyScenes.Add (renderer.gameObject.scene);
			}
		}

		foreach (Scene scene in dirtyScenes) {
			EditorSceneManager.MarkSceneDirty (scene);
		}

		if (dirtyAssets.Count > 0) {
			AssetDatabase.SaveAssets ();
		}

		Debug.LogFormat ("模型阴影工具：已{0} {1} 个 Renderer 的投射和接收阴影。", enabled ? "开启" : "关闭", renderers.Length);
	}

	private static Renderer[] GetSelectedRenderers ()
	{
		HashSet<Renderer> renderers = new HashSet<Renderer> ();
		foreach (GameObject selected in Selection.gameObjects) {
			if (selected == null)
				continue;

			foreach (Renderer renderer in selected.GetComponentsInChildren<Renderer> (true)) {
				if (renderer != null)
					renderers.Add (renderer);
			}
		}

		Renderer[] result = new Renderer[renderers.Count];
		renderers.CopyTo (result);
		return result;
	}
}
