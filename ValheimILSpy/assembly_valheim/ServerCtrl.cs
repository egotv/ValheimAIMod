using System.IO;
using UnityEngine;

public class ServerCtrl
{
	private static ServerCtrl m_instance;

	private float m_checkTimer;

	public static ServerCtrl instance => m_instance;

	public static void Initialize()
	{
		if (m_instance == null)
		{
			m_instance = new ServerCtrl();
		}
	}

	private ServerCtrl()
	{
		ClearExitFile();
	}

	public void Update(float dt)
	{
		CheckExit(dt);
	}

	private void CheckExit(float dt)
	{
		m_checkTimer += dt;
		if (m_checkTimer > 2f)
		{
			m_checkTimer = 0f;
			if (File.Exists("server_exit.drp"))
			{
				Application.Quit();
			}
		}
	}

	private void ClearExitFile()
	{
		try
		{
			File.Delete("server_exit.drp");
		}
		catch
		{
		}
	}
}
