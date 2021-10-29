using System;
using System.Runtime.InteropServices;

namespace UnityUtils {
	public static partial class RenderDoc {
		public static void StartCapture() {
			StartCapture_Impl();
		}

		public static void EndCapture() {
			EndCapture_Impl();
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	struct RENDERDOC_API_1_0_0 {
		public IntPtr GetAPIVersion;

		public IntPtr SetCaptureOptionU32;
		public IntPtr SetCaptureOptionF32;

		public IntPtr GetCaptureOptionU32;
		public IntPtr GetCaptureOptionF32;

		public IntPtr SetFocusToggleKeys;
		public IntPtr SetCaptureKeys;

		public IntPtr GetOverlayBits;
		public IntPtr MaskOverlayBits;

		public IntPtr Shutdown;
		public IntPtr UnloadCrashHandler;

		public IntPtr SetLogFilePathTemplate;
		public IntPtr GetLogFilePathTemplate;

		public IntPtr GetNumCaptures;
		public IntPtr GetCapture;

		public IntPtr TriggerCapture;

		public IntPtr IsRemoteAccessConnected;
		public IntPtr LaunchReplayUI;

		public IntPtr SetActiveWindow;

		public StartFrameCapture StartFrameCapture;
		public IntPtr IsFrameCapturing;
		public EndFrameCapture EndFrameCapture;
	}

	//typedef void (RENDERDOC_CC *pRENDERDOC_StartFrameCapture)(RENDERDOC_DevicePointer device, RENDERDOC_WindowHandle wndHandle);
	public delegate void StartFrameCapture(IntPtr device, IntPtr window);

	//typedef uint32_t (RENDERDOC_CC *pRENDERDOC_EndFrameCapture)(RENDERDOC_DevicePointer device, RENDERDOC_WindowHandle wndHandle);
	public delegate int EndFrameCapture(IntPtr device, IntPtr window);

	public static partial class RenderDoc {
		[DllImport("renderdoc.dll", CharSet = CharSet.Unicode,
			CallingConvention = CallingConvention.Cdecl)]
		private static extern int RENDERDOC_GetAPI(int version, out IntPtr outAPIPointers);

		private static int eRENDERDOC_API_Version_1_0_0 = 10000;

		private static void StartCapture_Impl() {
			IntPtr pAPI = new IntPtr();
			int ret = RENDERDOC_GetAPI(eRENDERDOC_API_Version_1_0_0, out pAPI);

			RENDERDOC_API_1_0_0 api =
				(RENDERDOC_API_1_0_0)Marshal.PtrToStructure(pAPI, typeof(RENDERDOC_API_1_0_0));

			api.StartFrameCapture(new IntPtr(), new IntPtr());
		}

		private static void EndCapture_Impl() {
			IntPtr pAPI = new IntPtr();
			int ret = RENDERDOC_GetAPI(eRENDERDOC_API_Version_1_0_0, out pAPI);

			RENDERDOC_API_1_0_0 api =
				(RENDERDOC_API_1_0_0)Marshal.PtrToStructure(pAPI, typeof(RENDERDOC_API_1_0_0));

			api.EndFrameCapture(new IntPtr(), new IntPtr());
		}
	}
}