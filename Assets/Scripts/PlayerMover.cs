using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using WebSocketSharp;
//using NumSharp;

//public class SimpleCapsuleWithStickMovement : MonoBehaviour
public class PlayerMover : MonoBehaviour
{
	public bool EnableLinearMovement = true;
	public bool EnableRotation = true;
	public bool HMDRotatesPlayer = false;
	public bool RotationEitherThumbstick = false;
	public float RotationAngle = (360f / 248f); 
	public float Speed = 10.0f;
	public OVRCameraRig CameraRig;

	private bool ReadyToSnapTurn;
	private Rigidbody _rigidbody;

	public event Action CameraUpdated;
	public event Action PreCharacterMove;

	// Adding websocket stuff:
	WebSocket ws;
	[System.Serializable]
	public class MessageData
	{
		public float translation = 0f;
		public int rotation = 0;
	}
	MessageData objectState = new MessageData();

	private void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
		if (CameraRig == null) CameraRig = GetComponentInChildren<OVRCameraRig>();
	}

	void Start ()
	{
		// Firing up the Websocket on start:

		// The socketAddress:
		//var nd = np.full(5, 12); //[5, 5, 5 .. 5]
		//var nn = np.load("244.npy");
		//Debug.Log(nn["0,0,:"]);

        // Raspberry Pi:
		string socketAddress = "ws://10.0.0.86:3140";

        // Macbook: 
        //string socketAddress = "ws://127.0.0.1:5678";

		ws = new WebSocket(socketAddress);
		ws.OnMessage += (sender, e) =>
		{
			// Parse the message and update state:
			objectState = UnityEngine.JsonUtility.FromJson<MessageData>(e.Data);
		};
		ws.Connect();
	}

	// This is what we us to make things smooth?

	// How can we incoporate the websocket into this: 
	private void FixedUpdate()
	{
        if (CameraUpdated != null) CameraUpdated();
        if (PreCharacterMove != null) PreCharacterMove();

        if (HMDRotatesPlayer) RotatePlayerToHMD();
		if (EnableLinearMovement) StickMovement();
		if (EnableRotation) SnapTurn();
	}

    // This could be messing with my rotation input below!! (uncheck the box the Component's dropdown menu!)
    void RotatePlayerToHMD()
    {
		Transform root = CameraRig.trackingSpace;
		Transform centerEye = CameraRig.centerEyeAnchor;

		Vector3 prevPos = root.position;
		Quaternion prevRot = root.rotation;

		transform.rotation = Quaternion.Euler(0.0f, centerEye.rotation.eulerAngles.y, 0.0f);

		root.position = prevPos;
		root.rotation = prevRot;
    }

    // Incrementally moves player in steps... a bit clunky! 
	void StickMovement()
	{
		Quaternion ort = CameraRig.centerEyeAnchor.rotation;
		Vector3 ortEuler = ort.eulerAngles;
		ortEuler.z = ortEuler.x = 0f;
		ort = Quaternion.Euler(ortEuler);

        // Stick move L/R only; 
		Vector3 moveDir = Vector3.zero;
		Vector2 primaryAxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
		//moveDir += ort * (primaryAxis.x * Vector3.right);
		moveDir += ort * (primaryAxis.y * Vector3.forward);
		//_rigidbody.MovePosition(_rigidbody.transform.position + moveDir * Speed * Time.fixedDeltaTime);
		_rigidbody.MovePosition(_rigidbody.position + moveDir * Speed * Time.fixedDeltaTime);
		// Prepping for websocket movement:
        // Websocket will take care of forward/back motion! 
		if (objectState.translation != 0.0f)
		{
			Quaternion ort_ws = CameraRig.centerEyeAnchor.rotation;
			Vector3 ortEuler_ws = ort_ws.eulerAngles;
			ortEuler_ws.z = ortEuler_ws.x = 0f;
			ort_ws = Quaternion.Euler(ortEuler_ws);
			Vector3 moveDir_ws = Vector3.zero;
			moveDir_ws += ort_ws * (objectState.translation * Vector3.forward);
			//_rigidbody.MovePosition(_rigidbody.transform.position + moveDir * Speed * Time.fixedDeltaTime);
			_rigidbody.MovePosition(_rigidbody.position + moveDir_ws * Speed * Time.fixedDeltaTime);
		}
		else
		{
			objectState.translation = 0.0f;
		}

	}

	// Rotates 
	void SnapTurn()
	{
        // Get angles: 
		Vector3 euler = transform.rotation.eulerAngles;
		Vector2 secondaryAxis = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if(Mathf.Abs(secondaryAxis.x) > 0.3 && ReadyToSnapTurn)
        {
			if(secondaryAxis.x < 0)
            {
			    euler.y -= RotationAngle;
				ReadyToSnapTurn = false;
				objectState.rotation = 0;
			}
            else
            {
				euler.y += RotationAngle;
				ReadyToSnapTurn = false;
				objectState.rotation = 0;
			}
		}
        else if (objectState.rotation != 0 && ReadyToSnapTurn)
        {
			euler.y += objectState.rotation * RotationAngle;
			ReadyToSnapTurn = false;
			objectState.rotation = 0;
		}
        else
        {
			ReadyToSnapTurn = true;
        }
		transform.rotation = Quaternion.Euler(euler);
	}
}
