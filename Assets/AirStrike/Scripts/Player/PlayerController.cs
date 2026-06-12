/// <summary>
/// Player controller.
/// </summary>
using UnityEngine;
using System.Collections;
using UnityEngine.XR;
using HWRWeaponSystem;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AirStrikeKit
{
	[RequireComponent (typeof(FlightSystem))]

	public class PlayerController : MonoBehaviour
	{
		// 飞行核心组件，负责姿态、速度和武器调用。
		FlightSystem flight;
		// 镜头切换组件，桌面版和调试界面会用到。
		FlightView View;
		// 玩家控制器总开关，关闭后不再处理输入。
		public bool Active = true;
		// 桌面/移动端的简化操控模式。
		public bool SimpleControl;
		// 移动端触屏控制灵敏度。
		public float AccelerationSensitivity = 5;
		// 移动端左半屏虚拟摇杆区域。
		private TouchScreenVal controllerTouch;
		// 移动端开火触控区域。
		private TouchScreenVal fireTouch;
		// 移动端切武器触控区域。
		private TouchScreenVal switchTouch;
		// 移动端油门滑动区域。
		private TouchScreenVal sliceTouch;
		// 调试界面使用的皮肤。
		public GUISkin skin;
		// 是否显示调试/教学按钮。
		public bool ShowHowto;
		[Header ("VR")]
		// VR 瞄准源提供者，负责头显/控制器射线方向。
		public VRAimProvider VRAimProvider;
		// VR 摇杆控制飞行姿态的灵敏度。
		public float VRAxisSensitivity = 1.2f;
		// VR 偏航控制灵敏度。
		public float VRYawSensitivity = 1.2f;
		// VR 右扳机判定为“开火”的阈值。
		public float VRTriggerThreshold = 0.6f;
		// VR 摇杆死区，避免控制器轻微漂移。
		public float VRJoystickDeadZone = 0.2f;
		// 是否使用右手控制器作为瞄准方向。
		public bool VRUseRightControllerAiming = true;

		// 左手 XR 设备句柄。
		private UnityEngine.XR.InputDevice leftHandDevice;
		// 右手 XR 设备句柄。
		private UnityEngine.XR.InputDevice rightHandDevice;
		// 记录上一帧是否按下切武器，避免长按连触发。
		private bool vrSwitchWeaponPressed;
		// 记录上一帧是否按下暂停键。
		private bool vrPausePressed;
		// 记录上一帧是否按下切视角键。
		private bool vrViewPressed;
		// 当前是否正在使用 VR 输入逻辑。
		private bool useVrInput;
		private Coroutine waitForVRReadyCoroutine;
		#if ENABLE_INPUT_SYSTEM
		// 左手主摇杆输入 action。
		private InputAction leftPrimary2DAxisAction;
		// 右手主摇杆输入 action。
		private InputAction rightPrimary2DAxisAction;
		// 右手扳机输入 action。
		private InputAction rightTriggerAction;
		// 左手主按钮输入 action。
		private InputAction leftPrimaryButtonAction;
		// 右手主按钮输入 action。
		private InputAction rightPrimaryButtonAction;
		// 左手菜单按钮输入 action。
		private InputAction leftMenuButtonAction;
		// 右手副按钮输入 action。
		private InputAction rightSecondaryButtonAction;
		// 左手副按钮输入 action。
		private InputAction leftSecondaryButtonAction;
		#endif

		// 对外暴露当前是否处于 VR 控制模式。
		public bool IsVRActive {
			get {
				return useVrInput;
			}
		}

		// 初始化全局玩家引用，方便其他系统访问当前玩家控制器。
		void Awake(){
			AirStrikeGame.playerController = this;
		}

		// 缓存核心组件并初始化移动端/VR 输入引用。
		void Start ()
		{
			flight = this.GetComponent<FlightSystem> ();
			View = (FlightView)GameObject.FindObjectOfType (typeof(FlightView));
			// setting all Touch screen controller in the position
			controllerTouch = new TouchScreenVal (new Rect (0, 0, Screen.width / 2, Screen.height - 100));
			fireTouch = new TouchScreenVal (new Rect (Screen.width / 2, 0, Screen.width / 2, Screen.height));
			switchTouch = new TouchScreenVal (new Rect (0, Screen.height - 100, Screen.width / 2, 100));
			sliceTouch = new TouchScreenVal (new Rect (0, 0, Screen.width / 2, 50));
			if (ShouldUseVRInput ()) {
				EnableVRInput ();
			} else {
				waitForVRReadyCoroutine = StartCoroutine (WaitForVRDeviceReady ());
			}
		}

		// 退出时释放 Input System 动态创建的 VR actions。
		void OnDestroy ()
		{
			DisposeVRActions ();
		}

		private bool ShouldUseVRInput ()
		{
			return XRSettings.isDeviceActive || IsEditorSimulatorActive ();
		}

		private void EnableVRInput ()
		{
			if (useVrInput)
				return;

			useVrInput = true;
			SetupVRActions ();
			RefreshVRState ();
		}

		private IEnumerator WaitForVRDeviceReady ()
		{
			const float timeout = 10f;
			float startTime = Time.realtimeSinceStartup;
			while (!useVrInput && Time.realtimeSinceStartup - startTime < timeout) {
				if (ShouldUseVRInput ()) {
					EnableVRInput ();
					yield break;
				}

				yield return null;
			}

			waitForVRReadyCoroutine = null;
		}

		// 每帧根据当前运行环境切换到 VR、桌面或移动端输入。
		void Update ()
		{
			if (!flight || !Active)
				return;

			//VR模式
			if (useVrInput)
			{
				RefreshVRState ();
				VRController();
				return;
			}
			else
			{
#if UNITY_EDITOR || UNITY_WEBPLAYER || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
				// On Desktop
				DesktopController();
#else
			// On Mobile device
			MobileController ();
#endif
			}
		}

		// 刷新 VR 状态，并在进入 VR 模式时补齐控制器与瞄准引用。
		void RefreshVRState ()
		{
			if (!useVrInput)
				return;

			if (!leftHandDevice.isValid) {
				leftHandDevice = InputDevices.GetDeviceAtXRNode (XRNode.LeftHand);
			}
			if (!rightHandDevice.isValid) {
				rightHandDevice = InputDevices.GetDeviceAtXRNode (XRNode.RightHand);
			}

			if (!VRAimProvider) {
				VRAimProvider = GameObject.FindObjectOfType<VRAimProvider> ();
			}
			if (!VRAimProvider) {
				VRAimProvider = CreateOrFindVRAimProvider ();
			}
			if (VRAimProvider) {
				VRAimProvider.CurrentAimSource = VRUseRightControllerAiming ? VRAimProvider.AimSource.RightController : VRAimProvider.AimSource.Head;
				VRAimProvider.RefreshReferences ();
			}
			ApplyVRAimingTargets ();
		}

		// 编辑器下通过场景里是否存在 XR Device Simulator 判断是否启用模拟 VR。
		bool IsEditorSimulatorActive ()
		{
			#if UNITY_EDITOR
			return GameObject.Find ("XR Device Simulator") != null;
			#else
			return false;
			#endif
		}

		// 把所有武器的瞄准来源改为当前 VR 头显/控制器方向。
		void ApplyVRAimingTargets ()
		{
			if (!useVrInput)
				return;

			if (!flight || flight.WeaponControl == null || VRAimProvider == null)
				return;

			WeaponLauncher[] launchers = flight.WeaponControl.WeaponLists;
			if (launchers == null)
				return;

			Transform aimTransform = VRAimProvider.GetAimTransform ();
			Camera aimCamera = VRAimProvider.GetAimCamera ();
			for (int i = 0; i < launchers.Length; i++) {
				if (launchers [i] == null)
					continue;
				launchers [i].AimOverride = aimTransform;
				launchers [i].AimCameraOverride = aimCamera;
			}
		}

		// VR 输入主循环，负责飞行、开火、切武器、暂停和切镜头。
		void VRController ()
		{
			if (!useVrInput)
				return;

			flight.SimpleControl = false;
			flight.FixedX = false;
			flight.FixedY = false;
			flight.FixedZ = false;
			MouseLock.MouseLocked = false;

			Vector2 leftAxis = ReadVRPrimary2DAxis (leftHandDevice, leftPrimary2DAxisAction);
			Vector2 rightAxis = ReadVRPrimary2DAxis (rightHandDevice, rightPrimary2DAxisAction);
			leftAxis = ApplyDeadZone (leftAxis);
			rightAxis = ApplyDeadZone (rightAxis);

			flight.AxisControl (new Vector2 (rightAxis.x, -leftAxis.y) * VRAxisSensitivity);
			flight.TurnControl (leftAxis.x * VRYawSensitivity);
			flight.SpeedUp (rightAxis.y);

			float triggerValue = ReadVRTrigger (rightHandDevice, rightTriggerAction);
			bool firePressed = triggerValue >= VRTriggerThreshold;
			if (firePressed) {
				flight.WeaponControl.LaunchWeapon ();
			}

			bool switchPressed = ReadVRButton (rightHandDevice, UnityEngine.XR.CommonUsages.primaryButton, rightPrimaryButtonAction) || ReadVRButton (leftHandDevice, UnityEngine.XR.CommonUsages.primaryButton, leftPrimaryButtonAction);
			if (switchPressed && !vrSwitchWeaponPressed) {
				flight.WeaponControl.SwitchWeapon ();
			}
			vrSwitchWeaponPressed = switchPressed;

			bool pausePressed = ReadVRButton (leftHandDevice, UnityEngine.XR.CommonUsages.menuButton, leftMenuButtonAction) || ReadVRButton (rightHandDevice, UnityEngine.XR.CommonUsages.secondaryButton, rightSecondaryButtonAction);
			if (pausePressed && !vrPausePressed && AirStrikeGame.gameUI) {
				AirStrikeGame.gameUI.TogglePause ();
			}
			vrPausePressed = pausePressed;

			bool viewPressed = ReadVRButton (leftHandDevice, UnityEngine.XR.CommonUsages.secondaryButton, leftSecondaryButtonAction);
			if (viewPressed && !vrViewPressed && View) {
				View.SwitchCameras ();
			}
			vrViewPressed = viewPressed;
		}

		// 读取 VR 摇杆输入，优先走 Input System，拿不到再回退到 XR CommonUsages。
		Vector2 ReadVRPrimary2DAxis (UnityEngine.XR.InputDevice device, InputAction action)
		{
			#if ENABLE_INPUT_SYSTEM
			if (action != null && action.enabled) {
				return action.ReadValue<Vector2> ();
			}
			#endif
			return ReadAxis2D (device, UnityEngine.XR.CommonUsages.primary2DAxis);
		}

		// 读取 VR 扳机值，优先走 Input System，拿不到再回退到 XR CommonUsages。
		float ReadVRTrigger (UnityEngine.XR.InputDevice device, InputAction action)
		{
			#if ENABLE_INPUT_SYSTEM
			if (action != null && action.enabled) {
				return action.ReadValue<float> ();
			}
			#endif
			return ReadAxis1D (device, UnityEngine.XR.CommonUsages.trigger);
		}

		// 从 XR 设备读取二维轴输入。
		Vector2 ReadAxis2D (UnityEngine.XR.InputDevice device, InputFeatureUsage<Vector2> usage)
		{
			Vector2 value;
			if (device.isValid && device.TryGetFeatureValue (usage, out value)) {
				return value;
			}
			return Vector2.zero;
		}

		// 从 XR 设备读取单轴输入。
		float ReadAxis1D (UnityEngine.XR.InputDevice device, InputFeatureUsage<float> usage)
		{
			float value;
			if (device.isValid && device.TryGetFeatureValue (usage, out value)) {
				return value;
			}
			return 0;
		}

		// 从 XR 设备读取布尔按钮输入。
		bool ReadButton (UnityEngine.XR.InputDevice device, InputFeatureUsage<bool> usage)
		{
			bool value;
			if (device.isValid && device.TryGetFeatureValue (usage, out value)) {
				return value;
			}
			return false;
		}

		// 读取 VR 按钮，优先走 Input System，拿不到再回退到 XR CommonUsages。
		bool ReadVRButton (UnityEngine.XR.InputDevice device, InputFeatureUsage<bool> usage, InputAction action)
		{
			#if ENABLE_INPUT_SYSTEM
			if (action != null && action.enabled) {
				return action.IsPressed ();
			}
			#endif
			return ReadButton (device, usage);
		}

		// 创建并启用 VR 所需的 Input System actions。
		void SetupVRActions ()
		{
			#if ENABLE_INPUT_SYSTEM
			if (leftPrimary2DAxisAction != null)
				return;

			leftPrimary2DAxisAction = CreateVRValueAction ("Left Primary 2D Axis", "<XRController>{LeftHand}/primary2DAxis");
			rightPrimary2DAxisAction = CreateVRValueAction ("Right Primary 2D Axis", "<XRController>{RightHand}/primary2DAxis");
			rightTriggerAction = CreateVRValueAction ("Right Trigger", "<XRController>{RightHand}/trigger");
			leftPrimaryButtonAction = CreateVRButtonAction ("Left Primary Button", "<XRController>{LeftHand}/primaryButton");
			rightPrimaryButtonAction = CreateVRButtonAction ("Right Primary Button", "<XRController>{RightHand}/primaryButton");
			leftMenuButtonAction = CreateVRButtonAction ("Left Menu Button", "<XRController>{LeftHand}/menuButton");
			rightSecondaryButtonAction = CreateVRButtonAction ("Right Secondary Button", "<XRController>{RightHand}/secondaryButton");
			leftSecondaryButtonAction = CreateVRButtonAction ("Left Secondary Button", "<XRController>{LeftHand}/secondaryButton");
			#endif
		}

		// 释放动态创建的 VR Input System actions。
		void DisposeVRActions ()
		{
			#if ENABLE_INPUT_SYSTEM
			DisposeAction (leftPrimary2DAxisAction);
			DisposeAction (rightPrimary2DAxisAction);
			DisposeAction (rightTriggerAction);
			DisposeAction (leftPrimaryButtonAction);
			DisposeAction (rightPrimaryButtonAction);
			DisposeAction (leftMenuButtonAction);
			DisposeAction (rightSecondaryButtonAction);
			DisposeAction (leftSecondaryButtonAction);
			leftPrimary2DAxisAction = null;
			rightPrimary2DAxisAction = null;
			rightTriggerAction = null;
			leftPrimaryButtonAction = null;
			rightPrimaryButtonAction = null;
			leftMenuButtonAction = null;
			rightSecondaryButtonAction = null;
			leftSecondaryButtonAction = null;
			#endif
		}

		#if ENABLE_INPUT_SYSTEM
		// 创建一个 Value 类型的 VR 输入 action。
		InputAction CreateVRValueAction (string actionName, string bindingPath)
		{
			InputAction action = new InputAction (actionName, InputActionType.Value, bindingPath);
			action.Enable ();
			return action;
		}

		// 创建一个 Button 类型的 VR 输入 action。
		InputAction CreateVRButtonAction (string actionName, string bindingPath)
		{
			InputAction action = new InputAction (actionName, InputActionType.Button, bindingPath);
			action.Enable ();
			return action;
		}

		// 安全释放单个 Input System action。
		void DisposeAction (InputAction action)
		{
			if (action == null)
				return;
			action.Disable ();
			action.Dispose ();
		}
		#endif

		// 对摇杆输入应用死区并重新映射有效范围。
		Vector2 ApplyDeadZone (Vector2 axis)
		{
			if (axis.magnitude < VRJoystickDeadZone)
				return Vector2.zero;
			float normalizedMagnitude = Mathf.InverseLerp (VRJoystickDeadZone, 1, axis.magnitude);
			return axis.normalized * normalizedMagnitude;
		}

		// 优先从主相机或当前对象上创建/获取 VRAimProvider。
		VRAimProvider CreateOrFindVRAimProvider ()
		{
			Camera mainCamera = Camera.main;
			if (!mainCamera) {
				mainCamera = GameObject.FindObjectOfType<Camera> ();
			}

			GameObject host = mainCamera != null ? mainCamera.gameObject : this.gameObject;
			VRAimProvider provider = host.GetComponent<VRAimProvider> ();
			if (!provider) {
				provider = host.AddComponent<VRAimProvider> ();
			}
			return provider;
		}

		// 桌面版输入：W/S 控制三档速度，A/D 控制左右侧飞，鼠标控制俯仰与偏航。
		void DesktopController ()
		{
			// Desktop controller
			flight.SimpleControl = SimpleControl;
		
			// lock mouse position to the center.
			MouseLock.MouseLocked = true;

			// 键盘横向输入对应 VR 右手摇杆 X：左右侧飞。
			float keyboardStrafe = Input.GetAxis ("Horizontal");
			// 鼠标纵向输入对应 VR 左手摇杆 Y：上下转向中的俯仰。
			float mousePitch = -Input.GetAxis ("Mouse Y");
			// 鼠标横向输入对应 VR 左手摇杆 X：左右转向中的偏航。
			float mouseYaw = Input.GetAxis ("Mouse X");
			// 键盘纵向输入对应 VR 右手摇杆 Y：三档速度控制。
			float keyboardSpeed = Input.GetAxis ("Vertical");

			flight.AxisControl (new Vector2 (keyboardStrafe, mousePitch));
			flight.TurnControl (mouseYaw);
			flight.SpeedUp (keyboardSpeed);
		
		
			if (Input.GetButton ("Fire1")) {
				flight.WeaponControl.LaunchWeapon ();
			}
		
			if (Input.GetButtonDown ("Fire2")) {
				flight.WeaponControl.SwitchWeapon ();
			}
		
			if (Input.GetKeyDown (KeyCode.C)) {
				if (View)
					View.SwitchCameras ();	
			}	
		}

		// 移动端输入：仅保留触屏虚拟摇杆控制。
		void MobileController ()
		{
			// Mobile controller
		
			flight.SimpleControl = SimpleControl;
			flight.FixedX = true;
			flight.FixedY = false;
			flight.FixedZ = true;
			// get axis control from touch screen
			Vector2 dir = controllerTouch.OnDragDirection (true);
			dir = Vector2.ClampMagnitude (dir, 1.0f);
			flight.AxisControl (new Vector2 (dir.x, -dir.y) * AccelerationSensitivity * 0.7f);
			flight.TurnControl (dir.x * AccelerationSensitivity * 0.3f);
			sliceTouch.OnDragDirection (true);
			// slice speed
			flight.SpeedUp (sliceTouch.slideVal.x);
		
			if (fireTouch.OnTouchPress ()) {
				flight.WeaponControl.LaunchWeapon ();
			}	
			if (switchTouch.OnTouchPress ()) {
		
			}	
		}
	
	
		// 调试 UI，可按需删除。
		void OnGUI ()
		{
			if (!ShowHowto)
				return;
		
			if (skin)
				GUI.skin = skin;
		
			if (GUI.Button (new Rect (20, 150, 200, 40), "Change View")) {
				if (View)
					View.SwitchCameras ();	
			}

			if (useVrInput) {
				GUI.Label (new Rect (20, 390, 500, 40), "VR Left Stick : Yaw/Pitch  Right Stick : Roll/3-Speed  Trigger : Fire");
			} else {
				GUI.Label (new Rect (20, 390, 500, 40), "PC Mouse : Yaw/Pitch  A/D : Strafe  W/S : 3-Speed  Left Click : Fire");
			}
		
			if (GUI.Button (new Rect (20, 200, 200, 40), "Change Weapons")) {
				if (flight)
					flight.WeaponControl.SwitchWeapon ();
			}
		
			if (GUI.Button (new Rect (20, 250, 200, 40), "Simple Control " + SimpleControl)) {
				if (flight)
					SimpleControl = !SimpleControl;
			}

			GUI.Label (new Rect (20, 300, 500, 40), "you can remove this in OnGUI in PlayerController.cs");
		}
	}
}
