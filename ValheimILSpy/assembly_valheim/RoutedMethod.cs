using System;

internal class RoutedMethod : RoutedMethodBase
{
	private Action<long> m_action;

	public RoutedMethod(Action<long> action)
	{
		m_action = action;
	}

	public void Invoke(long rpc, ZPackage pkg)
	{
		m_action(rpc);
	}
}
internal class RoutedMethod<T> : RoutedMethodBase
{
	private Action<long, T> m_action;

	public RoutedMethod(Action<long, T> action)
	{
		m_action = action;
	}

	public void Invoke(long rpc, ZPackage pkg)
	{
		m_action.DynamicInvoke(ZNetView.Deserialize(rpc, m_action.Method.GetParameters(), pkg));
	}
}
internal class RoutedMethod<T, U> : RoutedMethodBase
{
	private Action<long, T, U> m_action;

	public RoutedMethod(Action<long, T, U> action)
	{
		m_action = action;
	}

	public void Invoke(long rpc, ZPackage pkg)
	{
		m_action.DynamicInvoke(ZNetView.Deserialize(rpc, m_action.Method.GetParameters(), pkg));
	}
}
internal class RoutedMethod<T, U, V> : RoutedMethodBase
{
	private Action<long, T, U, V> m_action;

	public RoutedMethod(Action<long, T, U, V> action)
	{
		m_action = action;
	}

	public void Invoke(long rpc, ZPackage pkg)
	{
		m_action.DynamicInvoke(ZNetView.Deserialize(rpc, m_action.Method.GetParameters(), pkg));
	}
}
public class RoutedMethod<T, U, V, B> : RoutedMethodBase
{
	public delegate void Method(long sender, T p0, U p1, V p2, B p3);

	private Method m_action;

	public RoutedMethod(Method action)
	{
		m_action = action;
	}

	public void Invoke(long rpc, ZPackage pkg)
	{
		m_action.DynamicInvoke(ZNetView.Deserialize(rpc, m_action.Method.GetParameters(), pkg));
	}
}
public class RoutedMethod<T, U, V, B, K> : RoutedMethodBase
{
	public delegate void Method(long sender, T p0, U p1, V p2, B p3, K p4);

	private Method m_action;

	public RoutedMethod(Method action)
	{
		m_action = action;
	}

	public void Invoke(long rpc, ZPackage pkg)
	{
		m_action.DynamicInvoke(ZNetView.Deserialize(rpc, m_action.Method.GetParameters(), pkg));
	}
}
public class RoutedMethod<T, U, V, B, K, M> : RoutedMethodBase
{
	public delegate void Method(long sender, T p0, U p1, V p2, B p3, K p4, M p5);

	private Method m_action;

	public RoutedMethod(Method action)
	{
		m_action = action;
	}

	public void Invoke(long rpc, ZPackage pkg)
	{
		m_action.DynamicInvoke(ZNetView.Deserialize(rpc, m_action.Method.GetParameters(), pkg));
	}
}
