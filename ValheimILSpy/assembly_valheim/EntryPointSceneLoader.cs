using SoftReferenceableAssets.SceneManagement;
using UnityEngine;

public class EntryPointSceneLoader : MonoBehaviour
{
	[SerializeField]
	private SceneReference m_scene;

	private void Start()
	{
		LoadScene();
	}

	private void LoadScene()
	{
		SceneManager.LoadScene(m_scene);
	}
}
