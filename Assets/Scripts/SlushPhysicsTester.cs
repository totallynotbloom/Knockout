using UnityEngine;
using UnityEngine.InputSystem; // You need this namespace!

public class SlushPhysicsTester : MonoBehaviour
{
	private Rigidbody rb;

	void Start()
	{
		rb = GetComponent<Rigidbody>();
	}

	void Update()
	{
		// Use Keyboard.current to check for key presses in the Update loop
		var keyboard = Keyboard.current;
		if (keyboard == null) return; // Safely exit if no keyboard is detected

		// THE LAUNCH (W Key)
		if (keyboard.wKey.wasPressedThisFrame)
		{
			rb.AddForce(new Vector3(5, 15, 0), ForceMode.Impulse);
			Debug.Log("Launcher Hit!");
		}

		// THE BASH (D Key)
		if (keyboard.dKey.wasPressedThisFrame)
		{
			rb.AddForce(new Vector3(20, 2, 0), ForceMode.Impulse);
			Debug.Log("Basher Hit!");
		}

		// THE SLAM (S Key)
		if (keyboard.sKey.wasPressedThisFrame)
		{
			rb.AddForce(new Vector3(8, -15, 0), ForceMode.Impulse);
			Debug.Log("Spiker Hit!");
		}
	}
}