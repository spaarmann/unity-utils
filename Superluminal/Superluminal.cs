using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.LowLevel;

namespace UnityUtils {
	/* This class is based heavily on https://github.com/xoofx/SuperluminalPerf which carries the
	 * following license information:
Copyright (c) 2021, Alexandre Mutel
All rights reserved.

Redistribution and use in source and binary forms, with or without modification
, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this 
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice, 
   this list of conditions and the following disclaimer in the documentation 
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND 
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED 
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL 
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER 
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
	 */

	public static unsafe class Superluminal {
		private static delegate* unmanaged[Cdecl]<byte*, ushort, byte*, ushort, uint, void>
			nativeBeginEvent;
		private static delegate* unmanaged[Cdecl]<char*, ushort, char*, ushort, uint, void>
			nativeBeginEventWide;

		private static delegate* unmanaged[Cdecl]<PerformanceAPI_SuppressTailCallOptimization>
			nativeEndEvent;

		private static bool initialized;

		public const uint Version = (2 << 16);

		public static bool Enabled { get; set; } = true;

		[DllImport("kernel32.dll")]
		static extern IntPtr LoadLibrary(string lpLibFileName);
  
		[DllImport("kernel32.dll")]
		static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

		public static void Initialize() {
			if (initialized) return;
			initialized = true;

			IntPtr performanceAPI = LoadLibrary("PerformanceAPI.dll");
			if (performanceAPI == IntPtr.Zero) {
				Debug.LogError("Error initializing Superluminal Performance API: LoadLibrary failed.");
				return;
			}

			IntPtr getApiRaw = GetProcAddress(performanceAPI, "PerformanceAPI_GetAPI");
			if (getApiRaw == IntPtr.Zero) {
				Debug.LogError("Error initializing Superluminal Performance API: GetProcAddress failed.");
				return;
			}

			var getApi = (delegate* unmanaged[Cdecl]<uint, PerformanceAPI_Functions*, uint>) getApiRaw;
			PerformanceAPI_Functions functions;
			if (getApi(Version, &functions) == 1) {
				nativeBeginEvent =
					(delegate* unmanaged[Cdecl]<byte*, ushort, byte*, ushort, uint, void>)
					functions.BeginEventN;
				nativeBeginEventWide =
					(delegate* unmanaged[Cdecl]<char*, ushort, char*, ushort, uint, void>)
					functions.BeginEventWideN;
				nativeEndEvent =
					(delegate* unmanaged[Cdecl]<PerformanceAPI_SuppressTailCallOptimization>)
					functions.EndEvent;
			}
			else {
				Debug.LogError("Error initializing Superluminal Performance API: GetAPI failed.");
				return;
			}
		}

		public static void InstallPlayerLoopSystems() {
			PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
			for (var i = 0; i < loop.subSystemList.Length; i++) {
				if (loop.subSystemList[i].type == typeof(UnityEngine.PlayerLoop.Initialization)) {
					var initSystem = loop.subSystemList[i];

					var systemList = initSystem.subSystemList.ToList();
					systemList.Add(new PlayerLoopSystem {
						type = typeof(Superluminal),
						updateDelegate = () => {
							BeginEvent("Frame Start", $"Frame#: {Time.frameCount}");
							EndEvent();
						}
					});
					initSystem.subSystemList = systemList.ToArray();

					loop.subSystemList[i] = initSystem;
				}
			}
			PlayerLoop.SetPlayerLoop(loop);
		}

		public static void RemovePlayerLoopSystems() {
			PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
			for (var i = 0; i < loop.subSystemList.Length; i++) {
				if (loop.subSystemList[i].type == typeof(UnityEngine.PlayerLoop.Initialization)) {
					var initSystem = loop.subSystemList[i];

					var systemList = initSystem.subSystemList.ToList();
					systemList.RemoveAll(s => s.type == typeof(Superluminal));
					initSystem.subSystemList = systemList.ToArray();

					loop.subSystemList[i] = initSystem;
				}
			}
			PlayerLoop.SetPlayerLoop(loop);	
		}

		public static void BeginEvent(string eventId, string data) {
			BeginEvent(eventId, data, ProfilerColor.Default);
		}

		public static void BeginEvent(string eventId, string data, ProfilerColor color) {
			if (Enabled && nativeBeginEvent != null) {
				fixed (char* pEventId = eventId)
				fixed (char* pData = data) {
					nativeBeginEventWide(pEventId, (ushort) eventId.Length, pData,
						(ushort) (data?.Length ?? 0), color.Value);
				}
			}
		}

		public static void EndEvent() {
			if (Enabled && nativeEndEvent != null) {
				nativeEndEvent();
			}
		}

		public readonly struct ProfilerColor : IEquatable<ProfilerColor> {
			public static readonly ProfilerColor Default = new ProfilerColor(0xFFFF_FFFF);

			public ProfilerColor(byte r, byte g, byte b) {
				Value = (uint)((r << 24) | (g << 16) | (b << 8) | 0xFF);
			}

			public ProfilerColor(uint value) {
				Value = value;
			}

			public readonly uint Value;

			public bool Equals(ProfilerColor other) {
				return Value == other.Value;
			}

			public override bool Equals(object obj) {
				return obj is ProfilerColor other && Equals(other);
			}

			public override int GetHashCode() {
				return (int)Value;
			}

			public static bool operator ==(ProfilerColor left, ProfilerColor right) {
				return left.Equals(right);
			}

			public static bool operator !=(ProfilerColor left, ProfilerColor right) {
				return !left.Equals(right);
			}

			public override string ToString() {
				return $"#{Value:X8}";
			}
		}

#pragma warning disable 649
		/// <summary>
		/// Helper struct that is used to prevent calls to EndEvent from being optimized to jmp instructions as part of tail call optimization.
		/// You don't ever need to do anything with this as user of the API.
		/// </summary>
		private struct PerformanceAPI_SuppressTailCallOptimization {
			public long Value1;
			public long Value2;
			public long Value3;
		}

		private struct PerformanceAPI_Functions {
			public void* SetCurrentThreadName;
			public void* SetCurrentThreadNameN;
			public void* BeginEvent;
			public void* BeginEventN;
			public void* BeginEventWide;
			public void* BeginEventWideN;
			public void* EndEvent;
		}
#pragma warning restore 649
	}
}