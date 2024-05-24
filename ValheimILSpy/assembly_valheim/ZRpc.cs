using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

public class ZRpc : IDisposable
{
	public enum ErrorCode
	{
		Success,
		Disconnected,
		IncompatibleVersion
	}

	private interface RpcMethodBase
	{
		void Invoke(ZRpc rpc, ZPackage pkg);
	}

	public class RpcMethod : RpcMethodBase
	{
		public delegate void Method(ZRpc RPC);

		private Method m_action;

		public RpcMethod(Method action)
		{
			m_action = action;
		}

		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			m_action(rpc);
		}
	}

	private class RpcMethod<T> : RpcMethodBase
	{
		private Action<ZRpc, T> m_action;

		public RpcMethod(Action<ZRpc, T> action)
		{
			m_action = action;
		}

		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			m_action.DynamicInvoke(Deserialize(rpc, m_action.Method.GetParameters(), pkg));
		}
	}

	private class RpcMethod<T, U> : RpcMethodBase
	{
		private Action<ZRpc, T, U> m_action;

		public RpcMethod(Action<ZRpc, T, U> action)
		{
			m_action = action;
		}

		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			m_action.DynamicInvoke(Deserialize(rpc, m_action.Method.GetParameters(), pkg));
		}
	}

	private class RpcMethod<T, U, V> : RpcMethodBase
	{
		private Action<ZRpc, T, U, V> m_action;

		public RpcMethod(Action<ZRpc, T, U, V> action)
		{
			m_action = action;
		}

		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			m_action.DynamicInvoke(Deserialize(rpc, m_action.Method.GetParameters(), pkg));
		}
	}

	public class RpcMethod<T, U, V, B> : RpcMethodBase
	{
		public delegate void Method(ZRpc RPC, T p0, U p1, V p2, B p3);

		private Method m_action;

		public RpcMethod(Method action)
		{
			m_action = action;
		}

		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			m_action.DynamicInvoke(Deserialize(rpc, m_action.Method.GetParameters(), pkg));
		}
	}

	private ISocket m_socket;

	private ZPackage m_pkg = new ZPackage();

	private Dictionary<int, RpcMethodBase> m_functions = new Dictionary<int, RpcMethodBase>();

	private int m_sentPackages;

	private int m_sentData;

	private int m_recvPackages;

	private int m_recvData;

	private float m_pingTimer;

	private float m_timeSinceLastPing;

	private static float m_pingInterval = 1f;

	private static float m_timeout = 30f;

	private static bool m_DEBUG = false;

	public ZRpc(ISocket socket)
	{
		m_socket = socket;
	}

	public void Dispose()
	{
		m_socket.Dispose();
	}

	public ISocket GetSocket()
	{
		return m_socket;
	}

	public ErrorCode Update(float dt)
	{
		if (!m_socket.IsConnected())
		{
			return ErrorCode.Disconnected;
		}
		for (ZPackage zPackage = m_socket.Recv(); zPackage != null; zPackage = m_socket.Recv())
		{
			m_recvPackages++;
			m_recvData += zPackage.Size();
			try
			{
				HandlePackage(zPackage);
			}
			catch (EndOfStreamException ex)
			{
				ZLog.LogError("EndOfStreamException in ZRpc::HandlePackage: Assume incompatible version: " + ex.Message);
				return ErrorCode.IncompatibleVersion;
			}
			catch (Exception ex2)
			{
				ZLog.Log("Exception in ZRpc::HandlePackage: " + ex2);
			}
		}
		UpdatePing(dt);
		return ErrorCode.Success;
	}

	private void UpdatePing(float dt)
	{
		m_pingTimer += dt;
		if (m_pingTimer > m_pingInterval)
		{
			m_pingTimer = 0f;
			m_pkg.Clear();
			m_pkg.Write(0);
			m_pkg.Write(data: true);
			SendPackage(m_pkg);
		}
		m_timeSinceLastPing += dt;
		if (m_timeSinceLastPing > m_timeout)
		{
			ZLog.LogWarning("ZRpc timeout detected");
			m_socket.Close();
		}
	}

	private void ReceivePing(ZPackage package)
	{
		if (package.ReadBool())
		{
			m_pkg.Clear();
			m_pkg.Write(0);
			m_pkg.Write(data: false);
			SendPackage(m_pkg);
		}
		else
		{
			m_timeSinceLastPing = 0f;
		}
	}

	public float GetTimeSinceLastPing()
	{
		return m_timeSinceLastPing;
	}

	public bool IsConnected()
	{
		return m_socket.IsConnected();
	}

	private void HandlePackage(ZPackage package)
	{
		int num = package.ReadInt();
		RpcMethodBase value2;
		if (num == 0)
		{
			ReceivePing(package);
		}
		else if (m_DEBUG)
		{
			package.ReadString();
			if (m_functions.TryGetValue(num, out var value))
			{
				value.Invoke(this, package);
			}
		}
		else if (m_functions.TryGetValue(num, out value2))
		{
			value2.Invoke(this, package);
		}
	}

	public void Register(string name, RpcMethod.Method f)
	{
		int stableHashCode = name.GetStableHashCode();
		m_functions.Remove(stableHashCode);
		m_functions.Add(stableHashCode, new RpcMethod(f));
	}

	public void Register<T>(string name, Action<ZRpc, T> f)
	{
		int stableHashCode = name.GetStableHashCode();
		m_functions.Remove(stableHashCode);
		m_functions.Add(stableHashCode, new RpcMethod<T>(f));
	}

	public void Register<T, U>(string name, Action<ZRpc, T, U> f)
	{
		int stableHashCode = name.GetStableHashCode();
		m_functions.Remove(stableHashCode);
		m_functions.Add(stableHashCode, new RpcMethod<T, U>(f));
	}

	public void Register<T, U, V>(string name, Action<ZRpc, T, U, V> f)
	{
		int stableHashCode = name.GetStableHashCode();
		m_functions.Remove(stableHashCode);
		m_functions.Add(stableHashCode, new RpcMethod<T, U, V>(f));
	}

	public void Register<T, U, V, W>(string name, RpcMethod<T, U, V, W>.Method f)
	{
		int stableHashCode = name.GetStableHashCode();
		m_functions.Remove(stableHashCode);
		m_functions.Add(stableHashCode, new RpcMethod<T, U, V, W>(f));
	}

	public void Unregister(string name)
	{
		int stableHashCode = name.GetStableHashCode();
		m_functions.Remove(stableHashCode);
	}

	public void Invoke(string method, params object[] parameters)
	{
		if (IsConnected())
		{
			m_pkg.Clear();
			int stableHashCode = method.GetStableHashCode();
			m_pkg.Write(stableHashCode);
			if (m_DEBUG)
			{
				m_pkg.Write(method);
			}
			Serialize(parameters, ref m_pkg);
			SendPackage(m_pkg);
		}
	}

	private void SendPackage(ZPackage pkg)
	{
		m_sentPackages++;
		m_sentData += pkg.Size();
		m_socket.Send(m_pkg);
	}

	public static void Serialize(object[] parameters, ref ZPackage pkg)
	{
		foreach (object obj in parameters)
		{
			if (obj is int)
			{
				pkg.Write((int)obj);
			}
			else if (obj is uint)
			{
				pkg.Write((uint)obj);
			}
			else if (obj is long)
			{
				pkg.Write((long)obj);
			}
			else if (obj is float)
			{
				pkg.Write((float)obj);
			}
			else if (obj is double)
			{
				pkg.Write((double)obj);
			}
			else if (obj is bool)
			{
				pkg.Write((bool)obj);
			}
			else if (obj is string)
			{
				pkg.Write((string)obj);
			}
			else if (obj is ZPackage)
			{
				pkg.Write((ZPackage)obj);
			}
			else if (obj is List<string>)
			{
				List<string> list = obj as List<string>;
				pkg.Write(list.Count);
				foreach (string item in list)
				{
					pkg.Write(item);
				}
			}
			else if (obj is Vector3)
			{
				pkg.Write(((Vector3)obj).x);
				pkg.Write(((Vector3)obj).y);
				pkg.Write(((Vector3)obj).z);
			}
			else if (obj is Quaternion)
			{
				pkg.Write(((Quaternion)obj).x);
				pkg.Write(((Quaternion)obj).y);
				pkg.Write(((Quaternion)obj).z);
				pkg.Write(((Quaternion)obj).w);
			}
			else if (obj is ZDOID)
			{
				pkg.Write((ZDOID)obj);
			}
			else if (obj is HitData)
			{
				(obj as HitData).Serialize(ref pkg);
			}
			else if (obj is ISerializableParameter)
			{
				(obj as ISerializableParameter).Serialize(ref pkg);
			}
		}
	}

	public static object[] Deserialize(ZRpc rpc, ParameterInfo[] paramInfo, ZPackage pkg)
	{
		List<object> parameters = new List<object>();
		parameters.Add(rpc);
		Deserialize(paramInfo, pkg, ref parameters);
		return parameters.ToArray();
	}

	public static void Deserialize(ParameterInfo[] paramInfo, ZPackage pkg, ref List<object> parameters)
	{
		for (int i = 1; i < paramInfo.Length; i++)
		{
			ParameterInfo parameterInfo = paramInfo[i];
			if (parameterInfo.ParameterType == typeof(int))
			{
				parameters.Add(pkg.ReadInt());
			}
			else if (parameterInfo.ParameterType == typeof(uint))
			{
				parameters.Add(pkg.ReadUInt());
			}
			else if (parameterInfo.ParameterType == typeof(long))
			{
				parameters.Add(pkg.ReadLong());
			}
			else if (parameterInfo.ParameterType == typeof(float))
			{
				parameters.Add(pkg.ReadSingle());
			}
			else if (parameterInfo.ParameterType == typeof(double))
			{
				parameters.Add(pkg.ReadDouble());
			}
			else if (parameterInfo.ParameterType == typeof(bool))
			{
				parameters.Add(pkg.ReadBool());
			}
			else if (parameterInfo.ParameterType == typeof(string))
			{
				parameters.Add(pkg.ReadString());
			}
			else if (parameterInfo.ParameterType == typeof(ZPackage))
			{
				parameters.Add(pkg.ReadPackage());
			}
			else if (parameterInfo.ParameterType == typeof(List<string>))
			{
				int num = pkg.ReadInt();
				List<string> list = new List<string>(num);
				for (int j = 0; j < num; j++)
				{
					list.Add(pkg.ReadString());
				}
				parameters.Add(list);
			}
			else if (parameterInfo.ParameterType == typeof(Vector3))
			{
				Vector3 vector = new Vector3(pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle());
				parameters.Add(vector);
			}
			else if (parameterInfo.ParameterType == typeof(Quaternion))
			{
				Quaternion quaternion = new Quaternion(pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle());
				parameters.Add(quaternion);
			}
			else if (parameterInfo.ParameterType == typeof(ZDOID))
			{
				parameters.Add(pkg.ReadZDOID());
			}
			else if (parameterInfo.ParameterType == typeof(HitData))
			{
				HitData hitData = new HitData();
				hitData.Deserialize(ref pkg);
				parameters.Add(hitData);
			}
			else if (typeof(ISerializableParameter).IsAssignableFrom(parameterInfo.ParameterType))
			{
				ISerializableParameter serializableParameter = (ISerializableParameter)Activator.CreateInstance(parameterInfo.ParameterType);
				serializableParameter.Deserialize(ref pkg);
				parameters.Add(serializableParameter);
			}
			else if (typeof(ISerializableParameter).IsAssignableFrom(parameterInfo.ParameterType))
			{
				ISerializableParameter serializableParameter2 = (ISerializableParameter)Activator.CreateInstance(parameterInfo.ParameterType);
				serializableParameter2.Deserialize(ref pkg);
				parameters.Add(serializableParameter2);
			}
		}
	}

	public static void SetLongTimeout(bool enable)
	{
		if (enable)
		{
			m_timeout = 90f;
		}
		else
		{
			m_timeout = 30f;
		}
		ZLog.Log($"ZRpc timeout set to {m_timeout}s ");
	}
}
