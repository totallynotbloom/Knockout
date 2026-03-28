using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MapSwapper : MonoBehaviour
{
	void Update()
	{
		var keyboard = Keyboard.current;
		if (keyboard == null) return;

		// Press 'M' to toggle between Scene 0 and Scene 1
		if (keyboard.mKey.wasPressedThisFrame)
		{
			int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

			// If we are in Map 1 (Index 0), go to Map 2 (Index 1), and vice versa
			int nextSceneIndex = (currentSceneIndex == 0) ? 1 : 0;

			SceneManager.LoadScene(nextSceneIndex);
		}
	}
}