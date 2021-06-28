using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityUtils {
	public class TimeSlider : EditorWindow {
		[MenuItem("Window/Util/Time Slider")]
		public static void OpenWindow() => GetWindow<TimeSlider>(false, "Time", true);

		private void OnGUI() {
			if (Application.isPlaying) {
				GUILayout.Label($"Time Scale: {Time.timeScale}");
				Time.timeScale = GUILayout.HorizontalSlider(Time.timeScale, 0f, 2f);
			}
			else {
				GUILayout.Label("Use Project Settings to modify time scale in edit mode!");
			}
		}
	}
}
